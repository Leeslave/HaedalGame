using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ProgressionTests
{
    private GameObject inventoryObject;
    private IngredientInventoryService inventory;
    [SetUp] public void Setup()
    {
        GameSession.End(); GameFlow.SetPhase(GamePhase.Preparation);
        var data = NewGameConfig.Load().CreateSaveData(); data.introCompleted = true; data.progression.freeTime = true;
        GameSession.Begin(data, true);
        inventoryObject = new GameObject("Test inventory");
        inventory = inventoryObject.AddComponent<IngredientInventoryService>();
        GameSession.Register(inventory);
        inventory.RestoreState(data);
    }
    [TearDown] public void Teardown()
    {
        GameSession.End(); GameSession.Unregister(inventory); Object.DestroyImmediate(inventoryObject);
    }
    private static RecipeData Recipe(params RecipeIngredientRequirement[] requirements) => new RecipeData(999, "Test", 101, 201, 1, "", new List<RecipeIngredientRequirement>(requirements), null);
    [TestCase(202)]
    [TestCase(203)]
    [TestCase(204)]
    public void FirstTool_IsAlwaysFreeFryingPan(int otherTool)
    {
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryBuyTool(otherTool, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
        int gold = RestaurantProgress.Gold(GameSession.Current);
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out string error), error);
        CollectionAssert.AreEqual(new[] { 201 }, GameSession.Current.progression.tools);
        Assert.AreEqual(gold, RestaurantProgress.Gold(GameSession.Current));
        Assert.IsFalse(RestaurantProgress.TryBuyTool(otherTool, out _)); // 다음 도구는 여전히 레벨 제한을 따른다.
    }

    [Test] public void TutorialResearch_GiftIsOnce_AndRequiresSuccessfulPracticeToUnlock()
    {
        int id = RestaurantProgress.TutorialResearchRecipeId;
        Assert.IsTrue(GameDatabase.TryGetRecipe(id, out var recipe));
        Assert.AreEqual(201, recipe.ClassId);
        Assert.IsFalse(recipe.DefaultUnlock);
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out _));
        foreach (var req in RecipeResearchPlan.Requirements(recipe))
            if (!IngredientInventoryService.IsUnlimited(req.IngredientId)) inventory.SetCount(req.IngredientId, 0);
        Assert.IsTrue(RestaurantProgress.TryPrepareTutorialResearch(out string error), error);
        Assert.Contains(id, GameSession.Current.progression.blueprints);
        Assert.IsFalse(GameSession.Current.unlockedRecipeIds.Contains(id));
        string gifted = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsTrue(RestaurantProgress.TryPrepareTutorialResearch(out error), error);
        Assert.AreEqual(gifted, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
        Assert.IsTrue(RestaurantProgress.TryStartResearch(id, out error), error);
        string reserved = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryCompleteResearch(id, 3.99f, out _));
        Assert.AreEqual(reserved, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
        Assert.IsTrue(RestaurantProgress.TryCompleteResearch(id, 4f, out error), error);
        Assert.Contains(id, GameSession.Current.unlockedRecipeIds);
        Assert.IsFalse(GameSession.Current.progression.research.Any(x => x.recipeId == id));
    }

    [Test] public void RepeatedIngredientRequirements_CannotPartiallyConsume()
    {
        inventory.SetCount(1, 3);
        var recipe = Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 2), new RecipeIngredientRequirement(1, 1001, "새우", 2));
        Assert.IsFalse(inventory.TryConsumeRecipe(recipe, out _));
        Assert.AreEqual(3, inventory.GetCount(1));
    }
    [Test] public void MixedGrades_UseHighestFirst_ThenFallBackToLowerGrade()
    {
        // QA #18: F 10 + D 1이면 D를 먼저 쓰고, 떨어지면 F로 넘어간다. (예전에는 D 1개만 쓸 수 있었다)
        inventory.SetCount(1, 10); inventory.AddGrade(1, 2, 1);
        var recipe = Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 1));
        Assert.AreEqual(11, inventory.CraftableCount(recipe));
        Assert.IsTrue(inventory.TryConsumeRecipe(recipe, out int quality)); Assert.AreEqual(2, quality);
        Assert.IsTrue(inventory.TryConsumeRecipe(recipe, out quality)); Assert.AreEqual(0, quality);
        Assert.AreEqual(9, inventory.GetCount(1));
        // 한 접시에 두 등급이 섞이면 평균을 내림한다: (D + F) / 2 = E
        inventory.AddGrade(1, 2, 1);
        Assert.IsTrue(inventory.TryConsumeRecipe(Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 2)), out quality));
        Assert.AreEqual(1, quality); Assert.AreEqual(8, inventory.GetCount(1));
    }
    [Test] public void BasicSeasoning_IsUnlimitedAndDoesNotReduceQuality()
    {
        inventory.SetCount(1, 0); inventory.AddGrade(1, 3, 2);
        var recipe = Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 1), new RecipeIngredientRequirement(12, 1012, "소금", 100));
        Assert.IsTrue(inventory.TryConsumeRecipe(recipe, out int quality));
        Assert.AreEqual(3, quality); Assert.AreEqual(1, inventory.GetCount(1));
    }
    [Test] public void FailedResearch_IsAtomic_AndCancellationRefundsSameGrade()
    {
        GameSession.Current.progression.tools.AddRange(new[] {201,202,203,204});
        var recipe = GameDatabase.Recipes.Recipes.First(r => !r.DefaultUnlock);
        GameSession.Current.progression.blueprints.Add(recipe.RecipeId);
        foreach (var req in recipe.Requirements) { inventory.SetCount(req.IngredientId, 0); inventory.AddGrade(req.IngredientId, 2, req.Amount); }
        var required = recipe.Requirements.First(r => !IngredientInventoryService.IsUnlimited(r.IngredientId));
        inventory.SetCount(required.IngredientId, 0);
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryStartResearch(recipe.RecipeId, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
        inventory.AddGrade(required.IngredientId, 2, required.Amount);
        Assert.IsTrue(RestaurantProgress.TryStartResearch(recipe.RecipeId, out string error), error);
        Assert.IsTrue(RestaurantProgress.TryCancelResearch(recipe.RecipeId, out error), error);
        Assert.IsFalse(GameSession.Current.progression.blueprints.Contains(recipe.RecipeId));
        Assert.AreEqual(2, inventory.HighestGrade(required.IngredientId));
        Assert.AreEqual(required.Amount, inventory.GetCount(required.IngredientId));
    }
    [Test] public void RuntimeTransactions_PreserveElapsedTime_AndMenuDoesNotConsume()
    {
        GameSession.AddPlayTime(20);
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out _));
        Assert.AreEqual(20, GameSession.UnsavedPlayTime);
        int count = inventory.GetCount(2);
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, 1, out string error), error);
        Assert.AreEqual(count, inventory.GetCount(2));
        Assert.AreEqual(20, GameSession.CaptureSnapshot().playTimeSeconds);
    }
    [Test] public void MenuEditing_AllowsRepeatedChanges_ThenCooldownStartsOnExit()
    {
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out _));
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, 1, out string error), error);
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, -1, out error), error);
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, 1, out error), error);
        Assert.AreEqual(0, GameSession.Current.progression.menuCooldownUntil);
        // 다른 관리 거래가 세션 데이터를 교체해도 미완료 편집 상태가 유지되어야 한다.
        Assert.IsTrue(RestaurantProgress.TryScout(0, out error), error);
        RestaurantProgress.FinishMenuEditing();
        float until = GameSession.Current.progression.menuCooldownUntil;
        Assert.That(until - Time.realtimeSinceStartup, Is.InRange(29f, 30f));
        Assert.IsFalse(RestaurantProgress.TrySetMenu(0, -1, out _));
        RestaurantProgress.FinishMenuEditing();
        Assert.AreEqual(until, GameSession.Current.progression.menuCooldownUntil);
        GameSession.Current.progression.menuCooldownUntil = Time.realtimeSinceStartup - 1;
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, -1, out error), error);
    }

    [Test] public void MenuEditing_FailedEditOrNewSession_DoesNotStartCooldown()
    {
        Assert.IsFalse(RestaurantProgress.TrySetMenu(0, int.MaxValue, out _));
        RestaurantProgress.FinishMenuEditing();
        Assert.AreEqual(0, GameSession.Current.progression.menuCooldownUntil);
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out _));
        Assert.IsTrue(RestaurantProgress.TrySetMenu(0, 1, out _));
        GameSession.Begin(GameSession.Current.Clone(), true);
        RestaurantProgress.FinishMenuEditing();
        Assert.AreEqual(0, GameSession.Current.progression.menuCooldownUntil);
    }

    [Test] public void Snapshot_RoundTripsNewProgressionAndEmployeeFields()
    {
        var data = GameSession.Current;
        data.progression.boatParts.Add("mast"); data.progression.blueprints.Add(5);
        data.employees[0].appearanceSeed = 42; data.employees[0].stamina = 7;
        var clone = data.Clone();
        Assert.AreEqual("mast", clone.progression.boatParts[0]);
        Assert.AreEqual(42, clone.employees[0].appearanceSeed);
        Assert.AreEqual(7, clone.employees[0].stamina);
    }
    [TestCase(3.99f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(6f)]
    public void InvalidResearchScore_PreservesReservationAndLockedRecipe(float score)
    {
        var recipe = GameDatabase.Recipes.Recipes.First(r => !r.DefaultUnlock);
        GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = recipe.RecipeId });
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryCompleteResearch(recipe.RecipeId, score, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
    }
    [Test] public void SuccessfulResearch_UnlocksOnceWithoutAdditionalIngredients()
    {
        var recipe = GameDatabase.Recipes.Recipes.First(r => !r.DefaultUnlock);
        GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = recipe.RecipeId });
        var before = GameSession.CaptureSnapshot();
        Assert.IsTrue(RestaurantProgress.TryCompleteResearch(recipe.RecipeId, 4, out string error), error);
        Assert.Contains(recipe.RecipeId, GameSession.Current.unlockedRecipeIds);
        Assert.IsFalse(RestaurantProgress.TryCompleteResearch(recipe.RecipeId, 5, out _));
        CollectionAssert.AreEquivalent(before.progression.ingredientGrades.Select(x => (x.ingredientId, x.grade, x.amount)), GameSession.CaptureSnapshot().progression.ingredientGrades.Select(x => (x.ingredientId, x.grade, x.amount)));
        Assert.AreEqual(1, GameSession.Current.unlockedRecipeIds.Count(id => id == recipe.RecipeId));
    }
    [TestCase(.4f, 5f)]
    [TestCase(.5f, 5f)]
    [TestCase(.6f, 5f)]
    [TestCase(0f, 0f)]
    [TestCase(1f, 0f)]
    public void ResearchTiming_UsesVisibleTargetRange(float position, float expected)
    {
        Assert.AreEqual(expected, ResearchPractice.TimingScore(position), .001f);
    }

    [Test] public void ResearchCannotStartBeforeFreeTime()
    {
        GameSession.Current.progression.freeTime = false;
        GameSession.Current.progression.blueprints.Add(5);
        GameSession.Current.progression.tools.Add(204);
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryStartResearch(5, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
    }
    [Test] public void ResearchReservesItsOwnCostAndRefundsMultipleGrades()
    {
        // 새우 완자탕: 조리에는 간장이 들지만 연구 재료는 새우 2 + 무제한 전분.
        GameSession.Current.progression.blueprints.Add(13);
        GameSession.Current.progression.tools.Add(204);
        inventory.SetCount(1,0); inventory.AddGrade(1,3,1); inventory.AddGrade(1,1,1);
        inventory.SetCount(20,0);
        Assert.IsTrue(RestaurantProgress.TryStartResearch(13,out string error),error);
        Assert.AreEqual(0, inventory.GetCount(1));
        Assert.IsTrue(RestaurantProgress.TryCancelResearch(13,out error),error);
        Assert.AreEqual(2,inventory.GetCount(1));
        Assert.IsFalse(GameSession.Current.progression.blueprints.Contains(13));
        var stacks=GameSession.CaptureSnapshot().progression.ingredientGrades.Where(x=>x.ingredientId==1).ToList();
        Assert.AreEqual(1,stacks.Single(x=>x.grade==3).amount);
        Assert.AreEqual(1,stacks.Single(x=>x.grade==1).amount);
    }
    [Test] public void ScoutUsesPaidTierAndChargesOnce()
    {
        var s=GameSession.Current; s.restaurantLevel=10; s.wallets[0].amount=10000;
        Assert.IsTrue(RestaurantProgress.TryScout(3,out string error),error);
        Assert.AreEqual(4,GameSession.Current.progression.candidates.Count);
        Assert.AreEqual(5000,RestaurantProgress.Gold(GameSession.Current));
        foreach(var employee in GameSession.Current.progression.candidates)
            Assert.AreEqual(StaffRecruitmentRules.Total(RatingSystem.GradeToInt(employee.grade)),employee.cooking+employee.serving+employee.handy+employee.hp);
    }
    [Test] public void PayrollBlocksManagementUntilSelectionAndPayment()
    {
        var s=GameSession.Current; s.day=7; s.wallets[0].amount=400;
        Assert.IsFalse(RestaurantProgress.TrySleep(out _));
        Assert.AreEqual(2,GameSession.Current.progression.unpaidEmployeeIds.Count);
        Assert.IsFalse(RestaurantProgress.TryScout(0,out _));
        string id=GameSession.Current.employees[0].instanceId;
        Assert.IsTrue(RestaurantProgress.TryDismiss(id,out string error),error);
        Assert.AreEqual(1,GameSession.Current.unpaidDismissalCount);
        Assert.AreEqual(3.9f,GameSession.Current.dailyRatingHistory[0].value,.001f);
        Assert.IsTrue(RestaurantProgress.TryPayWages(out error),error);
        Assert.AreEqual(50,RestaurantProgress.Gold(GameSession.Current));
        Assert.IsFalse(RestaurantProgress.TryPayWages(out _));
        Assert.IsEmpty(GameSession.Current.progression.unpaidEmployeeIds);
    }
    [Test] public void ResidentVisitDoesNotGrantIngredients()
    {
        GameSession.Current.day=2;
        string before=JsonUtility.ToJson(GameSession.CaptureSnapshot());
        var stock=GameSession.CaptureSnapshot().ingredients.Select(i=>(i.ingredientId,i.amount)).ToArray();
        Assert.IsTrue(RestaurantProgress.TryExplore(out string error),error);
        CollectionAssert.AreEquivalent(stock,GameSession.CaptureSnapshot().ingredients.Select(i=>(i.ingredientId,i.amount)).ToArray());
    }
    private static void PrepareUpgrade(float rating, int gold, bool freeTime)
    {
        var s = GameSession.Current;
        s.restaurantLevel = 1; s.day = 3; s.progression.freeTime = freeTime;
        s.progression.pendingRestaurantLevel = 0; s.progression.constructionUntilDay = 0;
        s.dailyRatingHistory.Clear(); s.dailyRatingHistory.Add(new DailyRatingEntry { day = 0, value = rating });
        s.wallets.RemoveAll(w => w.currencyId == GameSession.GoldCurrencyId);
        s.wallets.Add(new WalletEntry { currencyId = GameSession.GoldCurrencyId, amount = gold });
    }
    [Test] public void RestaurantUpgrade_InMorning_ClosesToday()
    {
        PrepareUpgrade(4f, 1000, false);
        Assert.IsTrue(RestaurantProgress.TryUpgrade(out string error), error);
        var s = GameSession.Current;
        Assert.AreEqual(2, s.progression.pendingRestaurantLevel);
        Assert.AreEqual(4, s.progression.constructionUntilDay);
        Assert.AreEqual(1, s.restaurantLevel);
        Assert.AreEqual(500, RestaurantProgress.Gold(s));
        Assert.IsTrue(RestaurantRules.IsClosedForConstruction(s));
        Assert.IsFalse(RestaurantProgress.TryUpgrade(out _));
    }
    [Test] public void RestaurantUpgrade_AfterOperation_ClosesTomorrow()
    {
        PrepareUpgrade(4f, 1000, true);
        Assert.IsTrue(RestaurantProgress.TryUpgrade(out string error), error);
        var s = GameSession.Current;
        Assert.AreEqual(5, s.progression.constructionUntilDay);
        Assert.IsFalse(RestaurantRules.IsClosedForConstruction(s));
        s.day = 4; s.progression.freeTime = false;
        Assert.IsTrue(RestaurantRules.IsClosedForConstruction(s));
    }
    [Test] public void Research_OpensAllDayOnConstructionClosure_ButNotOnOrdinaryMorning()
    {
        PrepareUpgrade(4f, 1000, false);
        Assert.IsTrue(RestaurantProgress.ResearchLocked, "평소 아침에는 장사 전이라 연구소가 잠긴다");
        Assert.IsTrue(RestaurantProgress.TryUpgrade(out string error), error);
        var s = GameSession.Current;
        Assert.IsTrue(RestaurantRules.IsClosedForConstruction(s));
        Assert.IsFalse(RestaurantProgress.ResearchLocked, "공사로 쉬는 날은 자유시간이 오지 않으므로 하루 종일 연다");
        Assert.IsTrue(RestaurantProgress.CanResearch);
        Assert.IsTrue(RestaurantProgress.TryPrepareTutorialResearch(out error), error);
    }
    [Test] public void ClosureDay_AllowsMeetingResidents_AndSendingWholeCrewOnOneDayTrip()
    {
        PrepareUpgrade(4f, 1000, false);
        var s = GameSession.Current;
        s.storyFlags.Remove("ExploreDay-" + s.day);
        s.progression.questRecipeId = 0;
        s.progression.expedition = null;
        Assert.IsFalse(RestaurantProgress.OffDuty(s), "평소 아침은 장사 전이다");
        Assert.IsTrue(RestaurantProgress.TryUpgrade(out string error), error);
        s = GameSession.Current; // 바꾸기에 성공하면 세션이 새 저장 데이터로 바뀐다.
        Assert.IsTrue(RestaurantProgress.OffDuty(s), "공사로 쉬는 날은 하루 종일 쉬는 때다");
        Assert.IsTrue(RestaurantProgress.TryExplore(out error), error);

        s = GameSession.Current;
        s.employees.Clear();
        s.employees.Add(new EmployeeEntry { instanceId = "hall", role = nameof(PartTimerRole.Serving), slotIndex = 0 });
        s.employees.Add(new EmployeeEntry { instanceId = "cook", role = nameof(PartTimerRole.Kitchen), slotIndex = 0 });
        Assert.IsFalse(ReferenceExpedition.ReturnsTonight(s, 0), "영업이 없는 날은 다음 날 아침에 돌아온다");
        Assert.IsTrue(RestaurantProgress.TryDepart(1, new[] { "hall", "cook" }, out error), error);
        s = GameSession.Current;
        Assert.AreEqual(s.day + 1, s.progression.expedition.returnDay);
    }
    [Test] public void IslandRequest_PicksDishCookableWithOwnedTools()
    {
        var s = GameSession.Current;
        s.restaurantLevel = 2; s.day = 2;
        s.progression.tools.Clear(); s.progression.tools.Add(201);
        var recipes = GameDatabase.Recipes.Recipes;
        s.unlockedRecipeIds = recipes.Select(r => r.RecipeId).ToList(); // 도구가 없는 요리도 잔뜩 해금된 상태
        for (int day = 2; day < 12; day++)
        {
            s.day = day;
            Assert.IsTrue(GameDatabase.TryGetRecipe(RestaurantProgress.PickQuestRecipe(s), out var quest));
            Assert.IsTrue(RestaurantProgress.HasTool(s, quest), $"{day}일차 부탁 {quest.RecipeName}은 가진 도구(프라이팬)로 만들 수 있어야 한다");
        }
    }
    [Test] public void MissingToolHint_NamesToolAndUnlockLevel()
    {
        var s = GameSession.Current;
        var board = GameDatabase.Recipes.Recipes.First(r => r.ClassId == 202);
        s.progression.tools.Clear(); s.progression.tools.AddRange(new[] { 201, 203, 204 });
        s.restaurantLevel = 3;
        string hint = RestaurantProgress.MissingToolHint(s, board);
        StringAssert.Contains("도마가 필요해요", hint);
        StringAssert.Contains("식당 레벨 4부터", hint);
        s.restaurantLevel = 4;
        StringAssert.Contains("조리도구 화면에서 살 수 있어요", RestaurantProgress.MissingToolHint(s, board));
        s.progression.tools.Add(202);
        Assert.IsNull(RestaurantProgress.MissingToolHint(s, board));
    }
    [Test] public void UnreachableIslandRequest_IsReplaced_OnlyWhenToolCannotBeBought()
    {
        var s = GameSession.Current;
        var board = GameDatabase.Recipes.Recipes.First(r => r.ClassId == 202);
        s.unlockedRecipeIds = GameDatabase.Recipes.Recipes.Select(r => r.RecipeId).ToList();
        s.progression.tools.Clear(); s.progression.tools.AddRange(new[] { 201, 203, 204 });
        s.restaurantLevel = 4; s.progression.questRecipeId = board.RecipeId;
        RestaurantProgress.ReplaceUnreachableQuest(s);
        Assert.AreEqual(board.RecipeId, s.progression.questRecipeId, "도마를 지금 살 수 있으면 부탁은 그대로");
        s.restaurantLevel = 3;
        RestaurantProgress.ReplaceUnreachableQuest(s);
        Assert.IsTrue(GameDatabase.TryGetRecipe(s.progression.questRecipeId, out var quest));
        Assert.IsTrue(RestaurantProgress.HasTool(s, quest), "도마를 살 수 없으면 가진 도구로 만들 수 있는 음식으로 바뀐다");
    }
    [Test] public void RestaurantUpgrade_RequiresRatingAndCost_Atomically()
    {
        PrepareUpgrade(2.4f, 1000, false);
        Assert.IsFalse(RestaurantProgress.TryUpgrade(out _));
        PrepareUpgrade(2.5f, 499, false);
        Assert.IsFalse(RestaurantProgress.TryUpgrade(out _));
        Assert.AreEqual(0, GameSession.Current.progression.pendingRestaurantLevel);
        Assert.AreEqual(499, RestaurantProgress.Gold(GameSession.Current));
    }
    [Test] public void ShoppingCart_RejectsWholeOrder_WhenOneItemIsOutOfStock()
    {
        var items = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).Take(2).ToList();
        GameSession.Current.wallets[0].amount = 100000;
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        var cart = new Dictionary<int, int> { { items[0].IngredientId, 3 }, { items[1].IngredientId, 51 } };
        Assert.IsFalse(RestaurantProgress.TryBuyIngredients(cart, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
    }
    [Test] public void ShoppingCart_RejectsWholeOrder_WhenGoldIsInsufficient()
    {
        var item = GameDatabase.Recipes.Ingredients.First(i => !i.IsBasicSeasoning);
        GameSession.Current.wallets[0].amount = 0;
        string before = JsonUtility.ToJson(GameSession.CaptureSnapshot());
        Assert.IsFalse(RestaurantProgress.TryBuyIngredients(new Dictionary<int, int> { { item.IngredientId, 2 } }, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(GameSession.CaptureSnapshot()));
    }
    [Test] public void ShoppingCart_PurchasesAllItems_AndDeductsExactGoldAndStock()
    {
        var items = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning).Take(2).ToList();
        GameSession.Current.wallets[0].amount = 100000;
        GameSession.Current.progression.dailyStock.Clear();
        var cart = items.ToDictionary(i => i.IngredientId, i => 3);
        var counts = items.ToDictionary(i => i.IngredientId, i => inventory.GetCount(i.IngredientId));
        int cost = items.Sum(i => RestaurantProgress.RegularUnitPrice(i) * 3);
        Assert.IsTrue(RestaurantProgress.TryBuyIngredients(cart, out string error), error);
        Assert.AreEqual(100000 - cost, RestaurantProgress.Gold(GameSession.Current));
        foreach (var item in items)
        {
            Assert.AreEqual(counts[item.IngredientId] + 3, inventory.GetCount(item.IngredientId));
            Assert.AreEqual(47, GameSession.Current.progression.dailyStock.Single(x => x.itemId == item.IngredientId).currentStock);
        }
    }
    [Test] public void IslandSupply_SellsHigherGradeOnlyAfterIslandExchange()
    {
        var shrimp = GameDatabase.Recipes.Ingredients.First(i => i.IngredientName == "새우");
        GameSession.Current.wallets[0].amount = 100000;
        GameSession.Current.progression.dailyStock.Clear();
        GameSession.Current.progression.islandChapter = 1;   // 요정의 숲만 교류 중: 해산물은 아직 F만
        int key = RestaurantProgress.ShopKey(shrimp.IngredientId, 1);
        Assert.IsFalse(RestaurantProgress.TryBuyIngredients(new Dictionary<int, int> { { key, 2 } }, out _));

        int before = GameSession.Current.progression.ingredientGrades.Where(x => x.ingredientId == shrimp.IngredientId && x.grade == 1).Sum(x => x.amount);
        GameSession.Current.progression.islandChapter = 2;   // 돌고래의 바다 교류: 해산물 E
        Assert.AreEqual(1, IslandStory.SupplyGrade(GameSession.Current, "새우"));
        Assert.IsTrue(RestaurantProgress.TryBuyIngredients(new Dictionary<int, int> { { key, 2 } }, out string error), error);
        Assert.AreEqual(100000 - RestaurantProgress.RegularUnitPrice(shrimp) * 2 * 2, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(IslandStory.SupplyStock - 2, GameSession.Current.progression.dailyStock.Single(x => x.itemId == key).currentStock);
        Assert.AreEqual(before + 2, GameSession.Current.progression.ingredientGrades.Where(x => x.ingredientId == shrimp.IngredientId && x.grade == 1).Sum(x => x.amount));
        Assert.IsFalse(RestaurantProgress.TryBuyIngredients(new Dictionary<int, int> { { RestaurantProgress.ShopKey(shrimp.IngredientId, 2), 1 } }, out _));
    }
    [Test] public void FurniturePurchase_ChargesOnce_AndRejectsInsufficientFunds()
    {
        GameSession.Current.wallets[0].amount = 200;
        Assert.IsTrue(RestaurantProgress.TryBuyFurniture(4, out string error), error);
        Assert.AreEqual(40, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(1, RestaurantProgress.FurnitureCount(GameSession.Current, 4));
        Assert.IsFalse(RestaurantProgress.TryBuyFurniture(4, out _));
        Assert.AreEqual(40, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(1, RestaurantProgress.FurnitureCount(GameSession.Current, 4));
    }
    [Test] public void SixSeatTable_UnlocksAtLevelThree_AndRetainsStockInSave()
    {
        PrepareUpgrade(4f, 300, false);
        GameSession.Current.restaurantLevel = 2;
        Assert.IsFalse(RestaurantProgress.TryBuyFurniture(6, out _));
        Assert.AreEqual(300, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(0, RestaurantProgress.FurnitureCount(GameSession.Current, 6));

        GameSession.Current.restaurantLevel = 3;
        Assert.IsTrue(RestaurantProgress.TryBuyFurniture(6, out string error), error);
        Assert.IsFalse(RestaurantProgress.TryBuyFurniture(6, out _));
        var restored = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(GameSession.Current));
        Assert.AreEqual(60, RestaurantProgress.Gold(restored));
        Assert.AreEqual(1, RestaurantProgress.FurnitureCount(restored, 6));
    }
    [Test] public void Expedition_ReturnsRewardsOnce_AndProtectsOperatingStaff()
    {
        var s = GameSession.Current;
        string employee = s.employees[0].instanceId;
        s.progression.freeTime = false;
        Assert.IsFalse(RestaurantProgress.TryDepart(0, new[] { employee }, out _));
        s.progression.freeTime = true;
        Assert.IsTrue(RestaurantProgress.TryDepart(0, new[] { employee }, out string error), error);
        Assert.IsTrue(ReferenceExpedition.IsAway(GameSession.Current, employee));
        Assert.IsFalse(RestaurantProgress.TryDismiss(employee, out _));
        Assert.IsFalse(RestaurantProgress.TryClaimExpedition(out _));
        GameSession.Current.day++;
        var rewards = ReferenceExpedition.Rewards(GameSession.Current);
        Assert.IsNotEmpty(rewards);
        var before = rewards.Keys.ToDictionary(id => id, id => inventory.GetCount(id));
        Assert.IsTrue(RestaurantProgress.TryClaimExpedition(out error), error);
        foreach (var item in rewards) Assert.AreEqual(before[item.Key] + item.Value, inventory.GetCount(item.Key));
        Assert.IsFalse(RestaurantProgress.TryClaimExpedition(out _));
    }
    [Test] public void Expedition_HalfDaySentInMorning_ReturnsAfterOperation()
    {
        var s = GameSession.Current;
        string employee = s.employees[0].instanceId;
        s.progression.freeTime = false;
        Assert.IsTrue(ReferenceExpedition.ReturnsTonight(s, 0));
        Assert.IsFalse(ReferenceExpedition.ReturnsTonight(s, 1));
        s.progression.expedition = new ExpeditionState { destination = 0, departureDay = s.day, returnDay = s.day, returnsTonight = true, employeeIds = new List<string> { employee } };
        Assert.IsTrue(ReferenceExpedition.IsAway(s, employee));
        Assert.IsFalse(ReferenceExpedition.Returned(s));
        s.progression.freeTime = true;
        Assert.IsFalse(ReferenceExpedition.IsAway(s, employee));
        Assert.IsTrue(ReferenceExpedition.Returned(s));
    }
    [Test] public void LevelThree_HasTwoHallStaff_AndLevelUpFillsOpenSlotsFromStandby()
    {
        Assert.AreEqual(1, RestaurantRules.StaffLimit(2, PartTimerRole.Serving));
        Assert.AreEqual(2, RestaurantRules.StaffLimit(3, PartTimerRole.Serving), "Lv.3 하루 25명을 홀 직원 1명이 받지 못했다");
        var s = GameSession.Current;
        s.restaurantLevel = 3; s.progression.expedition = null;
        s.employees.Clear();
        s.employees.Add(new EmployeeEntry { instanceId = "hall", role = nameof(PartTimerRole.Serving), slotIndex = 0, serving = 3 });
        s.employees.Add(new EmployeeEntry { instanceId = "slow", role = nameof(PartTimerRole.None), serving = 5, cooking = 1 });
        s.employees.Add(new EmployeeEntry { instanceId = "fast", role = nameof(PartTimerRole.None), serving = 14, cooking = 2 });
        RestaurantProgress.FillOpenStaffSlots(s);
        var fast = s.employees.Find(e => e.instanceId == "fast");
        Assert.AreEqual(nameof(PartTimerRole.Serving), fast.role, "서빙을 잘하는 대기 직원이 홀에 앉는다");
        Assert.AreEqual(1, fast.slotIndex);
        // Lv.3 주방 자리는 2개라 남은 대기 직원이 주방으로 간다.
        Assert.AreEqual(nameof(PartTimerRole.Kitchen), s.employees.Find(e => e.instanceId == "slow").role);
    }
    [Test] public void Hire_SitsInFreeSlot_AndWarnsBeforePayingWhenOnlyStandbyIsLeft()
    {
        PrepareUpgrade(4f, 1000, false);
        var s = GameSession.Current;
        s.progression.unpaidEmployeeIds.Clear(); s.progression.expedition = null;
        s.employees.Clear(); s.progression.candidates.Clear();
        s.employees.Add(new EmployeeEntry { instanceId = "hall", role = nameof(PartTimerRole.Serving), slotIndex = 0 });
        s.progression.candidates.Add(new EmployeeEntry { instanceId = "cook", name = "해달 1", cooking = 9, serving = 2, wage = 100 });
        s.progression.candidates.Add(new EmployeeEntry { instanceId = "extra", name = "해달 2", cooking = 3, serving = 8, wage = 100 });
        // Lv.1은 홀 1·주방 1. 홀이 차 있으니 첫 직원은 주방으로 바로 들어간다.
        StringAssert.Contains("주방에 배치", RestaurantProgress.HireConfirmText(s, s.progression.candidates[0]));
        Assert.IsTrue(RestaurantProgress.TryHire("cook", out string error), error);
        s = GameSession.Current;
        var cook = s.employees.Find(e => e.instanceId == "cook");
        Assert.AreEqual(nameof(PartTimerRole.Kitchen), cook.role);
        Assert.AreEqual(0, cook.slotIndex);
        // 홀·주방이 다 차면 돈을 내기 전에 대기로 들어간다고 알린다.
        var extra = s.progression.candidates.Find(e => e.instanceId == "extra");
        StringAssert.Contains("대기 직원이 돼요", RestaurantProgress.HireConfirmText(s, extra));
        Assert.IsTrue(RestaurantProgress.TryHire("extra", out error), error);
        Assert.AreEqual(nameof(PartTimerRole.None), GameSession.Current.employees.Find(e => e.instanceId == "extra").role);
        Assert.AreEqual(800, RestaurantProgress.Gold(GameSession.Current));
    }
    [Test] public void HirePlacement_PicksTheRoleTheCandidateIsBetterAt_WhenBothAreFree()
    {
        var s = GameSession.Current;
        s.restaurantLevel = 1; s.employees.Clear();
        Assert.AreEqual(PartTimerRole.Kitchen, RestaurantProgress.HirePlacement(s, new EmployeeEntry { cooking = 9, serving = 2 }));
        Assert.AreEqual(PartTimerRole.Serving, RestaurantProgress.HirePlacement(s, new EmployeeEntry { cooking = 2, serving = 9 }));
    }
    [Test] public void NewGame_StartsWithTablesUpToLevelOneSeatLimit()
    {
        var data = NewGameConfig.Load().CreateSaveData();
        int seats = data.placedTables.Sum(t => RestaurantManagementUI.SeatsOf(t.tableType));
        Assert.AreEqual(RestaurantRules.SeatLimit(1), seats, "1레벨 한도 4석을 채워서 시작한다 (예전엔 2석)");
        StringAssert.Contains("다 찼어요", GameTutorial.SeatRoomText(data));
        StringAssert.Contains("6석", GameTutorial.SeatRoomText(data));
        data.placedTables.RemoveAt(1);
        StringAssert.Contains("2석 더 놓을 수 있어요", GameTutorial.SeatRoomText(data));
    }
    [Test] public void ExpansionGuide_OnlyShowsBeforeFirstUpgrade()
    {
        var data = NewGameConfig.Load().CreateSaveData();
        data.day = 7;
        Assert.IsFalse(GameTutorial.ExpansionGuideNeeded(data), "8일차 전에는 띄우지 않는다");
        data.day = 8;
        Assert.IsTrue(GameTutorial.ExpansionGuideNeeded(data));
        data.progression.pendingRestaurantLevel = 2;
        Assert.IsFalse(GameTutorial.ExpansionGuideNeeded(data), "이미 증축 공사 중");
        data.progression.pendingRestaurantLevel = 0; data.restaurantLevel = 3;
        Assert.IsFalse(GameTutorial.ExpansionGuideNeeded(data), "이미 Lv.3인데 증축하라고 하던 문제");
    }
    [Test] public void StaffOnlyDozeWhenTired_AndNapsRestoreEnoughToKeepWorking()
    {
        Assert.AreEqual(0f, PartTimerAgent.SleepChance(10, 10), "체력이 가득하면 졸지 않는다");
        Assert.AreEqual(0f, PartTimerAgent.SleepChance(5, 10), "예전엔 체력 50%에서도 7.5%씩 잠들었다");
        Assert.AreEqual(0f, PartTimerAgent.SleepChance(3, 10));
        Assert.Greater(PartTimerAgent.SleepChance(1, 10), 0f);
        Assert.AreEqual(PartTimerAgent.MaxSleepChance, PartTimerAgent.SleepChance(0, 10), 1e-5f);
        // F등급(체력 10)이 Lv.3 하루 몫(서빙 약 25건)을 하는 동안 체력이 30% 아래로 떨어지는 건 15번째 일부터다.
        float stamina = 10; int firstTired = 0;
        for (int task = 1; task <= 25 && firstTired == 0; task++)
        {
            stamina -= PartTimerAgent.StaminaPerTask;
            if (PartTimerAgent.SleepChance(stamina, 10) > 0) firstTired = task;
        }
        Assert.AreEqual(15, firstTired);
        Assert.Greater(PartTimerAgent.NapRecovery, PartTimerAgent.TiredRatio, "깨자마자 다시 졸지 않는다");
    }
    [Test] public void RestaurantLevel_FollowsLevelTable()
    {
        Assert.AreEqual(180, RestaurantRules.DailyVisitors(10));
        Assert.AreEqual(4, RestaurantRules.SeatLimit(1), "1레벨부터 2인 테이블 둘을 놓을 수 있다");
        StringAssert.Contains("6석", RestaurantRules.SeatLimitHelp(1));
        StringAssert.Contains("대장간", RestaurantRules.SeatLimitHelp(1));
        Assert.AreEqual(6, RestaurantRules.StaffLimit(10, PartTimerRole.Kitchen));
        Assert.AreEqual("F~D", RestaurantRules.GradeRange(1));
        Assert.AreEqual("C~A", RestaurantRules.GradeRange(10));
        GameSession.Current.restaurantLevel = 1;
        Assert.AreEqual(2, RestaurantRules.CapFoodQuality(5));
        Assert.AreEqual(1, RestaurantRules.CapFoodQuality(1));
        GameSession.Current.restaurantLevel = 10;
        Assert.AreEqual(5, RestaurantRules.CapFoodQuality(5));
    }
    // 연어 간장구이(10)는 연구할 때 연어·간장만 쓴다. 간장이 없으면 무엇이 모자란지 알려 주고, 사 오면 바로 시작된다.
    [Test] public void SalmonResearch_NamesMissingIngredient_AndStartsAfterBuying()
    {
        Assert.IsTrue(RestaurantProgress.TryBuyTool(201, out _));
        Assert.IsTrue(GameDatabase.TryGetRecipe(10, out var salmon));
        GameSession.Current.progression.blueprints.Add(10);
        inventory.SetCount(8, 5); inventory.SetCount(20, 0);
        var missing = RestaurantProgress.MissingResearchIngredients(GameSession.CaptureSnapshot(), salmon);
        CollectionAssert.AreEquivalent(new[] { 20 }, missing.Keys);
        Assert.AreEqual(1, missing[20]);
        Assert.IsFalse(RestaurantProgress.TryStartResearch(10, out string error));
        StringAssert.Contains("간장 1개", error);
        Assert.IsTrue(RestaurantProgress.TryBuyIngredient(20, 1, out error), error);
        Assert.AreEqual(0, RestaurantProgress.MissingResearchIngredients(GameSession.CaptureSnapshot(), salmon).Count);
        Assert.IsTrue(RestaurantProgress.TryStartResearch(10, out error), error);
        Assert.AreEqual(4, inventory.GetCount(8));
        Assert.AreEqual(0, inventory.GetCount(20));
    }

    // 굽기: 중불에서 초록 구간에 뒤집고 꺼내면 완벽, 강불로 온도를 못 지키면 점수가 깎인다.
    private static float SimulateGrill(int heat, float flipAt)
    {
        float t = GrillRules.StartTemperature, d = 0, inZone = 0, elapsed = 0, front = 0; bool back = false;
        while (elapsed < GrillRules.TimeLimit)
        {
            const float dt = .05f;
            t = GrillRules.StepTemperature(t, heat, dt); d += GrillRules.CookRate(t) * dt; elapsed += dt;
            if (GrillRules.InTempZone(t)) inZone += dt;
            if (d < flipAt) continue;
            if (!back) { front = GrillRules.SideScore(d); back = true; d = 0; continue; }
            return GrillRules.StepScore(front, GrillRules.SideScore(d), inZone / elapsed);
        }
        return GrillRules.StepScore(front, 0, inZone / elapsed);
    }
    [Test] public void Grill_MediumHeatAndFlipInGreenZone_IsPerfect()
    {
        float score = SimulateGrill(1, .86f);
        Assert.AreEqual("완벽!", GrillRules.Verdict(score), score.ToString());
    }
    [Test] public void Grill_HighHeatOrLateFlip_LosesPoints()
    {
        Assert.Less(SimulateGrill(2, .86f), 4.5f, "강불은 팬 온도 구간을 못 지켜서 완벽이 아니어야 해요");
        Assert.Less(SimulateGrill(1, 1.2f), 4f, "타고 나서 뒤집으면 점수가 깎여야 해요");
        Assert.AreEqual(5f, GrillRules.SideScore(.85f), .001f);
        Assert.AreEqual(1.5f, GrillRules.SideScore(.5f), .001f);
        Assert.AreEqual(0f, GrillRules.CookRate(110f), .0001f, "덜 달군 팬에서는 익지 않아요");
    }

    // 요리 완성 화면: 메달은 해금 기준(4점)과 같은 선에서 갈리고, 높을수록 좋은 메달이다.
    [TestCase(5f, "금메달")]
    [TestCase(4.75f, "금메달")]
    [TestCase(4.5f, "은메달")]
    [TestCase(4f, "동메달")]
    [TestCase(3.99f, null)]
    public void ResearchResult_MedalFollowsAverage(float average, string medal)
    {
        Assert.AreEqual(medal, ResearchResultRules.Medal(average));
        Assert.AreEqual(medal == null ? "Lab/LabMedalNone" : "Lab/LabMedal" + (medal == "금메달" ? "Gold" : medal == "은메달" ? "Silver" : "Bronze"), ResearchResultRules.MedalArt(average));
    }
    [Test] public void ResearchResult_MedalOnlyWhenResearchCompletes()
    {
        var recipe = GameDatabase.Recipes.Recipes.First(r => !GameSession.Current.unlockedRecipeIds.Contains(r.RecipeId));
        GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = recipe.RecipeId, remainingDays = 0 });
        Assert.IsFalse(RestaurantProgress.TryCompleteResearch(recipe.RecipeId, 3.99f, out _));
        Assert.IsNull(ResearchResultRules.Medal(3.99f));
        Assert.IsTrue(RestaurantProgress.TryCompleteResearch(recipe.RecipeId, ResearchResultRules.UnlockScore, out string error), error);
        Assert.IsNotNull(ResearchResultRules.Medal(ResearchResultRules.UnlockScore));
    }

    // ───────────── 결산 문구 ─────────────
    [Test] public void Settlement_CountsMorningShoppingAsExpense()
    {
        var item = GameDatabase.Recipes.Ingredients.First(i => !i.IsBasicSeasoning);
        GameSession.Current.wallets[0].amount = 100000;
        GameSession.Current.progression.dailyStock.Clear();
        Assert.IsTrue(RestaurantProgress.TryBuyIngredients(new Dictionary<int, int> { { item.IngredientId, 2 } }, out string error), error);
        var p = GameSession.Current.progression;
        Assert.AreEqual(GameSession.Current.day, p.moneyDay);
        Assert.AreEqual(RestaurantProgress.RegularUnitPrice(item) * 2, p.daySpent, "아침 장보기가 결산 지출에 들어간다 (예전엔 0G)");
    }

    [Test] public void Settlement_SoldOutIsBlamedBeforeSeats()
    {
        var r = new DaySettlementRecord { day = 10, visitedCount = 25, servedCount = 20, notServedCount = 5, soldOutCount = 5, seatWaitSum = 625, seatWaitSamples = 25 };
        string advice = DayLoopSave.Suggestion(r);
        StringAssert.Contains("재료가 떨어져", advice);
        StringAssert.DoesNotContain("테이블", advice);
    }

    [Test] public void SeatHelp_DuringConstructionTalksAboutConstruction()
    {
        var s = GameSession.Current;
        s.progression.pendingRestaurantLevel = s.restaurantLevel + 1;
        s.progression.constructionUntilDay = s.day + 1;
        string help = RestaurantRules.SeatLimitHelp(s);
        StringAssert.Contains("공사 중", help);
        StringAssert.DoesNotContain("올리면", help);
    }

    // ───────────── 염지 ─────────────
    // 타원 재료 위를 왼쪽에서 오른쪽으로 줄마다 문지른다. speed는 1초에 움직이는 uv 거리.
    private static (float coverage, float amount) SimulateRub(float[] rows, float speed, int passes = 1)
    {
        var dose = new float[40 * 40];
        var mask = SeasonRules.EllipseMask(40, 40);
        for (int p = 0; p < passes; p++)
            foreach (float v in rows)
                for (float u = .06f; u < .94f; u += speed / 60f)
                    SeasonRules.Sprinkle(dose, 40, 40, new Vector2(u, v), 1f / 60f);
        return (SeasonRules.Coverage(dose, mask), SeasonRules.Amount(dose, mask));
    }
    private static readonly float[] EvenRows = { .26f, .34f, .42f, .5f, .58f, .66f, .74f };

    [Test] public void Season_EvenRubIsPerfect_TooMuchIsSalty_HalfIsLow()
    {
        var even = SimulateRub(EvenRows, .8f);
        Assert.GreaterOrEqual(even.coverage, SeasonRules.TargetCoverage);
        Assert.GreaterOrEqual(SeasonRules.LayerScore(even.coverage, even.amount), 4.5f);

        var salty = SimulateRub(EvenRows, .8f, 3);
        Assert.Greater(salty.amount, SeasonRules.AmountHigh);
        Assert.Less(SeasonRules.LayerScore(salty.coverage, salty.amount), 2f);
        Assert.AreEqual("짜요!", SeasonRules.AmountLabel(salty.amount, SeasonKind.Grain));

        var half = SimulateRub(new[] { .5f, .58f, .66f }, .8f);
        Assert.Less(half.coverage, .7f);
        Assert.Less(SeasonRules.LayerScore(half.coverage, half.amount), 2f);
    }

    [Test] public void Season_ParsesSeasoningsInOrder()
    {
        CollectionAssert.AreEqual(new[] { "소금", "후추" }, SeasonRules.Parse("연어에 소금과 후추로 밑간하기").Select(s => s.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "밀가루", "계란", "튀김가루" }, SeasonRules.Parse("밀가루, 계란, 튀김가루 순서로 튀김옷 입히기").Select(s => s.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "된장소스" }, SeasonRules.Parse("해산물마다 된장소스를 얇게 바르기").Select(s => s.Name).ToArray());
        Assert.AreEqual(SeasonKind.Grain, SeasonRules.Parse("고등어에 소금으로 염지하기")[0].Kind);
        Assert.AreEqual(SeasonKind.Coat, SeasonRules.Parse("새우에 튀김가루를 묻히기")[0].Kind);
        Assert.AreEqual("소금", SeasonRules.Parse("재료에 밑간하기")[0].Name);
        Assert.AreEqual(1, SeasonRules.TargetIndex("새우와 가리비에 소금으로 밑간하기", new[] { "가리비", "새우" }));
        Assert.AreEqual("을", SeasonRules.Josa("소금", "을", "를"));
        Assert.AreEqual("를", SeasonRules.Josa("후추", "을", "를"));
    }

    [Test] public void Season_OnlyRubStepsUseTheBoard()
    {
        Assert.IsTrue(SeasonRules.IsRub("고등어에 소금으로 염지하기"));
        Assert.IsTrue(SeasonRules.IsRub("문어와 소스를 버무리기"));
        Assert.IsFalse(SeasonRules.IsRub("간장과 설탕을 섞어 양념 만들기"));
        Assert.IsFalse(SeasonRules.IsRub("새우, 전분, 소금, 후추를 섞기"));
    }

    // ───────────── 연구 실습 전용 화면 ─────────────
    [Test] public void LabStations_EveryResearchStepHasAScreen()
    {
        int checkedSteps = 0;
        for (int id = 1; id <= 40; id++)
        {
            var plan = RecipeResearchPlan.Find(id);
            if (plan == null) continue;
            foreach (var step in plan.steps) { Assert.IsNotNull(LabStations.StationFor(step), $"{id}: {step.kind} {step.text}"); checkedSteps++; }
        }
        Assert.Greater(checkedSteps, 50);
        Assert.AreEqual("Stir", LabStations.StationFor(new ResearchStep { kind = "Add", text = "프라이팬에서 해산물을 가볍게 볶기" }));
        Assert.AreEqual("Add", LabStations.StationFor(new ResearchStep { kind = "Add", text = "볶은 해산물을 넣고 소금과 후추로 간하기" }));
        Assert.AreEqual("Simmer", LabStations.StationFor(new ResearchStep { kind = "Stir", text = "냄비에 레몬과 꿀을 넣고 소스 졸이기" }));
        Assert.AreEqual("Sauce", LabStations.StationFor(new ResearchStep { kind = "Mix", text = "간장과 설탕을 섞어 양념 만들기" }));
        Assert.AreEqual("Season", LabStations.StationFor(new ResearchStep { kind = "Mix", text = "고등어에 소금으로 염지하기" }));
    }

    /// <summary>계란·양념·소스는 붓으로 바르고, 가루와 소금·후추는 통으로 뿌린다.</summary>
    [Test] public void Season_BrushOnlyForWetCoats()
    {
        var coat = SeasonRules.Parse("밀가루, 계란, 튀김가루 순서로 튀김옷 입히기");
        CollectionAssert.AreEqual(new[] { false, true, false }, coat.Select(SeasonRules.UsesBrush).ToArray());
        Assert.IsTrue(SeasonRules.UsesBrush(SeasonRules.Parse("연어에 양념을 바르기")[0]));
        Assert.IsTrue(SeasonRules.UsesBrush(SeasonRules.Parse("해산물마다 된장소스를 얇게 바르기")[0]));
        Assert.IsFalse(SeasonRules.UsesBrush(SeasonRules.Parse("연어에 소금과 후추로 밑간하기")[0]));
        Assert.IsFalse(SeasonRules.UsesBrush(SeasonRules.Dirt()));
        Assert.IsTrue(SeasonRules.IsPowder(coat[0]));
        // 튀김옷은 밀가루 포대가 아니라 생선에 입힌다.
        GameDatabase.TryGetRecipe(12, out var cutlet);
        var text = RecipeResearchPlan.Find(12).steps[2].text;
        Assert.IsFalse(LabItems.IsSeasoning(cutlet.Requirements[SeasonStation.TargetFor(cutlet, text)].IngredientName), text);
        foreach (var recipe in GameDatabase.Recipes.Recipes)
        {
            var plan = RecipeResearchPlan.Find(recipe.RecipeId);
            if (plan == null) continue;
            foreach (var step in plan.steps.Where(s => LabStations.StationFor(s) == "Season"))
                Assert.IsFalse(LabItems.IsSeasoning(recipe.Requirements[SeasonStation.TargetFor(recipe, step.text)].IngredientName), $"{recipe.RecipeName}: {step.text}");
        }
    }

    /// <summary>재료 이름 없이 "양념을 넣고 졸이기"면 앞에서 구운 재료를 냄비에 보인다. 육수만 끓일 때는 비운다.</summary>
    [Test] public void Grill_KeepsBrushedSauce()
    {
        Assert.IsTrue(GrillRules.Glaze(RecipeResearchPlan.Find(10).steps, 2).HasValue, "연어에 바른 양념이 굽기에도 보인다");
        Assert.IsTrue(GrillRules.Glaze(RecipeResearchPlan.Find(21).steps, 3).HasValue, "된장소스를 바른 해산물");
        Assert.IsFalse(GrillRules.Glaze(RecipeResearchPlan.Find(11).steps, 1).HasValue, "양념만 섞어 두고 가리비는 그냥 굽는다");
        Assert.IsFalse(GrillRules.Glaze(RecipeResearchPlan.Find(17).steps, 1).HasValue, "소금·후추 밑간은 색을 입히지 않는다");
        Assert.IsFalse(GrillRules.Glaze(RecipeResearchPlan.Find(17).steps, 2).HasValue);
        // 튀기기는 가루 튀김옷도 이어 보여 준다. 마지막에 입힌 튀김가루 색.
        var fishCutlet = RecipeResearchPlan.Find(12).steps;
        Assert.AreEqual(LabStations.StationFor(fishCutlet[3]), "Fry");
        var coat = SeasonRules.LastCoat(fishCutlet, 3, false);
        Assert.IsTrue(coat.HasValue);
        Assert.AreEqual(SeasonRules.Parse("튀김가루")[0].Color, coat.Value);
    }
    [Test] public void Heat_CarriesFoodFromEarlierSteps()
    {
        GameDatabase.TryGetRecipe(11, out var scallop);
        var plan = RecipeResearchPlan.Find(11).steps;
        CollectionAssert.AreEqual(new[] { "가리비" }, HeatStation.PotSolids(scallop, plan, 2).Select(i => i.Name).ToArray());
        GameDatabase.TryGetRecipe(17, out var salmon);
        Assert.IsFalse(HeatStation.PotSolids(salmon, RecipeResearchPlan.Find(17).steps, 3).Any(i => i.Name == "연어"), "레몬소스는 따로 졸인다");
        GameDatabase.TryGetRecipe(14, out var mackerel);
        CollectionAssert.AreEqual(new[] { "고등어" }, HeatStation.PotSolids(mackerel, RecipeResearchPlan.Find(14).steps, 3).Select(i => i.Name).ToArray());
        GameDatabase.TryGetRecipe(13, out var soup);
        Assert.IsEmpty(HeatStation.PotSolids(soup, RecipeResearchPlan.Find(13).steps, 3), "육수만 끓일 때는 재료를 띄우지 않는다");
        GameDatabase.TryGetRecipe(21, out var mix);
        var carried = HeatStation.CarriedSolids(mix, RecipeResearchPlan.Find(21).steps, 5).Select(i => i.Name).ToList();
        CollectionAssert.AreEquivalent(new[] { "가리비", "새우", "연어" }, carried);
    }

    /// <summary>연구할 수 있는 모든 요리에 완성 그림이 있다(주재료 그림으로 대신하지 않는다).</summary>
    [Test] public void LabStations_EveryResearchDishHasArt()
    {
        foreach (var recipe in GameDatabase.Recipes.Recipes)
        {
            if (RecipeResearchPlan.Find(recipe.RecipeId) == null) continue;
            var art = LabStation.DishArt(recipe);
            Assert.IsNotNull(art, recipe.RecipeName);
            StringAssert.Contains("Food/", art.name, recipe.RecipeName);
        }
    }

    /// <summary>모든 연구 레시피의 모든 단계 화면을 실제로 만들어 본다. (씻기 화면이 양념통 그림 없이 멈춘 일이 있었다)</summary>
    [Test] public void LabStations_EveryStepScreenBuildsWithoutErrors()
    {
        var canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            int opened = 0;
            foreach (var recipe in GameDatabase.Recipes.Recipes)
            {
                var plan = RecipeResearchPlan.Find(recipe.RecipeId);
                if (plan == null) continue;
                for (int i = 0; i < plan.steps.Length; i++)
                {
                    Assert.IsTrue(LabStations.TryOpen(canvas.transform, recipe, plan.steps, i, _ => { }, () => { }), $"{recipe.RecipeId}: {plan.steps[i].text}");
                    opened++;
                    canvas.transform.GetChild(canvas.transform.childCount - 1).gameObject.SetActive(false); // 지우면 글자 머티리얼을 함께 지워서 끝에 한꺼번에 지운다.
                }
            }
            Assert.Greater(opened, 50);
        }
        finally { Object.DestroyImmediate(canvas); }
    }

    private static float SimulateHeat(string station, int heat, float finishAt)
    {
        var p = HeatRules.Profile(station, "냄비에 문어를 찌기");
        float t = p.Start, progress = 0, spill = 0, peak = 0, inZone = 0, elapsed = 0, dt = .05f;
        while (elapsed < p.Limit && progress < finishAt)
        {
            t = HeatRules.StepTemperature(p, t, heat, dt);
            progress += HeatRules.ProgressRate(p, t, heat) * dt;
            spill = Mathf.Clamp01(spill + HeatRules.SpillRate(p, t, heat, progress) * dt); peak = Mathf.Max(peak, spill);
            if (HeatRules.InTempZone(p, t)) inZone += dt;
            elapsed += dt;
        }
        return HeatRules.StepScore(p, progress, peak, inZone / elapsed);
    }
    [Test] public void Heat_MediumHeatAndFinishInGreenZone_IsPerfect()
    {
        foreach (var station in new[] { "Boil", "Simmer", "Fry" })
            Assert.GreaterOrEqual(SimulateHeat(station, 1, .92f), 4.5f, station);
        Assert.Less(SimulateHeat("Boil", 2, .92f), 4.5f, "강불로 끓이면 넘쳐서 감점");
        Assert.Less(SimulateHeat("Boil", 1, .5f), 3f, "일찍 꺼내면 감점");
        Assert.Less(SimulateHeat("Fry", 0, .92f), SimulateHeat("Fry", 1, .92f), "약불 튀김은 기름 온도를 못 지킨다");
    }

    [Test] public void Cut_StraightSwipeOnGuideIsPerfect_CrookedLosesPoints()
    {
        var item = new Vector2(960, 555);
        var guides = CutRules.Guides("도마에서 문어를 얇게 썰기");
        Assert.AreEqual(6, guides.Count);
        Assert.AreEqual(6, CutRules.Guides("도마에서 새우를 잘게 다지기").Count);
        Assert.AreEqual(2, CutRules.GuidesFor("도마에서 새우, 가리비, 오징어를 손질하기", 3).Count);
        var g = guides[2];
        var c = CutRules.Center(item, g);
        CutRules.Measure(c + new Vector2(0, -200), c + new Vector2(0, 200), item, g, out float angle, out float distance, out float along);
        Assert.AreEqual(5f, CutRules.CutScore(angle, distance), .01f);
        CutRules.Measure(c + new Vector2(28, -200), c + new Vector2(28, 200), item, g, out angle, out distance, out along);
        Assert.AreEqual(3f, CutRules.CutScore(angle, distance), .05f);
        CutRules.Measure(c + new Vector2(-70, -200), c + new Vector2(70, 200), item, g, out angle, out distance, out along);
        Assert.Less(CutRules.CutScore(angle, distance), 2.5f, "비스듬히 썰면 감점");
        var diag = CutRules.Guides("광어 몸통에 칼집 넣기")[1];
        var dir = CutRules.Direction(diag.Angle);
        CutRules.Measure(item - dir * 200, item + dir * 200, item, diag, out angle, out distance, out along);
        Assert.AreEqual(5f, CutRules.CutScore(angle, distance), .01f);
        Assert.AreEqual(4.6f, CutRules.Score(new[] { 5f, 5f }, 2, 2), .01f);
        Assert.AreEqual(2.5f, CutRules.Score(new[] { 5f }, 2, 0), .01f, "다 못 썰면 남은 줄은 0점");
    }

    [Test] public void StirAndDrop_ScoreRules()
    {
        Assert.AreEqual(5f, StirRules.Score(StirRules.StirTurns, StirRules.Need(false), 1f), .01f);
        Assert.AreEqual(3.5f, StirRules.Score(StirRules.StirTurns / 2f, StirRules.Need(false), 1f), .01f);
        Assert.IsTrue(StirRules.GoodSpeed(1f)); Assert.IsFalse(StirRules.GoodSpeed(2f));
        Assert.AreEqual(5f, DropRules.OrderScore(3, 3, 0), .01f);
        Assert.AreEqual(3.4f, DropRules.OrderScore(3, 3, 2), .01f);
        Assert.AreEqual(5f, DropRules.PlaceScore(20f), .01f);
        Assert.AreEqual(3f, DropRules.PlaceScore(65f), .01f);
        Assert.IsTrue(DropRules.IsResult(DropMode.Sauce, new LabItem { Name = "양념" }));
        var order = new List<string> { "새우", "가리비", "소금" };
        var groups = new Dictionary<string, int> { { "새우", 3 }, { "가리비", 3 }, { "소금", -1 } };
        Assert.IsTrue(DropRules.InOrder(order, 0, "가리비", groups), "해산물끼리는 순서를 바꿔도 된다");
        Assert.IsFalse(DropRules.InOrder(order, 0, "소금", groups), "양념은 해산물 다음");
        Assert.IsFalse(DropRules.InOrder(order, 2, "새우", groups));
        Assert.IsFalse(DropRules.IsResult(DropMode.Add, new LabItem { Name = "양념" }));
    }
}
