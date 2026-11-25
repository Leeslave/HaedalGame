using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 영업 HUD. (HUD-01, HUD-02, CLOSE-01) 캐주얼 시안: 글자를 줄이고 네 귀퉁이에 카드만 둔다.
/// 카드 겉모양은 플레이씬 시안(Figma 386:73)의 나무 테·끼운 종이 조각을 쓴다. (DesignArt.WoodCard, Resources/UIDesign/Art/PlayHud)
///  - 왼쪽 위: 해 아이콘 · n일차 · 단계 칩 · 방문 게이지(마감 중에는 남은 손님)
///  - 왼쪽 위 일차 카드 아래: 직원별 상태 카드(시안 386:263). 초상 · 역할 · 상태 칩(대기 중/조리 중/서빙 중/잠듦) · 체력 막대,
///    잠들면 카드가 분홍빛이 되고 체력 막대 자리에 [깨우기]. 직원이 많으면 카드 줄을 줄여 메뉴판에 닿지 않게 한다.
///  - 오른쪽 위: 보유 골드 · 평점 · 배속(눌러서 x1 → x2 → x3) · 일시정지
///  - 왼쪽 아래: 오늘의 메뉴. 음식 그림에 남은 인분 스티커 (2인분 이하 코랄, 0이면 품절). 5개부터 작게 두 줄.
///    제목 옆 버튼으로 접으면 메뉴 슬롯 앞 2개만 남고, 숨은 메뉴가 2인분 이하면 버튼이 코랄색으로 바뀐다.
///  - 오른쪽 아래: 왼쪽으로 펼치는 주문 서랍 (받은 주문 · 조리 중, OrderDrawer)
/// 가운데는 월드(주방·식탁·밧줄·손님 길·웨이팅석)를 가리지 않게 비운다. 자리 대기 인원은 웨이팅석 아래 팻말(SeaRestaurantLayout)이 보여 준다.
/// 평점: 오늘 받은 평가의 평균(평가 전에는 식당 평점). 평가가 들어올 때마다 강조 이펙트와 점수 팝업.
/// </summary>
public class RestaurantHud : MonoBehaviour
{
    private const float Margin = 24f;                   // 화면 끝과 귀퉁이 카드 사이
    private const int MenuPerRow = 4;                   // 왼쪽 아래 메뉴 카드가 웨이팅석까지 뻗지 않게 한 줄에 이만큼만 둔다
    private const float MenuSize = 84f;                 // 메뉴 MenuPerRow개까지의 음식 크기
    private const float MenuSizeCompact = 64f;          // 메뉴가 두 줄일 때의 음식 크기 (카드가 손님 길에 닿지 않게)
    private const int MenuCollapsedCount = 2;           // 메뉴 카드를 접었을 때 남기는 메뉴 수 (슬롯 앞에서부터)

    // 대기석이 두 줄이면 메뉴 카드를 접어 통로를 더 보여 준다. 직접 펼치거나 접은 선택은 다음 날에도 유지한다.
    private static bool? s_menuExpanded;
    private static bool MenuExpanded => s_menuExpanded ?? (SeaRestaurantLayout.Instance?.HasSecondWaitingRow != true);

