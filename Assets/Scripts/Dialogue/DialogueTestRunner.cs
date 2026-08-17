using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// [테스트 전용] 대사를 단축키로 돌려보고 JSON을 점검하는 디버그 헬퍼.
/// 빈 오브젝트에 붙이고 Play → F5(재생) / F6(진행도 초기화) / F7(건너뛰기) / F8(강제 종료)
/// / F9(JSON 점검) / F10(플래그 확인) → 콘솔 확인.
/// 출시 전 씬에서 제거.
/// </summary>
public class DialogueTestRunner : MonoBehaviour
{
    [Tooltip("재생할 JSON 파일. 비우면 아래 Resource Path를 쓴다.")]
    [SerializeField] private TextAsset _scriptAsset;

    [Tooltip("Resources 아래의 JSON 경로. 확장자는 뺀다.")]
    [SerializeField] private string _resourcePath = "Dialogue/test_dialogue";

    [Tooltip("재생 전에 미리 켜둘 플래그. requireFlag 분기를 바로 확인할 때 쓴다.")]
    [SerializeField] private List<string> _presetFlags = new List<string>();

    [Header("키")]
    [Tooltip("진행 기록을 무시하고 처음부터 재생한다.")]
    [SerializeField] private KeyCode _playKey = KeyCode.F5;
    [SerializeField] private KeyCode _resetKey = KeyCode.F6;
    [SerializeField] private KeyCode _skipKey = KeyCode.F7;
    [SerializeField] private KeyCode _stopKey = KeyCode.F8;
    [SerializeField] private KeyCode _validateKey = KeyCode.F9;
    [SerializeField] private KeyCode _dumpFlagsKey = KeyCode.F10;

