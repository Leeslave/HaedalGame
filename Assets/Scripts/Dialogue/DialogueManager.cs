using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSON으로 적은 대사를 순서대로 재생하는 매니저.
/// 노드를 읽어 배경·초상화·글자를 DialogueView에 넘기고, 다음 줄로 넘어갈 시점만 판정한다.
/// 건너뛰기는 스크립트가 허락한 경우에만 열리고(canSkip), 한 번만 볼 대사는 아이디로 PlayerPrefs에 기록한다.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private const string CompletedKeyPrefix = "Dialogue_Completed_";

    [SerializeField] private DialogueView _view;

    [Tooltip("아이디로 재생하기 위해 미리 등록해두는 JSON 파일 목록.")]
    [SerializeField] private List<TextAsset> _scripts = new List<TextAsset>();

    [Header("이미지 찾기")]
    [Tooltip("JSON의 키를 직접 스프라이트에 이어붙일 때 쓴다. Resources보다 먼저 검사한다.")]
    [SerializeField] private List<DialogueSpriteEntry> _spriteTable = new List<DialogueSpriteEntry>();
    [Tooltip("Resources 아래에서 이미지를 찾을 폴더. 위에서부터 차례로 뒤진다.")]
    [SerializeField] private List<string> _resourceFolders = new List<string> { "Dialogue", "" };

    [Header("기본값")]
    [Tooltip("JSON에 backgroundStyle을 안 적었을 때 쓸 배경. 배경 이미지도 없을 때만 보인다.")]
    [SerializeField] private DialogueBackgroundStyle _defaultBackgroundStyle = DialogueBackgroundStyle.SeasideDusk;
    [SerializeField] private Color _defaultNameColor = Color.white;
    [SerializeField] private bool _dontDestroyOnLoad = true;

    [Tooltip("테스트용. 켜면 '한 번만 재생' 기록을 무시하고 항상 재생한다.")]
    [SerializeField] private bool _ignoreSavedProgress = false;

    /// <summary>대사가 시작됐다.</summary>
    public event Action<DialogueScriptData> OnDialogueStarted;

    /// <summary>대사 한 줄이 시작됐다.</summary>
    public event Action<DialogueNodeData> OnNodeStarted;

    /// <summary>노드나 선택지에 적어둔 eventKey가 나왔다. 게임 로직을 붙이는 자리다.</summary>
    public event Action<string> OnEvent;

    /// <summary>선택지를 골랐다. (노드, 고른 선택지)</summary>
    public event Action<DialogueNodeData, DialogueChoiceData> OnChoiceMade;

    /// <summary>대사가 끝났다.</summary>
    public event Action<DialogueScriptData> OnDialogueFinished;

    private DialogueAssetResolver _resolver;
    private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();
    private readonly List<DialogueChoiceData> _visibleChoices = new List<DialogueChoiceData>();
    private readonly List<string> _choiceLabels = new List<string>();

    private Coroutine _playRoutine;
    private Action _onFinished;

    private bool _advanceRequested;
    private bool _skipping;
    private bool _stopRequested;
    private int _selectedChoice = -1;

    private float _cachedTimeScale = 1f;

    public bool IsPlaying => _playRoutine != null;
    public DialogueScriptData CurrentScript { get; private set; }
    public DialogueNodeData CurrentNode { get; private set; }

    #region 생명 주기

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        _resolver = new DialogueAssetResolver(_resourceFolders);
        _resolver.RegisterAll(_spriteTable);

        if (_view != null)
        {
            _view.OnAdvanceRequested += HandleAdvanceRequested;
            _view.OnSkipRequested += Skip;
            _view.OnChoiceSelected += HandleChoiceSelected;
            _view.Hide();
        }
        else
        {
            Debug.LogError("[Dialogue] DialogueView가 연결되지 않았다.");
        }
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnAdvanceRequested -= HandleAdvanceRequested;
            _view.OnSkipRequested -= Skip;
            _view.OnChoiceSelected -= HandleChoiceSelected;
        }

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region 읽어오기

    /// <summary>JSON 글자를 스크립트로 바꾼다. 형식이 틀리면 null.</summary>
    public static DialogueScriptData Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[Dialogue] 비어 있는 JSON이다.");
            return null;
        }

        try
        {
            DialogueScriptData script = JsonUtility.FromJson<DialogueScriptData>(json);

            if (script == null)
            {
                Debug.LogError("[Dialogue] JSON을 스크립트로 읽지 못했다.");
                return null;
            }

            if (script.nodes == null)
                script.nodes = new List<DialogueNodeData>();

            if (script.characters == null)
                script.characters = new List<DialogueCharacterData>();

            if (script.NodeCount == 0)
                Debug.LogWarning($"[Dialogue] '{script.id}'에 nodes가 비어 있다.");

            return script;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Dialogue] JSON 형식이 잘못됐다: {exception.Message}");
            return null;
        }
    }

    /// <summary>Resources 아래의 JSON 파일을 읽어 스크립트로 바꾼다.</summary>
    public static DialogueScriptData LoadFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        TextAsset asset = Resources.Load<TextAsset>(resourcePath);

        if (asset == null)
        {
            Debug.LogWarning($"[Dialogue] Resources/{resourcePath} 에서 JSON을 찾지 못했다.");
            return null;
        }

        return Parse(asset.text);
    }

    #endregion

    #region 재생

    /// <summary>스크립트를 재생한다. 이미 재생 중이거나 한 번만 볼 대사를 이미 봤으면 false.</summary>
    public bool Play(DialogueScriptData script, Action onFinished = null)
    {
        if (script == null)
        {
            Debug.LogWarning("[Dialogue] 재생할 스크립트가 없다.");
            return false;
        }

        if (IsPlaying)
        {
            Debug.LogWarning($"[Dialogue] 이미 '{(CurrentScript != null ? CurrentScript.id : "?")}' 재생 중이라 '{script.id}'를 건너뛴다.");
            return false;
        }

        if (_view == null)
        {
            Debug.LogError("[Dialogue] DialogueView가 없어 재생할 수 없다.");
            return false;
        }

        if (script.NodeCount == 0)
            return false;

        if (script.playOnce && IsCompleted(script.id))
            return false;

        _onFinished = onFinished;
        _playRoutine = StartCoroutine(CoPlay(script));
        return true;
    }

    /// <summary>JSON 글자를 바로 재생한다.</summary>
    public bool PlayJson(string json, Action onFinished = null) => Play(Parse(json), onFinished);

    /// <summary>TextAsset을 바로 재생한다.</summary>
    public bool Play(TextAsset jsonAsset, Action onFinished = null)
    {
        if (jsonAsset == null)
        {
            Debug.LogWarning("[Dialogue] 재생할 JSON 파일이 비어 있다.");
            return false;
        }

        return Play(Parse(jsonAsset.text), onFinished);
    }

    /// <summary>Resources 경로로 읽어 재생한다.</summary>
    public bool PlayFromResources(string resourcePath, Action onFinished = null)
        => Play(LoadFromResources(resourcePath), onFinished);

    /// <summary>Scripts 목록에 등록해둔 JSON을 스크립트 id로 찾아 재생한다.</summary>
    public bool Play(string scriptId, Action onFinished = null)
    {
        DialogueScriptData script = FindScript(scriptId);

        if (script == null)
        {
            Debug.LogWarning($"[Dialogue] '{scriptId}' 스크립트를 Scripts 목록에서 찾지 못했다.");
            return false;
        }

        return Play(script, onFinished);
    }

    /// <summary>등록된 JSON 목록에서 id가 같은 스크립트를 찾는다.</summary>
    public DialogueScriptData FindScript(string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId))
            return null;

        for (int i = 0; i < _scripts.Count; i++)
        {
            if (_scripts[i] == null)
                continue;

            DialogueScriptData script = Parse(_scripts[i].text);

            // id를 안 적었으면 파일 이름을 아이디로 쳐준다.
            if (script == null)
                continue;

            string id = string.IsNullOrEmpty(script.id) ? _scripts[i].name : script.id;

            if (id == scriptId)
                return script;
        }

        return null;
    }

    private IEnumerator CoPlay(DialogueScriptData script)
    {
        CurrentScript = script;
        _stopRequested = false;
        _skipping = false;
        _advanceRequested = false;

        if (script.pauseGameTime)
        {
            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        _view.Show();
        _view.ClearChoices();
        _view.SetTextBoxVisible(true);

        // 건너뛰기를 허락하지 않은 대사에선 버튼이 아예 뜨지 않는다.
        _view.SetSkipButtonVisible(script.canSkip);

        ApplyBackground(script.background, script.backgroundStyle, script, DialogueTransition.Instant, true);

        OnDialogueStarted?.Invoke(script);

        int index = 0;

        while (index >= 0 && index < script.NodeCount && !_stopRequested)
        {
            DialogueNodeData node = script.nodes[index];

            if (node == null)
            {
                index++;
                continue;
            }

            yield return CoPlayNode(script, node);

            if (_stopRequested)
                break;

            index = ResolveNextIndex(script, node, index);
        }

        Finish(script);
    }

    private IEnumerator CoPlayNode(DialogueScriptData script, DialogueNodeData node)
    {
        CurrentNode = node;

        // 놓치면 안 되는 줄이나 선택지를 만나면 건너뛰기를 멈추고 다시 보통 속도로 돌아간다.
        if (_skipping && (node.blockSkip || node.HasChoices))
            _skipping = false;

        ApplyBackground(node.background, node.backgroundStyle, script,
            DialogueParse.ParseEnum(node.transition, DialogueTransition.Fade));

        DialogueCharacterData character = script.FindCharacter(node.speaker);
        ApplyPortrait(node, character);
        ApplySpeakerName(node, character);

        if (!string.IsNullOrEmpty(node.setFlag))
            SetFlag(node.setFlag, true);

        if (!string.IsNullOrEmpty(node.eventKey))
            OnEvent?.Invoke(node.eventKey);

        OnNodeStarted?.Invoke(node);

        float typeSpeed = node.typeSpeed >= 0f ? node.typeSpeed : script.typeSpeed;

        // 건너뛰는 중엔 타자 효과를 기다릴 이유가 없다.
        if (_skipping)
            typeSpeed = 0f;

        _view.BeginLine(node.text, typeSpeed);

        if (node.HasChoices)
        {
            yield return CoWaitForChoice(script, node);
            yield break;
        }

        yield return CoWaitForAdvance(script, node);
    }

    private IEnumerator CoWaitForAdvance(DialogueScriptData script, DialogueNodeData node)
    {
        _advanceRequested = false;

        float elapsed = 0f;

        while (!_stopRequested)
        {
            // 건너뛰는 중이면 기다리지 않고 곧장 다음 줄로 넘어간다.
            if (_skipping)
            {
                _view.CompleteTyping();
                yield break;
            }

            if (_advanceRequested)
            {
                _advanceRequested = false;

                // 글자가 아직 나오는 중이면 첫 클릭은 '마저 보여줘'라는 뜻이다.
                if (_view.IsTyping)
                {
                    if (script.allowFastForward)
                        _view.CompleteTyping();
                }
                else
                {
                    yield break;
                }
            }

            if (node.autoAdvance > 0f && !_view.IsTyping)
            {
                elapsed += Time.unscaledDeltaTime;

                if (elapsed >= node.autoAdvance)
                    yield break;
            }

            yield return null;
        }
    }

    private IEnumerator CoWaitForChoice(DialogueScriptData script, DialogueNodeData node)
    {
        // 글자가 다 나온 뒤에 선택지를 띄운다.
        while (_view.IsTyping && !_stopRequested)
        {
            if (_advanceRequested)
            {
                _advanceRequested = false;

                if (script.allowFastForward)
                    _view.CompleteTyping();
            }

            yield return null;
        }

        if (_stopRequested)
            yield break;

        BuildVisibleChoices(node);

        if (_visibleChoices.Count == 0)
        {
            // 조건에 걸려 보여줄 선택지가 하나도 없으면 막히지 않게 그냥 넘어간다.
            Debug.LogWarning($"[Dialogue] '{node.id}'의 선택지가 조건에 모두 걸려 하나도 없다. 그냥 다음 줄로 넘어간다.");
            yield break;
        }

        _selectedChoice = -1;
        _view.ShowChoices(_choiceLabels);

        while (_selectedChoice < 0 && !_stopRequested)
            yield return null;

        _view.ClearChoices();

        if (_stopRequested)
            yield break;

        DialogueChoiceData choice = _visibleChoices[_selectedChoice];

        if (!string.IsNullOrEmpty(choice.setFlag))
            SetFlag(choice.setFlag, true);

        if (!string.IsNullOrEmpty(choice.eventKey))
            OnEvent?.Invoke(choice.eventKey);

        OnChoiceMade?.Invoke(node, choice);
    }

    /// <summary>조건(requireFlag)을 통과한 선택지만 골라 담는다.</summary>
    private void BuildVisibleChoices(DialogueNodeData node)
    {
        _visibleChoices.Clear();
        _choiceLabels.Clear();

        for (int i = 0; i < node.choices.Count; i++)
        {
            DialogueChoiceData choice = node.choices[i];

            if (choice == null || !PassesCondition(choice.requireFlag))
                continue;

            _visibleChoices.Add(choice);
            _choiceLabels.Add(choice.text);
        }
    }

    /// <summary>"flag"면 켜져 있을 때, "!flag"면 꺼져 있을 때 통과한다. 비면 항상 통과.</summary>
    private bool PassesCondition(string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
            return true;

        string trimmed = requirement.Trim();
        bool expected = true;

        if (trimmed.StartsWith("!"))
        {
            expected = false;
            trimmed = trimmed.Substring(1).Trim();
        }

        if (string.IsNullOrEmpty(trimmed))
            return true;

        return GetFlag(trimmed) == expected;
    }

    /// <summary>이 줄 다음에 재생할 노드 번호를 정한다. 끝내야 하면 -1.</summary>
    private int ResolveNextIndex(DialogueScriptData script, DialogueNodeData node, int currentIndex)
    {
        // 선택지를 골랐다면 그 선택지의 next가 우선한다.
        string next = node.HasChoices && _selectedChoice >= 0 && _selectedChoice < _visibleChoices.Count
            ? _visibleChoices[_selectedChoice].next
            : node.next;

        _selectedChoice = -1;

        if (string.IsNullOrWhiteSpace(next))
            return currentIndex + 1;

        // 같은 이름의 노드가 실제로 있으면 그쪽을 먼저 본다.
        // 'end'/'finish' 같은 예약어를 노드 id로 써도 대사가 엉뚱하게 끝나지 않도록 하기 위해서다.
        int found = script.IndexOfNode(next);

        if (found >= 0)
            return found;

        if (DialogueParse.IsEndKeyword(next))
            return -1;

        Debug.LogWarning($"[Dialogue] '{next}' 노드를 찾지 못했다. 오타가 아닌지 확인한다. 일단 다음 줄로 넘어간다.");
        return currentIndex + 1;
    }

    private void Finish(DialogueScriptData script)
    {
        CurrentNode = null;
        _skipping = false;
        _selectedChoice = -1;

        _view.Hide();

        if (script.pauseGameTime)
            Time.timeScale = _cachedTimeScale;

        if (script.playOnce)
            MarkCompleted(script.id);

        _playRoutine = null;
        CurrentScript = null;

        OnDialogueFinished?.Invoke(script);

        Action finished = _onFinished;
        _onFinished = null;
        finished?.Invoke();
    }

    #endregion

    /// <summary>이미지 키가 실제로 찾아지는지 조용히 확인한다. 검사 도구에서 쓴다.</summary>
    public bool HasImage(string key)
    {
        if (_resolver == null || string.IsNullOrWhiteSpace(key))
            return false;

        return _resolver.Resolve(key, false) != null;
    }

    #region 화면에 반영

    /// <param name="applyDefaultWhenEmpty">
    /// 배경 얘기가 아예 없을 때 기본 스타일을 깔지. 대사 시작에선 켜고(빈 화면 방지),
    /// 줄마다 부를 땐 꺼서 앞의 배경이 유지되게 한다.
    /// </param>
    private void ApplyBackground(string imageKey, string styleKey, DialogueScriptData script,
        DialogueTransition transition, bool applyDefaultWhenEmpty = false)
    {
        bool hasImageKey = !string.IsNullOrWhiteSpace(imageKey);
        bool hasStyleKey = !string.IsNullOrWhiteSpace(styleKey);

        // 이 줄에서 배경 얘기를 안 했으면 앞의 배경을 그대로 둔다.
        if (!hasImageKey && !hasStyleKey && !applyDefaultWhenEmpty)
            return;

        Sprite sprite = hasImageKey ? _resolver.Resolve(imageKey) : null;

        if (sprite != null)
        {
            _view.SetBackground(sprite, transition);
            return;
        }

        // 그림을 못 찾았거나 애초에 스타일만 적었으면 코드로 그린 배경으로 대신한다.
        string styleSource = hasStyleKey ? styleKey : script.backgroundStyle;
        DialogueBackgroundStyle style = DialogueParse.ParseEnum(styleSource, _defaultBackgroundStyle);

        _view.SetBackground(DialogueBackgroundFactory.Get(style), transition);
    }

    private void ApplyPortrait(DialogueNodeData node, DialogueCharacterData character)
    {
        // 위치는 노드 → 캐릭터 기본값 순으로 정한다.
        string sideSource = !string.IsNullOrWhiteSpace(node.side)
            ? node.side
            : (character != null ? character.side : "");

        DialoguePortraitSide side = DialogueParse.ParseEnum(sideSource, DialoguePortraitSide.Left);

        if (side == DialoguePortraitSide.None)
        {
            _view.ClearPortraits();
            return;
        }

        bool clearRequested = DialogueParse.IsClearKeyword(node.portrait);

        string portraitKey = clearRequested
            ? ""
            : (!string.IsNullOrWhiteSpace(node.portrait) ? node.portrait : (character != null ? character.portrait : ""));

        Sprite sprite = string.IsNullOrWhiteSpace(portraitKey) ? null : _resolver.Resolve(portraitKey);

        _view.SetPortrait(sprite, side, clearRequested);
    }

    private void ApplySpeakerName(DialogueNodeData node, DialogueCharacterData character)
    {
        // 이름은 노드 → 캐릭터 이름 → 캐릭터 id → speaker에 적은 글자 순으로 고른다.
        string displayName = node.name;

        if (string.IsNullOrWhiteSpace(displayName) && character != null)
            displayName = !string.IsNullOrWhiteSpace(character.name) ? character.name : character.id;

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = node.speaker;

        Color color = character != null
            ? DialogueParse.ParseColor(character.color, _defaultNameColor)
            : _defaultNameColor;

        _view.SetSpeaker(displayName, color);
    }

    #endregion

    #region 입력 / 외부 제어

    private void HandleAdvanceRequested()
    {
        if (IsPlaying)
            _advanceRequested = true;
    }

    private void HandleChoiceSelected(int index)
    {
        if (IsPlaying)
            _selectedChoice = index;
    }

    /// <summary>
    /// 대사를 건너뛴다. 스크립트가 canSkip을 켜둔 경우에만 동작한다.
    /// 선택지나 blockSkip이 걸린 줄을 만나면 거기서 멈추고 다시 보통 속도로 돌아간다.
    /// </summary>
    public void Skip()
    {
        if (!IsPlaying)
            return;

        if (CurrentScript == null || !CurrentScript.canSkip)
        {
            Debug.Log("[Dialogue] 이 대사는 건너뛸 수 없다. (canSkip이 꺼져 있다)");
            return;
        }

        _skipping = true;
    }

    /// <summary>건너뛰는 중이면 멈춘다.</summary>
    public void StopSkipping() => _skipping = false;

    /// <summary>다음 줄로 넘긴다. 직접 만든 버튼에 이어붙일 때 쓴다.</summary>
    public void Advance() => HandleAdvanceRequested();

    /// <summary>대사를 즉시 끝낸다. canSkip과 상관없이 동작하니 씬 전환 같은 데서만 쓴다.</summary>
    public void Stop()
    {
        if (IsPlaying)
            _stopRequested = true;
    }

    #endregion

    #region 플래그

    public void SetFlag(string key, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _flags[key.Trim()] = value;
    }

    public bool GetFlag(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _flags.TryGetValue(key.Trim(), out bool value) && value;
    }

    public void ClearFlags() => _flags.Clear();

    #endregion

    #region 진행도 저장

    public static bool IsCompleted(string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId))
            return false;

        if (Instance != null && Instance._ignoreSavedProgress)
            return false;

        return PlayerPrefs.GetInt(CompletedKeyPrefix + scriptId, 0) == 1;
    }

    public static void MarkCompleted(string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId))
            return;

        PlayerPrefs.SetInt(CompletedKeyPrefix + scriptId, 1);
        PlayerPrefs.Save();
    }

    public static void ResetProgress(string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId))
            return;

        PlayerPrefs.DeleteKey(CompletedKeyPrefix + scriptId);
        PlayerPrefs.Save();
    }

    /// <summary>테스트용: 등록된 모든 스크립트의 '봤음' 기록을 지운다.</summary>
    [ContextMenu("모든 대사 진행도 초기화")]
    public void ResetAllProgress()
    {
        for (int i = 0; i < _scripts.Count; i++)
        {
            if (_scripts[i] == null)
                continue;

            DialogueScriptData script = Parse(_scripts[i].text);

            if (script != null)
                ResetProgress(string.IsNullOrEmpty(script.id) ? _scripts[i].name : script.id);
        }

        Debug.Log("[Dialogue] 등록된 스크립트의 진행 기록을 모두 지웠다.");
    }

    #endregion
}
