using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 식당 구역 카메라 구도. (VIEW-02)
/// 식당 구역(CameraZone.Restaurant)에서 배경 맵과 주방·테이블·대기석·입구·출구 전체를 담는다.
/// 위쪽 운영 HUD와 아래쪽 첫 손님 안내창 영역은 비워 둔다.
///  - 화면비가 바뀌어도(16:9, 16:10, 4:3, 21:9) 핵심 영역이 잘리지 않도록 가로·세로 중 더 빡빡한 쪽에 맞춘다.
///  - 손님처럼 움직이는 오브젝트는 범위에서 제외해 구도가 출렁이지 않는다. 고정 시설 배치만 주기적으로 확인한다.
///  - 월드 카메라만 조정하며 UI 스케일은 각 Canvas가 따로 처리한다.
/// </summary>
public class RestaurantCameraFramer : MonoBehaviour
{
    private const float HudTopFraction = 0.14f;
    private const float BottomFraction = 0.18f;
    private const float Padding = 1.2f; // 맵 가장자리 직원의 상태 문구까지 들어갈 여백

    private Camera _camera;
    private float _originalSize;
    private Transform _environment;
    private Transform _map;
    private Bounds _content;
    private bool _hasContent;
    private int _tableCount = -1;
    private float _aspect;
    private bool _framing;
    private float _nextBoundsRefresh;

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
        // 배경과 타일맵은 Environment가 아닌 별도 Grid 루트에 있다.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "Grid") { _map = root.transform; break; }
        }
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
        if (!_framing || tables != _tableCount || !Mathf.Approximately(_aspect, _camera.aspect)
            || Time.unscaledTime >= _nextBoundsRefresh)
        {
            _tableCount = tables;
            _aspect = _camera.aspect;
            _hasContent = CollectBounds(out _content);
            _nextBoundsRefresh = Time.unscaledTime + 0.5f;
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
        // 축소 상한을 두면 넓은 맵이나 좁은 화면에서 가장자리가 잘린다.
        float size = Mathf.Max(sizeForHeight, sizeForWidth);

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
        IncludeSprites(_map, ref bounds, ref any);
        IncludeSprites(_environment, ref bounds, ref any);
        return any;
    }

    private static void IncludeSprites(Transform root, ref Bounds bounds, ref bool any)
    {
        if (root == null) return;
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null) continue;
            if (sr.GetComponentInParent<CustomerAgent>() != null) continue;   // 움직이는 손님 제외
            if (sr.GetComponentInParent<PartTimerAgent>() != null) continue;  // 움직이는 직원 제외
            if (sr.gameObject.name == "Triangle") continue;                   // 개발용 위치 표시

            if (!any) { bounds = sr.bounds; any = true; }
            else bounds.Encapsulate(sr.bounds);
        }
    }
}
