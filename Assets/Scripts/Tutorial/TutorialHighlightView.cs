using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 하이라이트 팝업의 표현 담당.
/// 타깃 주변만 남기고 화면을 어둡게 덮는 딤 패널 4장을 런타임에 만들어 "구멍"을 뚫고,
/// 그 위에 강조 테두리와 안내 문구를 배치한다. (셰이더/마스크 없이 동작하므로 어떤 캔버스에서도 쓸 수 있다)
///
/// 프리팹 구성 예)
///   TutorialOverlay (Canvas / 최상단 Sorting Order, 이 스크립트, GraphicRaycaster)
///     └ HighlightImage (Image, 원형 테두리 스프라이트)
///     └ TextBlock (VerticalLayoutGroup + ContentSizeFitter)
///          ├ TitleText / DescriptionText / InstructionText (TMP_Text)
///          └ ExtraLineRoot (VerticalLayoutGroup)
///     └ SkipButton (Button)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialHighlightView : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("켜고 끌 오브젝트. 비우면 이 게임오브젝트를 사용한다.")]
    [SerializeField] private GameObject _root;
    [Tooltip("좌표 변환에 사용할 캔버스. 비우면 부모에서 찾는다.")]
    [SerializeField] private Canvas _canvas;
    [Tooltip("딤 패널이 생성될 부모. 비우면 이 오브젝트의 RectTransform을 사용한다.")]
    [SerializeField] private RectTransform _maskRoot;

    [Header("Dim")]
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.75f);
    [Tooltip("구멍 영역 안쪽을 덮어 타깃 클릭을 막는 투명 패널을 쓸지. (TargetClick 스텝에서는 자동으로 꺼진다)")]
    [SerializeField] private bool _useHoleBlocker = true;

    [Header("Highlight")]
    [Tooltip("강조 테두리 이미지. 없으면 테두리 없이 구멍만 뚫린다.")]
    [SerializeField] private Image _highlightImage;
    [SerializeField] private Sprite _circleSprite;
    [SerializeField] private Sprite _rectangleSprite;
    [SerializeField] private bool _usePulse = true;
    [SerializeField] private float _pulseScale = 1.06f;
    [SerializeField] private float _pulseDuration = 0.7f;

    [Header("Text")]
    [SerializeField] private RectTransform _textBlock;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _instructionText;
    [Tooltip("부연 설명 줄이 생성될 부모.")]
    [SerializeField] private RectTransform _extraLineRoot;
    [SerializeField] private TMP_Text _extraLinePrefab;
    [Tooltip("문구 묶음과 하이라이트 사이 간격(캔버스 단위).")]
    [SerializeField] private float _textMargin = 48f;

    [Header("Skip")]
    [SerializeField] private Button _skipButton;

    /// <summary>건너뛰기 버튼을 눌렀을 때.</summary>
    public event Action OnSkipRequested;

    private static readonly Vector3[] _cornerBuffer = new Vector3[4];

    private RectTransform _dimTop;
    private RectTransform _dimBottom;
    private RectTransform _dimLeft;
    private RectTransform _dimRight;
    private Image _holeBlocker;

    private readonly List<TMP_Text> _extraLineInstances = new List<TMP_Text>();

    private Coroutine _pulseRoutine;
    private bool _initialized;

    /// <summary>현재 뚫려 있는 구멍의 화면 좌표 영역. (패딩까지 반영된 값)</summary>
    public Rect HoleScreenRect { get; private set; }

    /// <summary>현재 스텝에 타깃이 있는지.</summary>
    public bool HasHole { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (_root == null)
            _root = gameObject;

        if (_maskRoot == null)
            _maskRoot = transform as RectTransform;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        // 딤 패널은 항상 맨 뒤에 깔리도록 먼저 생성한다.
        _dimTop = CreateDimPanel("Dim_Top", _dimColor, true);
        _dimBottom = CreateDimPanel("Dim_Bottom", _dimColor, true);
        _dimLeft = CreateDimPanel("Dim_Left", _dimColor, true);
        _dimRight = CreateDimPanel("Dim_Right", _dimColor, true);

        RectTransform blocker = CreateDimPanel("Hole_Blocker", Color.clear, _useHoleBlocker);
        _holeBlocker = blocker.GetComponent<Image>();

        if (_skipButton != null)
            _skipButton.onClick.AddListener(HandleSkipClicked);

        SetRootActive(false);
    }

    private RectTransform CreateDimPanel(string panelName, Color color, bool raycastTarget)
    {
        GameObject panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = (RectTransform)panelObject.transform;
        rectTransform.SetParent(_maskRoot, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
        rectTransform.SetAsFirstSibling();

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return rectTransform;
    }

    public void Show()
    {
        Initialize();
        SetRootActive(true);
    }

    public void Hide()
    {
        // 매니저가 뷰보다 먼저 Awake될 수 있어 여기서도 초기화를 보장한다.
        Initialize();
        StopPulse();
        SetRootActive(false);
        HasHole = false;
    }

    private void SetRootActive(bool active)
    {
        if (_root != null && _root.activeSelf != active)
            _root.SetActive(active);
    }

    /// <summary>
    /// 스텝의 문구/모양을 적용한다. 위치는 매 프레임 <see cref="UpdateHole"/>로 갱신한다.
    /// </summary>
    public void SetStep(TutorialStepSO step)
    {
        Initialize();

        if (step == null)
            return;

        ApplyTexts(step);
        ApplyShape(step);

        if (_skipButton != null)
            _skipButton.gameObject.SetActive(step.ShowSkipButton);
    }

    private void ApplyTexts(TutorialStepSO step)
    {
        SetText(_titleText, step.TitleText);
        SetText(_descriptionText, step.DescriptionText);
        SetText(_instructionText, step.InstructionText);

        ApplyExtraLines(step.ExtraLines);

        if (_textBlock != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_textBlock);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target == null)
            return;

        bool hasValue = !string.IsNullOrEmpty(value);
        target.text = value;

        if (target.gameObject.activeSelf != hasValue)
            target.gameObject.SetActive(hasValue);
    }

    private void ApplyExtraLines(IReadOnlyList<string> lines)
    {
        if (_extraLineRoot == null || _extraLinePrefab == null)
            return;

        int count = lines != null ? lines.Count : 0;

        // 부족하면 만들고, 남으면 꺼두고 재사용한다.
        while (_extraLineInstances.Count < count)
        {
            TMP_Text instance = Instantiate(_extraLinePrefab, _extraLineRoot);
            _extraLineInstances.Add(instance);
        }

        for (int i = 0; i < _extraLineInstances.Count; i++)
        {
            TMP_Text instance = _extraLineInstances[i];

            if (instance == null)
                continue;

            bool used = i < count;
            instance.gameObject.SetActive(used);

            if (used)
                instance.text = lines[i];
        }
    }

    private void ApplyShape(TutorialStepSO step)
    {
        if (_highlightImage == null)
            return;

        bool visible = step.HasTarget && step.Shape != TutorialHighlightShape.None;
        _highlightImage.gameObject.SetActive(visible);

        if (!visible)
        {
            StopPulse();
            return;
        }

        Sprite sprite = step.Shape == TutorialHighlightShape.Circle ? _circleSprite : _rectangleSprite;

        if (sprite != null)
            _highlightImage.sprite = sprite;

        StartPulse();
    }

    /// <summary>
    /// 타깃의 화면 영역을 받아 구멍/테두리/문구 위치를 갱신한다.
    /// 스크롤·애니메이션으로 타깃이 움직여도 따라가도록 매 프레임 호출하면 된다.
    /// </summary>
    /// <param name="targetScreenRect">타깃의 화면 좌표 영역. 타깃이 없으면 <c>null</c>.</param>
    public void UpdateHole(Rect? targetScreenRect, TutorialStepSO step)
    {
        Initialize();

        Rect area = _maskRoot.rect;
        Rect hole;

        if (targetScreenRect.HasValue)
        {
            hole = ScreenRectToLocalRect(targetScreenRect.Value);

            Vector2 padding = step != null ? step.HighlightPadding : Vector2.zero;

            if (step != null && step.Shape == TutorialHighlightShape.Circle)
            {
                // 원형은 정사각형 기준으로 잡아야 타깃이 잘리지 않는다.
                float side = Mathf.Max(hole.width, hole.height);
                Vector2 center = hole.center;
                hole = new Rect(center.x - side * 0.5f, center.y - side * 0.5f, side, side);
            }

            hole.xMin -= padding.x;
            hole.xMax += padding.x;
            hole.yMin -= padding.y;
            hole.yMax += padding.y;

            HasHole = true;
        }
        else
        {
            // 타깃이 없으면 화면 중앙에 크기 0짜리 구멍 = 전체 딤.
            hole = new Rect(area.center, Vector2.zero);
            HasHole = false;
        }

        ApplyDimPanels(area, hole);
        ApplyHighlight(hole);
        ApplyTextBlock(area, hole, step);

        HoleScreenRect = HasHole ? LocalRectToScreenRect(hole) : new Rect(Vector2.zero, Vector2.zero);
    }

    private void ApplyDimPanels(Rect area, Rect hole)
    {
        float holeMinX = Mathf.Clamp(hole.xMin, area.xMin, area.xMax);
        float holeMaxX = Mathf.Clamp(hole.xMax, area.xMin, area.xMax);
        float holeMinY = Mathf.Clamp(hole.yMin, area.yMin, area.yMax);
        float holeMaxY = Mathf.Clamp(hole.yMax, area.yMin, area.yMax);

        SetPanelRect(_dimTop, area.xMin, area.xMax, holeMaxY, area.yMax, area);
        SetPanelRect(_dimBottom, area.xMin, area.xMax, area.yMin, holeMinY, area);
        SetPanelRect(_dimLeft, area.xMin, holeMinX, holeMinY, holeMaxY, area);
        SetPanelRect(_dimRight, holeMaxX, area.xMax, holeMinY, holeMaxY, area);

        if (_holeBlocker != null)
            SetPanelRect(_holeBlocker.rectTransform, holeMinX, holeMaxX, holeMinY, holeMaxY, area);
    }

    private static void SetPanelRect(RectTransform rectTransform, float xMin, float xMax, float yMin, float yMax, Rect area)
    {
        if (rectTransform == null)
            return;

        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);

        rectTransform.sizeDelta = new Vector2(width, height);
        // 앵커가 부모 중앙(0.5, 0.5)이므로 부모 rect의 중심을 기준으로 오프셋을 잡는다.
        rectTransform.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f) - area.center;
    }

    private void ApplyHighlight(Rect hole)
    {
        if (_highlightImage == null || !_highlightImage.gameObject.activeSelf)
            return;

        RectTransform rectTransform = _highlightImage.rectTransform;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, hole.width);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hole.height);
        rectTransform.position = _maskRoot.TransformPoint(hole.center);
    }

    private void ApplyTextBlock(Rect area, Rect hole, TutorialStepSO step)
    {
        if (_textBlock == null || step == null)
            return;

        TutorialTextPlacement placement = step.TextPlacement;

        if (placement == TutorialTextPlacement.Fixed)
            return;

        if (!HasHole)
        {
            // 타깃이 없으면 화면 중앙.
            _textBlock.position = _maskRoot.TransformPoint(area.center + step.TextOffset);
            return;
        }

        float blockHeight = _textBlock.rect.height;
        float halfHeight = blockHeight * 0.5f;

        float spaceAbove = area.yMax - hole.yMax;
        float spaceBelow = hole.yMin - area.yMin;

        bool placeAbove;

        switch (placement)
        {
            case TutorialTextPlacement.Above:
                placeAbove = true;
                break;
            case TutorialTextPlacement.Below:
                placeAbove = false;
                break;
            default:
                placeAbove = spaceAbove > spaceBelow;
                break;
        }

        float centerY = placeAbove
            ? hole.yMax + _textMargin + halfHeight
            : hole.yMin - _textMargin - halfHeight;

        // 화면 밖으로 나가지 않게 가둔다.
        centerY = Mathf.Clamp(centerY, area.yMin + halfHeight, area.yMax - halfHeight);

        Vector2 localPosition = new Vector2(area.center.x, centerY) + step.TextOffset;
        _textBlock.position = _maskRoot.TransformPoint(localPosition);
    }

    /// <summary>구멍 안쪽 클릭을 막을지 여부. (TargetClick 스텝에서는 false로 두어 타깃을 실제로 누를 수 있게 한다)</summary>
    public void SetHoleBlocking(bool block)
    {
        Initialize();

        if (_holeBlocker != null)
            _holeBlocker.raycastTarget = _useHoleBlocker && block;
    }

    /// <summary>해당 화면 좌표가 현재 구멍 안쪽인지.</summary>
    public bool IsInsideHole(Vector2 screenPosition)
    {
        return HasHole && HoleScreenRect.Contains(screenPosition);
    }

    private Rect ScreenRectToLocalRect(Rect screenRect)
    {
        Camera camera = ResolveCamera();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_maskRoot, screenRect.min, camera, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_maskRoot, screenRect.max, camera, out Vector2 max);

        return new Rect(Vector2.Min(min, max), new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)));
    }

    private Rect LocalRectToScreenRect(Rect localRect)
    {
        Camera camera = ResolveCamera();

        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _maskRoot.TransformPoint(localRect.min));
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, _maskRoot.TransformPoint(localRect.max));

        Vector2 screenMin = Vector2.Min(min, max);
        Vector2 screenMax = Vector2.Max(min, max);

        return new Rect(screenMin, screenMax - screenMin);
    }

    private Camera ResolveCamera()
    {
        if (_canvas == null)
            return null;

        Canvas rootCanvas = _canvas.rootCanvas;
        return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
    }

    private void StartPulse()
    {
        StopPulse();

        if (!_usePulse || _highlightImage == null)
            return;

        _pulseRoutine = StartCoroutine(CoPulse());
    }

    private void StopPulse()
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        if (_highlightImage != null)
            _highlightImage.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator CoPulse()
    {
        RectTransform rectTransform = _highlightImage.rectTransform;
        float duration = Mathf.Max(0.01f, _pulseDuration);

        while (true)
        {
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                // 0 -> 1 -> 0 으로 부드럽게 왕복
                float t = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI);
                rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1f, _pulseScale, t);
                yield return null;
            }

            rectTransform.localScale = Vector3.one;
            yield return null;
        }
    }

    private void HandleSkipClicked()
    {
        OnSkipRequested?.Invoke();
    }
}