    // 캐주얼 시안 색 (Kit 색보다 밝은 파스텔)
    private static readonly Color Mint = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color Leaf = new Color32(0x5B, 0xBF, 0x8C, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    private static readonly Color Muted = new Color32(0xB8, 0xA8, 0x98, 0xFF);

    // 직원 상태 카드 색 (시안 386:263)
    private static readonly Color StaffCard = new Color32(0xF7, 0xFA, 0xF9, 0xFF);
    private static readonly Color StaffCardLine = new Color32(0xC9, 0xDA, 0xDF, 0xFF);
    private static readonly Color SleepCard = new Color32(0xFF, 0xF3, 0xEB, 0xFF);
    private static readonly Color SleepCardInner = new Color32(0xF9, 0xEC, 0xE7, 0xFF);
    private static readonly Color SleepCardLine = new Color32(0xDE, 0xAE, 0x9D, 0xFF);
    private static readonly Color SleepPortrait = new Color(1f, .86f, .80f, 1f);   // 회청색 초상 배경(PortraitFrame)을 분홍(#EFCDC1)에 가깝게
    private static readonly Color StaffName = new Color32(0x17, 0x2F, 0x3B, 0xFF);
    private static readonly Color StaffSoft = new Color32(0x42, 0x5B, 0x66, 0xFF);
    private static readonly Color StatusChip = new Color32(0xE7, 0xEE, 0xF1, 0xFF);
    private static readonly Color SleepChip = new Color32(0xF1, 0xD2, 0xC6, 0xFF);
    private static readonly Color WakeRed = new Color32(0x96, 0x3C, 0x2D, 0xFF);
    private static readonly Color WakeText = new Color32(0xFF, 0xF8, 0xEE, 0xFF);

    private class StaffSlot
    {
        public PartTimerAgent agent;
        public GameObject root;
        public Image card, inner, portrait;
        public Outline line;
        public Image statusChip;
        public TMP_Text status;
        public Image stamina;
        public GameObject staminaRow;
        public GameObject wake;
        public RectTransform wakeTip;   // 잠든 직원 머리 위 [직원 깨우기] 말풍선
        public SpriteRenderer body;
    }

    // 잠든 직원 머리 위 말풍선 (시안 386:251, 대상 점 386:253)
    private static readonly Vector2 WakeTipSize = new Vector2(240f, 64f);
    private static readonly Color WakeTipRed = new Color32(0xB7, 0x50, 0x45, 0xFF);
    private static readonly Color WakeTipLine = new Color32(0x4A, 0x2E, 0x1B, 0xFF);
    private static readonly Color WakeTipDrop = new Color32(0x3A, 0x27, 0x18, 0xBF);

    private class MenuSlot
    {
        public RecipeData recipe;
        public Image bubble;
        public Image icon;
        public Image badge;
        public TMP_Text count;
    }

    private DayCycleController _day;
    private Canvas _canvas;
    private RectTransform _bar;
    private TMP_Text _dayText, _phaseText, _progressText, _goldText, _ratingText;
    private Image _phaseChip, _progressFill;
    private RectTransform _ratingBlock;
    private RestaurantRatingManager _rating;
    private float _lastRatingAverage;
    private int _lastRatingCount;
    private Coroutine _ratingPunch;
    private Button _speedButton, _pauseButton;
    private RectTransform _menuRow, _staffRow, _wakeLayer;
    private GridLayoutGroup _menuGrid;
    private Button _menuToggle;
    private Image _menuChevron;
    private OrderDrawer _orderDrawer;
    private RectTransform _topRight;

    // 다른 UI가 영업 HUD 귀퉁이 카드를 피해 자리를 잡을 때 쓴다. (관리 버튼·첫날 안내 카드)
    public static RestaurantHud Current { get; private set; }
    public bool IsShowing => _bar != null && _bar.gameObject.activeInHierarchy;
    public float TopRightWidth => _topRight != null ? _topRight.rect.width : 0f;           // 오른쪽 위 알약 줄의 폭
    public float TopRightBottom => _topRight != null ? Margin + _topRight.rect.height : 0f; // 오른쪽 위 알약 줄 아랫변 (화면 위에서)
    private readonly List<MenuSlot> _menu = new List<MenuSlot>();
    private readonly List<StaffSlot> _staff = new List<StaffSlot>();
    private readonly List<int> _menuIds = new List<int>();
    private readonly List<RecipeData> _menuRecipes = new List<RecipeData>();
    private CanvasGroup _pauseOverlay;
    private CanvasGroup _toastGroup;
    private TMP_Text _toastText;
    private Coroutine _toastRoutine;
    private float _nextRefresh, _nextStaffScan;

    public void Init(DayCycleController day)
    {
        _day = day;
        Build();
        _day.OnPhaseChanged += HandlePhaseChanged;
        _day.OnToast += ShowToast;
        RestaurantSpeedController.OnFastForwardChanged += HandleSpeedChanged;
        _rating = RestaurantRatingManager.Instance;
        if (_rating != null)
        {
            _rating.OnTodayAverageChanged += HandleRatingChanged;
            _lastRatingAverage = _rating.TodayAverage;
            _lastRatingCount = _rating.TodayScoreCount;
        }
        HandlePhaseChanged(_day.Phase);
    }

    private void Awake() { Current = this; }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        if (_day != null)
        {
            _day.OnPhaseChanged -= HandlePhaseChanged;
            _day.OnToast -= ShowToast;
        }
        RestaurantSpeedController.OnFastForwardChanged -= HandleSpeedChanged;
        if (_rating != null) _rating.OnTodayAverageChanged -= HandleRatingChanged;
    }

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        bool operating = phase == DayLoopPhase.Open || phase == DayLoopPhase.Closing;
        _bar.gameObject.SetActive(operating);
        if (!operating) SetPauseOverlay(false);

        switch (phase)
        {
            case DayLoopPhase.Open:
                _phaseText.text = "영업 중";
                _phaseChip.color = Leaf;
                break;
            case DayLoopPhase.Closing:
                _phaseText.text = "입장 마감";
                _phaseChip.color = Peach;
                break;
            case DayLoopPhase.Settlement:
                _phaseText.text = "영업 종료";
                _phaseChip.color = Muted;
                break;
            default:
                _phaseText.text = "준비 중";
                _phaseChip.color = Muted;
                break;
        }

