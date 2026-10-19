using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>관리 화면의 거래는 사본에서 검증하고 한 번에 적용한다. 파일 쓰기는 취침에서만 한다.</summary>
public static class RestaurantProgress
{
    public static int Gold(GameSaveData s) => s.wallets.FirstOrDefault(w => w.currencyId == GameSession.GoldCurrencyId)?.amount ?? 0;
    public static bool CanManage => GameSession.IsActive && GameFlow.Phase != GamePhase.Operation && GameFlow.Phase != GamePhase.ClosingReport;
    private static bool Change(Func<GameSaveData, string> action, out string error)
    {
        error = null;
        if (!CanManage) { error = "영업 중에는 변경할 수 없어요."; return false; }
        var candidate = GameSession.CaptureSnapshot();
        error = action(candidate);
        if (error != null) return false;
        GameSession.ApplyRuntime(candidate);
        return true;
    }
    private static bool Spend(GameSaveData s, int amount)
    {
        var wallet = s.wallets.Find(w => w.currencyId == GameSession.GoldCurrencyId);
        if (amount < 0 || wallet == null || wallet.amount < amount) return false;
        wallet.amount -= amount; return true;
    }
    private static void Earn(GameSaveData s, int amount)
    {
        var w = s.wallets.Find(x => x.currencyId == GameSession.GoldCurrencyId);
        if (w == null) s.wallets.Add(new WalletEntry { currencyId = GameSession.GoldCurrencyId, amount = amount });
        else w.amount += amount;
    }
    public static bool HasTool(GameSaveData s, RecipeData recipe) => recipe.ClassId == 205
        ? s.progression.tools.Contains(201) && s.progression.tools.Contains(202)
        : s.progression.tools.Contains(recipe.ClassId);
    public static bool TrySetMenu(int slot, int id, out string error)
    {
        if (!GameSession.IsActive) { error = "진행 중인 게임이 없어요."; return false; }
        var ids = new List<int>(GameSession.Current.menuSlots);
        if (slot < 0 || slot >= RestaurantRules.MenuLimit(GameSession.Current.restaurantLevel)) { error = "사용할 수 없는 메뉴 칸이에요."; return false; }
        while (ids.Count < RestaurantRules.MenuLimit(GameSession.Current.restaurantLevel)) ids.Add(-1);
        ids[slot] = id;
        return TrySetMenus(ids, out error);
    }
    public static bool TrySetMenus(List<int> ids, out string error)
    {
        if (GameFlow.Phase != GamePhase.Operation) return Change(s => ValidateMenu(s, ids), out error);
        var candidate = GameSession.Current.Clone();
        error = ValidateMenu(candidate, ids);
        if (error != null) return false;
        GameSession.Current.menuSlots = candidate.menuSlots;
        GameSession.Current.progression.menuHistory = candidate.progression.menuHistory;
        GameSession.Current.progression.menuHistoryDay = candidate.progression.menuHistoryDay;
        GameSession.Current.progression.menuCooldownUntil = candidate.progression.menuCooldownUntil;
        if (MenuManager.Instance != null) MenuManager.Instance.RestoreState(GameSession.Current);
        foreach (var menu in UnityEngine.Object.FindObjectsByType<MenuRecipeService>(FindObjectsSortMode.None)) menu.RestoreState(GameSession.Current);
        return true;
    }
    private static string ValidateMenu(GameSaveData s, List<int> ids)
    {
        if (ids.Count > RestaurantRules.MenuLimit(s.restaurantLevel)) return "메뉴 칸이 부족해요.";
        if (s.progression.menuHistoryDay != s.day) { s.progression.menuHistory.Clear(); s.progression.menuHistoryDay = s.day; }
        bool first = !s.menuSlots.Any(i => i >= 0);
        if (!first && Time.realtimeSinceStartup < s.progression.menuCooldownUntil) return "메뉴 변경은 30초 후 다시 할 수 있어요.";
        var chosen = ids.Where(i => i >= 0).ToList();
        if (chosen.Distinct().Count() != chosen.Count) return "같은 메뉴를 중복 등록할 수 없어요.";
        if (s.progression.menuHistory.Concat(s.menuSlots).Concat(chosen).Where(i => i >= 0).Distinct().Count() > RestaurantRules.MenuLimit(s.restaurantLevel) * 2)
            return "오늘 판매할 수 있는 고유 메뉴 수를 모두 사용했어요.";
        foreach (int id in chosen)
        {
            if (!s.unlockedRecipeIds.Contains(id) || !GameDatabase.TryGetRecipe(id, out var recipe)) return "아직 연구하지 않은 요리예요.";
            if (!CookwareRules.CanCook(s, recipe)) return "필요한 조리도구 또는 도구 강화가 부족해요.";
            if (IngredientInventoryService.Instance != null && IngredientInventoryService.Instance.CraftableCount(recipe) < 1) return "조리에 필요한 재료가 부족해요.";
        }
        foreach (int id in s.menuSlots.Where(i => i >= 0 && !chosen.Contains(i)))
            if (!s.progression.menuHistory.Contains(id)) s.progression.menuHistory.Add(id);
        s.menuSlots = new List<int>(ids);
        while (s.menuSlots.Count < RestaurantRules.MenuLimit(s.restaurantLevel)) s.menuSlots.Add(-1);
        s.progression.menuCooldownUntil = Time.realtimeSinceStartup + 30;
        return null;
    }
    public static bool TryAutoMenu(out string error)
    {
        var s = GameSession.Current;
        var ids = GameDatabase.Recipes.Recipes.Where(r => s.unlockedRecipeIds.Contains(r.RecipeId) && CookwareRules.CanCook(s, r)
            && IngredientInventoryService.Instance != null && IngredientInventoryService.Instance.CraftableCount(r) > 10)
            .OrderByDescending(r => r.Price).Take(RestaurantRules.MenuLimit(s.restaurantLevel)).Select(r => r.RecipeId).ToList();
        if (ids.Count == 0) { error = "11인분 이상 준비된 메뉴가 없어요."; return false; }
        return TrySetMenus(ids, out error);
    }
    public static bool TryBuyTool(int id, out string error) => Change(s => {
        if (id < 201 || id > 204 || s.progression.tools.Contains(id)) return "이미 보유한 도구예요.";
        if (s.progression.tools.Count >= Mathf.Min(4, s.restaurantLevel)) return "식당 레벨이 오르면 다음 도구를 선택할 수 있어요.";
        int price = s.progression.firstToolChosen ? 500 : 0;
        if (!Spend(s, price)) return "500G가 필요해요.";
        s.progression.tools.Add(id); s.progression.firstToolChosen = true;
        GameSaveData.AddUnique(s.completedTutorialIds, "Progression.FirstTool");
        if (!s.menuSlots.Any(recipe => recipe >= 0))
        {
            var first = GameDatabase.Recipes.Recipes.FirstOrDefault(r => r.DefaultUnlock && r.ClassId == id);
            if (first != null) { if (s.menuSlots.Count == 0) s.menuSlots.Add(first.RecipeId); else s.menuSlots[0] = first.RecipeId; }
        }
        return null;
    }, out error);
    public static bool TryUpgrade(out string error) => Change(s => {
        if (s.restaurantLevel >= 10) return "최고 레벨이에요.";
        if (s.progression.pendingRestaurantLevel > 0) return "이미 공사 중이에요.";
        float rating = s.dailyRatingHistory.Count == 0 ? 0 : (float)s.dailyRatingHistory.Average(r => r.value);
        int next = s.restaurantLevel + 1;
        if (rating < RestaurantRules.UpgradeRating(next)) return $"평점 {RestaurantRules.UpgradeRating(next):0.0}이 필요해요.";
        if (!Spend(s, RestaurantRules.UpgradeCost(next))) return "공사 비용이 부족해요.";
        s.progression.pendingRestaurantLevel = next; s.progression.constructionUntilDay = s.day + 1; return null;
    }, out error);
    public static bool TryUpgradeTool(int id, out string error) => Change(s => {
        if (!s.progression.tools.Contains(id)) return "먼저 도구를 선택하세요.";
        var definition = CookwareRules.Definition(id);
        var entry = CookwareRules.Entry(s, id);
        if (definition == null || !definition.TryGetLevelData(entry.level + 1, out var next)) return "최고 단계예요.";
        int gold = 0, uses = 0;
        foreach (var condition in next.UpgradeConditions)
        {
            if (condition.Type == UpgradeConditionType.Gold) gold += condition.Amount;
            if (condition.Type == UpgradeConditionType.CookwareUseCount) uses += condition.Amount;
            if (condition.Type == UpgradeConditionType.RestaurantLevel && s.restaurantLevel < condition.Amount) return $"식당 Lv.{condition.Amount}이 필요해요.";
            if (condition.Type == UpgradeConditionType.BlacksmithLevel && s.blacksmithLevel < condition.Amount) return $"대장간 Lv.{condition.Amount}이 필요해요.";
        }
        if (entry.useCount < uses) return $"이 도구로 {uses}회 요리가 필요해요.";
        if (!Spend(s, gold)) return $"{gold:N0}G가 필요해요.";
        entry.useCount -= uses; entry.level++; s.blacksmithExp++;
        s.blacksmithLevel = 1 + s.blacksmithExp / 3; return null;
    }, out error);
    public static int MissionTarget => 5 + (GameSession.Current.progression.missionIndex / 3) * 5;
    public static void RecordSaleForMission(int amount)
    {
        if (!GameSession.IsActive) return;
        var p = GameSession.Current.progression;
        if (p.missionIndex % 3 == 0) p.missionProgress++;
        if (p.missionIndex % 3 == 1) p.missionProgress += amount;
    }
    public static bool TryClaimMission(out string error) => Change(s => {
        int type = s.progression.missionIndex % 3;
        int target = type == 1 ? MissionTarget * 100 : type == 2 ? 1 : MissionTarget;
        if (s.progression.missionProgress < target) return "아직 미션을 완료하지 못했어요.";
        Earn(s, 200 + s.progression.missionIndex * 100);
        s.progression.missionProgress = 0; s.progression.missionIndex++; return null;
    }, out error);
    private static void AddIngredient(GameSaveData s, int id, int grade, int amount)
    {
        if (IngredientInventoryService.IsUnlimited(id)) return;
        var stack = s.progression.ingredientGrades.Find(x => x.ingredientId == id && x.grade == grade);
        if (stack == null) s.progression.ingredientGrades.Add(new IngredientGradeEntry { ingredientId = id, grade = grade, amount = amount });
        else stack.amount += amount;
        SyncIngredients(s);
    }
    private static void EnsureGrades(GameSaveData s)
    {
        if (s.progression.ingredientGrades.Count == 0)
            foreach (var x in s.ingredients) s.progression.ingredientGrades.Add(new IngredientGradeEntry { ingredientId = x.ingredientId, amount = x.amount });
    }
    private static void SyncIngredients(GameSaveData s) => s.ingredients = s.progression.ingredientGrades.GroupBy(x => x.ingredientId)
        .Select(g => new IngredientStackEntry { ingredientId = g.Key, amount = g.Sum(x => x.amount), source = "Management" }).ToList();
    public static bool TryBuyIngredient(int id, int amount, out string error) => Change(s => {
        if (amount <= 0 || !GameDatabase.Recipes.TryGetIngredientById(id, out var item) || item.IsBasicSeasoning) return "구매할 수 없는 재료예요.";
        if (s.progression.regularStockDay != s.day) { s.progression.dailyStock.Clear(); s.progression.regularStockDay = s.day; }
        var stock = s.progression.dailyStock.Find(x => x.itemId == id);
        if (stock == null) { stock = new ShopStockEntry { itemId = id, currentStock = 50, maxStock = 50 }; s.progression.dailyStock.Add(stock); }
        if (stock.currentStock < amount) return "오늘 재고가 부족해요.";
        int cost = RegularUnitPrice(item) * amount;
        if (!Spend(s, cost)) return "골드가 부족해요.";
        stock.currentStock -= amount; EnsureGrades(s); AddIngredient(s, id, 0, amount); return null;
    }, out error);
    public static int RegularUnitPrice(IngredientData item) => Mathf.Max(1, Mathf.RoundToInt(item.Price * 1.2f));
    public static void AcquireBlueprint(int id)
    {
        if (GameSession.IsActive && !GameSession.Current.progression.blueprints.Contains(id)) GameSession.Current.progression.blueprints.Add(id);
    }
    public static bool TryBuyBlueprint(int id, out string error) => Change(s => {
        if (!GameDatabase.TryGetRecipe(id, out var r)) return "없는 레시피예요.";
        if (s.unlockedRecipeIds.Contains(id) || s.progression.blueprints.Contains(id)) return "이미 가지고 있어요.";
        if (!Spend(s, Mathf.RoundToInt(r.Price * 1.2f))) return "골드가 부족해요.";
        s.progression.blueprints.Add(id); return null;
    }, out error);
    public static bool TryStartResearch(int id, out string error) => Change(s => {
        if (!GameDatabase.TryGetRecipe(id, out var r) || !s.progression.blueprints.Contains(id)) return "먼저 레시피 설계도를 구매하세요.";
        if (s.unlockedRecipeIds.Contains(id) || s.progression.research.Any(x => x.recipeId == id)) return "이미 연구했거나 연구 중이에요.";
        if (s.progression.research.Count >= RestaurantRules.ResearchSlots(s.restaurantLevel)) return "연구 칸이 부족해요.";
        if (!HasTool(s, r)) return "조리도구가 필요해요.";
        EnsureGrades(s);
        var research = new ResearchEntry { recipeId = id, remainingDays = s.progression.timedResearch ? RestaurantRules.ResearchDays(r.RecipeGrade) : 0 };
        foreach (var group in r.Requirements.GroupBy(x => x.IngredientId))
        {
            if (IngredientInventoryService.IsUnlimited(group.Key)) continue;
            int amount = group.Sum(x => x.Amount);
            var stack = s.progression.ingredientGrades.Where(x => x.ingredientId == group.Key && x.amount > 0).OrderByDescending(x => x.grade).FirstOrDefault();
            if (stack == null || stack.amount < amount) return "연구 재료가 부족해요.";
            stack.amount -= amount;
            research.paidIngredients.Add(new IngredientGradeEntry { ingredientId = stack.ingredientId, grade = stack.grade, amount = amount });
        }
        SyncIngredients(s); s.progression.research.Add(research); return null;
    }, out error);
    public static bool TryCancelResearch(int id, out string error) => Change(s => {
        var entry = s.progression.research.Find(x => x.recipeId == id);
        if (entry == null) return "진행 중인 연구가 없어요.";
        EnsureGrades(s);
        foreach (var paid in entry.paidIngredients) AddIngredient(s, paid.ingredientId, paid.grade, paid.amount);
        s.progression.research.Remove(entry); return null;
    }, out error);
    public static bool TryCompleteResearch(int id, float score, out string error) => Change(s => {
        var r = s.progression.research.Find(x => x.recipeId == id);
        if (r == null || r.remainingDays > 0) return "연구 시간이 남았어요.";
        if (float.IsNaN(score) || float.IsInfinity(score) || score < 4 || score > 5) return "4점 이상이면 연구가 완료돼요. 다시 도전할 수 있어요.";
        if (!s.unlockedRecipeIds.Contains(id)) s.unlockedRecipeIds.Add(id);
        s.progression.research.Remove(r);
        if (s.progression.missionIndex % 3 == 2) s.progression.missionProgress++;
        return null;
    }, out error);
    public static bool TryScout(int tier, out string error) => Change(s => {
        tier = Mathf.Clamp(tier, 0, 3);
        if (s.restaurantLevel < RestaurantRules.ScoutLevel(tier)) return "식당 레벨이 부족해요.";
        if (!Spend(s, RestaurantRules.ScoutCost(tier))) return "스카우트 비용이 부족해요.";
        int candidates = 3 - (s.scoutPenaltyPending ? Mathf.Min(3, (s.unpaidDismissalCount + 1) / 2) : 0);
        s.scoutPenaltyPending = false;
        s.progression.candidates.Clear();
        for (int i = 0; i < candidates; i++)
        {
            int grade = Mathf.Clamp(tier + (UnityEngine.Random.value < .15f ? 2 : UnityEngine.Random.value < .4f ? 1 : 0), 0, 5);
            string gradeName = new[] { "F", "E", "D", "C", "B", "A" }[grade];
            s.progression.candidates.Add(new EmployeeEntry {
                instanceId = "emp-" + s.nextEmployeeSerial++, name = "해달 " + s.nextEmployeeSerial, grade = gradeName,
                serving = UnityEngine.Random.Range(2f + grade * 2, 5f + grade * 2), cooking = UnityEngine.Random.Range(2f + grade * 2, 5f + grade * 2),
                handy = UnityEngine.Random.Range(1f + grade, 3f + grade * 2), hp = 10 + grade * 5,
                wage = RestaurantRules.Wage(gradeName), appearanceSeed = UnityEngine.Random.Range(0, 100000) });
        }
        return null;
    }, out error);
    public static bool TryHire(string id, out string error) => Change(s => {
        var c = s.progression.candidates.Find(x => x.instanceId == id);
        if (c == null) return "지원자가 없어요.";
        if (s.employees.Count >= 10) return "직원은 최대 10명까지 고용할 수 있어요.";
        if (!Spend(s, c.wage)) return "계약금이 부족해요.";
        s.employees.Add(c); s.progression.candidates.Remove(c); return null;
    }, out error);
    public static bool TryAssign(string id, PartTimerRole role, out string error) => Change(s => {
        var e = s.employees.Find(x => x.instanceId == id);
        if (e == null) return "직원이 없어요.";
        var assigned = s.employees.Where(x => x.instanceId != id && x.role == role.ToString()).ToList();
        if (role != PartTimerRole.None && assigned.Count >= RestaurantRules.StaffLimit(s.restaurantLevel, role)) return "배치 가능한 인원이 가득 찼어요.";
        e.role = role.ToString(); e.slotIndex = role == PartTimerRole.None ? -1 : Enumerable.Range(0, 5).First(i => !assigned.Any(x => x.slotIndex == i)); return null;
    }, out error);
    public static bool TryDismiss(string id, out string error) => Change(s => {
        var e = s.employees.Find(x => x.instanceId == id); if (e == null) return "직원이 없어요.";
        if (s.progression.unpaidEmployeeIds.Contains(id)) { s.unpaidDismissalCount++; s.scoutPenaltyPending = true; }
        if (s.progression.unpaidEmployeeIds.Contains(id) && s.dailyRatingHistory.Count > 0)
            s.dailyRatingHistory[s.dailyRatingHistory.Count - 1].value = Mathf.Max(0, s.dailyRatingHistory[s.dailyRatingHistory.Count - 1].value - .1f);
        s.progression.unpaidEmployeeIds.Remove(id); s.employees.Remove(e); return null;
    }, out error);
    public static bool TrySleep(out string error)
    {
        error = null;
        if (!CanManage) { error = "지금은 잘 수 없어요."; return false; }
        var s = GameSession.CaptureSnapshot();
        if (!s.progression.freeTime && s.progression.constructionUntilDay <= s.day) { error = "영업을 마친 뒤 잠들 수 있어요."; return false; }
        int wages = s.day % 7 != 0 || s.progression.wagesPaidThroughDay >= s.day ? 0 : s.employees.Sum(e => e.wage);
        if (!Spend(s, wages))
        {
            GameSession.Current.progression.unpaidEmployeeIds = s.employees.Select(e => e.instanceId).ToList();
            error = $"임금 {wages:N0}G가 필요해요. 직원 관리에서 미지급 직원을 정리할 수 있어요."; return false;
        }
        s.progression.wagesPaidThroughDay = s.day;
        s.progression.unpaidEmployeeIds.Clear();
        s.day++; s.progression.freeTime = false; s.settlementPendingReview = false;
        GameSaveData.AddUnique(s.completedTutorialIds, "Progression.FirstSleep");
        s.progression.menuCooldownUntil = 0; s.progression.menuHistory.Clear(); s.progression.menuHistoryDay = s.day;
        foreach (var e in s.employees) e.stamina = e.hp;
        foreach (var r in s.progression.research) r.remainingDays = Mathf.Max(0, r.remainingDays - 1);
        if (s.progression.pendingRestaurantLevel > 0 && s.day >= s.progression.constructionUntilDay)
        { s.restaurantLevel = s.progression.pendingRestaurantLevel; s.progression.pendingRestaurantLevel = 0; }
        while (s.menuSlots.Count < RestaurantRules.MenuLimit(s.restaurantLevel)) s.menuSlots.Add(-1);
        GameSession.StampCheckpoint(s, GameSaveData.ReasonDayEnded);
        if (!GameSession.IsDevSession && !SaveService.TryWrite(s, out error)) return false;
        GameSession.ApplyCheckpoint(s); GameFlow.EnterDayStart(); return true;
    }
    public static bool TryExplore(out string error) => Change(s => {
        if (!s.progression.freeTime) return "영업을 마친 자유시간에 탐험할 수 있어요.";
        if (s.day < 2) return "둘째 날부터 주민을 만날 수 있어요.";
        if (s.progression.questRecipeId > 0) return "먼저 주민이 부탁한 음식을 전해주세요.";
        string flag = "ExploreDay-" + s.day;
        if (s.storyFlags.Contains(flag)) return "오늘 탐험은 마쳤어요.";
        int required = Mathf.Min(10, 1 + s.progression.islandChapter * 2);
        if (s.restaurantLevel < required) return $"다음 섬에는 식당 레벨 {required}이 필요해요.";
        s.storyFlags.Add(flag); EnsureGrades(s);
        int id = Mathf.Clamp(1 + s.progression.islandChapter * 3 + UnityEngine.Random.Range(0, 3), 1, 10);
        AddIngredient(s, id, Mathf.Min(5, s.progression.islandChapter), 10);
        var available = s.unlockedRecipeIds.Where(recipe => GameDatabase.TryGetRecipe(recipe, out _)).ToArray();
        if (available.Length > 0) { s.progression.questRecipeId = available[(s.day + s.progression.islandChapter) % available.Length]; s.progression.questIssuedDay = s.day; }
        if (s.progression.endingSeen) s.progression.epilogueSeen = true;
        return null;
    }, out error);
    public static bool TryDeliverQuest(out string error) => Change(s => {
        if (!s.progression.freeTime || !GameDatabase.TryGetRecipe(s.progression.questRecipeId, out var recipe)) return "전달할 음식 요청이 없어요.";
        if (!CookwareRules.CanCook(s, recipe)) return "요청한 음식을 만들 도구가 필요해요.";
        EnsureGrades(s);
        foreach (var group in recipe.Requirements.GroupBy(r => r.IngredientId))
        {
            if (IngredientInventoryService.IsUnlimited(group.Key)) continue;
            var stack = s.progression.ingredientGrades.Where(x => x.ingredientId == group.Key && x.amount > 0).OrderByDescending(x => x.grade).FirstOrDefault();
            int amount = group.Sum(r => r.Amount);
            if (stack == null || stack.amount < amount) return "요청 음식을 만들 재료가 부족해요.";
            stack.amount -= amount;
        }
        SyncIngredients(s);
        s.progression.questRecipeId = 0; s.progression.npcSatisfaction++;
        var reward = GameDatabase.Recipes.Recipes.FirstOrDefault(r => !s.unlockedRecipeIds.Contains(r.RecipeId) && !s.progression.blueprints.Contains(r.RecipeId));
        if (reward != null) s.progression.blueprints.Add(reward.RecipeId);
        if (s.progression.npcSatisfaction % 3 == 0 && s.progression.islandChapter < 5)
        {
            if (s.progression.islandChapter < 3) s.progression.boatParts.Add("Part-" + s.progression.islandChapter);
            s.progression.islandChapter++;
        }
        return null;
    }, out error);
    public static bool TryEnding(out string error) => Change(s => {
        if (s.progression.boatParts.Count < 3) return "배 부품 3개가 필요해요.";
        if (s.progression.endingSeen) return "배를 이미 수리했어요. 다시 섬을 방문하면 후일담을 볼 수 있어요.";
        s.progression.endingSeen = true;
        GameSaveData.AddUnique(s.storyFlags, "BoatRepaired"); return null;
    }, out error);
}
