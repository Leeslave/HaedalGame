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
    [Test] public void RepeatedIngredientRequirements_CannotPartiallyConsume()
    {
        inventory.SetCount(1, 3);
        var recipe = Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 2), new RecipeIngredientRequirement(1, 1001, "새우", 2));
        Assert.IsFalse(inventory.TryConsumeRecipe(recipe, out _));
        Assert.AreEqual(3, inventory.GetCount(1));
    }
    [Test] public void OperationFreezesGrade_WithoutFallingBackToLowerGrade()
    {
        inventory.SetCount(1, 10); inventory.AddGrade(1, 2, 1); inventory.BeginOperation();
        var recipe = Recipe(new RecipeIngredientRequirement(1, 1001, "새우", 1));
        Assert.IsTrue(inventory.TryConsumeRecipe(recipe, out int quality)); Assert.AreEqual(2, quality);
        Assert.IsFalse(inventory.TryConsumeRecipe(recipe, out _)); Assert.AreEqual(10, inventory.GetCount(1));
        inventory.EndOperation(); Assert.IsTrue(inventory.TryConsumeRecipe(recipe, out quality)); Assert.AreEqual(0, quality);
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
    [Test] public void RestaurantLevel_FollowsLevelTable()
    {
        Assert.AreEqual(180, RestaurantRules.DailyVisitors(10));
        Assert.AreEqual(6, RestaurantRules.StaffLimit(10, PartTimerRole.Kitchen));
        Assert.AreEqual("F~D", RestaurantRules.GradeRange(1));
        Assert.AreEqual("C~A", RestaurantRules.GradeRange(10));
        GameSession.Current.restaurantLevel = 1;
        Assert.AreEqual(2, RestaurantRules.CapFoodQuality(5));
        Assert.AreEqual(1, RestaurantRules.CapFoodQuality(1));
        GameSession.Current.restaurantLevel = 10;
        Assert.AreEqual(5, RestaurantRules.CapFoodQuality(5));
    }
}