        _nextStaffScan = 0f;
        Refresh();
    }

    private void HandleSpeedChanged(bool fast)
    {
        RefreshControls();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.2f;
        Refresh();
    }

    private void Refresh()
    {
        if (_day == null || !_bar.gameObject.activeSelf) return;

        _dayText.text = $"{_day.Day}일차";

        CustomerSpawner spawner = _day.Spawner;
        int planned = spawner != null ? spawner.PlannedCount : 0;
        int visited = _day.Ledger.Visited;
        if (_day.Phase == DayLoopPhase.Closing)
            _progressText.text = $"남은 손님 {(spawner != null ? spawner.ActiveCustomerCount : 0)}";
        else
            _progressText.text = planned > 0 ? $"손님 {visited}/{planned}" : $"손님 {visited}";
        DayLoopUI.SetBar(_progressFill, planned > 0 ? (float)visited / planned : 0f);

        int gold = _day.CurrentGold;
        _goldText.text = gold >= 0 ? gold.ToString("N0") : "-";

        RefreshMenu();
        _orderDrawer.Refresh();
        if (Time.unscaledTime >= _nextStaffScan) ScanStaff();
        RefreshStaff();
        RefreshRating();
        RefreshControls();
    }

    // ───── 오늘의 메뉴 ─────

    private void RefreshMenu()
    {
        if (!GameSession.IsActive) return;

        // 메뉴 구성이 바뀌었을 때만 다시 만든다.
        List<int> slots = GameSession.Current.menuSlots;
        bool changed = slots.Count != _menuIds.Count;
        for (int i = 0; !changed && i < slots.Count; i++) changed = slots[i] != _menuIds[i];
        if (changed)
        {
            _menuIds.Clear();
            _menuIds.AddRange(slots);
            foreach (MenuSlot slot in _menu) Destroy(slot.bubble.gameObject);
            _menu.Clear();
            _menuRow.parent.gameObject.SetActive(true);
            _menuRecipes.Clear();
            foreach (int id in slots)
                if (GameDatabase.TryGetRecipe(id, out RecipeData recipe)) _menuRecipes.Add(recipe);
            int shown = MenuExpanded ? _menuRecipes.Count : Mathf.Min(MenuCollapsedCount, _menuRecipes.Count);
            bool twoRows = shown > MenuPerRow;
            float size = twoRows ? MenuSizeCompact : MenuSize;
            _menuGrid.cellSize = new Vector2(size, size);
            _menuGrid.spacing = new Vector2(twoRows ? 10f : 18f, 14f);
            _menuGrid.constraintCount = Mathf.Clamp(shown, 1, MenuPerRow);
            for (int i = 0; i < shown; i++) _menu.Add(CreateMenuSlot(_menuRecipes[i], size));
            _menuRow.parent.gameObject.SetActive(_menu.Count > 0);
        }

        foreach (MenuSlot slot in _menu)
        {
            int count = Portions(slot.recipe);
            bool soldOut = count <= 0, low = !soldOut && count <= 2;
            slot.count.text = soldOut ? "품절" : count.ToString();
            slot.count.color = soldOut || low ? Color.white : DayLoopUI.Kit.Ink;
            slot.badge.color = soldOut ? Muted : low ? Peach : Color.white;
            slot.badge.rectTransform.sizeDelta = new Vector2(soldOut ? 58 : count >= 10 ? 44 : 36, 36);
            slot.bubble.color = soldOut ? new Color32(0xED, 0xE3, 0xD3, 0xFF) : Color.white;
            if (slot.icon != null) slot.icon.color = soldOut ? new Color(0.6f, 0.6f, 0.6f, 0.55f) : Color.white;
        }
        RefreshMenuToggle();
    }

    private static int Portions(RecipeData recipe)
    {
        return IngredientInventoryService.Instance != null ? IngredientInventoryService.Instance.CraftableCount(recipe) : 0;
    }

    // 접기 버튼: 숨길 메뉴가 있을 때만 보인다. 접혀 있으면 숨은 메뉴 수를 보여 주고, 그중 2인분 이하가 있으면 코랄색으로 알린다.
    private void RefreshMenuToggle()
    {
        bool usable = _menuRecipes.Count > MenuCollapsedCount;
        if (_menuToggle.gameObject.activeSelf != usable) _menuToggle.gameObject.SetActive(usable);
        if (!usable) return;

        int hidden = _menuRecipes.Count - _menu.Count;
        bool alert = false;
        for (int i = _menu.Count; i < _menuRecipes.Count; i++) alert |= Portions(_menuRecipes[i]) <= 2;
        DayLoopUI.SetButtonLabel(_menuToggle, hidden > 0 ? "+" + hidden : "접기");
        DayLoopUI.SetButtonColors(_menuToggle, alert ? Peach : DayLoopUI.Kit.WoodLight, Color.white);
        _menuChevron.rectTransform.localEulerAngles = new Vector3(0f, 0f, hidden > 0 ? 0f : 180f);   // 접혀 있으면 → (펼치기), 펼쳐 있으면 ← (접기)
    }

    private void ToggleMenu()
    {
        s_menuExpanded = !MenuExpanded;
        _menuIds.Clear();   // 메뉴 구성이 바뀐 것으로 쳐서 칸을 다시 만든다
        RefreshMenu();
    }

    private MenuSlot CreateMenuSlot(RecipeData recipe, float size)
    {
        MenuSlot slot = new MenuSlot { recipe = recipe };
        slot.bubble = Disc(_menuRow, Color.white, size, recipe.RecipeName);
        DayLoopUI.Chunky(slot.bubble, 3f, 0f);
        float iconSize = Mathf.Round(size * 60f / MenuSize);
        slot.icon = DayLoopUI.FoodIcon(slot.bubble.transform, recipe, iconSize);
        DayLoopUI.Place(slot.icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(iconSize, iconSize));

        slot.badge = Pill(slot.bubble.transform, Color.white, "Count", 3f, 0f);
        DayLoopUI.Place(slot.badge.rectTransform, new Vector2(1f, 1f), new Vector2(.5f, .5f), new Vector2(-8, -8), new Vector2(36, 36));
        if (size < MenuSize) slot.badge.rectTransform.localScale = Vector3.one * .75f;   // 작은 음식 칸에서 그림을 덮지 않게
        slot.count = Jua(DayLoopUI.Text(slot.badge.transform, "", 18, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(slot.count.rectTransform);
        return slot;
    }

    // ───── 직원 ─────

    private void ScanStaff()
    {
        _nextStaffScan = Time.unscaledTime + 1f;
        var agents = new List<PartTimerAgent>();
        foreach (PartTimerAgent agent in FindObjectsByType<PartTimerAgent>(FindObjectsSortMode.None))
            if (agent.isActiveAndEnabled) agents.Add(agent);
        // 주방 먼저, 같은 역할은 이름순
        agents.Sort((a, b) =>
        {
            int ra = a is ChefAgent ? 0 : 1, rb = b is ChefAgent ? 0 : 1;
            return ra != rb ? ra.CompareTo(rb) : string.CompareOrdinal(a.name, b.name);
        });
        // QA #20: 예전에는 4명까지만 보여 Lv.5(주방 3 + 홀 2)부터 직원이 빠졌다. 모두 보여 준다.

        bool same = agents.Count == _staff.Count;
        for (int i = 0; same && i < agents.Count; i++) same = agents[i] == _staff[i].agent;
        if (same) return;

        foreach (StaffSlot slot in _staff)
        {
            Destroy(slot.root);
            if (slot.wakeTip != null) Destroy(slot.wakeTip.gameObject);
        }
        _staff.Clear();
        // 카드를 먼저 켜 두고 만든다. (꺼진 채로 만들면 버튼 등의 Awake가 미뤄진다)
        _staffRow.gameObject.SetActive(agents.Count > 0);
        foreach (PartTimerAgent agent in agents) _staff.Add(CreateStaffSlot(agent));
        // 카드 줄이 StaffColumnMax보다 길면 위쪽 모서리를 기준으로 줄인다. (Lv.5 직원 5명이어도 메뉴판에 닿지 않게)
        float column = agents.Count * StaffCardSize.y + Mathf.Max(0, agents.Count - 1) * StaffCardGap;
        _staffRow.localScale = Vector3.one * Mathf.Min(1f, StaffColumnMax / Mathf.Max(1f, column));
    }

    // 시안 386:263 직원 상태 카드. 좌표는 시안 그대로(카드 왼쪽 위 기준, 아래로 +).
    private static readonly Vector2 StaffCardSize = new Vector2(320f, 136f);
    private const float StaffCardGap = 8f;
    private const float StaffColumnMax = 600f;   // 일차 카드 아래 ~ 메뉴판 위 사이에 들어가는 카드 줄 높이

    private StaffSlot CreateStaffSlot(PartTimerAgent agent)
    {
        bool chef = agent is ChefAgent;
        StaffSlot slot = new StaffSlot { agent = agent };

        slot.card = DayLoopUI.Panel(_staffRow, StaffCard, agent.name);
        slot.card.GetComponent<RoundedCorner>().radius = 16f;
        slot.card.raycastTarget = false;
        slot.line = DayLoopUI.AddOutline(slot.card, StaffCardLine, 1f);
        Shadow drop = slot.card.gameObject.AddComponent<Shadow>();
        drop.effectColor = new Color32(0x0E, 0x29, 0x39, 0x2B);
        drop.effectDistance = new Vector2(0f, -5f);
        DayLoopUI.Layout(slot.card, preferredWidth: StaffCardSize.x, preferredHeight: StaffCardSize.y);
        slot.root = slot.card.gameObject;
        Transform card = slot.card.transform;

        // 안쪽 테두리(밝은 띠)와 상태 카드 면. 잠들면 바깥·안쪽 면만 분홍빛이 된다.
        Image band = DayLoopUI.Panel(card, StaffCard, "Band");
        band.GetComponent<RoundedCorner>().radius = 16f;
        band.raycastTarget = false;
        DayLoopUI.Stretch(band.rectTransform, 4f, 4f, 4f, 9f);
        slot.inner = DayLoopUI.Panel(card, StaffCard, "Face");
        slot.inner.GetComponent<RoundedCorner>().radius = 13f;
        slot.inner.raycastTarget = false;
        DayLoopUI.Stretch(slot.inner.rectTransform, 8f, 8f, 8f, 13f);

        slot.portrait = DayLoopUI.Panel(card, Color.white, "PortraitFrame", false);
        DesignArt.ApplySliced(slot.portrait, "PlayHud/PortraitFrame");
        slot.portrait.raycastTarget = false;
        PlaceInCard(slot.portrait.rectTransform, 15f, 17f, 94f, 98f);
        Image face = DayLoopUI.Panel(card, Color.white, "Portrait", false);
        face.sprite = DesignArt.Load(chef ? "OtterChef" : "OtterServer");
        face.preserveAspect = true;
        face.raycastTarget = false;
        PlaceInCard(face.rectTransform, 16f, 24f, 88f, 88f);

        TMP_Text name = Jua(DayLoopUI.Text(card, chef ? "주방 직원" : "홀 직원", 25, StaffName));
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Overflow;
        PlaceInCard(name.rectTransform, 122f, 17f, 106f, 32f);

        slot.statusChip = DayLoopUI.Panel(card, StatusChip, "Status");
        slot.statusChip.GetComponent<RoundedCorner>().radius = 8f;
        slot.statusChip.raycastTarget = false;
        PlaceInCard(slot.statusChip.rectTransform, 232f, 17f, 68f, 32f);
        slot.status = Jua(DayLoopUI.Text(slot.statusChip.transform, "", 19, StaffSoft, TextAlignmentOptions.Center));
        slot.status.textWrappingMode = TextWrappingModes.NoWrap;
        slot.status.overflowMode = TextOverflowModes.Overflow;
        DayLoopUI.Stretch(slot.status.rectTransform);

        RectTransform staminaRow = DayLoopUI.Rect("Stamina", card);
        DayLoopUI.Stretch(staminaRow);
        slot.staminaRow = staminaRow.gameObject;
        TMP_Text label = Jua(DayLoopUI.Text(staminaRow, "체력", 18, StaffSoft));
        label.textWrappingMode = TextWrappingModes.NoWrap;
        PlaceInCard(label.rectTransform, 122f, 63f, 48f, 26f);
        slot.stamina = Gauge(staminaRow, "Bar");
        PlaceInCard((RectTransform)slot.stamina.transform.parent, 174f, 72f, 125f, 10f);

        Button wake = DayLoopUI.Button(card, "깨우기", WakeRed, WakeText, 22, agent.WakeUp, "Wake");
        RoundedCorner wakeCorner = wake.image.GetComponent<RoundedCorner>();
        if (wakeCorner != null) wakeCorner.radius = 9f;
        Jua(wake.GetComponentInChildren<TMP_Text>());
        PlaceInCard((RectTransform)wake.transform, 122f, 64f, 177f, 42f);
        slot.wake = wake.gameObject;

        slot.body = agent.GetComponentInChildren<SpriteRenderer>();
        slot.wakeTip = CreateWakeTip(agent);
        return slot;
    }

    // [직원 깨우기] 말풍선: 빨간 둥근 단추에 진갈색 테와 아래 그림자, 밑에 직원 머리를 가리키는 대상 점.
    // 아래 가운데(pivot)를 직원 머리 위 화면 좌표에 맞춘다. 누르면 그 직원을 깨운다.
    private RectTransform CreateWakeTip(PartTimerAgent agent)
    {
        Button tip = DayLoopUI.Button(_wakeLayer, "직원 깨우기", WakeTipRed, Color.white, 23, agent.WakeUp, "WakeTip " + agent.name);
        RoundedCorner corner = tip.image.GetComponent<RoundedCorner>();
        if (corner != null) corner.radius = 16f;
        DayLoopUI.AddOutline(tip.image, WakeTipLine, 4f);
        Shadow drop = tip.gameObject.AddComponent<Shadow>();
        drop.effectColor = WakeTipDrop;
        drop.effectDistance = new Vector2(0f, -4f);
        Jua(tip.GetComponentInChildren<TMP_Text>()).textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform rt = (RectTransform)tip.transform;
        DayLoopUI.Place(rt, new Vector2(.5f, .5f), new Vector2(.5f, 0f), Vector2.zero, WakeTipSize);

        Image dot = DayLoopUI.Panel(rt, Color.white, "TargetDot", false);
        dot.sprite = DesignArt.Load("PlayHud/TargetDot");
        dot.raycastTarget = false;
        // 조각(48px, 2x)에 그림자 여백이 있어 24×24로 놓아야 점이 시안대로 8px이 된다. 점 가운데를 말풍선 아래 12에 둔다.
        DayLoopUI.Place(dot.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, -12f), new Vector2(24f, 24f));
        rt.gameObject.SetActive(false);
        return rt;
    }

    // 말풍선은 직원·카메라가 움직여도 따라가도록 매 프레임 자리를 잡는다.
    private void LateUpdate()
    {
        if (_bar == null || !_bar.gameObject.activeInHierarchy) return;
        Camera cam = Camera.main;
        foreach (StaffSlot slot in _staff)
        {
            if (slot.wakeTip == null) continue;
            bool show = cam != null && slot.agent != null && slot.agent.isActiveAndEnabled && slot.agent.IsSleeping;
            if (show)
            {
                Vector3 head = slot.body != null && slot.body.sprite != null
                    ? new Vector3(slot.body.bounds.center.x, slot.body.bounds.max.y, 0f)
                    : slot.agent.transform.position + Vector3.up * .6f;
                Vector3 screen = cam.WorldToScreenPoint(head);
                Vector2 local = Vector2.zero;
                show = screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(_wakeLayer, screen, null, out local);
                if (show) slot.wakeTip.anchoredPosition = local + new Vector2(0f, 20f);   // 대상 점(8)과 틈을 머리 위에 둔다
            }
            if (slot.wakeTip.gameObject.activeSelf != show) slot.wakeTip.gameObject.SetActive(show);
        }
    }

    private static void PlaceInCard(RectTransform rt, float x, float y, float width, float height)
    {
        DayLoopUI.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));
    }

    private void RefreshStaff()
    {
        foreach (StaffSlot slot in _staff)
        {
            if (slot.agent == null) { _nextStaffScan = 0f; continue; }
            bool sleeping = slot.agent.IsSleeping;
            slot.card.color = sleeping ? SleepCard : StaffCard;
            slot.line.effectColor = sleeping ? SleepCardLine : StaffCardLine;
            slot.inner.color = sleeping ? SleepCardInner : StaffCard;
            slot.portrait.color = sleeping ? SleepPortrait : Color.white;
            slot.statusChip.color = sleeping ? SleepChip : StatusChip;
            slot.status.color = sleeping ? WakeRed : StaffSoft;
            string status = sleeping ? "잠듦" : StaffWork(slot.agent);
            if (slot.status.text != status) slot.status.text = status;
            slot.staminaRow.SetActive(!sleeping);
            slot.wake.SetActive(sleeping);
            DayLoopUI.SetBar(slot.stamina, slot.agent.StaminaRatio);
        }
    }

    private static string StaffWork(PartTimerAgent agent)
    {
        if (agent is ChefAgent chef && chef.CookingRecipe != null) return "조리 중";
        if (agent is ServerAgent server && server.CarriedFood != null) return "서빙 중";
        return "대기 중";
    }

    // ───── 평점 ─────

    private void RefreshRating()
    {
        if (_rating == null)
        {
            _ratingText.text = "-";
            return;
        }

        int count = _rating.TodayScoreCount;
        if (count == 0)
        {
            float restaurant = _rating.RestaurantRating;
            _ratingText.text = restaurant > 0f ? DayLoopUI.Rating(restaurant) : "-";
            _ratingText.color = DayLoopUI.Kit.Ink;
            return;
        }

        _ratingText.text = DayLoopUI.Rating(_rating.TodayAverage);
        _ratingText.color = _rating.CurrentTier == RatingBuffTier.Buff ? DayLoopUI.Kit.GoodInk
            : _rating.CurrentTier == RatingBuffTier.Debuff ? DayLoopUI.Kit.WarnInk : DayLoopUI.Kit.Ink;
    }

    private void HandleRatingChanged(float average)
    {
        int count = _rating != null ? _rating.TodayScoreCount : 0;
        // 방금 들어온 평가 한 건의 점수 = 새 합계 - 이전 합계
        float score = count > _lastRatingCount ? average * count - _lastRatingAverage * _lastRatingCount : average;
        _lastRatingAverage = average;
        _lastRatingCount = count;

        if (!_bar.gameObject.activeSelf) return;
        RefreshRating();
        if (_ratingPunch != null) StopCoroutine(_ratingPunch);
        _ratingPunch = StartCoroutine(RatingPunch());
        StartCoroutine(RatingPopup(score));
    }

    private IEnumerator RatingPunch()
    {
        const float duration = 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            _ratingBlock.localScale = Vector3.one * (1f + 0.25f * k);
            yield return null;
        }
        _ratingBlock.localScale = Vector3.one;
        _ratingPunch = null;
    }

    private IEnumerator RatingPopup(float score)
    {
        Color color = score >= 4f ? Leaf : score <= 2f ? Peach : DayLoopUI.Kit.Coin;
        Image popup = Pill(_ratingBlock, Color.white, "RatingPopup", 3f, 0f);
        popup.raycastTarget = false;
        popup.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        RectTransform rt = popup.rectTransform;
        DayLoopUI.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(110, 42));
        TMP_Text text = Jua(DayLoopUI.Text(popup.transform, "+" + DayLoopUI.Rating(Mathf.Clamp(score, 0f, 5f)), 24, color, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(text.rectTransform);

        const float duration = 1.4f;
        float t = 0f;
        while (t < duration && popup != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            rt.anchoredPosition = new Vector2(0f, -12f - 36f * Mathf.Sqrt(k));
            group.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }
        if (popup != null) Destroy(popup.gameObject);
    }

    private void RefreshControls()
    {
        DayLoopUI.SetButtonLabel(_speedButton, "x" + (RestaurantSpeedController.SpeedIndex + 1));
        // 나무 테 칸은 바탕색을 바꾸지 않고 글자색·밝기로만 상태를 알린다.
        DayLoopUI.SetButtonColors(_speedButton, Color.white, RestaurantSpeedController.SpeedIndex > 0 ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Ink);

        bool paused = _day != null && _day.IsPaused;
        _pauseButton.image.color = paused ? Butter : Color.white;
        SetPauseOverlay(paused);
    }

    private void SetPauseOverlay(bool visible)
    {
        if (_pauseOverlay == null) return;
        _pauseOverlay.alpha = visible ? 1f : 0f;
        _pauseOverlay.blocksRaycasts = visible;
        _pauseOverlay.interactable = visible;
    }

    // ───── 알림 ─────

    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        _toastText.text = message;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        _toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.8f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            _toastGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        _toastGroup.alpha = 0f;
        _toastRoutine = null;
    }

    // ───── 조립 ─────

    private void Build()
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] HUD", 40, gameObject.scene);
        _canvas.transform.SetParent(transform, false);
        _bar = DayLoopUI.Rect("OperationHud", _canvas.transform);
        DayLoopUI.Stretch(_bar);
        // 깨우기 말풍선은 월드 위에 뜨므로 귀퉁이 카드들보다 먼저(아래에) 깐다.
        _wakeLayer = DayLoopUI.Rect("WakeTips", _bar);
        DayLoopUI.Stretch(_wakeLayer);

        BuildTopLeft();
        BuildTopRight();
        BuildMenuTray();
        _orderDrawer = OrderDrawer.Create(_bar);   // 일시정지 화면보다 먼저 만들어 그 아래에 깔리게 한다
        BuildPauseOverlay();
        BuildToast();
    }

    // 왼쪽 위: 일차 카드 아래에 직원 카드를 쌓는다. 뗏목 왼쪽 빈 바다에 둔다. (시안 386:73)
    private void BuildTopLeft()
    {
        RectTransform group = DayLoopUI.Rect("TopLeft", _bar);
        DayLoopUI.Place(group, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -Margin), Vector2.zero);
        DayLoopUI.Vertical(group, 0, 16, TextAnchor.UpperLeft).childForceExpandWidth = false;
        Fit(group);
        BuildToday(group);
        BuildStaffTray(group);
    }

    // 해 · n일차 · 단계 칩 / 방문 게이지
    private void BuildToday(Transform parent)
    {
        // 시안의 영업 팻말: 나무 테에 끼운 종이와 황동 핀
        Image today = DesignArt.WoodCard(parent, "Today", "PlayHud/SignPaper", pins: true);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(today, 0, 14);
        row.padding = new RectOffset(22, 34, 18, 20);

        Image sun = Disc(today.transform, Butter, 52, "Sun");
        DayLoopUI.Chunky(sun, 3f, 0f);

        RectTransform info = DayLoopUI.Rect("Info", today.transform);
        DayLoopUI.Vertical(info, 0, 6).childForceExpandWidth = false;

        RectTransform heading = DayLoopUI.Rect("DayAndPhase", info);
        DayLoopUI.Horizontal(heading, 0, 10);
        _dayText = Jua(DayLoopUI.Text(heading, "", 30, DayLoopUI.Kit.Ink));
        _dayText.textWrappingMode = TextWrappingModes.NoWrap;
        _phaseChip = Pill(heading, Leaf, "Phase", 0f, 0f);
        DayLoopUI.Horizontal(_phaseChip, 0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(14, 14, 3, 3);
        _phaseText = Jua(DayLoopUI.Text(_phaseChip.transform, "", 18, Color.white, TextAlignmentOptions.Center));
        _phaseText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform progress = DayLoopUI.Rect("Visits", info);
        DayLoopUI.Horizontal(progress, 0, 10);
        _progressFill = Gauge(progress, "Bar");
        DayLoopUI.Layout(_progressFill.transform.parent, preferredWidth: 150, preferredHeight: 12);
        _progressText = Jua(DayLoopUI.Text(progress, "", 19, DayLoopUI.Kit.InkSoft));
        _progressText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // 오른쪽 위: 골드 · 평점 · 배속 · 일시정지
    private void BuildTopRight()
    {
        RectTransform group = DayLoopUI.Rect("TopRight", _bar);
        _topRight = group;
        DayLoopUI.Place(group, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-Margin, -Margin), Vector2.zero);
        DayLoopUI.Horizontal(group, 0, 12, TextAnchor.MiddleRight);
        Fit(group);

        // 시안의 우측 조작 HUD: 칸마다 나무 테에 종이를 끼운다.
        Image gold = DesignArt.WoodCard(group, "Gold", frame: "PlayHud/HudGoldFrame");
        DayLoopUI.Horizontal(gold, 0, 8).padding = new RectOffset(14, 24, 0, 0);
        DayLoopUI.Layout(gold, preferredHeight: 64);
        Image coin = Disc(gold.transform, Color.white, 40, "Coin");
        coin.sprite = DesignArt.Load("PlayHud/IconGold");
        _goldText = Jua(DayLoopUI.Text(gold.transform, "", 30, DayLoopUI.Kit.Ink));
        _goldText.textWrappingMode = TextWrappingModes.NoWrap;

        Image rating = DesignArt.WoodCard(group, "Rating");
        _ratingBlock = rating.rectTransform;
        DayLoopUI.Horizontal(rating, 0, 6).padding = new RectOffset(18, 24, 0, 0);
        DayLoopUI.Layout(rating, preferredHeight: 64);
        string star = DayLoopUI.Glyph("★", "평점");
        TMP_Text starText = DayLoopUI.Text(rating.transform, star, star == "★" ? 36 : 20, DayLoopUI.Kit.Coin, TextAlignmentOptions.Center);
        starText.font = DayLoopUI.Font;
        starText.fontSharedMaterial = starText.font.material;
        starText.outlineWidth = 0.2f;
        starText.outlineColor = DayLoopUI.Kit.Line;
        starText.textWrappingMode = TextWrappingModes.NoWrap;
        _ratingText = Jua(DayLoopUI.Text(rating.transform, "", 30, DayLoopUI.Kit.Ink));
        _ratingText.textWrappingMode = TextWrappingModes.NoWrap;

        _speedButton = DesignArt.WoodButton(group, "x1", 26, RestaurantSpeedController.CycleSpeed, "Speed", "PlayHud/HudSpeedFrame");
        Jua(_speedButton.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(_speedButton, preferredWidth: 64, preferredHeight: 64);

        // 일시정지 칸은 시안 그림에 II 표시까지 들어 있다.
        _pauseButton = DayLoopUI.Button(group, "", Color.white, DayLoopUI.Kit.Ink, 26, () => _day.SetPaused(!_day.IsPaused), "Pause");
        _pauseButton.image.sprite = DesignArt.Load("PlayHud/HudPauseButton");
        _pauseButton.image.type = Image.Type.Simple;
        _pauseButton.image.pixelsPerUnitMultiplier = 1f;
        DayLoopUI.Layout(_pauseButton, preferredWidth: 64, preferredHeight: 64);
    }

    // 왼쪽 아래: 오늘의 메뉴
    private void BuildMenuTray()
    {
        // 시안의 탁상 메뉴판: 나무 테에 메뉴지를 끼우고 아래에 받침목
        Image tray = DesignArt.WoodCard(_bar, "TodayMenu", "PlayHud/MenuCardPaper", paperBottom: 16f, pins: true);
        DayLoopUI.Place(tray.rectTransform, Vector2.zero, Vector2.zero, new Vector2(Margin, Margin), Vector2.zero);
        DayLoopUI.Vertical(tray, 0, 10).padding = new RectOffset(28, 30, 18, 30);
        Fit(tray);
        Image stand = DesignArt.Backing(tray.transform, "PlayHud/MenuCardStand", 0f, 0f, 0f, 0f, "Stand");
        stand.transform.SetSiblingIndex(1);   // 종이 위, 내용 아래
        stand.rectTransform.anchorMax = new Vector2(1f, 0f);   // 아래 끝에 높이 16으로 깐다
        stand.rectTransform.offsetMax = new Vector2(0f, 16f);

        RectTransform header = DayLoopUI.Rect("Header", tray.transform);
        DayLoopUI.Horizontal(header, 0, 10);
        DayLoopUI.Layout(header, preferredHeight: 28);
        TMP_Text title = Jua(DayLoopUI.Text(header, "오늘의 메뉴", 18, DayLoopUI.Kit.InkSoft));
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(title, flexibleWidth: 1);

        _menuToggle = DayLoopUI.KitButton(header, "", DayLoopUI.Kit.WoodLight, Color.white, 15, ToggleMenu, "MenuToggle");
        Round(_menuToggle.image);
        DayLoopUI.Layout(_menuToggle, preferredWidth: 64, preferredHeight: 28);
        TMP_Text toggleText = Jua(_menuToggle.GetComponentInChildren<TMP_Text>());
        toggleText.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Stretch(toggleText.rectTransform, 6f, 0f, 24f, 0f);
        _menuChevron = DayLoopUI.Panel(_menuToggle.transform, Color.white, "Chevron", false);
        _menuChevron.sprite = DesignArt.Load("IconChevronRight");
        _menuChevron.preserveAspect = true;
        _menuChevron.raycastTarget = false;
        DayLoopUI.Place(_menuChevron.rectTransform, new Vector2(1f, .5f), new Vector2(.5f, .5f), new Vector2(-14f, 0f), new Vector2(14f, 14f));

        _menuRow = DayLoopUI.Rect("Dishes", tray.transform);
        _menuGrid = _menuRow.gameObject.AddComponent<GridLayoutGroup>();
        _menuGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tray.gameObject.SetActive(false);
    }

    // 왼쪽 위 일차 카드 아래: 직원별 상태 카드 줄. 위쪽 모서리를 기준으로 줄일 수 있게 pivot을 왼쪽 위에 둔다.
    private void BuildStaffTray(Transform parent)
    {
        _staffRow = DayLoopUI.Rect("Staff", parent);
        _staffRow.pivot = new Vector2(0f, 1f);
        DayLoopUI.Vertical(_staffRow, 0, StaffCardGap, TextAnchor.UpperLeft).childForceExpandWidth = false;
        Fit(_staffRow);
        _staffRow.gameObject.SetActive(false);
    }

    // 일시정지: 화면을 살짝 어둡게 하고 가운데에 [다시 시작]
    private void BuildPauseOverlay()
    {
        Image dim = DayLoopUI.Panel(_bar, new Color(0.16f, 0.24f, 0.31f, 0.45f), "PauseOverlay", false);
        DayLoopUI.Stretch(dim.rectTransform);
        _pauseOverlay = dim.gameObject.AddComponent<CanvasGroup>();

        Image card = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "PauseCard", 4f, 8f);
        card.GetComponent<RoundedCorner>().radius = 40f;
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(480, 0));
        DayLoopUI.Vertical(card, 0, 14, TextAnchor.UpperCenter).padding = new RectOffset(36, 36, 28, 32);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image chef = DayLoopUI.Panel(card.transform, Color.white, "Chef", false);
        chef.sprite = DesignArt.Load("OtterChef");
        chef.preserveAspect = true;
        chef.raycastTarget = false;
        DayLoopUI.Layout(chef, preferredHeight: 120);
        DayLoopUI.Layout(Jua(DayLoopUI.Text(card.transform, "잠깐 쉬어요", 42, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center)), preferredHeight: 54);
        Button resume = DayLoopUI.KitButton(card.transform, "다시 시작", Mint, Color.white, 30, () => _day.SetPaused(false), "Resume");
        Round(resume.image);
        Jua(resume.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(resume, preferredHeight: 72);
        SetPauseOverlay(false);
    }

    private void BuildToast()
    {
        Image toast = Pill(_canvas.transform, DayLoopUI.Kit.Sail, "Toast", 3f, 4f);
        DayLoopUI.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -116), Vector2.zero);
        toast.raycastTarget = false;
        DayLoopUI.Horizontal(toast, 0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(26, 26, 10, 12);
        Fit(toast);
        _toastText = Jua(DayLoopUI.Text(toast.transform, "", 24, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center));
        _toastText.textWrappingMode = TextWrappingModes.NoWrap;
        _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.blocksRaycasts = false;
        _toastGroup.alpha = 0f;
    }

    // ───── 조립 도구 ─────

    /// <summary>끝이 반원인 알약 카드. drop이 0이면 외곽선만.</summary>
    private static Image Pill(Transform parent, Color color, string name, float line = 3f, float drop = 5f)
    {
        Image pill = line > 0f ? DayLoopUI.Card(parent, color, name, line, drop) : DayLoopUI.Panel(parent, color, name);
        Round(pill);
        return pill;
    }

    /// <summary>시안의 체력 게이지 조각으로 만든 게이지. 반환한 채움 Image는 DayLoopUI.SetBar로 채운다.</summary>
    private static Image Gauge(Transform parent, string name)
    {
        Image track = DayLoopUI.Panel(parent, Color.white, name, false);
        DesignArt.ApplySliced(track, "PlayHud/HealthTrack");
        track.raycastTarget = false;
        Image fill = DayLoopUI.Panel(track.transform, Color.white, "Fill", false);
        DesignArt.ApplySliced(fill, "PlayHud/HealthFill");
        fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.pivot = new Vector2(0f, .5f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
        DayLoopUI.SetBar(fill, 0f);
        return fill;
    }

    private static void Round(Image image)
    {
        RoundedCorner corner = image.GetComponent<RoundedCorner>();
        if (corner != null) corner.radius = 999f;
    }

    /// <summary>원. 레이아웃 안에서는 size 크기로 잡힌다.</summary>
    private static Image Disc(Transform parent, Color color, float size, string name)
    {
        Image disc = DayLoopUI.Panel(parent, color, name, false);
        disc.sprite = DayLoopUI.Circle;
        disc.type = Image.Type.Simple;
        disc.raycastTarget = false;
        DayLoopUI.Layout(disc, preferredWidth: size, preferredHeight: size);
        disc.rectTransform.sizeDelta = new Vector2(size, size);
        return disc;
    }

    private static void Fit(Component target)
    {
        ContentSizeFitter fit = target.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>캐주얼 HUD는 제목·숫자 모두 주아체를 쓴다.</summary>
    private static T Jua<T>(T text) where T : TMP_Text
    {
        text.font = DesignFonts.Heading;
        text.fontSharedMaterial = text.font.material;
        return text;
    }
}
