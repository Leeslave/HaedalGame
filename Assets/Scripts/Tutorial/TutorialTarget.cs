using UnityEngine;

/// <summary>
/// 튜토리얼 하이라이트가 가리킬 수 있도록 오브젝트에 "키"를 붙여주는 컴포넌트.
/// UI(RectTransform), 스프라이트가 있는 월드 오브젝트, 그냥 Transform 모두 지원한다.
/// 켜질 때 자동으로 <see cref="TutorialTargetRegistry"/>에 등록되고 꺼질 때 해제된다.
/// </summary>
[DisallowMultipleComponent]
public class TutorialTarget : MonoBehaviour
{
    [Tooltip("튜토리얼 스텝(TutorialStepSO)의 Target Key와 일치시켜야 한다. 예) shop.buyButton")]
    [SerializeField] private string _key;

    [Header("영역 보정")]
    [Tooltip("하이라이트 영역을 좌우/상하로 더 넓히거나(양수) 좁힌다(음수). 화면 픽셀 단위.")]
    [SerializeField] private Vector2 _screenPadding = Vector2.zero;

    [Header("월드 오브젝트 전용")]
    [Tooltip("UI가 아닐 때 화면 좌표 계산에 쓸 카메라. 비우면 Camera.main을 사용한다.")]
    [SerializeField] private Camera _worldCamera;
    [Tooltip("RectTransform도 Renderer도 없을 때 사용할 기본 하이라이트 크기(화면 픽셀).")]
    [SerializeField] private Vector2 _fallbackScreenSize = new Vector2(160f, 160f);

    private static readonly Vector3[] _cornerBuffer = new Vector3[4];

    private RectTransform _rectTransform;
    private Renderer _renderer;
    private bool _registered;

    public string Key => _key;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _renderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        RegisterSelf();
    }

    private void OnDisable()
    {
        UnregisterSelf();
    }

    /// <summary>런타임에 생성되는 슬롯 등에서 키를 코드로 지정할 때 사용한다.</summary>
    public void SetKey(string key)
    {
        if (_key == key)
            return;

        UnregisterSelf();
        _key = key;

        if (isActiveAndEnabled)
            RegisterSelf();
    }

    /// <summary>이 대상이 화면에서 차지하는 영역(스크린 좌표계).</summary>
    public Rect GetScreenRect()
    {
        Rect rect;

        if (_rectTransform != null)
            rect = GetRectTransformScreenRect(_rectTransform);
        else if (_renderer != null)
            rect = GetBoundsScreenRect(_renderer.bounds);
        else
            rect = GetFallbackScreenRect();

        if (_screenPadding != Vector2.zero)
        {
            rect.xMin -= _screenPadding.x;
            rect.xMax += _screenPadding.x;
            rect.yMin -= _screenPadding.y;
            rect.yMax += _screenPadding.y;
        }

        return rect;
    }

    private void RegisterSelf()
    {
        if (_registered || string.IsNullOrEmpty(_key))
            return;

        TutorialTargetRegistry.Register(this);
        _registered = true;
    }

    private void UnregisterSelf()
    {
        if (!_registered)
            return;

        TutorialTargetRegistry.Unregister(this);
        _registered = false;
    }

    private Rect GetRectTransformScreenRect(RectTransform rectTransform)
    {
        Camera camera = ResolveCanvasCamera(rectTransform);
        rectTransform.GetWorldCorners(_cornerBuffer);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _cornerBuffer[0]);
        Vector2 max = min;

        for (int i = 1; i < 4; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, _cornerBuffer[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return new Rect(min, max - min);
    }

    private Rect GetBoundsScreenRect(Bounds bounds)
    {
        Camera camera = ResolveWorldCamera();

        if (camera == null)
            return GetFallbackScreenRect();

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        bool initialized = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z);

            Vector2 point = camera.WorldToScreenPoint(corner);

            if (!initialized)
            {
                min = point;
                max = point;
                initialized = true;
                continue;
            }

            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return new Rect(min, max - min);
    }

    private Rect GetFallbackScreenRect()
    {
        Camera camera = ResolveWorldCamera();
        Vector2 center = camera != null
            ? (Vector2)camera.WorldToScreenPoint(transform.position)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        return new Rect(center - _fallbackScreenSize * 0.5f, _fallbackScreenSize);
    }

    private Camera ResolveWorldCamera()
    {
        return _worldCamera != null ? _worldCamera : Camera.main;
    }

    private static Camera ResolveCanvasCamera(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        canvas = canvas.rootCanvas;

        // Overlay 캔버스는 카메라가 null이어야 스크린 좌표가 맞는다.
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}
