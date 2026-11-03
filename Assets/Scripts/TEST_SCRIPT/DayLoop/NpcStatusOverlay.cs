using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 월드 상태 표시. (NPC-01, NPC-02, ECON-01, ART-10/11 임시 표현) 캐주얼 시안: 글자 대신 그림과 링.
///  - 주문: 머리 위 동그란 말풍선에 주문한 음식 그림, 둘레 링이 남은 인내심(초록 → 노랑 → 코랄).
///          손님 프리팹의 기존 말풍선 캔버스는 겹치므로 끈다.
///  - 웨이팅석: 머리에 대기 번호 스티커, 아래에 인내심 게이지
///  - 인내심이 5초 이하로 남았을 때와 퇴장 반응(평점)만 글자로 띄운다.
///  - 식사 중: 주문한 메뉴 아이콘을 좌석 앞 식탁 위치에 놓고 머리 위에 하트
///  - 홀 직원: 음식 운반 중에는 머리 위에 같은 메뉴 아이콘. 서빙 순간 직원 쪽 아이콘이 사라지고 식탁에 나타난다.
///    주방 직원: 조리 중인 메뉴 아이콘. 잠든 직원은 Zz. (역할·작업 글자 칩은 띄우지 않는다)
///    메뉴 아이콘이 아직 없는 음식은 아이콘 대신 음식 이름표를 같은 자리에 띄운다.
///  - 계산: 실제 지급 이벤트 금액으로 동전 알약 팝업 (오늘 첫 수익은 크게)
/// 표시만 담당하며 손님·직원의 행동이나 지급에는 관여하지 않는다.
/// </summary>
public class NpcStatusOverlay : MonoBehaviour
{
    private const float ChipHeight = 0.5f;
    private const float ChipFontSize = 3.0f;
    private const float IconSize = 0.6f;
    private const int ChipOrder = 4000;

    private static readonly Color Mint = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    private static readonly Color RingTrack = new Color32(0xF3, 0xE6, 0xD3, 0xFF);
    private static readonly Color SleepInk = new Color32(0x5A, 0x9F, 0xD8, 0xFF);
    private static readonly Color HeartPink = new Color32(0xFF, 0x8F, 0xA3, 0xFF);
    private static readonly Color CoinInk = new Color32(0xB0, 0x7A, 0x00, 0xFF);

    private class Chip
    {
        public Transform root;
        public SpriteRenderer background;
        public TextMeshPro text;
        public SpriteRenderer gaugeBack;
        public SpriteRenderer gaugeFill;
        public string shown;
    }

    // 주문 말풍선: 테두리 · 링 바탕 · 인내심 링 · 흰 속 · 음식 그림
    private class Bubble
    {
        public Transform root;
        public LineRenderer ring;
        public SpriteRenderer icon;
        public TextMeshPro initial;
        public RecipeData shown;
    }

    private class Sticker
    {
        public Transform root;
        public TextMeshPro text;
    }

    private class CustomerView
    {
        public SpriteRenderer body;
        public Chip chip;
        public Bubble bubble;
        public Sticker sticker;
        public TextMeshPro heart;
        public SpriteRenderer food;
        public Chip foodTag;
    }

    private class StaffView
    {
        public SpriteRenderer body;
        public Chip chip;
        public SpriteRenderer food;
        public Chip foodTag;
    }

    private DayCycleController _day;
    private readonly Dictionary<CustomerAgent, CustomerView> _customers = new Dictionary<CustomerAgent, CustomerView>();
    private readonly Dictionary<PartTimerAgent, StaffView> _staff = new Dictionary<PartTimerAgent, StaffView>();
    private readonly List<CustomerAgent> _removeCustomers = new List<CustomerAgent>();
    private readonly List<PartTimerAgent> _removeStaff = new List<PartTimerAgent>();
    private readonly Dictionary<float, Sprite> _pills = new Dictionary<float, Sprite>();
    private Transform _root;
    private Sprite _chipSprite;
    private Material _lineMaterial;
    private string _heartGlyph;
    private int _layerId;
    private float _nextStaffScan;
    private bool _visible;

