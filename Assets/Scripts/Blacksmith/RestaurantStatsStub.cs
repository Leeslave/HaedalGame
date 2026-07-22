using System;
using UnityEngine;

/// <summary>
/// [임시 스텁] 식당 레벨 강화 조건에 쓰이는 좌석 수 / 주간 평점 / 직원 등급 제공자.
/// 해당 시스템(좌석 배치, 평점, 직원 등급)이 구현되면 이 값을 실제 시스템에서 읽도록 교체한다.
/// 그 전까지는 인스펙터 값으로 테스트한다.
/// </summary>
public class RestaurantStatsStub : MonoBehaviour
{
    public static RestaurantStatsStub Instance { get; private set; }

    [Header("스텁 값 (실제 시스템 연결 전 테스트용)")]
    [SerializeField] private int _currentSeatCount = 12;
    [SerializeField] private float _weeklyRating = 3.0f;

    // 요구 등급 이상 직원 보유 수 (직원 등급 시스템 연결 전 단순화)
    [SerializeField] private int _staffCountAtOrAboveRequiredGrade = 0;

    public event Action OnChanged;

    public int CurrentSeatCount => _currentSeatCount;
    public float WeeklyRating => _weeklyRating;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>요구 등급(grade) 이상 직원을 count명 이상 보유했는가. 등급 비교는 시스템 연결 시 구현.</summary>
    public bool HasStaff(string grade, int count)
    {
        return _staffCountAtOrAboveRequiredGrade >= count;
    }

    // 테스트/추후 시스템 연결용 세터
    public void SetSeatCount(int value)
    {
        _currentSeatCount = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }

    public void SetWeeklyRating(float value)
    {
        _weeklyRating = Mathf.Max(0f, value);
        OnChanged?.Invoke();
    }

    public void SetStaffCount(int value)
    {
        _staffCountAtOrAboveRequiredGrade = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }
}
