using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 월드 상태 표시. (NPC-01, NPC-02, ECON-01, ART-10/11 임시 표현)
///  - 손님: 발밑 상태 칩(글자) + 인내심이 실제로 줄어드는 동안 게이지. 색만으로 상태를 구분하지 않는다.
///  - 식사 중: 주문한 메뉴 아이콘을 좌석 앞 식탁 위치에 표시
///  - 홀 직원: 음식 운반 중에는 머리 위에 같은 메뉴 아이콘. 서빙 순간 직원 쪽 아이콘이 사라지고 식탁에 나타난다.
///  - 직원: 역할·현재 작업 상태 칩
///  - 계산: 실제 지급 이벤트 금액으로 +nG 팝업 (오늘 첫 수익은 크게)
/// 표시만 담당하며 손님·직원의 행동이나 지급에는 관여하지 않는다.
/// </summary>
public class NpcStatusOverlay : MonoBehaviour
{
    private const float ChipHeight = 0.5f;
    private const float ChipFontSize = 3.0f;
    private const float IconSize = 0.6f;
    private const int ChipOrder = 4000;

    private class Chip
    {
        public Transform root;
        public SpriteRenderer background;
        public TextMeshPro text;
        public SpriteRenderer gaugeBack;
        public SpriteRenderer gaugeFill;
        public string shown;
    }

    private class CustomerView
    {
        public SpriteRenderer body;
        public Chip chip;
        public SpriteRenderer food;
    }

    private class StaffView
    {
        public SpriteRenderer body;
        public Chip chip;
        public SpriteRenderer food;
    }

    private DayCycleController _day;
    private readonly Dictionary<CustomerAgent, CustomerView> _customers = new Dictionary<CustomerAgent, CustomerView>();
    private readonly Dictionary<PartTimerAgent, StaffView> _staff = new Dictionary<PartTimerAgent, StaffView>();
    private readonly List<CustomerAgent> _removeCustomers = new List<CustomerAgent>();
    private readonly List<PartTimerAgent> _removeStaff = new List<PartTimerAgent>();
    private Transform _root;
    private Sprite _chipSprite;
    private int _layerId;
    private float _nextStaffScan;
    private bool _visible;

    public void Init(DayCycleController day)
    {
        _day = day;
        _root = new GameObject("[DayLoop] WorldStatus").transform;
        _root.SetParent(transform, false);
        _layerId = DayLoopUI.TopSortingLayerId;

        // 월드용 둥근 칩: 같은 텍스처를 작은 유닛 크기로 다시 자른다.
        Sprite ui = DayLoopUI.Rounded;
        _chipSprite = Sprite.Create(ui.texture, ui.rect, new Vector2(0.5f, 0.5f), 160f, 0, SpriteMeshType.FullRect, ui.border);

        CustomerAgent.OnAnySpawned += HandleSpawned;
        _day.Ledger.OnPayment += HandlePayment;
        _day.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(_day.Phase);
    }