    public void Init(DayCycleController day)
    {
        _day = day;
        _root = new GameObject("[DayLoop] WorldStatus").transform;
        _root.SetParent(transform, false);
        _layerId = DayLoopUI.TopSortingLayerId;

        // 월드용 둥근 칩(게이지): 같은 텍스처를 작은 유닛 크기로 다시 자른다.
        Sprite ui = DayLoopUI.Rounded;
        // 예전 48px 텍스처를 PPU 160으로 쓰던 크기를 유지한다.
        _chipSprite = Sprite.Create(ui.texture, ui.rect, new Vector2(0.5f, 0.5f), 160f * ui.rect.width / 48f, 0, SpriteMeshType.FullRect, ui.border);
        _lineMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
        _heartGlyph = DayLoopUI.Glyph("♥", "");

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
        if (_lineMaterial != null) Destroy(_lineMaterial);
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

        // 프리팹의 말풍선(음식·감정 아이콘)은 주문 말풍선과 겹치므로 그리지 않는다.
        foreach (Canvas canvas in customer.GetComponentsInChildren<Canvas>(true))
            if (canvas.renderMode == RenderMode.WorldSpace) canvas.enabled = false;

        _customers[customer] = new CustomerView
        {
            body = customer.GetComponentInChildren<SpriteRenderer>(),
            chip = CreateChip("Customer"),
            bubble = CreateBubble(),
            sticker = CreateSticker(),
            heart = CreateHeart(),
            food = CreateIcon("TableFood"),
            foodTag = CreateChip("TableFoodTag"),
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
            foodTag = CreateChip("CarriedFoodTag"),
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
            Destroy(view.bubble.root.gameObject);
            Destroy(view.sticker.root.gameObject);
            Destroy(view.heart.gameObject);
            Destroy(view.food.gameObject);
            Destroy(view.foodTag.root.gameObject);
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
            Destroy(view.foodTag.root.gameObject);
            _staff.Remove(dead);
        }
    }

    // ───── 손님 ─────

    private void UpdateCustomer(CustomerAgent customer, CustomerView view)
    {
        Bounds bounds = GetBounds(customer.transform, view.body);
        CustomerState state = customer.State;
        RecipeData order = customer.coc != null ? customer.coc.GetOrderData() : null;
        CustomerPatienceComponent patience = customer.cpc;
        bool draining = patience != null && patience.IsDraining;

        // 주문 말풍선: 주문이 정해진 뒤 음식이 올 때까지
        bool ordering = order != null && (state == CustomerState.WaitingForOrder || state == CustomerState.WaitingForFood);
        ShowBubble(view.bubble, ordering ? order : null, draining ? patience.Ratio : 1f,
            new Vector3(bounds.center.x, bounds.max.y + 0.38f, 0f));

        // 진행 중 상태는 글자를 띄우지 않는다. (말풍선 링·게이지와 캐릭터 동작으로 충분하다)
        Color textColor = DayLoopUI.Ink;
        string label = ShowsCustomerLabel(state) ? CustomerLabel(customer, state, out textColor) : "";
        SetChip(view.chip, label, textColor);
        view.chip.root.position = new Vector3(bounds.center.x, bounds.min.y - ChipHeight * 0.6f, 0f);

        // 말풍선이 없는 기다림(웨이팅석, 메뉴 고르는 중)은 아래 게이지로 보여 준다.
        bool waitingState = state == CustomerState.WaitingRoom || state == CustomerState.WaitingForOrder || state == CustomerState.WaitingForFood;
        SetGauge(view.chip, waitingState && draining && !ordering, patience != null ? patience.Ratio : 1f);
        if (waitingState && draining && patience.RemainingSeconds <= 5)
            SetChip(view.chip, Mathf.CeilToInt(patience.RemainingSeconds) + "초!", Mathf.PingPong(Time.time * 4, 1) > .5f ? DayLoopUI.Warn : Color.white);

        // 웨이팅석 번호 스티커
        SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
        int line = state == CustomerState.WaitingRoom && seats != null ? seats.WaitingIndexOf(customer) : -1;
        view.sticker.root.gameObject.SetActive(line >= 0);
        if (line >= 0)
        {
            string number = (line + 1).ToString();
            if (view.sticker.text.text != number) view.sticker.text.text = number;
            view.sticker.root.position = new Vector3(bounds.min.x + 0.12f, bounds.max.y - 0.08f, 0f);
        }

        // 식탁 위 음식: 식사 중에만, 앉은 좌석 앞(손님이 바라보는 방향)에 놓는다.
        Seat seat = customer.GetCurrentSeat();
        bool eating = state == CustomerState.Eating && order != null && seat != null;
        Vector3 tablePos = Vector3.zero;
        if (eating)
        {
            Vector2 facing = seat.GetFacingDirection();
            Vector3 basePos = seat.GetSeatPoint() != null ? seat.GetSeatPoint().position : customer.transform.position;
            tablePos = basePos + (Vector3)(facing.normalized * 0.6f);
        }
        ShowFood(view.food, view.foodTag, eating ? order : null, tablePos);

        // 먹는 동안 머리 위에 하트가 콩콩
        bool heart = state == CustomerState.Eating && _heartGlyph.Length > 0;
        view.heart.gameObject.SetActive(heart);
        if (heart)
        {
            float bob = Mathf.Abs(Mathf.Sin((Time.time + customer.GetInstanceID() * 0.37f) * 2.4f)) * 0.12f;
            view.heart.transform.position = new Vector3(bounds.max.x - 0.12f, bounds.max.y + 0.08f + bob, 0f);
        }
    }

