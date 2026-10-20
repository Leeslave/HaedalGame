using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 빌드에 포함된 섬과 식당의 공용 관리 창.
/// 왼쪽 나무 막대에 탭을 "식당 / 섬"으로 묶고, 메뉴 탭은 음식 카드, 집 탭은 미션·주급·직원 체력 카드로 보여 준다.
/// 섬에서는 오른쪽 아래 빠른 관리 막대로, 식당에서는 [관리] 버튼으로 연다.
/// 임금이 밀리면 "임금 정산" 화면으로 고정되고 정산 전에는 닫을 수 없다.
/// </summary>
public class RestaurantManagementUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform window, content;
    private TMP_Text message, title, info;
    private string tab = "메뉴";
    private int page;
    private readonly List<Button> openers = new List<Button>();
    private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
    private ResearchPractice practice;
    private static readonly string[] RestaurantTabs = { "메뉴", "재료", "직원", "가구", "식당" };
    private static readonly string[] IslandTabs = { "상점", "엘프", "연구", "대장간", "주민", "집" };
    private static readonly string[] DestructiveActions = { "제외", "해고", "철거", "취소·환불", "돌아가기" };

    private static string TabLabel(string section) => section == "엘프" ? "엘프 상점" : section;

    private void Start()
    {
        if (!GameSession.IsActive) return;
        GameSettings.ApplySaved();
        if (IngredientInventoryService.Instance == null) new GameObject("Ingredient inventory").AddComponent<IngredientInventoryService>();
        if (CurrencyManager.Instance == null) new GameObject("Currency wallets").AddComponent<CurrencyManager>();
        if (ElfShopManager.Instance == null) new GameObject("Elf shop").AddComponent<ElfShopManager>();
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name == "Lab") button.onClick.AddListener(() => Open("연구"));
            if (button.name == "House") button.onClick.AddListener(() => Open("집"));
        }
        canvas = DayLoopUI.CreateCanvas("Restaurant management", 140, gameObject.scene);
        if (gameObject.scene.name == GameScenes.Restaurant) BuildOpener();
        else BuildQuickBar();
        BuildWindow();
        window.gameObject.SetActive(false);
        if (!GameSession.Current.progression.firstToolChosen)
        {
            Open("대장간"); message.text = "첫 조리도구를 무료로 선택하세요. 만들 수 있는 기본 메뉴가 자동 등록돼요.";
        }
        else if (GameSession.Current.day == 1 && GameSession.Current.progression.freeTime)
        {
            Open("집"); message.text = "첫 영업을 마쳤어요. 자유시간을 보낸 뒤 취침하면 다음 날 아침으로 저장돼요.";
        }
    }

    // 식당: 하단에 운영 시작 버튼이 있으므로 관리 버튼은 그 위에 배치한다.
    private void BuildOpener()
    {
        var opener = DayLoopUI.KitButton(canvas.transform, "관리", DayLoopUI.Kit.Sea, Color.white, 28, () => Open("메뉴"), "Opener");
        DayLoopUI.Place((RectTransform)opener.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 124), new Vector2(170, 66));
        openers.Add(opener);
    }

    // 섬: 자주 여는 탭을 바로 여는 빠른 관리 막대
    private void BuildQuickBar()
    {
        var bar = DayLoopUI.Card(canvas.transform, DayLoopUI.Kit.Sail, "QuickBar", 3f, 6f);
        DayLoopUI.Place(bar.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 30), new Vector2(0, 104));
        var row = DayLoopUI.Horizontal(bar, 0, 10, TextAnchor.MiddleCenter);
        row.padding = new RectOffset(16, 16, 14, 20);
        var fit = bar.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (var section in new[] { "메뉴", "재료", "직원", "상점" })
        {
            string target = section;
            var b = DayLoopUI.KitButton(bar.transform, section, DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, 24, () => Open(target), section);
            DayLoopUI.Layout(b, preferredWidth: 104, preferredHeight: 66);
            openers.Add(b);
        }
        var all = DayLoopUI.KitButton(bar.transform, "전체", DayLoopUI.Kit.Wood, Color.white, 24, () => Open(tab), "All");
        DayLoopUI.Layout(all, preferredWidth: 104, preferredHeight: 66);
        openers.Add(all);
    }

    private void BuildWindow()
    {
        var frame = DayLoopUI.Card(canvas.transform, DayLoopUI.Kit.Sail, "ManagementWindow", 4f, 8f);
        window = frame.rectTransform;
        DayLoopUI.Place(window, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 6), new Vector2(1580, 930));

        // 왼쪽 탭 막대
        var rail = DayLoopUI.Panel(window, DayLoopUI.Kit.Wood, "Tabs");
        rail.rectTransform.anchorMin = new Vector2(0, 0);
        rail.rectTransform.anchorMax = new Vector2(0, 1);
        rail.rectTransform.pivot = new Vector2(0, .5f);
        rail.rectTransform.offsetMin = new Vector2(0, 0);
        rail.rectTransform.offsetMax = new Vector2(260, 0);
        var railLayout = DayLoopUI.Vertical(rail, 0, 4);
        railLayout.padding = new RectOffset(16, 16, 22, 22);
        var railTitle = DayLoopUI.Text(rail.transform, "관리", 38, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(railTitle, preferredHeight: 56);
        TabGroup(rail.transform, "식당", RestaurantTabs);
        TabGroup(rail.transform, "섬", IslandTabs);
        DayLoopUI.Spacer(rail.transform);
        TabButton(rail.transform, "설정");

        title = DayLoopUI.Text(window, "", 40, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(292, -22), new Vector2(560, 64));
        info = DayLoopUI.Text(window, "", 23, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        DayLoopUI.Place(info.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-112, -22), new Vector2(640, 64));
        var close = DayLoopUI.KitButton(window, "X", DayLoopUI.Kit.Awning, Color.white, 30, () => { if (PayrollPending) return; window.gameObject.SetActive(false); StopPractice(); }, "Close");
        DayLoopUI.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -22), new Vector2(64, 64));

        content = DayLoopUI.Rect("Rows", window);
        DayLoopUI.Place(content, new Vector2(0, 1), new Vector2(0, 1), new Vector2(292, -104), new Vector2(1258, 720));
        DayLoopUI.Vertical(content, 0, 10);
        message = DayLoopUI.Text(window, "", 23, DayLoopUI.Kit.SeaDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Place(message.rectTransform, new Vector2(0, 0), Vector2.zero, new Vector2(292, 22), new Vector2(1258, 70));
    }

    private void TabGroup(Transform rail, string label, string[] sections)
    {
        var heading = DayLoopUI.Text(rail, label, 19, new Color32(0xE9, 0xC9, 0xA4, 0xFF), TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        DayLoopUI.Layout(heading, preferredHeight: 38);
        foreach (var section in sections) TabButton(rail, section);
    }

    private void TabButton(Transform rail, string section)
    {
        var b = DayLoopUI.Button(rail, TabLabel(section), Color.clear, DayLoopUI.Kit.Sail, 25, () => Open(section), section);
        DayLoopUI.Chunky(b.image, 3f, 0f).GetComponent<Outline>().enabled = false;
        var label = b.GetComponentInChildren<TMP_Text>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.offsetMin = new Vector2(18, 0);
        DayLoopUI.Layout(b, preferredHeight: 50);
        tabButtons[section] = b;
    }

    private void PaintTabs()
    {
        foreach (var pair in tabButtons)
        {
            bool selected = pair.Key == tab;
            DayLoopUI.SetButtonColors(pair.Value, selected ? DayLoopUI.Kit.Sail : Color.clear, selected ? DayLoopUI.Kit.Line : DayLoopUI.Kit.Sail);
            pair.Value.GetComponent<Outline>().enabled = selected;
        }
    }

    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    private bool PayrollPending => GameSession.IsActive && GameSession.Current.progression.unpaidEmployeeIds.Count > 0;
    private void Update()
    {
        if (openers.Count == 0) return;
        if (PayrollPending && (tab != "임금 정산" || !window.gameObject.activeSelf)) Open("임금 정산");
        bool usable = RestaurantProgress.CanManage || GameFlow.Phase == GamePhase.Operation;
        foreach (var opener in openers) opener.interactable = usable;
        if (!RestaurantProgress.CanManage && GameFlow.Phase != GamePhase.Operation && window.gameObject.activeSelf) { window.gameObject.SetActive(false); StopPractice(); }
    }

    public void Open(string section)
    {
        if (window == null || (!RestaurantProgress.CanManage && !(GameFlow.Phase == GamePhase.Operation && section == "메뉴"))) return;
        if (PayrollPending) section = "임금 정산";
        tab = section; page = 0; StopPractice(); message.text = "";
        window.gameObject.SetActive(true); Refresh();
    }
    private void Clear()
    {
        StopPractice();
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var s = GameSession.Current;
        title.text = TabLabel(tab);
        info.text = $"{s.day}일차  ·  식당 Lv.{s.restaurantLevel}  ·  <color={DayLoopUI.Hex(DayLoopUI.Kit.GoldInk)}>{RestaurantProgress.Gold(GameSession.CaptureSnapshot()):N0}G</color>";
        PaintTabs();
    }
    private void Note(string value)
    {
        var text = DayLoopUI.Text(content, value, 23, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, preferredHeight: value.Contains("\n") ? 80 : 52);
    }
    private RectTransform Row(string label, params (string label, Action action)[] actions)
    {
        bool twoLines = label.Contains("\n");
        var row = DayLoopUI.Chunky(DayLoopUI.Panel(content, Color.white), 2f, 0f);
        DayLoopUI.Layout(row, preferredHeight: twoLines ? 90 : 72);
        var layout = DayLoopUI.Horizontal(row, 0, 10);
        layout.padding = new RectOffset(20, 14, 9, 13);
        var text = DayLoopUI.Text(row.transform, label, 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, flexibleWidth: 1, preferredHeight: twoLines ? 68 : 50);
        text.enableAutoSizing = true; text.fontSizeMin = 17; text.fontSizeMax = 23;
        foreach (var action in actions) ActionButton(row.transform, action.label, action.action, 135, 50);
        return row.rectTransform;
    }
    private void IconRow(Sprite icon, string label, params (string label, Action action)[] actions)
    {
        var row = Row(label, actions);
        if (icon == null) return;
        var image = DayLoopUI.Panel(row, Color.white, "Icon", false);
        image.sprite = icon; image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = false;
        image.transform.SetAsFirstSibling(); DayLoopUI.Layout(image, preferredWidth: 52, preferredHeight: 52);
    }
    private Button ActionButton(Transform parent, string label, Action action, float width, float height)
    {
        bool destructive = DestructiveActions.Contains(label);
        var b = DayLoopUI.KitButton(parent, label, destructive ? DayLoopUI.Kit.Sand : DayLoopUI.Kit.Sea, destructive ? DayLoopUI.Kit.WarnInk : Color.white, 21, () => action(), label);
        DayLoopUI.Layout(b, preferredWidth: width, preferredHeight: height);
        return b;
    }
    private delegate bool Command(out string error);
    private void Run(Command action)
    {
        bool ok = action(out string error);
        Refresh(); message.text = ok ? "반영했어요." : error;
    }
    private void Pages(int total, int pageSize = 6)
    {
        int last = Mathf.Max(0, (total - 1) / pageSize);
        Row($"{page + 1} / {last + 1} 페이지", ("이전", () => { page = Mathf.Max(0, page - 1); Refresh(); }), ("다음", () => { page = Mathf.Min(last, page + 1); Refresh(); }));
    }
    private void Refresh()
    {
        if (!GameSession.IsActive) return;
        Clear();
        var s = GameSession.Current;
        var inventory = IngredientInventoryService.Instance;
        switch (tab)
        {
            case "메뉴":
                Row($"오늘의 메뉴 {s.menuSlots.Count(x => x >= 0)} / {RestaurantRules.MenuLimit(s.restaurantLevel)} · 변경 간격 30초", ("자동 편성", () => Run(RestaurantProgress.TryAutoMenu)));
                MenuSlots(s);
                var recipes = GameDatabase.Recipes.Recipes.Where(r => s.unlockedRecipeIds.Contains(r.RecipeId)).ToList();
                MenuGrid(recipes.Skip(page * 8).Take(8), s, inventory);
                Pages(recipes.Count, 8); break;
            case "재료":
                Note("최고 등급 재료로 조리해요. 같은 등급 10개를 모으면 다음 등급 1개로 합칠 수 있어요.");
                var ingredients = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).ToList();
                foreach (var i in ingredients.Skip(page * 6).Take(6))
                    IconRow(i.Icon, $"{i.IngredientName} · {inventory.GetCount(i.IngredientId)}개 · 최고 {"FEDCBA"[inventory.HighestGrade(i.IngredientId)]}",
                        ("합성", () => { bool ok = inventory.PromoteAll(i.IngredientId); Refresh(); message.text = ok ? "합성했어요." : "같은 등급 재료 10개가 필요해요."; }));
                Pages(ingredients.Count); break;
            case "상점":
                Note("재료는 하루 50개까지 구매할 수 있어요. 기본 양념은 무제한이에요.");
                var stock = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).ToList();
                foreach (var i in stock.Skip(page * 6).Take(6))
                    IconRow(i.Icon, $"{i.IngredientName} · 10개 {RestaurantProgress.RegularUnitPrice(i) * 10:N0}G", ("10개 구매", () => Run((out string e) => RestaurantProgress.TryBuyIngredient(i.IngredientId, 10, out e))));
                Pages(stock.Count); break;
            case "연구":
                if (!RestaurantProgress.CanResearch) { Note("연구실은 영업을 마친 자유시간에 열립니다."); break; }
                Note($"연구 {s.progression.research.Count} / {RestaurantRules.ResearchSlots(s.restaurantLevel)} · 설계도 구매 → 연구 재료 투입 → 실습 4점 이상");
                var research = GameDatabase.Recipes.Recipes.Where(r => !s.unlockedRecipeIds.Contains(r.RecipeId)).ToList();
                foreach (var r in research.Skip(page * 6).Take(6))
                {
                    var pending = s.progression.research.Find(x => x.recipeId == r.RecipeId);
                    if (pending != null)
                        IconRow(r.Icon, $"{r.RecipeName} · 실습 준비 완료", ("실습", () => BeginChallenge(r.RecipeId)), ("취소·환불", () => ConfirmCancelResearch(r.RecipeId)));
                    else if (s.progression.blueprints.Contains(r.RecipeId))
                        IconRow(r.Icon, r.RecipeName + " · " + RecipeResearchPlan.CostLabel(r), ("연구 시작", () => Run((out string e) => RestaurantProgress.TryStartResearch(r.RecipeId, out e))));
                    else IconRow(r.Icon, $"{r.RecipeName} · 설계도 {Mathf.RoundToInt(r.Price * 1.2f):N0}G", ("구매", () => Run((out string e) => RestaurantProgress.TryBuyBlueprint(r.RecipeId, out e))));
                }
                Pages(research.Count); break;
            case "엘프":
                var elf = ElfShopManager.Instance;
                Note($"오늘의 {elf.CurrentElfType} 엘프 · 기본 가격의 80% · 하루 재고");
                var elfStock = elf.ElfStocks.ToList();
                foreach (var item in elfStock.Skip(page * 6).Take(6))
                {
                    string label = item.IngredientID.ToString();
                    if (item.ItemType == ElfShopItemType.Recipe && GameDatabase.TryGetRecipe(item.IngredientID, out var recipe)) label = recipe.RecipeName + " 설계도";
                    else if (GameDatabase.Recipes.TryGetIngredientById(item.IngredientID, out var ingredient)) label = ingredient.IngredientName;
                    Row($"{label} · {elf.GetElfUnitPrice(item)}G · 재고 {item.CurrentStock}", ("1개 구매", () => { bool ok = elf.PurchaseFromElf(item, 1); Refresh(); message.text = ok ? "구매했어요." : "재고 또는 골드가 부족해요."; }));
                }
                Pages(elfStock.Count); break;
            case "임금 정산":
                Note($"임금이 부족해요. 해고할 직원을 직접 선택한 뒤 남은 임금을 정산하세요.\n필요 {s.employees.Sum(e => e.wage):N0}G / 보유 {RestaurantProgress.Gold(s):N0}G · 미지급 해고 1명당 평점 -0.1");
                foreach (var employee in s.employees.Skip(page * 5).Take(5))
                {
                    string id = employee.instanceId;
                    Row($"{employee.name} [{employee.grade}] · 주급 {employee.wage:N0}G", ("해고", () => Run((out string e) => RestaurantProgress.TryDismiss(id, out e))));
                }
                Row("남은 직원 임금 정산", ("정산", () => { if (RestaurantProgress.TryPayWages(out string e)) Open("집"); else message.text = e; }));
                Pages(s.employees.Count, 5); break;
            case "직원":
                Row("스카우트 · 계약금은 직원 첫 주급과 같아요.", ("일반 100G", () => Run((out string e) => RestaurantProgress.TryScout(0, out e))), ("고급 500G", () => Run((out string e) => RestaurantProgress.TryScout(1, out e))), ("특급 2000G", () => Run((out string e) => RestaurantProgress.TryScout(2, out e))), ("최고 5000G", () => Run((out string e) => RestaurantProgress.TryScout(3, out e))));
                var employees = s.employees.Concat(s.progression.candidates).ToList();
                foreach (var employee in employees.Skip(page * 5).Take(5))
                {
                    string id = employee.instanceId;
                    string label = $"{employee.name} [{employee.grade}] · 주급 {employee.wage}G · {RoleLabel(employee.role)}\n요리 {employee.cooking:0} / 서빙 {employee.serving:0} / 솜씨 {employee.handy:0} / 체력 {employee.hp:0}";
                    if (s.progression.candidates.Contains(employee)) Row(label, ("계약", () => Run((out string e) => RestaurantProgress.TryHire(id, out e))));
                    else Row(label, ("주방", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.Kitchen, out e))), ("홀", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.Serving, out e))), ("대기", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.None, out e))), ("해고", () => Run((out string e) => RestaurantProgress.TryDismiss(id, out e))));
                }
                Pages(employees.Count, 5); break;
            case "식당":
                Note($"식당 Lv.{s.restaurantLevel} · 손님 {RestaurantRules.DailyVisitors(s.restaurantLevel)}명 · 좌석 {RestaurantRules.SeatLimit(s.restaurantLevel)}석 · 대기열 {RestaurantRules.WaitingLimit(s.restaurantLevel)}명 · 주방 {RestaurantRules.StaffLimit(s.restaurantLevel, PartTimerRole.Kitchen)} / 홀 {RestaurantRules.StaffLimit(s.restaurantLevel, PartTimerRole.Serving)}명 · 레시피 {RestaurantRules.GradeRange(s.restaurantLevel)}");
                Row(s.progression.pendingRestaurantLevel > 0 ? $"Lv.{s.progression.pendingRestaurantLevel} 확장 공사 중 · {s.progression.constructionUntilDay}일차 아침 완공" : "식당 확장과 조리도구 강화는 대장간에서 맡겨요.", ("대장간", () => Open("대장간")));
                Note($"식당 평점 {RestaurantRules.RestaurantRating(s):0.0} (최근 7일 평균)");
                RatingHistory(s);
                break;
            case "대장간":
                BlacksmithTab(s);
                break;
            case "주민":
                string island = new[] { "요정의 숲", "돌고래 바다", "해달 왕국", "곡물 섬", "목장 섬", "다시 만난 이웃들" }[Mathf.Clamp(s.progression.islandChapter, 0, 5)];
                Note($"{island} · 배 부품 {s.progression.boatParts.Count}/3 · 하루 1회");
                Row("섬에 찾아온 주민의 음식 요청을 들어주세요. 식재료는 상점에서 구매할 수 있어요.", ("주민 만나기", () => Run(RestaurantProgress.TryExplore)));
                if (GameDatabase.TryGetRecipe(s.progression.questRecipeId, out var requested))
                    Row($"주민의 부탁: {requested.RecipeName} 1인분 · 보상: 새 설계도", ("만들어 전달", () => Run(RestaurantProgress.TryDeliverQuest)));
                if (s.progression.boatParts.Count >= 3 && !s.progression.endingSeen)
                    Row("부품을 모았어요. 배를 수리하고 이웃들의 배웅을 받을까요?", ("배 수리·출항", () => Run(RestaurantProgress.TryEnding)));
                if (s.progression.endingSeen) Note("모든 부품을 모아 배를 고쳤어요. 해달식당의 여행은 계속됩니다.");
                if (s.progression.epilogueSeen) Note("다시 찾아온 주민들과 함께 식당의 새로운 하루를 맞이했어요.");
                break;
            case "가구":
                if (TablePlacementManager.Instance == null || TableSaveLoadManager.Instance == null) { Note("가구 배치는 식당 안에서 할 수 있어요."); break; }
                Note("배치 중 R 키로 90도 회전 · Esc 취소 · 식당 레벨별 좌석 한도 적용");
                Row("테이블 설치", ("1인석", () => PlaceTable(1)), ("2인석", () => PlaceTable(2)), ("4인석", () => PlaceTable(4)));
                Row("편집 기록", ("되돌리기", () => { TableSaveLoadManager.Instance.UndoEdit(); Refresh(); }), ("전체 복원", () => { TableSaveLoadManager.Instance.ResetEdits(); Refresh(); }));
                foreach (var table in PlacedTable.Active.ToList().Skip(page * 4).Take(4))
                    Row($"{table.tableData.tableType} · {table.anchorCell}",
                        ("이동·회전", () => { window.gameObject.SetActive(false); TablePlacementManager.Instance.EnterPlacementMode(); TablePlacementManager.Instance.StartMoving(table); }),
                        ("색상", () => { TableSaveLoadManager.Instance.RememberEdit(); table.SetSkin(1 - table.Skin); TableSaveLoadManager.Instance.SavePlacement(); Refresh(); }),
                        ("철거", () => { TableSaveLoadManager.Instance.RememberEdit(); table.RemoveTable(); TableSaveLoadManager.Instance.SavePlacement(); Refresh(); }));
                Row("", ("이전", () => { page = Mathf.Max(0, page - 1); Refresh(); }), ("다음", () => { page = Mathf.Min(Mathf.Max(0, (PlacedTable.Active.Count - 1) / 4), page + 1); Refresh(); }));
                break;
            case "집":
                HouseTab(s);
                break;
            case "설정":
                Row("소리·화면 설정", ("열기", () => SettingsWindow.Open(gameObject.scene)));
                Row("타이틀로 돌아가면 오늘의 저장하지 않은 진행을 잃어요.", ("돌아가기", () => { Clear(); Note("마지막 취침 이후 진행을 버리고 타이틀로 돌아갈까요?"); Row("", ("돌아가기", GameFlow.ReturnToTitle), ("계속하기", Refresh)); }));
                break;
        }
    }
    private void ConfirmCancelResearch(int id)
    {
        ManagementDialog.Show(canvas.transform, "정말 연구를 취소하시겠습니까?\n투입된 식재료는 모두 돌려받지만, 펼쳐둔 레시피 도안은 파손되어 사라집니다.", () => {
            if (RestaurantProgress.TryCancelResearch(id, out string error))
            { Refresh(); message.text = "도안이 파손되고 식재료가 가방으로 반환되었습니다."; ManagementDialog.RefundEffect(canvas.transform); }
            else message.text = error;
        });
    }

    // ───── 메뉴 탭 ─────

    private void MenuSlots(GameSaveData s)
    {
        var strip = DayLoopUI.Chunky(DayLoopUI.Panel(content, DayLoopUI.Kit.Sand, "Slots"), 3f, 0f);
        DayLoopUI.Layout(strip, preferredHeight: 84);
        var layout = DayLoopUI.Horizontal(strip, 0, 10);
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.childForceExpandWidth = true;
        foreach (int id in s.menuSlots)
        {
            if (id >= 0 && GameDatabase.TryGetRecipe(id, out var recipe))
            {
                var cell = DayLoopUI.Chunky(DayLoopUI.Panel(strip.transform, Color.white, "Slot"), 2f, 0f);
                DayLoopUI.Layout(cell, flexibleWidth: 1, preferredHeight: 64);
                var row = DayLoopUI.Horizontal(cell, 0, 8);
                row.padding = new RectOffset(10, 10, 6, 6);
                DayLoopUI.FoodIcon(cell.transform, recipe, 48);
                DayLoopUI.Layout(DayLoopUI.Text(cell.transform, recipe.RecipeName, 20, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 48);
            }
            else
            {
                var empty = DayLoopUI.Panel(strip.transform, new Color(1, 1, 1, .35f), "EmptySlot");
                DayLoopUI.Layout(empty, flexibleWidth: 1, preferredHeight: 64);
                var label = DayLoopUI.Text(empty.transform, "빈 슬롯", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
                DayLoopUI.Stretch(label.rectTransform);
            }
        }
    }

    private void MenuGrid(IEnumerable<RecipeData> recipes, GameSaveData s, IngredientInventoryService inventory)
    {
        var grid = DayLoopUI.Rect("Recipes", content);
        DayLoopUI.Layout(grid, preferredHeight: 438);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(298, 206);
        layout.spacing = new Vector2(14, 16);
        layout.padding = new RectOffset(2, 2, 6, 4);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        foreach (var r in recipes)
        {
            int id = r.RecipeId;
            bool selected = s.menuSlots.Contains(id);
            int portions = inventory != null ? inventory.CraftableCount(r) : -1;
            bool toolMissing = !RestaurantProgress.HasTool(s, r);

            var card = DayLoopUI.Chunky(DayLoopUI.Panel(grid, selected ? DayLoopUI.Kit.SeaTint : Color.white, "Recipe"), 3f, 4f);
            if (selected) card.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
            var column = DayLoopUI.Vertical(card, 0, 2, TextAnchor.UpperCenter);
            column.padding = new RectOffset(12, 12, 12, 14);
            column.childControlWidth = false;
            column.childForceExpandWidth = false;

            DayLoopUI.FoodIcon(card.transform, r, 70);
            var name = DayLoopUI.Text(card.transform, r.RecipeName, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.rectTransform.sizeDelta = new Vector2(270, 30);
            string meta = $"★{r.RecipeGrade} · {r.Price:N0}G" + (portions >= 0 ? $" · {portions}인분" : "");
            var metaText = DayLoopUI.Text(card.transform, meta, 18, portions == 0 ? DayLoopUI.Kit.WarnInk : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            metaText.rectTransform.sizeDelta = new Vector2(270, 26);
            DayLoopUI.Spacer(card.transform, 10, 6);
            var action = ActionButton(card.transform, selected ? "제외" : "등록", () => {
                int slot = selected ? GameSession.Current.menuSlots.IndexOf(id) : GameSession.Current.menuSlots.IndexOf(-1);
                Run((out string e) => RestaurantProgress.TrySetMenu(slot, selected ? -1 : id, out e));
            }, 150, 44);
            ((RectTransform)action.transform).sizeDelta = new Vector2(150, 44);

            string flag = selected ? "등록됨" : toolMissing ? "도구 필요" : portions == 0 ? "재료 부족" : null;
            if (flag != null)
            {
                var chip = DayLoopUI.Chip(card.transform, flag, selected ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand, selected ? Color.white : DayLoopUI.Kit.WarnInk, 17, "Flag");
                var chipRect = (RectTransform)chip.transform.parent;
                chipRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var chipFit = chipRect.gameObject.AddComponent<ContentSizeFitter>();
                chipFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                chipFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                DayLoopUI.Place(chipRect, new Vector2(1, 1), new Vector2(1, .5f), new Vector2(-10, 0), Vector2.zero);
            }
        }
    }

    // ───── 대장간 탭 ─────

    private static readonly string[] ToolNames = { "프라이팬", "도마", "튀김기", "냄비" };

    private void BlacksmithTab(GameSaveData s)
    {
        int level = s.restaurantLevel;
        int pending = s.progression.pendingRestaurantLevel;
        bool isMax = level >= RestaurantRules.MaxLevel;
        int next = Mathf.Min(level + 1, RestaurantRules.MaxLevel);

        var top = DayLoopUI.Rect("Restaurant level", content);
        DayLoopUI.Layout(top, preferredHeight: 338);
        var topLayout = DayLoopUI.Horizontal(top, 0, 18);
        topLayout.childForceExpandHeight = true;
        topLayout.padding = new RectOffset(0, 0, 0, 8);

        // 왼쪽: 현재 → 다음 레벨, 강화 조건, 강화 버튼, 공사 안내
        var upgrade = BlacksmithCard(top, "식당 레벨 강화", 560);
        var levels = DayLoopUI.Text(upgrade, isMax ? $"Lv.{level}  ·  최고 레벨" : pending > 0 ? $"Lv.{level}  →  Lv.{pending}  공사 중" : $"Lv.{level}  →  Lv.{next}",
            36, pending > 0 ? DayLoopUI.Kit.SeaDeep : DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(levels, preferredHeight: 46);

        if (pending > 0)
        {
            bool closedToday = RestaurantRules.IsClosedForConstruction(s);
            string when = closedToday
                ? $"오늘은 공사로 휴업해요. 집에서 잠들면 {s.progression.constructionUntilDay}일차 아침에 Lv.{pending} 식당이 문을 열어요."
                : $"내일({s.day + 1}일차) 하루 공사로 휴업하고, {s.progression.constructionUntilDay}일차 아침에 Lv.{pending} 식당이 문을 열어요.";
            var status = DayLoopUI.Text(upgrade, when, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DayLoopUI.Layout(status, preferredHeight: 96);
        }
        else if (!isMax)
        {
            float rating = Mathf.Round(RestaurantRules.RestaurantRating(s) * 10f) / 10f;
            float needRating = RestaurantRules.UpgradeRating(next);
            int gold = RestaurantProgress.Gold(GameSession.CaptureSnapshot());
            int cost = RestaurantRules.UpgradeCost(next);
            bool ratingMet = rating >= needRating, goldMet = gold >= cost;
            ConditionRow(upgrade, "식당 평점", $"{rating:0.0} / {needRating:0.0}", ratingMet);
            ConditionRow(upgrade, "공사 비용", $"{gold:N0} / {cost:N0}G", goldMet);
            DayLoopUI.Spacer(upgrade);
            var button = DayLoopUI.KitButton(upgrade, $"Lv.{next}로 강화", ratingMet && goldMet ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand,
                ratingMet && goldMet ? Color.white : DayLoopUI.Kit.InkSoft, 28, () => Run(RestaurantProgress.TryUpgrade), "Upgrade");
            DayLoopUI.Layout(button, preferredHeight: 56);
            string closed = s.progression.freeTime ? "지금 맡기면 내일 하루 쉬어요" : "지금 맡기면 오늘 하루 쉬어요";
            var note = DayLoopUI.Text(upgrade, $"공사 기간 {RestaurantRules.ConstructionDays(next)}일 (휴업) · {closed}", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            DayLoopUI.Layout(note, preferredHeight: 26);
        }
        else
        {
            DayLoopUI.Layout(DayLoopUI.Text(upgrade, "모든 확장을 마쳤어요.", 22, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 40);
        }

        // 오른쪽: 레벨 혜택 (현재 / 다음)
        int target = pending > 0 ? pending : next;
        bool hasNext = pending > 0 || !isMax;
        var benefits = BlacksmithCard(top, "레벨 혜택", -1);
        BenefitRow(benefits, "", $"현재 Lv.{level}", hasNext ? $"다음 Lv.{target}" : "-", true);
        BenefitRow(benefits, "손님 좌석", $"{RestaurantRules.SeatLimit(level)}개", hasNext ? Delta(RestaurantRules.SeatLimit(level), RestaurantRules.SeatLimit(target), "개") : "-");
        BenefitRow(benefits, "요리 공간", $"{RestaurantRules.CookingSpace(level)}개", hasNext ? Delta(RestaurantRules.CookingSpace(level), RestaurantRules.CookingSpace(target), "개") : "-");
        BenefitRow(benefits, "대기열", $"{RestaurantRules.WaitingLimit(level)}명", hasNext ? Delta(RestaurantRules.WaitingLimit(level), RestaurantRules.WaitingLimit(target), "명") : "-");
        BenefitRow(benefits, "레시피 등급", RestaurantRules.GradeRange(level), hasNext ? RestaurantRules.GradeRange(target) : "-");
        BenefitRow(benefits, "하루 방문 손님", $"{RestaurantRules.DailyVisitors(level)}명", hasNext ? Delta(RestaurantRules.DailyVisitors(level), RestaurantRules.DailyVisitors(target), "명") : "-");

        // 아래: 조리도구 선택·강화
        var toolsTitle = DayLoopUI.Text(content, "조리도구 강화", 26, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(toolsTitle, preferredHeight: 36);
        for (int id = 201; id <= 204; id++)
        {
            int tool = id;
            string state = s.progression.tools.Contains(id) ? $" · Lv.{CookwareRules.Level(s, id)}" : s.progression.firstToolChosen ? " · 500G" : " · 첫 도구 무료";
            Row(ToolNames[id - 201] + state,
                ("선택·구매", () => Run((out string e) => RestaurantProgress.TryBuyTool(tool, out e))),
                ("강화", () => Run((out string e) => RestaurantProgress.TryUpgradeTool(tool, out e))));
        }
    }

    private static RectTransform BlacksmithCard(Transform parent, string heading, float width)
    {
        var card = DayLoopUI.Chunky(DayLoopUI.Panel(parent, DayLoopUI.Kit.Sail, heading), 3f, 5f);
        if (width > 0) DayLoopUI.Layout(card, preferredWidth: width);
        else DayLoopUI.Layout(card, flexibleWidth: 1);
        var layout = DayLoopUI.Vertical(card, 0, 6);
        layout.padding = new RectOffset(24, 24, 16, 18);
        DayLoopUI.Layout(DayLoopUI.Text(card.transform, heading, 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 36);
        return card.rectTransform;
    }

    private static void ConditionRow(Transform parent, string label, string value, bool met)
    {
        var row = DayLoopUI.Chunky(DayLoopUI.Panel(parent, Color.white, label), 2f, 0f);
        DayLoopUI.Layout(row, preferredHeight: 46);
        var layout = DayLoopUI.Horizontal(row, 0, 12);
        layout.padding = new RectOffset(16, 12, 6, 6);
        DayLoopUI.Layout(DayLoopUI.Text(row.transform, label, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 40);
        DayLoopUI.Layout(DayLoopUI.Text(row.transform, value, 22, met ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.WarnInk, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 220, preferredHeight: 40);
        DayLoopUI.Chip(row.transform, met ? "충족" : "부족", met ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sand, met ? Color.white : DayLoopUI.Kit.WarnInk, 18);
    }

    private static void BenefitRow(Transform parent, string label, string current, string next, bool header = false)
    {
        var row = DayLoopUI.Rect(string.IsNullOrEmpty(label) ? "Header" : label, parent);
        DayLoopUI.Horizontal(row, 0, 10);
        DayLoopUI.Layout(row, preferredHeight: header ? 30 : 34);
        Color soft = DayLoopUI.Kit.InkSoft;
        DayLoopUI.Layout(DayLoopUI.Text(row, label, 22, soft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 34);
        DayLoopUI.Layout(DayLoopUI.Text(row, current, header ? 19 : 22, header ? soft : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 150, preferredHeight: 34);
        DayLoopUI.Layout(DayLoopUI.Text(row, next, header ? 19 : 22, header ? soft : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 210, preferredHeight: 34);
    }

    /// <summary>"16개 (+4)" 형식. 늘지 않으면 값만.</summary>
    private static string Delta(int current, int next, string unit)
    {
        int delta = next - current;
        return delta > 0 ? $"{next}{unit} <color={DayLoopUI.Hex(DayLoopUI.Kit.GoodInk)}>(+{delta})</color>" : $"{next}{unit}";
    }

    // ───── 집 탭 ─────

    private void HouseTab(GameSaveData s)
    {
        Mission(s, out string missionName, out int target);
        int gold = RestaurantProgress.Gold(GameSession.CaptureSnapshot());
        int wages = s.employees.Sum(e => e.wage);
        int daysToWage = (7 - s.day % 7) % 7;

        var top = DayLoopUI.Rect("Top", content);
        DayLoopUI.Layout(top, preferredHeight: 214);
        var topLayout = DayLoopUI.Horizontal(top, 0, 18);
        topLayout.childForceExpandWidth = true;
        topLayout.childForceExpandHeight = true;
        topLayout.padding = new RectOffset(0, 0, 0, 8);

        var mission = HouseCard(top, $"미션 · {missionName}");
        var progressRow = DayLoopUI.Rect("Progress", mission);
        DayLoopUI.Horizontal(progressRow, 0, 10);
        DayLoopUI.Layout(progressRow, preferredHeight: 30);
        DayLoopUI.Layout(DayLoopUI.Text(progressRow, "진행", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        bool done = s.progression.missionProgress >= target;
        DayLoopUI.Layout(DayLoopUI.Text(progressRow, $"{s.progression.missionProgress:N0} / {target:N0}", 22, done ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 220);
        var bar = DayLoopUI.Bar(mission, target > 0 ? (float)s.progression.missionProgress / target : 0f, done ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sea);
        DayLoopUI.Layout(bar.transform.parent, preferredHeight: 16);
        DayLoopUI.Spacer(mission);
        var claim = DayLoopUI.KitButton(mission, $"보상 받기 · {200 + s.progression.missionIndex * 100:N0}G", done ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand, done ? Color.white : DayLoopUI.Kit.InkSoft, 23, () => Run(RestaurantProgress.TryClaimMission), "Claim");
        DayLoopUI.Layout(claim, preferredHeight: 54);

        var wage = HouseCard(top, "다음 주급");
        var amountRow = DayLoopUI.Rect("Amount", wage);
        DayLoopUI.Horizontal(amountRow, 0, 12, TextAnchor.LowerLeft);
        DayLoopUI.Layout(amountRow, preferredHeight: 52);
        var amount = DayLoopUI.Text(amountRow, $"{wages:N0}G", 44, DayLoopUI.Kit.GoldInk, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        amount.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(amount, preferredHeight: 52);
        DayLoopUI.Layout(DayLoopUI.Text(amountRow, (daysToWage == 0 ? "오늘 밤 지급" : $"{daysToWage}일 뒤") + " · 7일마다", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.BottomLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 44);
        bool enough = gold >= wages;
        var wageNote = DayLoopUI.Text(wage, enough ? "보유 골드로 충분해요" : $"골드가 {wages - gold:N0}G 부족해요", 21, enough ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.WarnInk, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(wageNote, preferredHeight: 32);

        // 직원 체력
        var staff = DayLoopUI.Chunky(DayLoopUI.Panel(content, DayLoopUI.Kit.Sail, "Staff"), 3f, 0f);
        int shown = Mathf.Min(4, s.employees.Count);
        DayLoopUI.Layout(staff, preferredHeight: 86 + Mathf.Max(1, shown) * 46 + (s.employees.Count > shown ? 30 : 0));
        var staffLayout = DayLoopUI.Vertical(staff, 0, 8);
        staffLayout.padding = new RectOffset(24, 24, 16, 16);
        var staffHeader = DayLoopUI.Rect("Header", staff.transform);
        DayLoopUI.Horizontal(staffHeader, 0, 10);
        DayLoopUI.Layout(staffHeader, preferredHeight: 36);
        DayLoopUI.Layout(DayLoopUI.Text(staffHeader, "직원 체력", 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(staffHeader, "자고 나면 모두 회복해요", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 360);
        if (s.employees.Count == 0)
            DayLoopUI.Layout(DayLoopUI.Text(staff.transform, "아직 직원이 없어요. 직원 탭에서 스카우트할 수 있어요.", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 38);
        foreach (var e in s.employees.Take(shown))
        {
            var row = DayLoopUI.Rect("Employee", staff.transform);
            DayLoopUI.Horizontal(row, 0, 14);
            DayLoopUI.Layout(row, preferredHeight: 38);
            DayLoopUI.Layout(DayLoopUI.Text(row, $"{e.name} <color={DayLoopUI.Hex(DayLoopUI.Kit.InkSoft)}>· {RoleLabel(e.role)} · {e.grade}등급</color>", 21, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
            float ratio = e.stamina < 0 ? 1f : Mathf.Clamp01(e.stamina / Mathf.Max(1f, e.hp));
            var stamina = DayLoopUI.Bar(row, ratio, ratio > .6f ? DayLoopUI.Kit.Kelp : ratio > .3f ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Alert);
            DayLoopUI.Layout(stamina.transform.parent, preferredWidth: 300, preferredHeight: 16);
            DayLoopUI.Layout(DayLoopUI.Text(row, $"{Mathf.RoundToInt(ratio * 100)}%", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 64);
        }
        if (s.employees.Count > shown)
            DayLoopUI.Layout(DayLoopUI.Text(staff.transform, $"외 {s.employees.Count - shown}명", 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 24);

        // 취침
        var bottom = DayLoopUI.Rect("Sleep", content);
        DayLoopUI.Horizontal(bottom, 0, 20);
        DayLoopUI.Layout(bottom, preferredHeight: 104);
        string status = s.progression.freeTime ? "오늘 영업을 마쳤어요. 잠들면 지급일에 주급을 지급하고 다음 날로 넘어가요." : "영업 후 또는 공사 중에 잠들 수 있어요.";
        DayLoopUI.Layout(DayLoopUI.Text(bottom, $"{status}\n<size=85%><color={DayLoopUI.Hex(DayLoopUI.Kit.InkSoft)}>파일 저장은 취침할 때 이루어져요. 그전에 종료하면 마지막으로 저장한 아침으로 돌아가요.</color></size>", 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 96);
        var sleep = DayLoopUI.KitButton(bottom, $"취침 · 저장\n<size=60%>{s.day + 1}일차 아침으로</size>", DayLoopUI.Kit.Night, Color.white, 32, () => { if (!RestaurantProgress.TrySleep(out string error)) message.text = error; }, "Sleep");
        DayLoopUI.Layout(sleep, preferredWidth: 340, preferredHeight: 92);
    }

    /// <summary>현재 미션 이름과 목표치. (보상 조건은 RestaurantProgress.TryClaimMission과 같다)</summary>
    public static void Mission(GameSaveData s, out string name, out int target)
    {
        int type = s.progression.missionIndex % 3;
        name = new[] { "손님 대접", "매출 달성", "새 요리 연구" }[type];
        target = type == 1 ? RestaurantProgress.MissionTarget * 100 : type == 2 ? 1 : RestaurantProgress.MissionTarget;
    }

    private static RectTransform HouseCard(Transform parent, string heading)
    {
        var card = DayLoopUI.Chunky(DayLoopUI.Panel(parent, DayLoopUI.Kit.Sail, heading), 3f, 5f);
        var layout = DayLoopUI.Vertical(card, 0, 8);
        layout.padding = new RectOffset(24, 24, 16, 18);
        DayLoopUI.Layout(DayLoopUI.Text(card.transform, heading, 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 38);
        return card.rectTransform;
    }

    private static string RoleLabel(string role) => role == nameof(PartTimerRole.Serving) ? "홀" : role == nameof(PartTimerRole.Kitchen) ? "주방" : "대기";

    private void PlaceTable(int seats)
    {
        window.gameObject.SetActive(false);
        var placement = TablePlacementManager.Instance;
        placement.EnterPlacementMode();
        if (seats == 1) placement.OnOneSeatButtonClicked();
        else if (seats == 2) placement.OnTwoSeatButtonClicked();
        else placement.OnFourSeatButtonClicked();
    }
    private void RatingHistory(GameSaveData data)
    {
        var row = DayLoopUI.Rect("Rating history", content);
        DayLoopUI.Layout(row, preferredHeight: 105);
        DayLoopUI.Horizontal(row, 4, 12, TextAnchor.LowerLeft);
        foreach (var entry in data.dailyRatingHistory.Where(x => x.day >= data.day - 7).Take(7))
        {
            var bar = DayLoopUI.Chunky(DayLoopUI.Panel(row, entry.value <= 1.3f ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sea), 2f, 0f);
            DayLoopUI.Layout(bar, preferredWidth: 145, preferredHeight: Mathf.Max(35, entry.value * 20));
            var label = DayLoopUI.Text(bar.transform, $"{entry.day}일 · {entry.value:0.0}", 20, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            DayLoopUI.Stretch(label.rectTransform);
        }
        if (data.dailyRatingHistory.Count > 0 && data.dailyRatingHistory.Average(x => x.value) <= 1.3f)
            message.text = "평점이 낮아요. 재료 등급과 대기 시간, 쓰레기를 확인하세요.";
    }
    private void StopPractice()
    {
        if (practice == null) return;
        practice.enabled = false;
        Destroy(practice);
        practice = null;
    }
    private void BeginChallenge(int id)
    {
        if (!RestaurantProgress.CanResearch) { message.text = "실습은 자유시간에 할 수 있어요."; return; }
        var research = GameSession.Current.progression.research.Find(x => x.recipeId == id);
        if (research == null || research.remainingDays > 0) { message.text = "연구 시간이 남았어요."; return; }
        if (!GameDatabase.TryGetRecipe(id, out var recipe)) return;
        Clear();
        title.text = "연구 실습";
        practice = gameObject.AddComponent<ResearchPractice>();
        practice.Begin(content, recipe, score => {
            bool ok = RestaurantProgress.TryCompleteResearch(id, score, out string error);
            Refresh();
            message.text = $"실습 평균 {score:0.0}/5점 · " + (ok ? "새 요리를 배웠어요." : error);
        });
    }
}