using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>2026-06-10 종합표. UI와 실제 운영이 같은 제한을 사용한다.</summary>
public static class RestaurantRules
{
    private static readonly int[] Visitors = { 10, 15, 20, 26, 32, 40, 47, 54, 62, 70 };
    private static readonly int[] Seats = { 2, 4, 6, 8, 10, 12, 15, 18, 22, 26 };
    private static readonly int[] Menus = { 3, 3, 4, 4, 5, 5, 6, 6, 7, 8 };
    private static readonly int[] Chefs = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };
    private static readonly int[] Servers = { 1, 1, 1, 2, 2, 3, 3, 4, 4, 5 };
    private static readonly int[] Waiting = { 2, 3, 4, 6, 8, 10, 12, 15, 18, 22 };
    private static readonly int[] Costs = { 0, 500, 1200, 3000, 6500, 12000, 25000, 45000, 80000, 150000 };
    private static readonly float[] Ratings = { 0, 2.5f, 3, 3, 3, 3.5f, 3.5f, 3.5f, 4, 4 };
    private static int Index(int level) => Mathf.Clamp(level, 1, 10) - 1;
    public static int DailyVisitors(int level) => Visitors[Index(level)];
    public static int SeatLimit(int level) => Seats[Index(level)];
    public static int MenuLimit(int level) => Menus[Index(level)];
    public static int StaffLimit(int level, PartTimerRole role) => role == PartTimerRole.Kitchen ? Chefs[Index(level)] : Servers[Index(level)];
    public static int WaitingLimit(int level) => Waiting[Index(level)];
    public static int UpgradeCost(int nextLevel) => Costs[Index(nextLevel)];
    public static float UpgradeRating(int nextLevel) => Ratings[Index(nextLevel)];
    public static int ResearchSlots(int level) => level >= 8 ? 3 : level >= 4 ? 2 : 1;
    public static int Wage(string grade) => new[] {350, 700, 1200, 2500, 5000, 10000}[Mathf.Clamp(RatingSystem.GradeToInt(grade), 0, 5)];
    public static int ScoutLevel(int tier) => new[] {1, 2, 4, 7}[Mathf.Clamp(tier, 0, 3)];
    public static int ScoutCost(int tier) => new[] {100, 500, 2000, 5000}[Mathf.Clamp(tier, 0, 3)];
    public static float TrashTipMultiplier(int count) => count >= 5 ? 0 : count >= 3 ? 0.5f : count > 0 ? 0.7f : 1;
    public static int ResearchDays(int stars) => stars >= 5 ? 3 : stars >= 3 ? 2 : 1;
}

[Serializable]
public class ProgressionState
{
    public bool freeTime;
    public int constructionUntilDay;
    public int pendingRestaurantLevel;
    public List<int> tools = new List<int>();
    public List<int> blueprints = new List<int>();
    public List<ResearchEntry> research = new List<ResearchEntry>();
    public List<IngredientGradeEntry> ingredientGrades = new List<IngredientGradeEntry>();
    public List<int> menuHistory = new List<int>();
    public int menuHistoryDay;
    public float menuCooldownUntil;
    public int wagesPaidThroughDay;
    public List<string> unpaidEmployeeIds = new List<string>();
    public int missionIndex;
    public int missionProgress;
    public int islandChapter;
    public List<string> boatParts = new List<string>();
    public bool endingSeen;
    public bool epilogueSeen;
    public int questRecipeId;
    public int questIssuedDay;
    public int npcSatisfaction;
    public int regularStockDay;
    public List<ShopStockEntry> dailyStock = new List<ShopStockEntry>();
    public List<EmployeeEntry> candidates = new List<EmployeeEntry>();
    public bool timedResearch;
    public int tableSkin;
    public bool firstToolChosen;
}

[Serializable]
public class ResearchEntry
{
    public int recipeId;
    public int remainingDays;
    public bool practiced;
    public List<IngredientGradeEntry> paidIngredients = new List<IngredientGradeEntry>();
}

[Serializable]
public class IngredientGradeEntry
{
    public int ingredientId;
    public int grade;
    public int amount;
}