    private static bool ShowsCustomerLabel(CustomerState state) => state == CustomerState.Paying || state == CustomerState.Exit;

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

    private void ShowBubble(Bubble bubble, RecipeData food, float ratio, Vector3 position)
    {
        bubble.root.gameObject.SetActive(food != null);
        if (food == null) return;
        bubble.root.position = position;

        if (bubble.shown != food)
        {
            bubble.shown = food;
            Sprite icon = DesignArt.Food(food);
            bubble.icon.gameObject.SetActive(icon != null);
            if (icon != null) SetIcon(bubble.icon, icon, 0.34f);
            bubble.initial.gameObject.SetActive(icon == null);
            bubble.initial.text = icon == null && !string.IsNullOrEmpty(food.RecipeName) ? food.RecipeName.Substring(0, 1) : "";
        }

        // 링: 12시 방향에서 시계 방향으로 남은 비율만큼
        ratio = Mathf.Clamp01(ratio);
        const float radius = 0.255f;
        int count = Mathf.Max(2, Mathf.CeilToInt(40 * ratio) + 1);
        bubble.ring.enabled = ratio > 0.01f;
        bubble.ring.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 0.5f - Mathf.PI * 2f * ratio * i / (count - 1);
            bubble.ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
        Color color = ratio > 0.6f ? Mint : ratio > 0.3f ? Butter : Peach;
        bubble.ring.startColor = bubble.ring.endColor = color;
    }

    // ───── 직원 ─────

    private void UpdateStaff(PartTimerAgent agent, StaffView view)
    {
        Bounds bounds = GetBounds(agent.transform, view.body);
        RecipeData food = null;
        if (agent is ServerAgent server) food = server.CarriedFood;
        else if (agent is ChefAgent chef) food = chef.CookingRecipe;

        SetChip(view.chip, agent.IsSleeping ? "Zz" : "", SleepInk);
        view.chip.root.position = new Vector3(bounds.center.x, bounds.max.y + ChipHeight * 0.6f, 0f);
        SetGauge(view.chip, false, 1f);

        ShowFood(view.food, view.foodTag, agent.IsSleeping ? null : food, new Vector3(bounds.center.x, bounds.max.y + IconSize * 0.45f, 0f));
    }

    // 음식 아이콘을 보여 준다. 아이콘이 없는 메뉴는 같은 자리에 음식 이름표를 띄운다. food가 null이면 모두 숨긴다.
    private static void ShowFood(SpriteRenderer icon, Chip tag, RecipeData food, Vector3 position)
    {
        Sprite designIcon = DesignArt.Food(food);
        bool hasIcon = designIcon != null;
        icon.gameObject.SetActive(hasIcon);
        if (hasIcon)
        {
            SetIcon(icon, food.Icon);
            icon.transform.position = position;
        }

        bool showTag = food != null && !hasIcon;
        SetChip(tag, showTag ? food.RecipeName : "", DayLoopUI.WoodDark);
        SetGauge(tag, false, 1f);
        if (showTag) tag.root.position = position;
    }

    // ───── 결제 팝업 ─────

