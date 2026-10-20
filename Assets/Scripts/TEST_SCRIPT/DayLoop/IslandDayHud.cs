using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 섬(하루 시작) 화면 표시. (HUD-01, SAVE-02)
///  - 왼쪽 위 하루 명판: 일차, 하루 흐름(아침 → 영업 → 밤) 중 지금 단계, 보유 골드·식당 평점·레벨, 미션 진행
///  - 섬 건물 버튼(레스토랑·실험실·집)에 들를 이유가 있을 때만 작은 배지를 붙인다. (버튼 자체는 그대로)
///  - 레스토랑을 누르면 오늘의 메뉴·직원·재료 부족을 보여 주는 식당 카드를 띄운다.
///  - 결산을 확정한 뒤 결산 화면을 닫기 전에 종료했다면, 이어하기 때 저장된 결산을 한 번 더 보여 준다.
///    (값은 저장된 스냅샷 그대로이며 매출을 다시 지급하지 않는다)
/// </summary>
public class IslandDayHud : MonoBehaviour
{
    private static IslandDayHud _instance;

    private Canvas _canvas;
    private Button _prepareAction;
    private TMP_Text _day, _stats, _mission;
    private Image _missionBar;
    private readonly Image[] _stepDots = new Image[3];
    private readonly TMP_Text[] _stepLabels = new TMP_Text[3];
    private readonly Dictionary<string, TMP_Text> _badges = new Dictionary<string, TMP_Text>();
    private RectTransform _restaurantCard;
    private SettlementPanel _settlement;
    private float _nextRefresh;

    private void Awake()
    {
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Start()
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] IslandHUD", 30, gameObject.scene);
        _canvas.transform.SetParent(transform, false);

        var sea = DayLoopUI.Panel(_canvas.transform, new Color32(0x2C, 0x8F, 0xB0, 255), "Sea", false); DayLoopUI.Stretch(sea.rectTransform);
        var background = ReferenceTitleView.Picture(_canvas.transform, "IslandBackground");
        ReferenceTitleView.Place(background.rectTransform, 150, 0, 1920, 1080);
        BuildPlaque();
        BuildMapButtons();
        AttachBadge("Restaurant");
        AttachBadge("Lab");
        AttachBadge("House");
        Refresh();