    private bool _subscribed;
    private int _nodeCounter;

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_playKey))
            PlayScript();

        if (Input.GetKeyDown(_resetKey))
            ResetProgress();

        if (Input.GetKeyDown(_skipKey))
            SkipDialogue();

        if (Input.GetKeyDown(_stopKey))
            StopDialogue();

        if (Input.GetKeyDown(_validateKey))
            ValidateScript();

        if (Input.GetKeyDown(_dumpFlagsKey))
            DumpFlags();
    }

    #region 조작

    /// <summary>진행 기록을 지우고 처음부터 재생한다.</summary>
    [ContextMenu("Play Script")]
    public void PlayScript()
    {
        if (!HasManager())
            return;

        DialogueScriptData script = LoadScript();

        if (script == null)
            return;

        Subscribe();

        // 테스트는 항상 처음부터 볼 수 있어야 하므로 진행 기록을 먼저 지운다.
        DialogueManager.ResetProgress(ScriptId(script));

        // 지난 재생에서 켜진 플래그가 남아 분기가 달라지지 않도록 정리하고 시작한다.
        DialogueManager.Instance.ClearFlags();

        for (int i = 0; i < _presetFlags.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(_presetFlags[i]))
                continue;

            DialogueManager.Instance.SetFlag(_presetFlags[i], true);
            Debug.Log($"[DialogueTest] 미리 켠 플래그: '{_presetFlags[i]}'");
        }

        _nodeCounter = 0;
        DialogueManager.Instance.Play(script);
    }

    /// <summary>진행 기록만 지운다. (playOnce 대사를 다시 보고 싶을 때)</summary>
    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        DialogueScriptData script = LoadScript();

        if (script == null)
            return;

        string id = ScriptId(script);
        DialogueManager.ResetProgress(id);
        Debug.Log($"[DialogueTest] '{id}' 진행 기록 삭제 — {_resetKey}");
    }

    [ContextMenu("Skip Dialogue")]
    public void SkipDialogue()
    {
        if (!HasManager())
            return;

        DialogueScriptData current = DialogueManager.Instance.CurrentScript;

        if (current != null && !current.canSkip)
        {
            Debug.LogWarning($"[DialogueTest] 이 대사는 canSkip이 꺼져 있어 건너뛸 수 없다. " +
                             "(의도한 설정인지 확인) — 버튼도 뜨지 않는다.");
            return;
        }

        DialogueManager.Instance.Skip();
        Debug.Log($"[DialogueTest] 건너뛰기 요청 — {_skipKey}");
    }

    [ContextMenu("Stop Dialogue")]
    public void StopDialogue()
    {
        if (!HasManager())
            return;

        DialogueManager.Instance.Stop();
        Debug.Log($"[DialogueTest] 강제 종료 요청 — {_stopKey}");
    }

    [ContextMenu("Dump Flags")]
    public void DumpFlags()
    {
        DialogueScriptData script = LoadScript();

        if (script == null || !HasManager())
            return;

        // 스크립트가 쓰는 플래그 이름을 모아 지금 상태를 함께 보여준다.
        HashSet<string> names = new HashSet<string>();

        foreach (DialogueNodeData node in script.nodes)
        {
            if (node == null)
                continue;

            if (!string.IsNullOrWhiteSpace(node.setFlag))
                names.Add(node.setFlag.Trim());

            foreach (DialogueChoiceData choice in node.choices)
            {
                if (choice == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(choice.setFlag))
                    names.Add(choice.setFlag.Trim());

                if (!string.IsNullOrWhiteSpace(choice.requireFlag))
                    names.Add(choice.requireFlag.Trim().TrimStart('!').Trim());
            }
        }

        if (names.Count == 0)
        {
            Debug.Log("[DialogueTest] 이 스크립트는 플래그를 쓰지 않는다.");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"[DialogueTest] 플래그 상태 ({names.Count}개):");

        foreach (string name in names)
            builder.Append($"\n - {name} = {(DialogueManager.Instance.GetFlag(name) ? "ON" : "off")}");

        Debug.Log(builder.ToString());
    }

    #endregion

    #region JSON 점검

    /// <summary>
    /// JSON을 재생 전에 점검한다.
    /// JsonUtility는 모르는 필드를 조용히 무시하기 때문에 오타가 나도 에러가 안 난다.
    /// 그래서 여기서 id 참조·열거형 값·이미지 키·도달 못 하는 줄을 미리 잡아준다.
    /// </summary>
    [ContextMenu("Validate Script")]
    public void ValidateScript()
    {
        DialogueScriptData script = LoadScript();

        if (script == null)
            return;

        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        List<string> notes = new List<string>();

        List<DialogueNodeData> nodes = script.nodes;

        if (nodes.Count == 0)
            errors.Add("nodes가 비어 있다.");

        // 1) 노드 id 중복
        Dictionary<string, int> idToIndex = new Dictionary<string, int>();

        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNodeData node = nodes[i];

            if (node == null)
            {
                errors.Add($"{i}번 노드가 비어 있다.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.id))
                continue;

            if (idToIndex.ContainsKey(node.id))
            {
                errors.Add($"노드 id '{node.id}'가 {idToIndex[node.id]}번과 {i}번에 중복으로 있다. 앞의 것만 찾아진다.");
                continue;
            }

            idToIndex[node.id] = i;

            // 'end'/'exit'/'finish'는 대사를 끝내라는 예약어이기도 하다.
            // 지금은 노드 id가 우선하지만, 읽는 사람이 헷갈리므로 알려둔다.
            if (DialogueParse.IsEndKeyword(node.id))
                notes.Add($"노드 id '{node.id}'는 '대사 끝내기' 예약어와 같은 이름이다. " +
                          "이 id로 가리키면 종료가 아니라 이 줄로 이동한다. 헷갈리면 이름을 바꾸는 편이 낫다.");
        }

        // 2) 줄마다 세부 점검
        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNodeData node = nodes[i];

            if (node == null)
                continue;

            string label = string.IsNullOrWhiteSpace(node.id) ? $"{i}번" : $"'{node.id}'({i}번)";

            CheckJump(node.next, label, "next", idToIndex, errors);

            if (!DialogueParse.IsValidEnum<DialoguePortraitSide>(node.side))
                errors.Add($"{label}의 side '{node.side}'는 없는 값이다. (None/Left/Right/Center)");

            if (!DialogueParse.IsValidEnum<DialogueTransition>(node.transition))
                errors.Add($"{label}의 transition '{node.transition}'는 없는 값이다. (Instant/Fade)");

            if (!DialogueParse.IsValidEnum<DialogueBackgroundStyle>(node.backgroundStyle))
                errors.Add($"{label}의 backgroundStyle '{node.backgroundStyle}'는 없는 값이다.");

            if (string.IsNullOrWhiteSpace(node.text) && !node.HasChoices)
                warnings.Add($"{label}의 text가 비어 있다. 빈 대사창이 뜬다.");

            if (node.HasChoices && !string.IsNullOrWhiteSpace(node.next))
                notes.Add($"{label}은 choices와 next를 함께 갖고 있다. 선택지의 next가 우선한다.");

            if (!string.IsNullOrWhiteSpace(node.speaker) && script.FindCharacter(node.speaker) == null)
                notes.Add($"{label}의 speaker '{node.speaker}'는 characters에 없다. 적은 글자가 그대로 이름표에 뜬다.");

            // 이미지 키는 못 찾아도 기본 배경으로 대체되므로 경고까지만 한다.
            CheckImage(node.background, label, "background", warnings);

            if (!DialogueParse.IsClearKeyword(node.portrait))
                CheckImage(node.portrait, label, "portrait", warnings);

            for (int c = 0; c < node.choices.Count; c++)
            {
                DialogueChoiceData choice = node.choices[c];

                if (choice == null)
                {
                    errors.Add($"{label}의 {c}번 선택지가 비어 있다.");
                    continue;
                }

                CheckJump(choice.next, $"{label}의 {c}번 선택지", "next", idToIndex, errors);

                if (string.IsNullOrWhiteSpace(choice.text))
                    warnings.Add($"{label}의 {c}번 선택지 text가 비어 있다. 빈 버튼이 뜬다.");
            }
        }

        // 3) 캐릭터 점검
        foreach (DialogueCharacterData character in script.characters)
        {
            if (character == null)
                continue;

            if (string.IsNullOrWhiteSpace(character.id))
                errors.Add("characters에 id가 빈 항목이 있다. speaker로 가리킬 수 없다.");

            if (!DialogueParse.IsValidEnum<DialoguePortraitSide>(character.side))
                errors.Add($"캐릭터 '{character.id}'의 side '{character.side}'는 없는 값이다.");

            if (!string.IsNullOrWhiteSpace(character.color) &&
                !ColorUtility.TryParseHtmlString(character.color.StartsWith("#") ? character.color : "#" + character.color, out _))
                warnings.Add($"캐릭터 '{character.id}'의 color '{character.color}'를 읽을 수 없다. 기본색이 쓰인다.");

            CheckImage(character.portrait, $"캐릭터 '{character.id}'", "portrait", warnings);
        }

        if (!DialogueParse.IsValidEnum<DialogueBackgroundStyle>(script.backgroundStyle))
            errors.Add($"스크립트의 backgroundStyle '{script.backgroundStyle}'는 없는 값이다.");

        CheckImage(script.background, "스크립트", "background", warnings);

        // 4) 도달 못 하는 줄
        foreach (string unreachable in FindUnreachable(nodes, idToIndex))
            warnings.Add($"{unreachable}에 도달할 방법이 없다. (오타이거나 안 쓰는 줄)");

        // 5) 설정 조합
        if (!script.canSkip)
        {
            foreach (DialogueNodeData node in nodes)
            {
                if (node != null && node.blockSkip)
                {
                    notes.Add("canSkip이 꺼져 있어 blockSkip은 의미가 없다. (건너뛰기 자체가 불가능)");
                    break;
                }
            }
        }

        Report(ScriptId(script), nodes.Count, errors, warnings, notes);
    }

    private static void CheckJump(string target, string label, string field,
        Dictionary<string, int> idToIndex, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(target))
            return;

        // 실제 노드가 예약어보다 우선하므로 id부터 본다. (DialogueManager.ResolveNextIndex와 같은 순서)
        if (idToIndex.ContainsKey(target.Trim()) || DialogueParse.IsEndKeyword(target))
            return;

        errors.Add($"{label}의 {field} '{target}'에 해당하는 노드가 없다. 실행하면 그냥 다음 줄로 넘어간다.");
    }

    private static void CheckImage(string key, string label, string field, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        // 이미지 검사는 매니저의 Sprite Table과 Resources를 봐야 해서 Play 중에만 정확하다.
        if (DialogueManager.Instance == null)
            return;

        if (!DialogueManager.Instance.HasImage(key))
            warnings.Add($"{label}의 {field} '{key}'를 찾지 못했다. " +
                         (field == "background" ? "기본 배경 스타일로 대체된다." : "초상화 없이 진행된다."));
    }

    /// <summary>첫 줄에서 출발해 실제로 닿는 노드를 세고, 나머지를 돌려준다.</summary>
    private static List<string> FindUnreachable(List<DialogueNodeData> nodes, Dictionary<string, int> idToIndex)
    {
        List<string> result = new List<string>();

        if (nodes.Count == 0)
            return result;

        HashSet<int> seen = new HashSet<int>();
        Stack<int> stack = new Stack<int>();
        stack.Push(0);

        while (stack.Count > 0)
        {
            int index = stack.Pop();

            if (index < 0 || index >= nodes.Count || !seen.Add(index))
                continue;

            DialogueNodeData node = nodes[index];

            if (node == null)
                continue;

            if (node.HasChoices)
            {
                foreach (DialogueChoiceData choice in node.choices)
                {
                    if (choice != null)
                        PushNext(choice.next, index, idToIndex, stack);
                }

                continue;
            }

            PushNext(node.next, index, idToIndex, stack);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (seen.Contains(i))
                continue;

            DialogueNodeData node = nodes[i];
            result.Add(node != null && !string.IsNullOrWhiteSpace(node.id) ? $"'{node.id}'({i}번)" : $"{i}번 노드");
        }

        return result;
    }

    private static void PushNext(string target, int currentIndex, Dictionary<string, int> idToIndex, Stack<int> stack)
    {
        // next를 비우면 배열의 바로 다음 줄로 흘러간다.
        if (string.IsNullOrWhiteSpace(target))
        {
            stack.Push(currentIndex + 1);
            return;
        }

        // 실제 노드가 예약어보다 우선한다.
        if (idToIndex.TryGetValue(target.Trim(), out int found))
        {
            stack.Push(found);
            return;
        }

        if (DialogueParse.IsEndKeyword(target))
            return;

        stack.Push(currentIndex + 1);
    }

    private static void Report(string scriptId, int nodeCount,
        List<string> errors, List<string> warnings, List<string> notes)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"[DialogueTest] '{scriptId}' 점검 결과 — 줄 {nodeCount}개, ");
        builder.Append($"오류 {errors.Count} / 경고 {warnings.Count} / 참고 {notes.Count}");

        Append(builder, "오류", errors);
        Append(builder, "경고", warnings);
        Append(builder, "참고", notes);

        if (DialogueManager.Instance == null)
            builder.Append("\n\n※ Play 중이 아니라 이미지 키 검사는 건너뛰었다.");

        if (errors.Count > 0)
            Debug.LogError(builder.ToString());
        else if (warnings.Count > 0)
            Debug.LogWarning(builder.ToString());
        else
            Debug.Log(builder.ToString() + "\n문제 없음.");
    }

    private static void Append(StringBuilder builder, string title, List<string> lines)
    {
        if (lines.Count == 0)
            return;

        builder.Append($"\n\n[{title}]");

        for (int i = 0; i < lines.Count; i++)
            builder.Append($"\n - {lines[i]}");
    }

    #endregion

    #region 도우미

    private DialogueScriptData LoadScript()
    {
        if (_scriptAsset != null)
            return DialogueManager.Parse(_scriptAsset.text);

        if (!string.IsNullOrWhiteSpace(_resourcePath))
            return DialogueManager.LoadFromResources(_resourcePath);

        Debug.LogError("[DialogueTest] Script Asset과 Resource Path가 모두 비어 있다.");
        return null;
    }

    /// <summary>id를 안 적은 스크립트는 파일 이름을 아이디로 쳐준다.</summary>
    private string ScriptId(DialogueScriptData script)
    {
        if (script != null && !string.IsNullOrWhiteSpace(script.id))
            return script.id;

        if (_scriptAsset != null)
            return _scriptAsset.name;

        return _resourcePath;
    }

    private bool HasManager()
    {
        if (DialogueManager.Instance != null)
            return true;

        Debug.LogError("[DialogueTest] DialogueManager가 씬에 없다. " +
                       "(Hierarchy 우클릭 → UI > Dialogue System 으로 만든다)");
        return false;
    }

    private void Subscribe()
    {
        if (_subscribed || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnDialogueStarted += HandleStarted;
        DialogueManager.Instance.OnNodeStarted += HandleNodeStarted;
        DialogueManager.Instance.OnChoiceMade += HandleChoiceMade;
        DialogueManager.Instance.OnEvent += HandleEvent;
        DialogueManager.Instance.OnDialogueFinished += HandleFinished;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || DialogueManager.Instance == null)
        {
            _subscribed = false;
            return;
        }

        DialogueManager.Instance.OnDialogueStarted -= HandleStarted;
        DialogueManager.Instance.OnNodeStarted -= HandleNodeStarted;
        DialogueManager.Instance.OnChoiceMade -= HandleChoiceMade;
        DialogueManager.Instance.OnEvent -= HandleEvent;
        DialogueManager.Instance.OnDialogueFinished -= HandleFinished;
        _subscribed = false;
    }

    private void HandleStarted(DialogueScriptData script)
    {
        Debug.Log($"[DialogueTest] ▶ 시작: {ScriptId(script)} (줄 {script.NodeCount}개, " +
                  $"건너뛰기 {(script.canSkip ? "허용" : "불가")})");
    }

    private void HandleNodeStarted(DialogueNodeData node)
    {
        _nodeCounter++;

        string id = string.IsNullOrWhiteSpace(node.id) ? "-" : node.id;
        string speaker = string.IsNullOrWhiteSpace(node.speaker) ? "(지문)" : node.speaker;
        string extra = node.HasChoices ? $" / 선택지 {node.choices.Count}개" : "";

        if (node.blockSkip)
            extra += " / blockSkip";

        Debug.Log($"[DialogueTest]   {_nodeCounter}. [{id}] {speaker}{extra}");
    }

    private void HandleChoiceMade(DialogueNodeData node, DialogueChoiceData choice)
    {
        Debug.Log($"[DialogueTest]   └ 선택: '{choice.text}' → {(string.IsNullOrWhiteSpace(choice.next) ? "(다음 줄)" : choice.next)}");
    }

    private void HandleEvent(string eventKey)
    {
        Debug.Log($"[DialogueTest]   ★ 이벤트: '{eventKey}'");
    }

    private void HandleFinished(DialogueScriptData script)
    {
        Debug.Log($"[DialogueTest] ◀ 종료: {ScriptId(script)} (재생한 줄 {_nodeCounter}개)");
    }

    #endregion
}
