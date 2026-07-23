using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 식당 레벨 1개 분량 데이터 (식당 레벨 시스템 기획서 기준, 최대 10레벨).
/// 혜택 = 이 레벨에서의 수치(표시용), 조건 = 이 레벨로 올라가기 위한 요구치(평점 + 비용).
/// </summary>
[Serializable]
public class RestaurantLevelEntry
{
    [Header("혜택 (표시용)")]
    [SerializeField] private int _customerSeats = 2;       // 손님 좌석 (개)
    [SerializeField] private int _cookingSpace = 1;        // 요리 공간 (개)
    [SerializeField] private int _waitingQueue = 2;        // 대기열 (명)
    [SerializeField] private string _recipeGradeMin = "F"; // 레시피 등급 하한
    [SerializeField] private string _recipeGradeMax = "D"; // 레시피 등급 상한
    [SerializeField] private int _dailyVisitors = 10;      // 하루 방문 손님 (명)

    [Header("강화 조건 (이전 레벨 → 이 레벨). Lv.1은 무시")]
    [SerializeField] private int _goldCost = 0;            // 골드(비용)
    [SerializeField] private float _requiredRating = 0f;   // 평점 요구치 (0이면 조건 행 생략)
    [SerializeField] private int _requiredSalesAmount = 0; // 판매 금액 요구치 (0이면 생략)
    [SerializeField] private int _requiredSeatCount = 0;   // 좌석 개수 요구치 (0이면 생략)

    [Header("기타")]
    [SerializeField] private int _constructionDays = 0;    // 공사 기간(일). 0이면 즉시(Lv.1 등)

    public int CustomerSeats => _customerSeats;
    public int CookingSpace => _cookingSpace;
    public int WaitingQueue => _waitingQueue;
    public string RecipeGradeMin => _recipeGradeMin;
    public string RecipeGradeMax => _recipeGradeMax;
    public int DailyVisitors => _dailyVisitors;

    public int GoldCost => _goldCost;
    public float RequiredRating => _requiredRating;
    public int RequiredSalesAmount => _requiredSalesAmount;
    public int RequiredSeatCount => _requiredSeatCount;

    public int ConstructionDays => _constructionDays;
}

/// <summary>
/// 식당 레벨 테이블. levels[0] = Lv.1, levels[1] = Lv.2, ... (최대 10레벨).
/// 식당 레벨은 순차적으로만 강화할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "RestaurantLevelTableSO", menuName = "Game Data/Blacksmith/Restaurant Level Table")]
public class RestaurantLevelTableSO : ScriptableObject
{
    [SerializeField] private List<RestaurantLevelEntry> _levels = new List<RestaurantLevelEntry>();

    public int MaxLevel => _levels.Count;

    /// <summary>level은 1-based.</summary>
    public bool TryGetEntry(int level, out RestaurantLevelEntry entry)
    {
        int index = level - 1;

        if (index < 0 || index >= _levels.Count)
        {
            entry = null;
            return false;
        }

        entry = _levels[index];
        return entry != null;
    }
}
