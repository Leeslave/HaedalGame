using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 영업 HUD 오른쪽 아래 귀퉁이의 주문 서랍. 화면 오른쪽 끝 탭을 눌러 접고, 왼쪽으로 펼친다.
///  - 자리: 펼쳐도 밧줄 아래 손님 길(화면 아래에서 242 위)과 웨이팅석 오른쪽 끝(x 1470)에 닿지 않는 크기로 맞췄다.
///  - 주문: 홀 직원이 받아 간 주문 중 아직 아무도 만들기 시작하지 않은 것. 아래 막대가 그 손님의 남은 인내심.
///  - 조리 중: 주방 직원이 맡은 요리(아래 막대가 조리 진행도)와, 다 만들어 손님에게 가는 중인 요리(체크 표시).
///  - 두 줄 모두 칸이 넘치면 가로로 스크롤된다. (마우스 휠·끌기)
///  - 접으면 탭에 주문·조리 수만 남고, 인내심이 얼마 안 남은 주문이 있으면 탭이 코랄색으로 바뀐다.
/// 표시만 담당하며 손님·직원의 행동에는 관여하지 않는다. 순서는 주문을 받은 순서.
/// </summary>
public class OrderDrawer : MonoBehaviour
{
    private const float PanelWidth = 400f;
    private const float RowHeight = 92f;
    private const float RowGap = 10f;
    private const float TabWidth = 66f;
    private const float TabHeight = 170f;
    private const float TabTuck = 24f;                       // 탭 오른쪽이 패널 뒤로 숨는 폭
    private const float BottomMargin = 24f;                  // 화면 아래 끝과의 간격
    private const float SlotWidth = 80f;
    private const float SlotHeight = 72f;
    private const float IconSize = 44f;
    private const float SlideSpeed = 12f;