    private void OnDestroy()
    {
        CustomerAgent.OnAnySpawned -= HandleSpawned;
        if (_day != null)
        {
            _day.Ledger.OnPayment -= HandlePayment;
            _day.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        _visible = phase == DayLoopPhase.Open || phase == DayLoopPhase.Closing;
        _root.gameObject.SetActive(_visible);
        if (_visible) ScanStaff();
    }

    private void HandleSpawned(CustomerAgent customer)
    {
        if (customer == null || _customers.ContainsKey(customer)) return;
        _customers[customer] = new CustomerView
        {
            body = customer.GetComponentInChildren<SpriteRenderer>(),
            chip = CreateChip("Customer"),
            food = CreateIcon("TableFood"),
        };
    }

    private void ScanStaff()
    {
        _nextStaffScan = Time.unscaledTime + 2f;
        foreach (ServerAgent server in FindObjectsByType<ServerAgent>(FindObjectsSortMode.None))
            AddStaff(server);
        foreach (ChefAgent chef in FindObjectsByType<ChefAgent>(FindObjectsSortMode.None))
            AddStaff(chef);
    }

    private void AddStaff(PartTimerAgent agent)
    {
        if (agent == null || _staff.ContainsKey(agent)) return;
        _staff[agent] = new StaffView
        {
            body = agent.GetComponentInChildren<SpriteRenderer>(),
            chip = CreateChip("Staff"),
            food = CreateIcon("CarriedFood"),
        };
    }

    private void LateUpdate()
    {
        if (!_visible) return;
        if (Time.unscaledTime >= _nextStaffScan) ScanStaff();

        _removeCustomers.Clear();
        foreach (KeyValuePair<CustomerAgent, CustomerView> pair in _customers)
        {
            if (pair.Key == null) { _removeCustomers.Add(pair.Key); continue; }
            UpdateCustomer(pair.Key, pair.Value);
        }
        foreach (CustomerAgent dead in _removeCustomers)
        {
            CustomerView view = _customers[dead];
            Destroy(view.chip.root.gameObject);
            Destroy(view.food.gameObject);
            _customers.Remove(dead);
        }

        _removeStaff.Clear();
        foreach (KeyValuePair<PartTimerAgent, StaffView> pair in _staff)
        {
            if (pair.Key == null) { _removeStaff.Add(pair.Key); continue; }
            UpdateStaff(pair.Key, pair.Value);
        }
        foreach (PartTimerAgent dead in _removeStaff)
        {
            StaffView view = _staff[dead];
            Destroy(view.chip.root.gameObject);
            Destroy(view.food.gameObject);
            _staff.Remove(dead);
        }
    }

    // ───── 손님 ─────

    private void UpdateCustomer(CustomerAgent customer, CustomerView view)
    {
        Bounds bounds = GetBounds(customer.transform, view.body);
        CustomerState state = customer.State;

        string label = CustomerLabel(customer, state, out Color textColor);
        SetChip(view.chip, label, textColor);
        view.chip.root.position = new Vector3(bounds.center.x, bounds.min.y - ChipHeight * 0.6f, 0f);

        bool waitingState = state == CustomerState.WaitingRoom || state == CustomerState.WaitingForOrder || state == CustomerState.WaitingForFood;
        CustomerPatienceComponent patience = customer.cpc;
        bool showGauge = waitingState && patience != null && patience.IsDraining;
        SetGauge(view.chip, showGauge, patience != null ? patience.Ratio : 1f);
        if (showGauge && patience.RemainingSeconds <= 5)
            SetChip(view.chip, Mathf.CeilToInt(patience.RemainingSeconds) + "초!", Mathf.PingPong(Time.time * 4, 1) > .5f ? DayLoopUI.Warn : Color.white);

        // 식탁 위 음식: 식사 중에만, 앉은 좌석 앞(손님이 바라보는 방향)에 놓는다.
        RecipeData order = customer.coc != null ? customer.coc.GetOrderData() : null;
        Seat seat = customer.GetCurrentSeat();
        bool eating = state == CustomerState.Eating && order != null && order.Icon != null && seat != null;
        view.food.gameObject.SetActive(eating);
        if (eating)
        {
            SetIcon(view.food, order.Icon);
            Vector2 facing = seat.GetFacingDirection();
            Vector3 basePos = seat.GetSeatPoint() != null ? seat.GetSeatPoint().position : customer.transform.position;
            view.food.transform.position = basePos + (Vector3)(facing.normalized * 0.6f);
        }
    }

    private static string CustomerLabel(CustomerAgent customer, CustomerState state, out Color color)
    {
        color = DayLoopUI.Ink;
        switch (state)
        {
            case CustomerState.Seating:
                return "자리로 이동";
            case CustomerState.WaitingRoom:
                return "자리 대기";
            case CustomerState.WaitingForOrder:
                return customer.coc != null && customer.coc.GetOrderData() != null ? "주문 대기" : "메뉴 고르는 중";
            case CustomerState.WaitingForFood:
                return "요리 기다리는 중";
            case CustomerState.Eating:
                return "식사 중";
            case CustomerState.Paying:
                color = DayLoopUI.GoldText;
                return "계산";
            case CustomerState.Exit:
                if (customer.WasServed) return new[] { "아쉬워요", "괜찮았어요", "잘 먹었어요", "맛있어요!", "최고예요!" }[Mathf.Clamp(Mathf.FloorToInt(customer.LastRating), 0, 4)] + $" {customer.LastRating:0.0}";
                color = DayLoopUI.Warn;
                return "그냥 돌아가요";
            default:
                return "입장";
        }
    }

    // ───── 직원 ─────

    private void UpdateStaff(PartTimerAgent agent, StaffView view)
    {
        Bounds bounds = GetBounds(agent.transform, view.body);
        string label;
        RecipeData carried = null;

        if (agent is ServerAgent server)
        {
            label = "홀 • " + server.StateLabel;
            carried = server.CarriedFood;
        }
        else if (agent is ChefAgent chef)
        {
            label = "주방 • " + chef.StateLabel;
        }
        else
        {
            label = "직원";
        }

        SetChip(view.chip, label, DayLoopUI.WoodDark);
        view.chip.root.position = new Vector3(bounds.center.x, bounds.min.y - ChipHeight * 0.6f, 0f);
        SetGauge(view.chip, false, 1f);

        bool carrying = carried != null && carried.Icon != null;
        view.food.gameObject.SetActive(carrying);
        if (carrying)
        {
            SetIcon(view.food, carried.Icon);
            view.food.transform.position = new Vector3(bounds.center.x, bounds.max.y + IconSize * 0.45f, 0f);
        }
    }

    // ───── 결제 팝업 ─────

    private void HandlePayment(CustomerAgent customer, int amount, bool first)
    {
        if (customer == null) return;
        CustomerView view;
        _customers.TryGetValue(customer, out view);
        Bounds bounds = GetBounds(customer.transform, view != null ? view.body : null);
        StartCoroutine(GoldPopup(new Vector3(bounds.center.x, bounds.max.y + 0.2f, 0f), amount, first));
    }

    private IEnumerator GoldPopup(Vector3 position, int amount, bool first)
    {
        TextMeshPro text = CreateWorldText("GoldPopup", first ? 5.2f : 4.0f, ChipOrder + 20);
        text.text = first ? $"첫 수익! +{amount}G" : $"+{amount}G";
        text.color = new Color32(0xF6, 0xC3, 0x3B, 0xFF);
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0.28f;
        text.outlineColor = new Color32(0x4A, 0x2C, 0x12, 0xFF);

        float duration = first ? 2.2f : 1.3f;
        float rise = first ? 1.1f : 0.8f;
        float t = 0f;
        while (t < duration && text != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            text.transform.position = position + Vector3.up * (rise * Mathf.Sqrt(k));
            Color c = text.color;
            c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            text.color = c;
            yield return null;
        }

        if (text != null) Destroy(text.gameObject);
    }

    // ───── 조립 도구 ─────

    private Chip CreateChip(string name)
    {
        Chip chip = new Chip();
        chip.root = new GameObject(name + "Chip").transform;
        chip.root.SetParent(_root, false);

        chip.background = new GameObject("Bg").AddComponent<SpriteRenderer>();
        chip.background.transform.SetParent(chip.root, false);
        chip.background.sprite = _chipSprite;
        chip.background.drawMode = SpriteDrawMode.Sliced;
        chip.background.color = new Color(DayLoopUI.Cream.r, DayLoopUI.Cream.g, DayLoopUI.Cream.b, 0.94f);
        chip.background.sortingLayerID = _layerId;
        chip.background.sortingOrder = ChipOrder;

        chip.text = CreateWorldText("Label", ChipFontSize, ChipOrder + 1);
        chip.text.transform.SetParent(chip.root, false);

        chip.gaugeBack = new GameObject("GaugeBack").AddComponent<SpriteRenderer>();
        chip.gaugeBack.transform.SetParent(chip.root, false);
        chip.gaugeBack.sprite = _chipSprite;
        chip.gaugeBack.drawMode = SpriteDrawMode.Sliced;
        chip.gaugeBack.color = new Color(0.2f, 0.15f, 0.1f, 0.75f);
        chip.gaugeBack.sortingLayerID = _layerId;
        chip.gaugeBack.sortingOrder = ChipOrder;

        chip.gaugeFill = new GameObject("GaugeFill").AddComponent<SpriteRenderer>();
        chip.gaugeFill.transform.SetParent(chip.root, false);
        chip.gaugeFill.sprite = _chipSprite;
        chip.gaugeFill.drawMode = SpriteDrawMode.Sliced;
        chip.gaugeFill.sortingLayerID = _layerId;
        chip.gaugeFill.sortingOrder = ChipOrder + 1;
        return chip;
    }

    private TextMeshPro CreateWorldText(string name, float fontSize, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(_root, false);
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.font = DayLoopUI.Font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(8f, 1f);
        text.color = DayLoopUI.Ink;
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerID = _layerId;
        meshRenderer.sortingOrder = order;
        return text;
    }

    private SpriteRenderer CreateIcon(string name)
    {
        SpriteRenderer icon = new GameObject(name).AddComponent<SpriteRenderer>();
        icon.transform.SetParent(_root, false);
        icon.sortingLayerID = _layerId;
        icon.sortingOrder = ChipOrder - 10;
        icon.gameObject.SetActive(false);
        return icon;
    }

    private static void SetIcon(SpriteRenderer icon, Sprite sprite)
    {
        if (icon.sprite == sprite) return;
        icon.sprite = sprite;
        Vector3 size = sprite.bounds.size;
        float largest = Mathf.Max(size.x, size.y);
        float scale = largest > 0.0001f ? IconSize / largest : 1f;
        icon.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void SetChip(Chip chip, string label, Color color)
    {
        chip.text.color = color;
        if (chip.shown == label) return;
        chip.shown = label;
        chip.text.text = label;
        float width = chip.text.GetPreferredValues(label).x + 0.36f;
        chip.background.size = new Vector2(Mathf.Max(0.6f, width), ChipHeight);
    }

    private static void SetGauge(Chip chip, bool visible, float ratio)
    {
        chip.gaugeBack.gameObject.SetActive(visible);
        chip.gaugeFill.gameObject.SetActive(visible);
        if (!visible) return;

        const float width = 0.9f;
        const float height = 0.11f;
        float y = -ChipHeight * 0.5f - height;
        chip.gaugeBack.size = new Vector2(width, height);
        chip.gaugeBack.transform.localPosition = new Vector3(0f, y, 0f);

        float fillWidth = Mathf.Max(height, width * Mathf.Clamp01(ratio));
        chip.gaugeFill.size = new Vector2(fillWidth, height);
        chip.gaugeFill.transform.localPosition = new Vector3(-width * 0.5f + fillWidth * 0.5f, y, 0f);
        chip.gaugeFill.color = ratio > 0.6f ? DayLoopUI.Good : ratio > 0.3f ? new Color32(0xE0, 0xA1, 0x2A, 0xFF) : DayLoopUI.Warn;
    }

    private static Bounds GetBounds(Transform target, SpriteRenderer body)
    {
        if (body != null && body.sprite != null)
            return body.bounds;
        return new Bounds(target.position, new Vector3(0.8f, 1.2f, 0f));
    }
}
