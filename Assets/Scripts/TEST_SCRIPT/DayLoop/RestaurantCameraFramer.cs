using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 식당 구역 카메라 구도. (VIEW-02)
/// 식당 구역(CameraZone.Restaurant)에서만 동작한다. 주방·테이블·대기석·입구·출구 등 고정 오브젝트의 범위를
/// 화면에 꽉 차게 맞추되, 위쪽 HUD 영역(화면 높이의 11%)과 아래 여백은 비워 둔다.
///  - 화면비가 바뀌어도(16:9, 16:10, 4:3, 21:9) 핵심 영역이 잘리지 않도록 가로·세로 중 더 빡빡한 쪽에 맞춘다.
///  - 손님처럼 움직이는 오브젝트는 범위에서 제외해 구도가 출렁이지 않는다. 테이블 배치가 바뀔 때만 다시 계산한다.
///  - 월드 카메라만 조정하며 UI 스케일은 각 Canvas가 따로 처리한다.
/// </summary>
public class RestaurantCameraFramer : MonoBehaviour
{
    private const float HudTopFraction = 0.11f;
    private const float BottomFraction = 0.03f;
    private const float Padding = 0.6f;
    private const float MinScale = 0.6f;    // 원래 크기 대비 최대 확대
    private const float MaxScale = 1.25f;   // 좁은 화면비에서 핵심 영역을 담기 위한 최대 축소

    private Camera _camera;
    private float _originalSize;
    private Transform _environment;
    private Bounds _content;
    private bool _hasContent;
    private int _tableCount = -1;
    private float _aspect;
    private bool _framing;

    private void Start()
    {
        _camera = Camera.main;
        if (_camera == null || !_camera.orthographic)
        {
            enabled = false;
            return;
        }

        _originalSize = _camera.orthographicSize;
        GameObject env = GameObject.Find("Environment");
        _environment = env != null ? env.transform : null;
    }

    private void LateUpdate()
    {
        if (_camera == null) return;

        bool inRestaurant = CameraController.Instance != null && CameraController.Instance.GetCurZone() == CameraZone.Restaurant;
        if (!inRestaurant)
        {
            if (_framing)
            {
                _camera.orthographicSize = _originalSize;
                _framing = false;
            }
            return;
        }

        int tables = PlacedTable.Active.Count;
        if (!_framing || tables != _tableCount || !Mathf.Approximately(_aspect, _camera.aspect))
        {
            _tableCount = tables;
            _aspect = _camera.aspect;
            _hasContent = CollectBounds(out _content);
        }

        _framing = true;
        if (_hasContent)
            Apply();
    }

    private void Apply()
    {
        float usable = 1f - HudTopFraction - BottomFraction;
        float sizeForHeight = (_content.size.y + Padding * 2f) / (2f * usable);
        float sizeForWidth = (_content.size.x + Padding * 2f) / (2f * Mathf.Max(0.1f, _camera.aspect));
        float size = Mathf.Clamp(Mathf.Max(sizeForHeight, sizeForWidth), _originalSize * MinScale, _originalSize * MaxScale);

        // 쓸 수 있는 띠(HUD 아래 ~ 아래 여백 위)의 가운데에 내용의 가운데를 맞춘다.
        float y = _content.center.y + size * (2f * HudTopFraction - 2f * BottomFraction) * 0.5f;
        Vector3 pos = _camera.transform.position;
        _camera.transform.position = new Vector3(_content.center.x, y, pos.z);
        _camera.orthographicSize = size;
    }

    private bool CollectBounds(out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        if (_environment == null) return false;

        foreach (SpriteRenderer sr in _environment.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null) continue;
            if (sr.GetComponentInParent<CustomerAgent>() != null) continue;   // 움직이는 손님 제외
            if (sr.GetComponentInParent<PartTimerAgent>() != null) continue;  // 움직이는 직원 제외
            if (sr.gameObject.name == "Triangle") continue;                   // 개발용 위치 표시

            if (!any) { bounds = sr.bounds; any = true; }
            else bounds.Encapsulate(sr.bounds);
        }

        return any;
    }
}