        if (GameSession.IsActive && GameSession.Current.settlementPendingReview
            && GameSession.Current.lastSettlement != null && GameSession.Current.lastSettlement.day > 0)
        {
            _settlement = gameObject.AddComponent<SettlementPanel>();
            _settlement.Show(GameSession.Current.lastSettlement, $"{GameSession.Current.day}일차 시작", CloseReplay, null);
            _settlement.SetSaveState(SettlementPanel.SaveState.Saved);
        }
    }

    private void CloseReplay()
    {
        DayLoopSave.AcknowledgeSettlement();
        _settlement.Hide();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.5f;
        Refresh();
    }

    // ───── 하루 명판 ─────

    private void BuildPlaque()
    {
        var rail = DayLoopUI.ScreenRail(_canvas.transform, "IslandRail");
        var status = DayLoopUI.Card(rail, DayLoopUI.Kit.Sail, "DayStatus");
        DayLoopUI.Layout(status, preferredHeight: 150);
        DayLoopUI.Vertical(status, 20, 8);
        _day = DayLoopUI.Text(status.transform, "", 38, DayLoopUI.Kit.Ink, style: FontStyles.Bold);
        DayLoopUI.Layout(_day, preferredHeight: 48);
        var steps = DayLoopUI.Rect("Steps", status.transform);
        DayLoopUI.Layout(steps, preferredHeight: 26);
        DayLoopUI.Horizontal(steps, 0, 10);
        string[] names = { "아침", "영업", "밤" };
        for (int i = 0; i < names.Length; i++)
        {
            var dot = DayLoopUI.Panel(steps, DayLoopUI.Kit.Sand, names[i]);
            DayLoopUI.Layout(dot, preferredWidth: 22, preferredHeight: 22);
            _stepDots[i] = dot;
            _stepLabels[i] = DayLoopUI.Text(steps, names[i], 20, DayLoopUI.Kit.Ink);
            DayLoopUI.Layout(_stepLabels[i], preferredWidth: 54, preferredHeight: 24);
        }
        _stats = DayLoopUI.Text(status.transform, "", 23, DayLoopUI.Kit.Ink, style: FontStyles.Bold);
        DayLoopUI.Layout(_stats, preferredHeight: 32);
        var mission = DayLoopUI.Card(rail, DayLoopUI.Kit.Sail, "Mission");
        DayLoopUI.Layout(mission, preferredHeight: 130.5f);
        DayLoopUI.Vertical(mission, 16, 10);
        _mission = DayLoopUI.Text(mission.transform, "", 21, DayLoopUI.Kit.Ink, style: FontStyles.Bold);
        DayLoopUI.Layout(_mission, preferredHeight: 66);
        _missionBar = DayLoopUI.Bar(mission.transform, 0, DayLoopUI.Kit.Sea);
        DayLoopUI.Layout(_missionBar.transform.parent, preferredHeight: 14);
        _prepareAction = DayLoopUI.RailButton(rail, "영업 준비", () => {
            if (!GameFlow.TryEnterRestaurant()) _mission.text = "오늘은 영업을 시작할 수 없어요.\n집에서 다음 날을 준비해요.";
        }, true, 99);
        foreach (string section in new[] { "상점", "연구", "대장간", "주민", "집" })
        {
            string target = section;
            DayLoopUI.RailButton(rail, section == "주민" ? "선착장 · 탐험" : section == "연구" ? "연구소" : section,
                () => { if (target == "집") ReferenceHouseView.Open(gameObject.scene); else RestaurantManagementUI.OpenSection(target); }, height: 63);
        }
        DayLoopUI.Spacer(rail);
        var footer = DayLoopUI.Rect("Footer", rail); DayLoopUI.Layout(footer, preferredHeight: 63); DayLoopUI.Horizontal(footer, 0, 12).childForceExpandWidth = true;
        var manage = DayLoopUI.RailButton(footer, "관리", () => RestaurantManagementUI.OpenSection("메뉴"), height: 63); DayLoopUI.Layout(manage, flexibleWidth: 1, preferredHeight: 63);
        var settings = DayLoopUI.RailButton(footer, "설정", () => SettingsWindow.Open(gameObject.scene), height: 63); DayLoopUI.Layout(settings, flexibleWidth: 1, preferredHeight: 63);
    }

    private void BuildMapButtons()
    {
        string[] labels = { "식당", "집", "상점", "대장간", "연구소", "선착장 · 탐험" };
        string[] sections = { "", "집", "상점", "대장간", "연구", "주민" };
        Vector2[] positions = { new Vector2(350, 470), new Vector2(692, 318), new Vector2(652, 178), new Vector2(1005, 96), new Vector2(1140, 104), new Vector2(1190, 336) };
        string[] icons = { "Shop", "House", "Bag", "Hammer", "Research", "Explore" };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var button = DayLoopUI.Button(_canvas.transform, "", Color.clear, Color.white, 25.5f, () => {
                if (index == 0) { if (!GameFlow.TryEnterRestaurant()) _mission.text = "오늘 영업을 마쳤거나 공사 중이에요."; }
                else if (index == 1) ReferenceHouseView.Open(gameObject.scene);
                else RestaurantManagementUI.OpenSection(sections[index]);
            }, "Map " + labels[i]);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            DayLoopUI.Place((RectTransform)button.transform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(positions[i].x * 1.5f, -positions[i].y * 1.5f), new Vector2(90, 136.5f));
            var circle = DayLoopUI.Card(button.transform, i == 0 ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sail, "Marker", 4.5f, 6);
            circle.sprite = DayLoopUI.Circle; circle.type = Image.Type.Simple; circle.raycastTarget = false;
            ReferenceTitleView.Place(circle.rectTransform, 0, 0, 90, 90);
            var icon = ReferenceTitleView.Picture(circle.transform, "Icon" + icons[i]); icon.color = i == 0 ? Color.white : DayLoopUI.Kit.Line;
            ReferenceTitleView.Place(icon.rectTransform, 24, 24, 42, 42);
            var caption = DayLoopUI.Text(button.transform, labels[i], 25.5f, Color.white, TextAlignmentOptions.Center);
            DesignFonts.Apply(caption, true);
            float width = caption.GetPreferredValues(labels[i]).x + 36;
            var plaque = DayLoopUI.Panel(button.transform, new Color(58f / 255, 39f / 255, 24f / 255, .82f), "Caption"); plaque.raycastTarget = false;
            ReferenceTitleView.Place(plaque.rectTransform, (90 - width) / 2, 96, width, 40.5f);
            caption.transform.SetParent(plaque.transform, false); DayLoopUI.Stretch(caption.rectTransform);
        }
    }

    private void Refresh()
    {
        if (_day == null) return;
        int day = GameSession.IsActive ? GameSession.Current.day : (InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 1);
        int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrencyById(GameSession.GoldCurrencyId) : GameSession.IsActive ? RestaurantProgress.Gold(GameSession.Current) : -1;
        _day.text = day + "일차";

        // 하루 흐름: 영업을 마쳤으면 밤(자유시간), 아니면 아침
        int current = GameSession.OperationCompletedToday ? 2 : 0;
        for (int i = 0; i < _stepDots.Length; i++)
        {
            bool done = i < current, now = i == current;
            _stepDots[i].color = now ? DayLoopUI.Kit.Coin : done ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sail;
            _stepLabels[i].color = now ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft;
        }

        string soft = DayLoopUI.Hex(DayLoopUI.Kit.InkSoft);
        string stats = gold >= 0 ? $"<color={DayLoopUI.Hex(DayLoopUI.Kit.GoldInk)}>{DayLoopUI.Gold(gold)}</color>" : "";
        if (GameSession.IsActive)
        {
            DaySettlementRecord last = GameSession.Current.lastSettlement;
            if (last != null && last.day > 0) stats += $"   {DayLoopUI.Rating(RestaurantRules.RestaurantRating(GameSession.Current))}";
            stats += $"   <color={soft}>Lv.{GameSession.Current.restaurantLevel}</color>";
            if (GameSession.Current.progression.pendingRestaurantLevel > 0)
                stats += $" <color={DayLoopUI.Hex(DayLoopUI.Kit.SeaDeep)}>→ {GameSession.Current.progression.pendingRestaurantLevel} 공사 중</color>";

            RestaurantManagementUI.Mission(GameSession.Current, out string missionName, out int target);
            int progress = GameSession.Current.progression.missionProgress;
            _mission.text = $"오늘의 할 일\n{missionName} <color={soft}>{progress:N0} / {target:N0}</color>";
            DayLoopUI.SetBar(_missionBar, target > 0 ? (float)progress / target : 0f);
            _missionBar.color = progress >= target ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sea;
        }
        _mission.transform.parent.gameObject.SetActive(GameSession.IsActive);
        _stats.text = stats;

        if (GameSession.IsActive && _prepareAction != null)
            DayLoopUI.SetButtonLabel(_prepareAction, $"영업 준비\n<size=65%>메뉴 {GameSession.Current.menuSlots.Count(x => x >= 0)}개 · 직원 {GameSession.Current.employees.Count}명 · {RestaurantRules.SeatLimit(GameSession.Current.restaurantLevel)}석</size>");
        RefreshBadges();
    }

    // ───── 건물 배지 ─────

    private void AttachBadge(string buttonName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name != buttonName || !(button.transform is RectTransform)) continue;
                TMP_Text badge = DayLoopUI.Chip(button.transform, "", DayLoopUI.Kit.Awning, Color.white, 20, "[DayLoop] Badge");
                RectTransform rect = (RectTransform)badge.transform.parent;
                rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                ContentSizeFitter fit = rect.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                DayLoopUI.Place(rect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                _badges[buttonName] = badge;
                return;
            }
        }
    }

    private void RefreshBadges()
    {
        if (!GameSession.IsActive) return;
        GameSaveData s = GameSession.Current;

        bool closed = GameSession.OperationCompletedToday || RestaurantRules.IsClosedForConstruction(s);
        SetBadge("Restaurant", RestaurantRules.IsClosedForConstruction(s) ? "공사 중" : GameSession.OperationCompletedToday ? "영업 끝" : "영업 가능",
            closed ? DayLoopUI.Kit.Sand : DayLoopUI.Kit.Awning, closed ? DayLoopUI.Kit.InkSoft : Color.white);

        string lab = null;
        if (s.progression.research.Count > 0)
        {
            int soonest = s.progression.research.Min(r => r.remainingDays);
            lab = soonest <= 0 ? "실습 가능" : $"{soonest}일";
        }
        SetBadge("Lab", lab, DayLoopUI.Kit.Sea, Color.white);

        SetBadge("House", s.progression.freeTime || RestaurantRules.IsClosedForConstruction(s) ? "취침 가능" : null, DayLoopUI.Kit.Night, Color.white);
    }

    private void SetBadge(string buttonName, string label, Color background, Color foreground)
    {
        if (!_badges.TryGetValue(buttonName, out TMP_Text badge) || badge == null) return;
        GameObject chip = badge.transform.parent.gameObject;
        chip.SetActive(!string.IsNullOrEmpty(label));
        if (string.IsNullOrEmpty(label)) return;
        badge.text = label;
        badge.color = foreground;
        chip.GetComponent<Image>().color = background;
    }

    // ───── 식당 카드 ─────

    /// <summary>레스토랑 버튼을 눌렀을 때 식당 카드를 띄운다. 섬 HUD가 없으면 false.</summary>
    public static bool TryShowRestaurantCard(Action onEnter)
    {
        if (_instance == null || _instance._canvas == null || !GameSession.IsActive) return false;
        _instance.ShowRestaurantCard(onEnter);
        return true;
    }

    private void ShowRestaurantCard(Action onEnter)
    {
        if (_restaurantCard != null) Destroy(_restaurantCard.gameObject);
        GameSaveData s = GameSession.Current;
        IngredientInventoryService inventory = IngredientInventoryService.Instance;

        RectTransform root = DayLoopUI.Rect("[DayLoop] RestaurantCard", _canvas.transform);
        DayLoopUI.Stretch(root);
        _restaurantCard = root;
        _canvas.sortingOrder = 160;   // 관리 막대(140) 위에 뜬다

        Button dim = DayLoopUI.Button(root, "", new Color(0.05f, 0.08f, 0.10f, 0.55f), Color.clear, 10, CloseRestaurantCard, "Dim");
        dim.image.sprite = null;
        dim.transition = Selectable.Transition.None;
        DayLoopUI.Stretch((RectTransform)dim.transform);

        Image card = DayLoopUI.Card(root, DayLoopUI.Kit.Sail, "Card", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(760, 0));
        VerticalLayoutGroup layout = DayLoopUI.Vertical(card, 0, 16);
        layout.padding = new RectOffset(34, 34, 28, 32);
        ContentSizeFitter fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        List<RecipeData> menus = new List<RecipeData>();
        foreach (int id in s.menuSlots)
            if (id >= 0 && GameDatabase.TryGetRecipe(id, out RecipeData recipe)) menus.Add(recipe);
        int servers = s.employees.Count(e => e.role == nameof(PartTimerRole.Serving));
        int chefs = s.employees.Count(e => e.role == nameof(PartTimerRole.Kitchen));

        List<string> warnings = new List<string>();
        if (menus.Count == 0) warnings.Add("등록된 메뉴가 없어요. 관리 > 메뉴에서 등록해 주세요.");
        if (servers == 0 || chefs == 0) warnings.Add("홀과 주방에 직원이 한 명 이상 있어야 해요.");
        if (inventory != null)
        {
            foreach (RecipeData menu in menus)
            {
                int portions = inventory.CraftableCount(menu);
                if (portions < 3) warnings.Add(portions == 0 ? $"{menu.RecipeName} 재료가 없어요." : $"{menu.RecipeName} 재료가 {portions}인분 남았어요.");
            }
        }

        RectTransform header = DayLoopUI.Rect("Header", card.transform);
        DayLoopUI.Horizontal(header, 0, 14);
        DayLoopUI.Layout(header, preferredHeight: 56);
        TMP_Text title = DayLoopUI.Text(header, "식당", 44, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(title, preferredWidth: 100, preferredHeight: 56);
        bool ready = menus.Count > 0 && servers > 0 && chefs > 0;
        DayLoopUI.Chip(header, ready ? "영업할 수 있어요" : "확인이 필요해요", ready ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sand, ready ? Color.white : DayLoopUI.Kit.WarnInk, 21);
        DayLoopUI.Spacer(header);

        RectTransform menuRow = DayLoopUI.Rect("Menus", card.transform);
        DayLoopUI.Horizontal(menuRow, 0, 10);
        DayLoopUI.Layout(menuRow, preferredHeight: 64);
        foreach (RecipeData menu in menus.Take(6)) DayLoopUI.FoodIcon(menuRow, menu, 60);
        TMP_Text summary = DayLoopUI.Text(menuRow, $"오늘의 메뉴 {menus.Count}개\n직원 홀 {servers}명 · 주방 {chefs}명", 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(summary, flexibleWidth: 1, preferredHeight: 64);

        foreach (string warning in warnings.Take(3))
        {
            TMP_Text line = DayLoopUI.Text(card.transform, warning, 21, DayLoopUI.Kit.WarnInk, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            DayLoopUI.Layout(line, preferredHeight: 28);
        }

        RectTransform buttons = DayLoopUI.Rect("Buttons", card.transform);
        DayLoopUI.Horizontal(buttons, 0, 14);
        DayLoopUI.Layout(buttons, preferredHeight: 82);
        Button close = DayLoopUI.KitButton(buttons, "닫기", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, 26, CloseRestaurantCard, "Close");
        DayLoopUI.Layout(close, preferredWidth: 150, preferredHeight: 74);
        Button enter = DayLoopUI.KitButton(buttons, "들어가서 영업 준비", DayLoopUI.Kit.Awning, Color.white, 30, () => { CloseRestaurantCard(); onEnter?.Invoke(); }, "Enter");
        DayLoopUI.Layout(enter, flexibleWidth: 1, preferredHeight: 74);
    }

    private void CloseRestaurantCard()
    {
        if (_restaurantCard != null) Destroy(_restaurantCard.gameObject);
        _restaurantCard = null;
        _canvas.sortingOrder = 30;
    }
}
