using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 식당 레벨 시스템 기획서(최대 10레벨) 기준 표. UI와 실제 운영이 같은 제한을 사용한다.
/// 레벨업 조건(평점·비용)과 혜택(손님 좌석·요리 공간·대기열·레시피 등급·하루 방문 손님)은
/// RestaurantLevelTableSO 에셋과 같은 값이다. 홀 직원 수와 메뉴 칸은 기획서에 없어 기존 값을 유지한다.
/// </summary>
public static class RestaurantRules
{
    public const int MaxLevel = 10;
    private static readonly int[] Visitors = { 10, 15, 25, 35, 50, 60, 80, 100, 135, 180 };
    private static readonly int[] Seats = { 2, 4, 6, 8, 10, 12, 15, 18, 22, 26 };
    private static readonly int[] Menus = { 3, 3, 4, 4, 5, 5, 6, 6, 7, 8 };
    private static readonly int[] Chefs = { 1, 1, 2, 2, 3, 3, 4, 4, 5, 6 };   // 요리 공간 = 주방 직원 배치 한도
    private static readonly int[] Servers = { 1, 1, 1, 2, 2, 3, 3, 4, 4, 5 };
    private static readonly int[] Waiting = { 2, 3, 4, 6, 8, 10, 12, 15, 18, 22 };
    private static readonly int[] Costs = { 0, 500, 1200, 3000, 6500, 12000, 25000, 45000, 80000, 150000 };
    private static readonly float[] Ratings = { 0, 2.5f, 3, 3, 3, 3.5f, 3.5f, 3.5f, 4, 4 };
    // 레시피(음식 품질) 등급 F=0 … A=5. 상한은 그 레벨에서 나올 수 있는 최고 품질, 하한은 표시용.
    private static readonly int[] GradeMin = { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3 };
    private static readonly int[] GradeMax = { 2, 2, 3, 3, 3, 4, 4, 4, 5, 5 };
    private const int ConstructionDayCount = 1;
    private static int Index(int level) => Mathf.Clamp(level, 1, MaxLevel) - 1;
    public static int DailyVisitors(int level) => Visitors[Index(level)];
    public static int SeatLimit(int level) => Seats[Index(level)];
    public static int MenuLimit(int level) => Menus[Index(level)];
    public static int StaffLimit(int level, PartTimerRole role) => role == PartTimerRole.Kitchen ? Chefs[Index(level)] : Servers[Index(level)];
    public static int CookingSpace(int level) => Chefs[Index(level)];
    public static int WaitingLimit(int level) => Waiting[Index(level)];
    public static int UpgradeCost(int nextLevel) => Costs[Index(nextLevel)];
    public static float UpgradeRating(int nextLevel) => Ratings[Index(nextLevel)];
    public static int ConstructionDays(int nextLevel) => nextLevel <= 1 ? 0 : ConstructionDayCount;
    public static int MinFoodGrade(int level) => GradeMin[Index(level)];
    public static int MaxFoodGrade(int level) => GradeMax[Index(level)];
    public static string GradeLetter(int grade) => "FEDCBA"[Mathf.Clamp(grade, 0, 5)].ToString();
    public static string GradeRange(int level) => $"{GradeLetter(MinFoodGrade(level))}~{GradeLetter(MaxFoodGrade(level))}";

    /// <summary>음식 품질을 현재 식당 레벨의 레시피 등급 상한으로 자른다. (세션이 없으면 그대로)</summary>
    public static int CapFoodQuality(int quality)
    {
        if (!GameSession.IsActive) return quality;
        return Mathf.Min(quality, MaxFoodGrade(GameSession.Current.restaurantLevel));
    }

    /// <summary>손님 기대치 = 레시피 등급 상한 - 1. (기존 RestaurantLevelTableSO 기준과 같다)</summary>
    public static int Expectation(int level) => Mathf.Max(0, MaxFoodGrade(level) - 1);

    /// <summary>식당 평점 = 저장된 일일 평점 이력(시작 평판 포함, 최근 7일)의 평균. 레벨업 조건과 화면 표시가 같은 값을 쓴다.</summary>
    public static float RestaurantRating(GameSaveData s) => s.dailyRatingHistory.Count == 0 ? 0f : s.dailyRatingHistory.Average(r => r.value);

    /// <summary>공사 중이면 완공되는 아침의 일차, 아니면 0.</summary>
    public static int ConstructionDoneDay(GameSaveData s) => s.progression.pendingRestaurantLevel > 0 ? s.progression.constructionUntilDay : 0;

    /// <summary>
    /// 오늘이 공사로 휴업하는 날인지. 영업을 마친 뒤 맡긴 공사는 다음 날이 휴업일이라 오늘(자유시간)은 아니다.
    /// </summary>
    public static bool IsClosedForConstruction(GameSaveData s) => s.progression.pendingRestaurantLevel > 0
        && s.progression.constructionUntilDay > s.day && !s.progression.freeTime;
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
    public ExpeditionState expedition = new ExpeditionState();
    public List<FurnitureStock> furniture = new List<FurnitureStock>();
    public List<int> unlockedTableSkins = new List<int> { 0, 1 };
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
public class ExpeditionState
{
    public int destination = -1;
    public int departureDay;
    public int returnDay;
    public List<string> employeeIds = new List<string>();
}

[Serializable]
public class FurnitureStock
{
    public int seats;
    public int amount;
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