    private void HandlePayment(CustomerAgent customer, int amount, bool first)
    {
        if (customer == null) return;
        CustomerView view;
        _customers.TryGetValue(customer, out view);
        Bounds bounds = GetBounds(customer.transform, view != null ? view.body : null);
        StartCoroutine(GoldPopup(new Vector3(bounds.center.x, bounds.max.y + 0.3f, 0f), amount, first));
    }

    // 크림색 알약 안에 동전과 +금액
    private IEnumerator GoldPopup(Vector3 position, int amount, bool first)
    {
        Transform popup = new GameObject("GoldPopup").transform;
        popup.SetParent(_root, false);
        float height = first ? 0.62f : 0.5f;
        float coin = height * 0.72f;

        TextMeshPro text = CreateWorldText("Amount", first ? 4.0f : 3.3f, ChipOrder + 24);
        text.transform.SetParent(popup, false);
        text.font = DesignFonts.Heading;
        text.fontSharedMaterial = text.font.material;
        text.text = first ? $"첫 수익! +{amount}" : $"+{amount}";
        text.color = CoinInk;
        float textWidth = text.GetPreferredValues(text.text).x;
        float width = 0.12f + coin + 0.08f + textWidth + 0.22f;

        PillRenderer(popup, "Line", width + 0.08f, height + 0.08f, DayLoopUI.Kit.Line, ChipOrder + 20);
        PillRenderer(popup, "Back", width, height, DayLoopUI.Kit.Sail, ChipOrder + 21);
        float coinX = -width * 0.5f + 0.12f + coin * 0.5f;
        Disc(popup, "CoinLine", DayLoopUI.Kit.Line, coin + 0.07f, ChipOrder + 22).transform.localPosition = new Vector3(coinX, 0f, 0f);
        Disc(popup, "Coin", Butter, coin, ChipOrder + 23).transform.localPosition = new Vector3(coinX, 0f, 0f);
        text.transform.localPosition = new Vector3(coinX + coin * 0.5f + 0.08f + textWidth * 0.5f, 0f, 0f);

        SpriteRenderer[] sprites = popup.GetComponentsInChildren<SpriteRenderer>();
        float duration = first ? 2.2f : 1.3f;
        float rise = first ? 1.1f : 0.8f;
        float t = 0f;
        while (t < duration && popup != null)
        {
            t += Time.deltaTime;
            float k = t / duration;
            popup.position = position + Vector3.up * (rise * Mathf.Sqrt(k));
            float alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            foreach (SpriteRenderer sr in sprites) { Color c = sr.color; c.a = alpha; sr.color = c; }
            Color tc = text.color; tc.a = alpha; text.color = tc;
            yield return null;
        }

        if (popup != null) Destroy(popup.gameObject);
    }

    // ───── 조립 도구 ─────

