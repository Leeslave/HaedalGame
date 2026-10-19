using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>빌드에 포함된 섬과 식당의 공용 관리 창.</summary>
public class RestaurantManagementUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform window, content;
    private TMP_Text message, title;
    private string tab = "메뉴";
    private int page;
    private Button opener;
    private ResearchPractice practice;
    private static readonly string[] Tabs = { "메뉴", "재료", "상점", "엘프", "연구", "직원", "식당", "가구", "탐험", "집", "설정" };

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
        opener = DayLoopUI.Button(canvas.transform, "관리", DayLoopUI.Sea, Color.white, 26, () => Open("메뉴"));
        // 식당 하단에는 운영 시작 버튼이 있으므로 관리 버튼은 그 위에 배치한다.
        float openerBottom = gameObject.scene.name == GameScenes.Restaurant ? 120f : 28f;
        DayLoopUI.Place((RectTransform)opener.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, openerBottom), new Vector2(160, 62));
        window = DayLoopUI.Panel(canvas.transform, DayLoopUI.Cream, "ManagementWindow").rectTransform;
        DayLoopUI.Place(window, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1500, 900));
        title = DayLoopUI.Text(window, "해달식당", 32, DayLoopUI.Ink);
        DayLoopUI.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -20), new Vector2(1240, 60));
        var close = DayLoopUI.Button(window, "닫기", DayLoopUI.Wood, Color.white, 24, () => { window.gameObject.SetActive(false); StopPractice(); });
        DayLoopUI.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(130, 55));
        for (int i = 0; i < Tabs.Length; i++)
        {
            string section = Tabs[i];
            var b = DayLoopUI.Button(window, section, DayLoopUI.CreamDeep, DayLoopUI.Ink, 25, () => Open(section));
            DayLoopUI.Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, -100 - i * 66), new Vector2(170, 56));
        }
        content = DayLoopUI.Rect("Rows", window);
        DayLoopUI.Place(content, new Vector2(0, 1), new Vector2(0, 1), new Vector2(220, -105), new Vector2(1250, 690));
        DayLoopUI.Vertical(content, 0, 10);
        message = DayLoopUI.Text(window, "", 23, DayLoopUI.Sea);
        DayLoopUI.Place(message.rectTransform, new Vector2(0, 0), Vector2.zero, new Vector2(220, 28), new Vector2(1220, 72));
        window.gameObject.SetActive(false);
        if (!GameSession.Current.progression.firstToolChosen)
        {
            Open("식당"); message.text = "첫 조리도구를 무료로 선택하세요. 만들 수 있는 기본 메뉴가 자동 등록돼요.";
        }
        else if (GameSession.Current.day == 1 && GameSession.Current.progression.freeTime)
        {
            Open("집"); message.text = "첫 영업을 마쳤어요. 자유시간을 보낸 뒤 취침하면 다음 날 아침으로 저장돼요.";
        }
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    private void Update()
    {
        if (opener == null) return;
        opener.interactable = RestaurantProgress.CanManage || GameFlow.Phase == GamePhase.Operation;
        if (!RestaurantProgress.CanManage && GameFlow.Phase != GamePhase.Operation && window.gameObject.activeSelf) { window.gameObject.SetActive(false); StopPractice(); }
    }

    public void Open(string section)
    {
        if (window == null || (!RestaurantProgress.CanManage && !(GameFlow.Phase == GamePhase.Operation && section == "메뉴"))) return;
        tab = section; page = 0; StopPractice(); message.text = "";
        window.gameObject.SetActive(true); Refresh();
    }
    private void Clear()
    {
        StopPractice();
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var s = GameSession.Current;
        title.text = $"{tab}  ·  {s.day}일차  ·  Lv.{s.restaurantLevel}  ·  {RestaurantProgress.Gold(GameSession.CaptureSnapshot()):N0}G";
    }
    private void Note(string value)
    {
        var text = DayLoopUI.Text(content, value, 24, DayLoopUI.InkSoft);
        DayLoopUI.Layout(text, preferredHeight: 55);
    }
    private void Row(string label, params (string label, Action action)[] actions)
    {
        var row = DayLoopUI.Panel(content, DayLoopUI.CreamDeep);
        DayLoopUI.Layout(row, preferredHeight: 67);
        DayLoopUI.Horizontal(row, 8, 8);
        var text = DayLoopUI.Text(row.transform, label, 23, DayLoopUI.Ink);
        DayLoopUI.Layout(text, flexibleWidth: 1, preferredHeight: 50);
        foreach (var action in actions)
        {
            var b = DayLoopUI.Button(row.transform, action.label, DayLoopUI.Sea, Color.white, 21, () => action.action());
            DayLoopUI.Layout(b, preferredWidth: 135, preferredHeight: 48);
        }
    }
    private delegate bool Command(out string error);
    private void Run(Command action)
    {
        bool ok = action(out string error);
        Refresh(); message.text = ok ? "반영했어요." : error;
    }
    private void Pages(int total)
    {
        int last = Mathf.Max(0, (total - 1) / 6);
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
                Row($"메뉴 {s.menuSlots.Count(x => x >= 0)} / {RestaurantRules.MenuLimit(s.restaurantLevel)} · 변경 간격 30초", ("자동 편성", () => Run(RestaurantProgress.TryAutoMenu)));
                var recipes = GameDatabase.Recipes.Recipes.Where(r => s.unlockedRecipeIds.Contains(r.RecipeId)).ToList();
                foreach (var r in recipes.Skip(page * 6).Take(6))
                {
                    int id = r.RecipeId;
                    bool selected = s.menuSlots.Contains(id);
                    Row($"{(selected ? "✓ " : "")}{r.RecipeName} · {r.Price:N0}G · {inventory.CraftableCount(r)}인분",
                        (selected ? "제외" : "등록", () => {
                            int slot = selected ? GameSession.Current.menuSlots.IndexOf(id) : GameSession.Current.menuSlots.IndexOf(-1);
                            Run((out string e) => RestaurantProgress.TrySetMenu(slot, selected ? -1 : id, out e));
                        }));
                }
                Pages(recipes.Count); break;
            case "재료":
                Note("최고 등급 재료로 조리해요. 같은 등급 10개를 모으면 다음 등급 1개로 합칠 수 있어요.");
                var ingredients = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).ToList();
                foreach (var i in ingredients.Skip(page * 6).Take(6))
                    Row($"{i.IngredientName} · {inventory.GetCount(i.IngredientId)}개 · 최고 {"FEDCBA"[inventory.HighestGrade(i.IngredientId)]}",
                        ("합성", () => { bool ok = inventory.PromoteAll(i.IngredientId); Refresh(); message.text = ok ? "합성했어요." : "같은 등급 재료 10개가 필요해요."; }));
                Pages(ingredients.Count); break;
            case "상점":
                Note("재료는 하루 50개까지 구매할 수 있어요. 기본 양념은 무제한이에요.");
                var stock = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).ToList();
                foreach (var i in stock.Skip(page * 6).Take(6))
                    Row($"{i.IngredientName} · 10개 {RestaurantProgress.RegularUnitPrice(i) * 10:N0}G", ("10개 구매", () => Run((out string e) => RestaurantProgress.TryBuyIngredient(i.IngredientId, 10, out e))));
                Pages(stock.Count); break;
            case "연구":
                Note($"연구 {s.progression.research.Count} / {RestaurantRules.ResearchSlots(s.restaurantLevel)} · 설계도 구매 → 연구 재료 투입 → 실습 4점 이상");
                var research = GameDatabase.Recipes.Recipes.Where(r => !s.unlockedRecipeIds.Contains(r.RecipeId)).ToList();
                foreach (var r in research.Skip(page * 6).Take(6))
                {
                    var pending = s.progression.research.Find(x => x.recipeId == r.RecipeId);
                    if (pending != null)
                        Row($"{r.RecipeName} · 남은 {pending.remainingDays}일", ("실습", () => BeginChallenge(r.RecipeId)), ("취소·환불", () => Run((out string e) => RestaurantProgress.TryCancelResearch(r.RecipeId, out e))));
                    else if (s.progression.blueprints.Contains(r.RecipeId))
                        Row(r.RecipeName + " · 설계도 보유", ("연구 시작", () => Run((out string e) => RestaurantProgress.TryStartResearch(r.RecipeId, out e))));
                    else Row($"{r.RecipeName} · 설계도 {Mathf.RoundToInt(r.Price * 1.2f):N0}G", ("구매", () => Run((out string e) => RestaurantProgress.TryBuyBlueprint(r.RecipeId, out e))));
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
            case "직원":
                Row("스카우트 · 계약금은 직원 첫 주급과 같아요.", ("일반 100G", () => Run((out string e) => RestaurantProgress.TryScout(0, out e))), ("고급 500G", () => Run((out string e) => RestaurantProgress.TryScout(1, out e))), ("특급 2000G", () => Run((out string e) => RestaurantProgress.TryScout(2, out e))), ("최고 5000G", () => Run((out string e) => RestaurantProgress.TryScout(3, out e))));
                var employees = s.employees.Concat(s.progression.candidates).ToList();
                foreach (var employee in employees.Skip(page * 6).Take(6))
                {
                    string id = employee.instanceId;
                    string label = $"{employee.name} [{employee.grade}] · 임금 {employee.wage}G · {employee.role}";
                    if (s.progression.candidates.Contains(employee)) Row(label, ("계약", () => Run((out string e) => RestaurantProgress.TryHire(id, out e))));
                    else Row(label, ("주방", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.Kitchen, out e))), ("홀", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.Serving, out e))), ("대기", () => Run((out string e) => RestaurantProgress.TryAssign(id, PartTimerRole.None, out e))), ("해고", () => Run((out string e) => RestaurantProgress.TryDismiss(id, out e))));
                }
                Pages(employees.Count); break;
            case "식당":
                Note($"손님 {RestaurantRules.DailyVisitors(s.restaurantLevel)}명 · 좌석 {RestaurantRules.SeatLimit(s.restaurantLevel)}석 · 주방 {RestaurantRules.StaffLimit(s.restaurantLevel, PartTimerRole.Kitchen)} / 홀 {RestaurantRules.StaffLimit(s.restaurantLevel, PartTimerRole.Serving)}명");
                for (int id = 201; id <= 204; id++)
                {
                    int tool = id;
                    Row(new[] { "프라이팬", "도마", "튀김기", "냄비" }[id - 201] + (s.progression.tools.Contains(id) ? " · 보유" : s.progression.firstToolChosen ? " · 500G" : " · 첫 도구 무료"),
                        ("선택·구매", () => Run((out string e) => RestaurantProgress.TryBuyTool(tool, out e))),
                        ("강화", () => Run((out string e) => RestaurantProgress.TryUpgradeTool(tool, out e))));
                }
                Row(s.restaurantLevel == 10 ? "최고 레벨" : $"다음 레벨 · {RestaurantRules.UpgradeCost(s.restaurantLevel + 1):N0}G · 평점 {RestaurantRules.UpgradeRating(s.restaurantLevel + 1):0.0} · 1일 공사", ("확장", () => Run(RestaurantProgress.TryUpgrade)));
                RatingHistory(s);
                break;
            case "탐험":
                string island = new[] { "요정의 숲", "돌고래 바다", "해달 왕국", "곡물 섬", "목장 섬", "다시 만난 이웃들" }[Mathf.Clamp(s.progression.islandChapter, 0, 5)];
                Note($"{island} · 배 부품 {s.progression.boatParts.Count}/3 · 하루 1회");
                Row("주민을 만나 재료를 얻고 음식 요청을 받아요. 세 번 도우면 다음 섬으로 나아가요.", ("주민 만나기", () => Run(RestaurantProgress.TryExplore)));
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
                int target = s.progression.missionIndex % 3 == 1 ? RestaurantProgress.MissionTarget * 100 : s.progression.missionIndex % 3 == 2 ? 1 : RestaurantProgress.MissionTarget;
                Row($"미션 {new[] { "손님 대접", "매출 달성", "새 요리 연구" }[s.progression.missionIndex % 3]} · {s.progression.missionProgress}/{target}", ("보상 받기", () => Run(RestaurantProgress.TryClaimMission)));
                Note(s.progression.freeTime ? "오늘 영업을 마쳤어요. 잠들면 지급일에 주급을 지급하고 다음 날로 넘어가요." : "영업 후 또는 공사 중에 잠들 수 있어요.");
                Row($"다음 주급 {s.employees.Sum(e => e.wage):N0}G (7일마다) · 보유 {RestaurantProgress.Gold(GameSession.CaptureSnapshot()):N0}G", ("취침·저장", () => { if (!RestaurantProgress.TrySleep(out string error)) message.text = error; }));
                Note("파일 저장은 취침할 때 이루어져요. 그전에 종료하면 마지막으로 저장한 아침으로 돌아가요.");
                break;
            case "설정":
                Row("소리·화면 설정", ("열기", () => SettingsWindow.Open(gameObject.scene)));
                Row(s.progression.timedResearch ? "연구: 시간 경과 + 실습" : "연구: 실습", ("연구 방식", () => { s.progression.timedResearch = !s.progression.timedResearch; Refresh(); }));
                Row("타이틀로 돌아가면 오늘의 저장하지 않은 진행을 잃어요.", ("돌아가기", () => { Clear(); Note("마지막 취침 이후 진행을 버리고 타이틀로 돌아갈까요?"); Row("", ("돌아가기", GameFlow.ReturnToTitle), ("계속하기", Refresh)); }));
                break;
        }
    }
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
            var bar = DayLoopUI.Panel(row, entry.value <= 1.3f ? DayLoopUI.Warn : DayLoopUI.Sea);
            DayLoopUI.Layout(bar, preferredWidth: 145, preferredHeight: Mathf.Max(35, entry.value * 20));
            var label = DayLoopUI.Text(bar.transform, $"{entry.day}일 · {entry.value:0.0}", 20, Color.white, TextAlignmentOptions.Center);
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
        var research = GameSession.Current.progression.research.Find(x => x.recipeId == id);
        if (research == null || research.remainingDays > 0) { message.text = "연구 시간이 남았어요."; return; }
        if (!GameDatabase.TryGetRecipe(id, out var recipe)) return;
        Clear();
        practice = gameObject.AddComponent<ResearchPractice>();
        practice.Begin(content, recipe, score => {
            bool ok = RestaurantProgress.TryCompleteResearch(id, score, out string error);
            Refresh();
            message.text = $"실습 평균 {score:0.0}/5점 · " + (ok ? "새 요리를 배웠어요." : error);
        });
    }
}
