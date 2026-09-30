using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>배경 그림을 화면에 어떻게 맞출지.</summary>
public enum DialogueBackgroundFit
{
    Stretch, // 화면에 꽉 차게 늘린다. 그러데이션 배경엔 이게 맞다.
    Cover,   // 비율을 지키면서 화면을 덮는다. 남는 부분은 잘린다. 사진 배경에 쓴다.
    Contain  // 비율을 지키면서 전부 보이게 넣는다. 위아래(또는 좌우)에 여백이 생긴다.
}

/// <summary>
/// 대사의 겉모습만 담당한다. 배경·초상화·이름표·글자·선택지를 그리고,
/// 클릭과 건너뛰기 요청을 이벤트로 올려보낼 뿐 진행 판단은 DialogueManager가 한다.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Canvas _canvas;

    [Header("배경")]
    [SerializeField] private RectTransform _backgroundRoot;
    [Tooltip("현재 보이는 배경.")]
    [SerializeField] private Image _backgroundImage;
    [Tooltip("전환할 때 위에 겹쳐 띄우는 배경.")]
    [SerializeField] private Image _backgroundNextImage;
    [SerializeField] private DialogueBackgroundFit _backgroundFit = DialogueBackgroundFit.Cover;
    [Tooltip("Fade 전환에 걸리는 시간(초).")]
    [SerializeField] private float _backgroundFadeDuration = 0.35f;

    [Header("초상화")]
    [SerializeField] private Image _leftPortrait;
    [SerializeField] private Image _centerPortrait;
    [SerializeField] private Image _rightPortrait;
    [Tooltip("지금 말하고 있지 않은 쪽 초상화에 씌울 색. 흰색으로 두면 어두워지지 않는다.")]
    [SerializeField] private Color _inactivePortraitTint = new Color(0.62f, 0.62f, 0.70f, 1f);

    [Header("대사 상자")]
    [SerializeField] private GameObject _textBox;
    [SerializeField] private GameObject _nameBox;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _bodyText;
    [Tooltip("글자가 다 나온 뒤 깜빡이는 표시.")]
    [SerializeField] private GameObject _nextIndicator;

    [Header("선택지")]
    [SerializeField] private RectTransform _choiceRoot;
    [Tooltip("복제해서 쓸 선택지 버튼 원본. 꺼둔 채로 둔다.")]
    [SerializeField] private Button _choiceTemplate;

    [Header("건너뛰기")]
    [Tooltip("건너뛰기가 허용된 대사에서만 켜진다.")]
    [SerializeField] private Button _skipButton;

    [Header("입력")]
    [SerializeField] private DialogueClickCatcher _clickCatcher;

    /// <summary>화면 아무 곳이나 눌렀다. (다음으로 넘겨달라는 뜻)</summary>
    public event Action OnAdvanceRequested;

    /// <summary>건너뛰기 버튼을 눌렀다.</summary>
    public event Action OnSkipRequested;

    /// <summary>선택지를 골랐다. 인자는 ShowChoices에 넘긴 목록에서의 번호.</summary>
    public event Action<int> OnChoiceSelected;

    private readonly List<Button> _spawnedChoices = new List<Button>();

    private Coroutine _typeRoutine;
    private Coroutine _backgroundRoutine;
    private Coroutine _indicatorRoutine;

    private Sprite _currentBackground;
    private int _visibleCharacterTarget;

    /// <summary>글자가 아직 나오는 중인지.</summary>
    public bool IsTyping => _typeRoutine != null;

    public bool IsVisible => _root != null && _root.activeSelf;

    /// <summary>대사 화면의 캔버스. 다른 UI와 겹치는 순서를 조정할 때 쓴다.</summary>
    public Canvas Canvas => _canvas;

    #region 생명 주기

    private void Awake()
    {
        if (_skipButton != null)
        {
            _skipButton.onClick.AddListener(HandleSkipClicked);
            _skipButton.gameObject.SetActive(false);
        }

        if (_clickCatcher != null)
            _clickCatcher.OnClicked += HandleSurfaceClicked;

        if (_choiceTemplate != null)
            _choiceTemplate.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(HandleSkipClicked);

        if (_clickCatcher != null)
            _clickCatcher.OnClicked -= HandleSurfaceClicked;
    }

    private void HandleSkipClicked() => OnSkipRequested?.Invoke();

    private void HandleSurfaceClicked() => OnAdvanceRequested?.Invoke();

    #endregion

    #region 표시 / 숨김

    public void Show()
    {
        if (_root != null)
            _root.SetActive(true);
    }

    public void Hide()
    {
        StopAllViewRoutines();
        ClearChoices();

        _currentBackground = null;

        if (_backgroundImage != null)
            _backgroundImage.enabled = false;

        if (_backgroundNextImage != null)
            _backgroundNextImage.enabled = false;

        SetPortraitSprite(_leftPortrait, null);
        SetPortraitSprite(_centerPortrait, null);
        SetPortraitSprite(_rightPortrait, null);

        if (_skipButton != null)
            _skipButton.gameObject.SetActive(false);

        if (_root != null)
            _root.SetActive(false);
    }

    private void StopAllViewRoutines()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        if (_backgroundRoutine != null)
        {
            StopCoroutine(_backgroundRoutine);
            _backgroundRoutine = null;
        }

        StopIndicator();
    }

    /// <summary>대사 상자와 초상화를 감춘다. 배경만 보여주는 연출에 쓴다.</summary>
    public void SetTextBoxVisible(bool visible)
    {
        if (_textBox != null)
            _textBox.SetActive(visible);
    }

    /// <summary>건너뛰기 버튼을 보여줄지 정한다. 허용되지 않은 대사에선 아예 뜨지 않는다.</summary>
    public void SetSkipButtonVisible(bool visible)
    {
        if (_skipButton != null)
            _skipButton.gameObject.SetActive(visible);
    }

    #endregion

    #region 배경

    /// <summary>배경을 바꾼다. sprite가 null이면 배경을 지운다.</summary>
    public void SetBackground(Sprite sprite, DialogueTransition transition)
    {
        if (_backgroundImage == null)
            return;

        if (sprite == _currentBackground)
            return;

        if (_backgroundRoutine != null)
        {
            StopCoroutine(_backgroundRoutine);
            _backgroundRoutine = null;
        }

        _currentBackground = sprite;

        if (transition == DialogueTransition.Fade && _backgroundNextImage != null && isActiveAndEnabled)
        {
            _backgroundRoutine = StartCoroutine(CoFadeBackground(sprite));
            return;
        }

        ApplyBackgroundImmediate(sprite);
    }

    private void ApplyBackgroundImmediate(Sprite sprite)
    {
        ApplySpriteToBackground(_backgroundImage, sprite);

        if (_backgroundNextImage != null)
            _backgroundNextImage.enabled = false;
    }

    private IEnumerator CoFadeBackground(Sprite sprite)
    {
        // 새 배경을 위층에 깔고 서서히 드러낸 다음, 다 드러나면 아래층으로 옮겨 담는다.
        ApplySpriteToBackground(_backgroundNextImage, sprite);

        Color color = _backgroundNextImage.color;
        color.a = 0f;
        _backgroundNextImage.color = color;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _backgroundFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            _backgroundNextImage.color = color;
            yield return null;
        }

        ApplySpriteToBackground(_backgroundImage, sprite);

        color.a = 0f;
        _backgroundNextImage.color = color;
        _backgroundNextImage.enabled = false;

        _backgroundRoutine = null;
    }

    private void ApplySpriteToBackground(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;

        Color color = image.color;
        color.a = 1f;
        image.color = color;

        if (sprite != null)
            FitBackground(image, sprite);
    }

    /// <summary>배경 그림을 설정한 방식대로 화면에 맞춘다.</summary>
    private void FitBackground(Image image, Sprite sprite)
    {
        RectTransform rect = image.rectTransform;

        if (_backgroundFit == DialogueBackgroundFit.Stretch)
        {
            image.preserveAspect = false;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        if (_backgroundFit == DialogueBackgroundFit.Contain)
        {
            // Image가 알아서 비율을 지켜 안쪽에 맞춰준다.
            image.preserveAspect = true;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        // Cover: 비율을 지키면서 화면을 덮도록 직접 크기를 계산한다. 넘치는 부분은 부모의 마스크가 잘라준다.
        image.preserveAspect = false;

        Rect parentRect = _backgroundRoot != null
            ? _backgroundRoot.rect
            : (rect.parent as RectTransform)?.rect ?? rect.rect;

        float spriteHeight = sprite.rect.height;

        if (parentRect.width <= 0f || parentRect.height <= 0f || spriteHeight <= 0f)
            return;

        float parentAspect = parentRect.width / parentRect.height;
        float spriteAspect = sprite.rect.width / spriteHeight;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        rect.sizeDelta = spriteAspect > parentAspect
            ? new Vector2(parentRect.height * spriteAspect, parentRect.height)
            : new Vector2(parentRect.width, parentRect.width / spriteAspect);
    }

    #endregion

    #region 초상화

    /// <summary>말하는 쪽 초상화를 세우고, 나머지 쪽은 살짝 어둡게 만든다.</summary>
    public void SetPortrait(Sprite sprite, DialoguePortraitSide side, bool clearRequested)
    {
        Image target = GetPortraitImage(side);

        if (clearRequested)
        {
            SetPortraitSprite(target, null);
        }
        else if (sprite != null && target != null)
        {
            SetPortraitSprite(target, sprite);
        }

        HighlightSide(side);
    }

    /// <summary>모든 초상화를 지운다.</summary>
    public void ClearPortraits()
    {
        SetPortraitSprite(_leftPortrait, null);
        SetPortraitSprite(_centerPortrait, null);
        SetPortraitSprite(_rightPortrait, null);
    }

    private Image GetPortraitImage(DialoguePortraitSide side)
    {
        switch (side)
        {
            case DialoguePortraitSide.Left: return _leftPortrait;
            case DialoguePortraitSide.Right: return _rightPortrait;
            case DialoguePortraitSide.Center: return _centerPortrait;
            default: return null;
        }
    }

    private static void SetPortraitSprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private void HighlightSide(DialoguePortraitSide activeSide)
    {
        ApplyTint(_leftPortrait, activeSide == DialoguePortraitSide.Left);
        ApplyTint(_centerPortrait, activeSide == DialoguePortraitSide.Center);
        ApplyTint(_rightPortrait, activeSide == DialoguePortraitSide.Right);
    }

    private void ApplyTint(Image image, bool isActive)
    {
        if (image == null)
            return;

        image.color = isActive ? Color.white : _inactivePortraitTint;
    }

    #endregion

    #region 글자

    /// <summary>이름표를 설정한다. 이름이 비면 이름표 자체를 감춘다.</summary>
    public void SetSpeaker(string speakerName, Color color)
    {
        bool hasName = !string.IsNullOrWhiteSpace(speakerName);

        if (_nameBox != null)
            _nameBox.SetActive(hasName);

        if (_nameText != null)
        {
            _nameText.text = hasName ? speakerName : "";
            _nameText.color = color;
        }
    }

    /// <summary>대사 한 줄을 타자 효과로 출력하기 시작한다.</summary>
    public void BeginLine(string text, float charactersPerSecond)
    {
        if (_bodyText == null)
            return;

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        StopIndicator();

        _bodyText.text = text ?? "";
        _bodyText.ForceMeshUpdate();
        _visibleCharacterTarget = _bodyText.textInfo.characterCount;

        if (charactersPerSecond <= 0f || _visibleCharacterTarget <= 0 || !isActiveAndEnabled)
        {
            _bodyText.maxVisibleCharacters = int.MaxValue;
            ShowIndicator();
            return;
        }

        _bodyText.maxVisibleCharacters = 0;
        _typeRoutine = StartCoroutine(CoType(charactersPerSecond));
    }

    private IEnumerator CoType(float charactersPerSecond)
    {
        float revealed = 0f;

        while (revealed < _visibleCharacterTarget)
        {
            // 대사 중엔 게임 시간이 멈춰 있을 수 있어 항상 실제 시간을 쓴다.
            revealed += charactersPerSecond * Time.unscaledDeltaTime;
            _bodyText.maxVisibleCharacters = Mathf.Min(_visibleCharacterTarget, Mathf.FloorToInt(revealed));
            yield return null;
        }

        _bodyText.maxVisibleCharacters = int.MaxValue;
        _typeRoutine = null;

        ShowIndicator();
    }

    /// <summary>나오는 중인 글자를 즉시 전부 채운다.</summary>
    public void CompleteTyping()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        if (_bodyText != null)
            _bodyText.maxVisibleCharacters = int.MaxValue;

        ShowIndicator();
    }

    #endregion

    #region 다음 표시

    private void ShowIndicator()
    {
        if (_nextIndicator == null)
            return;

        _nextIndicator.SetActive(true);

        if (_indicatorRoutine == null && isActiveAndEnabled)
            _indicatorRoutine = StartCoroutine(CoPulseIndicator());
    }

    private void StopIndicator()
    {
        if (_indicatorRoutine != null)
        {
            StopCoroutine(_indicatorRoutine);
            _indicatorRoutine = null;
        }

        if (_nextIndicator != null)
            _nextIndicator.SetActive(false);
    }

    private IEnumerator CoPulseIndicator()
    {
        Graphic graphic = _nextIndicator.GetComponent<Graphic>();

        if (graphic == null)
            yield break;

        float time = 0f;

        while (true)
        {
            time += Time.unscaledDeltaTime;

            Color color = graphic.color;
            color.a = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(time * 1.6f, 1f));
            graphic.color = color;

            yield return null;
        }
    }

    #endregion

    #region 선택지

    /// <summary>선택지 버튼을 만들어 띄운다. 고르면 OnChoiceSelected로 번호가 온다.</summary>
    public void ShowChoices(IReadOnlyList<string> labels)
    {
        ClearChoices();

        if (_choiceRoot == null || _choiceTemplate == null || labels == null)
            return;

        // 선택지를 고를 차례엔 '클릭해서 다음' 표시가 헷갈리게 하므로 감춘다.
        StopIndicator();

        for (int i = 0; i < labels.Count; i++)
        {
            Button button = Instantiate(_choiceTemplate, _choiceRoot);
            button.gameObject.SetActive(true);
            button.name = $"Choice_{i}";

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = labels[i];

            int index = i; // 클로저가 마지막 값만 잡지 않도록 복사해둔다.
            button.onClick.AddListener(() => OnChoiceSelected?.Invoke(index));

            _spawnedChoices.Add(button);
        }

        _choiceRoot.gameObject.SetActive(true);
    }

    /// <summary>띄워둔 선택지를 모두 치운다.</summary>
    public void ClearChoices()
    {
        for (int i = 0; i < _spawnedChoices.Count; i++)
        {
            if (_spawnedChoices[i] == null)
                continue;

            _spawnedChoices[i].onClick.RemoveAllListeners();

            // Destroy는 프레임 끝에야 실제로 지워진다. 같은 프레임에 새 선택지가 뜨면
            // 옛 버튼이 한 프레임 같이 보이므로 먼저 꺼서 배치에서 빼둔다.
            _spawnedChoices[i].gameObject.SetActive(false);
            Destroy(_spawnedChoices[i].gameObject);
        }

        _spawnedChoices.Clear();

        if (_choiceRoot != null)
            _choiceRoot.gameObject.SetActive(false);
    }

    #endregion
}
