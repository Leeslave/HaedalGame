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
        var data = NewGameConfig.Load().CreateSaveData(); data.introCompleted = true;
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
