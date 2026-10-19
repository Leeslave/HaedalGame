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

        BuildPlaque();
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
        Image plaque = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "DayPlaque", 3f, 6f);
        plaque.raycastTarget = false;
        DayLoopUI.Place(plaque.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -24), Vector2.zero);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(plaque, 0, 0);
        row.childForceExpandHeight = true;
        ContentSizeFitter fit = plaque.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image dayBlock = DayLoopUI.Panel(plaque.transform, DayLoopUI.Kit.Wood, "Day");
        dayBlock.raycastTarget = false;
        DayLoopUI.Layout(dayBlock, preferredWidth: 116, preferredHeight: 134);
        VerticalLayoutGroup dayLayout = DayLoopUI.Vertical(dayBlock, 0, 0, TextAnchor.MiddleCenter);
        dayLayout.padding = new RectOffset(8, 8, 14, 14);
        _day = DayLoopUI.Text(dayBlock.transform, "", 54, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(_day, preferredHeight: 62);
        TMP_Text dayUnit = DayLoopUI.Text(dayBlock.transform, "일차", 22, new Color32(0xF6, 0xDE, 0xC4, 0xFF), TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(dayUnit, preferredHeight: 28);

        RectTransform right = DayLoopUI.Rect("Info", plaque.transform);
        VerticalLayoutGroup rightLayout = DayLoopUI.Vertical(right, 0, 10, TextAnchor.MiddleLeft);
        rightLayout.padding = new RectOffset(22, 26, 14, 14);
        rightLayout.childControlWidth = true;
        rightLayout.childForceExpandWidth = false;

        RectTransform steps = DayLoopUI.Rect("Steps", right);
        DayLoopUI.Horizontal(steps, 0, 0, TextAnchor.UpperLeft);
        string[] names = { "아침", "영업", "밤" };
        for (int i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                Image connector = DayLoopUI.Panel(steps, DayLoopUI.Kit.Divider, "Connector", false);
                connector.raycastTarget = false;
                DayLoopUI.Layout(connector, preferredWidth: 48, preferredHeight: 6);
            }
            RectTransform step = DayLoopUI.Rect(names[i], steps);
            DayLoopUI.Vertical(step, 0, 2, TextAnchor.UpperCenter).childControlWidth = false;
            DayLoopUI.Layout(step, preferredWidth: 58, preferredHeight: 62);
            Image dot = DayLoopUI.Chunky(DayLoopUI.Panel(step, DayLoopUI.Kit.Sail, "Dot"), 3f, 0f);
            dot.raycastTarget = false;
            dot.rectTransform.sizeDelta = new Vector2(32, 32);
            DayLoopUI.Layout(dot, preferredHeight: 32);
            TMP_Text label = DayLoopUI.Text(step, names[i], 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.sizeDelta = new Vector2(58, 24);
            _stepDots[i] = dot;
            _stepLabels[i] = label;
        }

        _stats = DayLoopUI.Text(right, "", 27, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        _stats.textWrappingMode = TextWrappingModes.NoWrap;
        _stats.overflowMode = TextOverflowModes.Overflow;
        DayLoopUI.Layout(_stats, preferredHeight: 34);

        // 미션 진행 (명판 아래)
        Image mission = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "Mission", 3f, 4f);
        mission.raycastTarget = false;
        DayLoopUI.Place(mission.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -186), new Vector2(400, 84));
        VerticalLayoutGroup missionLayout = DayLoopUI.Vertical(mission, 0, 8);
        missionLayout.padding = new RectOffset(20, 20, 12, 14);
        _mission = DayLoopUI.Text(mission.transform, "", 21, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_mission, preferredHeight: 26);
        _missionBar = DayLoopUI.Bar(mission.transform, 0f, DayLoopUI.Kit.Sea);
        DayLoopUI.Layout(_missionBar.transform.parent, preferredHeight: 14);
    }

    private void Refresh()
    {
        if (_day == null) return;
        int day = GameSession.IsActive ? GameSession.Current.day : (InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 1);
        int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrencyById(GameSession.GoldCurrencyId) : -1;
        _day.text = day.ToString();

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
            if (last != null && last.day > 0) stats += $"   <color={soft}>평점</color> {DayLoopUI.Rating(last.totalRating)}";
            stats += $"   <color={soft}>Lv.{GameSession.Current.restaurantLevel}</color>";

            RestaurantManagementUI.Mission(GameSession.Current, out string missionName, out int target);
            int progress = GameSession.Current.progression.missionProgress;
            _mission.text = $"미션 · {missionName} <color={soft}>{progress:N0} / {target:N0}</color>";
            DayLoopUI.SetBar(_missionBar, target > 0 ? (float)progress / target : 0f);
            _missionBar.color = progress >= target ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sea;
        }
        _mission.transform.parent.gameObject.SetActive(GameSession.IsActive);
        _stats.text = stats;

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

        SetBadge("Restaurant", GameSession.OperationCompletedToday ? "영업 끝" : "영업 가능",
            GameSession.OperationCompletedToday ? DayLoopUI.Kit.Sand : DayLoopUI.Kit.Awning,
            GameSession.OperationCompletedToday ? DayLoopUI.Kit.InkSoft : Color.white);

        string lab = null;
        if (s.progression.research.Count > 0)
        {
            int soonest = s.progression.research.Min(r => r.remainingDays);
            lab = soonest <= 0 ? "실습 가능" : $"{soonest}일";
        }
        SetBadge("Lab", lab, DayLoopUI.Kit.Sea, Color.white);

        SetBadge("House", s.progression.freeTime ? "취침 가능" : null, DayLoopUI.Kit.Night, Color.white);
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