    private static readonly Color Sky = new Color32(0x7C, 0xC8, 0xEE, 0xFF);
    private static readonly Color Mint = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);

    // 펼침 여부는 하루가 바뀌어도 유지한다.
    private static bool s_open = true;

    private class Entry
    {
        public CustomerAgent customer;
        public RecipeData recipe;
        public float value;     // 주문: 남은 인내심, 조리: 진행도
        public bool done;       // 다 만들어 손님에게 가는 중
    }

    private class SlotView
    {
        public Image card;
        public Image icon;
        public RecipeData recipe;
        public Image bar;
        public GameObject check;
    }

    private class Row
    {
        public RectTransform content;
        public ScrollRect scroll;
        public TMP_Text count;
        public TMP_Text empty;
        public readonly List<SlotView> slots = new List<SlotView>();
    }

    private readonly List<CustomerAgent> _customers = new List<CustomerAgent>();
    private readonly Dictionary<CustomerAgent, float> _orderedAt = new Dictionary<CustomerAgent, float>();
    private readonly HashSet<CustomerAgent> _ready = new HashSet<CustomerAgent>();
    private readonly Dictionary<CustomerAgent, float> _cooking = new Dictionary<CustomerAgent, float>();
    private readonly List<Entry> _orderEntries = new List<Entry>();
    private readonly List<Entry> _cookEntries = new List<Entry>();
    private readonly List<CustomerAgent> _stale = new List<CustomerAgent>();
    private ChefAgent[] _chefs = new ChefAgent[0];
    private float _nextChefScan;

    private RectTransform _root;
    private Row _orders, _cookingRow;
    private Button _tab;
    private Image _chevron;
    private TMP_Text _tabText;

    public static OrderDrawer Create(Transform parent)
    {
        RectTransform root = DayLoopUI.Rect("OrderDrawer", parent);
        OrderDrawer drawer = root.gameObject.AddComponent<OrderDrawer>();
        drawer.Build(root);
        return drawer;
    }

    private void Awake()
    {
        CustomerAgent.OnAnySpawned += HandleSpawned;
        KitchenSystem.OnFoodCompleted += HandleFoodCompleted;
    }

    private void OnDestroy()
    {
        CustomerAgent.OnAnySpawned -= HandleSpawned;
        KitchenSystem.OnFoodCompleted -= HandleFoodCompleted;
    }

    private void HandleSpawned(CustomerAgent customer)
    {
        if (customer != null && !_customers.Contains(customer)) _customers.Add(customer);
    }

    private void HandleFoodCompleted(CustomerAgent customer)
    {
        if (customer != null) _ready.Add(customer);
    }

    private void Update()
    {
        // 접고 펼치기: 접으면 패널 폭만큼 오른쪽 화면 밖으로 밀어 탭만 남긴다.
        float target = s_open ? 0f : PanelWidth;
        Vector2 pos = _root.anchoredPosition;
        if (Mathf.Abs(pos.x - target) < 0.5f) pos.x = target;
        else pos.x = Mathf.Lerp(pos.x, target, 1f - Mathf.Exp(-SlideSpeed * Time.unscaledDeltaTime));
        _root.anchoredPosition = pos;
    }

    private void Toggle()
    {
        s_open = !s_open;
        Refresh();
    }

    // ───── 내용 갱신 (RestaurantHud가 0.2초마다 부른다) ─────

    public void Refresh()
    {
        if (Time.unscaledTime >= _nextChefScan)
        {
            _nextChefScan = Time.unscaledTime + 1f;
            _chefs = FindObjectsByType<ChefAgent>(FindObjectsSortMode.None);
        }

        _cooking.Clear();
        foreach (ChefAgent chef in _chefs)
        {
            if (chef == null || !chef.isActiveAndEnabled) continue;
            CustomerAgent customer = chef.CookingFor;
            if (customer != null) _cooking[customer] = chef.CookProgress;
        }

        _stale.Clear();
        foreach (CustomerAgent customer in _customers)
            if (customer == null || customer.State == CustomerState.Exit) _stale.Add(customer);
        foreach (CustomerAgent customer in _stale)
        {
            _customers.Remove(customer);
            _orderedAt.Remove(customer);
            _ready.Remove(customer);
        }
        _ready.RemoveWhere(c => c == null || c.State != CustomerState.WaitingForFood);

        _orderEntries.Clear();
        _cookEntries.Clear();
        bool urgent = false;
        foreach (CustomerAgent customer in _customers)
        {
            RecipeData recipe = customer.coc != null ? customer.coc.GetOrderData() : null;
            if (customer.State != CustomerState.WaitingForFood || recipe == null) continue;
            if (!_orderedAt.ContainsKey(customer)) _orderedAt[customer] = Time.time;

            var entry = new Entry { customer = customer, recipe = recipe };
            if (_ready.Contains(customer)) { entry.value = 1f; entry.done = true; _cookEntries.Add(entry); }
            else if (_cooking.TryGetValue(customer, out float progress)) { entry.value = progress; _cookEntries.Add(entry); }
            else
            {
                CustomerPatienceComponent patience = customer.cpc;
                entry.value = patience != null && patience.IsDraining ? patience.Ratio : 1f;
                urgent |= entry.value <= .3f;
                _orderEntries.Add(entry);
            }
        }
        _orderEntries.Sort((a, b) => _orderedAt[a.customer].CompareTo(_orderedAt[b.customer]));
        _cookEntries.Sort((a, b) => _orderedAt[a.customer].CompareTo(_orderedAt[b.customer]));

        Fill(_orders, _orderEntries, false);
        Fill(_cookingRow, _cookEntries, true);

        _chevron.rectTransform.localEulerAngles = new Vector3(0f, 0f, s_open ? 0f : 180f);   // 펼치면 → (접기), 접으면 ← (펼치기)
        _tabText.gameObject.SetActive(!s_open);
        _tabText.text = $"주문\n{_orderEntries.Count}\n\n조리\n{_cookEntries.Count}";
        DayLoopUI.SetButtonColors(_tab, !s_open && urgent ? Peach : DayLoopUI.Kit.WoodLight, Color.white);
    }

    private void Fill(Row row, List<Entry> entries, bool cooking)
    {
        while (row.slots.Count < entries.Count) row.slots.Add(CreateSlot(row.content));
        for (int i = 0; i < row.slots.Count; i++)
        {
            SlotView slot = row.slots[i];
            bool used = i < entries.Count;
            if (slot.card.gameObject.activeSelf != used) slot.card.gameObject.SetActive(used);
            if (!used) continue;

            Entry entry = entries[i];
            if (slot.recipe != entry.recipe)
            {
                slot.recipe = entry.recipe;
                if (slot.icon != null) Destroy(slot.icon.gameObject);
                slot.icon = DayLoopUI.FoodIcon(slot.card.transform, entry.recipe, IconSize);
                DayLoopUI.Place(slot.icon.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -4), new Vector2(IconSize, IconSize));
                slot.icon.transform.SetSiblingIndex(0);
            }
            DayLoopUI.SetBar(slot.bar, entry.value);
            slot.bar.color = cooking ? (entry.done ? Mint : Sky) : entry.value > .6f ? Mint : entry.value > .3f ? Butter : Peach;
            slot.check.SetActive(entry.done);
        }

        row.count.text = entries.Count.ToString();
        row.empty.gameObject.SetActive(entries.Count == 0);
    }

    // ───── 조립 ─────

    private void Build(RectTransform root)
    {
        _root = root;
        float height = RowHeight * 2 + RowGap * 3;
        DayLoopUI.Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(s_open ? 0f : PanelWidth, BottomMargin), new Vector2(PanelWidth - TabTuck + TabWidth, Mathf.Max(height, TabHeight)));

        // 패널 오른쪽 둥근 모서리는 화면 밖으로 내보내 화면 끝에 붙은 서랍처럼 보이게 한다.
        Image panel = DayLoopUI.Card(root, DayLoopUI.Kit.Sail, "Panel", 3f, 5f);
        panel.GetComponent<RoundedCorner>().radius = 28f;
        DayLoopUI.Place(panel.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(28f, 0f), new Vector2(PanelWidth + 28f, height));
        VerticalLayoutGroup rows = DayLoopUI.Vertical(panel, (int)RowGap, RowGap, TextAnchor.MiddleLeft);
        rows.padding.left = 14;
        rows.padding.right = 28 + 14;
        _orders = BuildRow(panel.transform, "주문", "아직 받은 주문이 없어요", Sky);
        _cookingRow = BuildRow(panel.transform, "조리 중", "조리 중인 요리가 없어요", Mint);

        _tab = DayLoopUI.KitButton(root, "", DayLoopUI.Kit.WoodLight, Color.white, 16, Toggle, "Tab");
        _tab.image.GetComponent<RoundedCorner>().radius = 22f;
        // 탭은 패널 뒤에 깔고 오른쪽을 패널 밑으로 넣어, 패널 왼쪽 끝에서 손잡이가 튀어나온 모양으로 만든다.
        _tab.transform.SetSiblingIndex(0);
        DayLoopUI.Place(_tab.image.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(TabWidth, TabHeight));
        _chevron = DayLoopUI.Panel(_tab.transform, Color.white, "Chevron", false);
        _chevron.sprite = DesignArt.Load("IconChevronRight");
        _chevron.preserveAspect = true;
        _chevron.raycastTarget = false;
        DayLoopUI.Place(_chevron.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(-TabTuck * .5f, -26f), new Vector2(26f, 26f));
        _tabText = Jua(_tab.GetComponentInChildren<TMP_Text>());
        _tabText.lineSpacing = -18f;
        _tabText.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Stretch(_tabText.rectTransform, 0f, 46f, TabTuck, 10f);
    }

    // 왼쪽에 제목과 개수, 오른쪽에 가로로 스크롤되는 칸 줄
    private Row BuildRow(Transform parent, string title, string emptyText, Color badgeColor)
    {
        Row row = new Row();
        RectTransform line = DayLoopUI.Rect(title, parent);
        DayLoopUI.Horizontal(line, 0, 12);
        DayLoopUI.Layout(line, preferredHeight: RowHeight);

        RectTransform head = DayLoopUI.Rect("Head", line);
        DayLoopUI.Vertical(head, 0, 6, TextAnchor.MiddleCenter);
        DayLoopUI.Layout(head, preferredWidth: 64, preferredHeight: RowHeight, flexibleWidth: 0);   // 0이어야 남는 폭이 전부 칸 줄로 간다
        TMP_Text label = Jua(DayLoopUI.Text(head, title, 20, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center));
        label.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(label, preferredHeight: 28);
        Image badge = DayLoopUI.Chunky(DayLoopUI.Panel(head, badgeColor, "Count"), 2f, 0f);
        badge.raycastTarget = false;
        badge.GetComponent<RoundedCorner>().radius = 999f;
        DayLoopUI.Layout(badge, preferredWidth: 44, preferredHeight: 26);
        row.count = Jua(DayLoopUI.Text(badge.transform, "0", 18, Color.white, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(row.count.rectTransform);

        Image lane = DayLoopUI.Panel(line, DayLoopUI.Kit.Sand, "Lane");
        lane.GetComponent<RoundedCorner>().radius = 18f;
        DayLoopUI.Layout(lane, preferredHeight: RowHeight, flexibleWidth: 1);

        // 스크롤 영역: 아래 10px는 스크롤 막대 자리
        Image viewport = DayLoopUI.Panel(lane.transform, Color.clear, "Viewport", false);
        DayLoopUI.Stretch(viewport.rectTransform, 6f, 0f, 6f, 14f);
        viewport.gameObject.AddComponent<RectMask2D>();

        row.content = DayLoopUI.Rect("Content", viewport.transform);
        row.content.anchorMin = new Vector2(0f, 0f);
        row.content.anchorMax = new Vector2(0f, 1f);
        row.content.pivot = new Vector2(0f, .5f);
        row.content.anchoredPosition = Vector2.zero;
        row.content.sizeDelta = Vector2.zero;
        HorizontalLayoutGroup slots = DayLoopUI.Horizontal(row.content, 0, 12);
        slots.padding = new RectOffset(8, 8, 6, 0);
        row.content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        row.scroll = lane.gameObject.AddComponent<ScrollRect>();
        row.scroll.viewport = viewport.rectTransform;
        row.scroll.content = row.content;
        row.scroll.horizontal = true;
        row.scroll.vertical = false;
        row.scroll.movementType = ScrollRect.MovementType.Clamped;
        row.scroll.scrollSensitivity = 40f;
        row.scroll.horizontalScrollbar = BuildScrollbar(lane.transform);
        row.scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        row.empty = DayLoopUI.Text(lane.transform, emptyText, 18, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center);
        DayLoopUI.Stretch(row.empty.rectTransform);
        return row;
    }

    private static Scrollbar BuildScrollbar(Transform lane)
    {
        Image track = DayLoopUI.Panel(lane, DayLoopUI.Kit.Divider, "Scrollbar");
        track.GetComponent<RoundedCorner>().radius = 999f;
        RectTransform rt = track.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(.5f, 0f);
        rt.offsetMin = new Vector2(14f, 5f);
        rt.offsetMax = new Vector2(-14f, 13f);

        RectTransform area = DayLoopUI.Rect("SlidingArea", track.transform);
        DayLoopUI.Stretch(area);
        Image handle = DayLoopUI.Panel(area, DayLoopUI.Kit.WoodLight, "Handle");
        handle.GetComponent<RoundedCorner>().radius = 999f;
        DayLoopUI.Stretch(handle.rectTransform);

        Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = handle.rectTransform;
        bar.targetGraphic = handle;
        bar.direction = Scrollbar.Direction.LeftToRight;
        return bar;
    }

    private static SlotView CreateSlot(Transform content)
    {
        SlotView slot = new SlotView();
        slot.card = DayLoopUI.Chunky(DayLoopUI.Panel(content, Color.white, "Order"), 2f, 0f);
        slot.card.raycastTarget = false;
        slot.card.GetComponent<RoundedCorner>().radius = 16f;
        DayLoopUI.Layout(slot.card, preferredWidth: SlotWidth, preferredHeight: SlotHeight);

        slot.bar = DayLoopUI.Bar(slot.card.transform, 1f, Mint);
        DayLoopUI.Place((RectTransform)slot.bar.transform.parent, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 7f), new Vector2(SlotWidth - 18f, 9f));

        Image check = DayLoopUI.Chunky(DayLoopUI.Panel(slot.card.transform, Mint, "Done", false), 2f, 0f);
        check.sprite = DayLoopUI.Circle;
        check.raycastTarget = false;
        DayLoopUI.Place(check.rectTransform, new Vector2(1f, 1f), new Vector2(.5f, .5f), new Vector2(-6f, -6f), new Vector2(24f, 24f));
        Image mark = DayLoopUI.Panel(check.transform, Color.white, "Check", false);
        mark.sprite = DesignArt.Load("IconCheck");
        mark.preserveAspect = true;
        mark.raycastTarget = false;
        DayLoopUI.Place(mark.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(16f, 16f));
        slot.check = check.gameObject;
        slot.check.SetActive(false);
        return slot;
    }

    private static T Jua<T>(T text) where T : TMP_Text
    {
        text.font = DesignFonts.Heading;
        text.fontSharedMaterial = text.font.material;
        return text;
    }
}