    private Chip CreateChip(string name)
    {
        Chip chip = new Chip();
        chip.root = new GameObject(name + "Chip").transform;
        chip.root.SetParent(_root, false);

        chip.background = new GameObject("Bg").AddComponent<SpriteRenderer>();
        chip.background.transform.SetParent(chip.root, false);
        chip.background.sprite = Pill(ChipHeight);
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

    private Bubble CreateBubble()
    {
        Bubble bubble = new Bubble { root = new GameObject("OrderBubble").transform };
        bubble.root.SetParent(_root, false);
        Disc(bubble.root, "Line", DayLoopUI.Kit.Line, 0.68f, ChipOrder + 2);
        Disc(bubble.root, "Track", RingTrack, 0.62f, ChipOrder + 3);

        bubble.ring = new GameObject("Patience").AddComponent<LineRenderer>();
        bubble.ring.transform.SetParent(bubble.root, false);
        bubble.ring.sharedMaterial = _lineMaterial;
        bubble.ring.useWorldSpace = false;
        bubble.ring.startWidth = bubble.ring.endWidth = 0.09f;
        bubble.ring.numCapVertices = 3;
        bubble.ring.sortingLayerID = _layerId;
        bubble.ring.sortingOrder = ChipOrder + 4;

        Disc(bubble.root, "Inner", Color.white, 0.44f, ChipOrder + 5);
        bubble.icon = new GameObject("Food").AddComponent<SpriteRenderer>();
        bubble.icon.transform.SetParent(bubble.root, false);
        bubble.icon.sortingLayerID = _layerId;
        bubble.icon.sortingOrder = ChipOrder + 6;
        bubble.initial = CreateWorldText("Initial", 2.4f, ChipOrder + 6);
        bubble.initial.transform.SetParent(bubble.root, false);
        bubble.root.gameObject.SetActive(false);
        return bubble;
    }

    private Sticker CreateSticker()
    {
        Sticker sticker = new Sticker { root = new GameObject("WaitingNumber").transform };
        sticker.root.SetParent(_root, false);
        Disc(sticker.root, "Line", DayLoopUI.Kit.Line, 0.4f, ChipOrder + 2);
        Disc(sticker.root, "Back", Butter, 0.34f, ChipOrder + 3);
        sticker.text = CreateWorldText("Number", 2.2f, ChipOrder + 4);
        sticker.text.transform.SetParent(sticker.root, false);
        sticker.text.font = DesignFonts.Heading;
        sticker.text.fontSharedMaterial = sticker.text.font.material;
        sticker.root.gameObject.SetActive(false);
        return sticker;
    }

    private TextMeshPro CreateHeart()
    {
        TextMeshPro heart = CreateWorldText("Heart", 3.2f, ChipOrder + 2);
        heart.text = _heartGlyph;
        heart.color = HeartPink;
        heart.outlineWidth = 0.25f;
        heart.outlineColor = DayLoopUI.Kit.Line;
        heart.gameObject.SetActive(false);
        return heart;
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

    /// <summary>지름 diameter의 원. (Circle 스프라이트는 1유닛)</summary>
    private SpriteRenderer Disc(Transform parent, string name, Color color, float diameter, int order)
    {
        SpriteRenderer disc = new GameObject(name).AddComponent<SpriteRenderer>();
        disc.transform.SetParent(parent, false);
        disc.sprite = DayLoopUI.Circle;
        disc.color = color;
        disc.transform.localScale = new Vector3(diameter, diameter, 1f);
        disc.sortingLayerID = _layerId;
        disc.sortingOrder = order;
        return disc;
    }

    private SpriteRenderer PillRenderer(Transform parent, string name, float width, float height, Color color, int order)
    {
        SpriteRenderer pill = new GameObject(name).AddComponent<SpriteRenderer>();
        pill.transform.SetParent(parent, false);
        pill.sprite = Pill(height);
        pill.drawMode = SpriteDrawMode.Sliced;
        pill.size = new Vector2(width, height);
        pill.color = color;
        pill.sortingLayerID = _layerId;
        pill.sortingOrder = order;
        return pill;
    }

    /// <summary>높이 height에서 끝이 반원이 되는 9-slice 스프라이트. 모서리 반지름 = 텍스처 모서리 / PPU.</summary>
    private Sprite Pill(float height)
    {
        if (_pills.TryGetValue(height, out Sprite cached)) return cached;
        Sprite ui = DayLoopUI.Rounded;
        Sprite sprite = Sprite.Create(ui.texture, ui.rect, new Vector2(0.5f, 0.5f), ui.border.x / (height * 0.5f), 0, SpriteMeshType.FullRect, ui.border);
        sprite.hideFlags = HideFlags.DontSave;
        _pills[height] = sprite;
        return sprite;
    }

    private static void SetIcon(SpriteRenderer icon, Sprite sprite, float size = IconSize)
    {
        if (icon.sprite == sprite) return;
        icon.sprite = sprite;
        Vector3 bounds = sprite.bounds.size;
        float largest = Mathf.Max(bounds.x, bounds.y);
        float scale = largest > 0.0001f ? size / largest : 1f;
        icon.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void SetChip(Chip chip, string label, Color color)
    {
        chip.text.color = color;
        if (chip.shown == label) return;
        chip.shown = label;
        chip.text.text = label;
        bool visible = !string.IsNullOrEmpty(label);
        chip.background.enabled = visible;
        chip.text.enabled = visible;
        if (!visible) return;
        float width = chip.text.GetPreferredValues(label).x + 0.4f;
        chip.background.size = new Vector2(Mathf.Max(ChipHeight, width), ChipHeight);
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
        chip.gaugeFill.color = ratio > 0.6f ? Mint : ratio > 0.3f ? Butter : Peach;
    }

    private static Bounds GetBounds(Transform target, SpriteRenderer body)
    {
        if (body != null && body.sprite != null)
            return body.bounds;
        return new Bounds(target.position, new Vector3(0.8f, 1.2f, 0f));
    }
}
