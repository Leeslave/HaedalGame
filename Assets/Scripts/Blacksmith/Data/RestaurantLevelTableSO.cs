using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 식당 레벨 1개 분량 데이터.
/// 혜택 = 이 레벨에서의 상한치, 조건 = 이 레벨로 올라가기 위한 요구치 (Lv.1은 조건 무시).
/// </summary>
[Serializable]
public class RestaurantLevelEntry
{
    [Header("혜택 (이 레벨의 상한)")]
    [SerializeField] private int _maxSeatCount = 12;        // 최대 좌석 수
    [SerializeField] private float _weeklyRatingCap = 3.0f; // 주간 평점 상한
    [SerializeField] private int _maxStaffCount = 8;        // 보유 직원 수 상한
    [SerializeField] private string _gradeCap = "C";        // 레스토랑 등급 상한

    [Header("강화 조건 (이전 레벨 → 이 레벨)")]
    [SerializeField] private int _goldCost = 5000;
    [SerializeField] private int _requiredSeatCount = 0;        // 0이면 조건 없음
    [SerializeField] private float _requiredWeeklyRating = 0f;  // 0이면 조건 없음
    [SerializeField] private string _requiredStaffGrade = "";   // 비우면 조건 없음 (예: "B")
    [SerializeField] private int _requiredStaffCount = 1;       // 해당 등급 이상 직원 몇 명

    public int MaxSeatCount => _maxSeatCount;
    public float WeeklyRatingCap => _weeklyRatingCap;
    public int MaxStaffCount => _maxStaffCount;
    public string GradeCap => _gradeCap;

    public int GoldCost => _goldCost;
    public int RequiredSeatCount => _requiredSeatCount;
    public float RequiredWeeklyRating => _requiredWeeklyRating;
    public string RequiredStaffGrade => _requiredStaffGrade;
    public int RequiredStaffCount => _requiredStaffCount;
}

/// <summary>
/// 식당 레벨 테이블. levels[0] = Lv.1, levels[1] = Lv.2, ...
/// 식당 레벨은 순차적으로만 강화할 수 있다 (목업 명시).
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
