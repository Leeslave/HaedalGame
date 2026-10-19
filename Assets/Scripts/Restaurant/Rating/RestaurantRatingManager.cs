using System;
using System.Collections.Generic;
using UnityEngine;

public enum RatingBuffTier
{
    Neutral,
    Buff,
    Debuff
}

// 식당 평점(개인 평점 누적 + 7일 이동평균) 및 평점 구간별 실시간 버프/디버프 배수를 관리하는 싱글턴.
public class RestaurantRatingManager : MonoBehaviour, ISaveParticipant
{
    public static RestaurantRatingManager Instance { get; private set; }

    private const int WindowSize = 7;
    private const float BaselineRating = 4.0f; // 7일치 데이터가 다 쌓이기 전, 아직 없는 날짜를 대신하는 기본값

    [SerializeField] private RestaurantLevelTableSO _levelTable;

    private List<float> _todayScores = new List<float>();
    private List<DailyRatingEntry> _dailyHistory = new List<DailyRatingEntry>();
    private int _lastRecordedRatingDay;

    public Action<float> OnTodayAverageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetHistoryToSeed();
        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (InGameTimeManager.Instance != null)
            InGameTimeManager.Instance.OnDayAdvanced -= OnDayAdvanced;

        GameSession.Unregister(this);
    }

    private void Start()
    {
        if (InGameTimeManager.Instance != null)
        {
            InGameTimeManager.Instance.OnDayAdvanced -= OnDayAdvanced;
            InGameTimeManager.Instance.OnDayAdvanced += OnDayAdvanced;
        }

        UpdateRestaurantStatsStub();
    }

    // 현재 식당 레벨의 RecipeGradeMax를 기준으로 한 기대치. (기대치 = 상한 등급 - 1)
    public int CurrentExpectation
    {
        get
        {
            int level = RestaurantLevelManager.Instance != null ? RestaurantLevelManager.Instance.CurrentLevel : 1;

            if (_levelTable != null && _levelTable.TryGetEntry(level, out RestaurantLevelEntry entry))
            {
                return Mathf.Max(0, RatingSystem.GradeToInt(entry.RecipeGradeMax) - 1);
            }

            return 0;
        }
    }

    // 오늘 지금까지 발생한 개인 평점들의 평균 (실시간 버프/디버프 판정 기준)
    public float TodayAverage
    {
        get
        {
            if (_todayScores.Count == 0) { return 0f; }

            float sum = 0f;
            foreach (float score in _todayScores) { sum += score; }
            return sum / _todayScores.Count;
        }
    }

    // 최근 7일(오늘 제외, 이미 마감된 일자) 일일 평점의 평균. 식당 대표 평점으로 표시된다.
    public float RestaurantRating
    {
        get
        {
            if (_dailyHistory.Count == 0) { return 0f; }

            float sum = 0f;
            foreach (DailyRatingEntry entry in _dailyHistory) { sum += entry.value; }
            return sum / _dailyHistory.Count;
        }
    }

    public RatingBuffTier CurrentTier
    {
        get
        {
            float avg = TodayAverage;
            if (avg >= 4.0f && avg <= 5.0f) { return RatingBuffTier.Buff; }
            if (avg >= 1.1f && avg <= 2.0f) { return RatingBuffTier.Debuff; }
            return RatingBuffTier.Neutral;
        }
    }

    public float PatienceDrainMultiplier
    {
        get
        {
            if (CurrentTier == RatingBuffTier.Buff) { return 0.85f; }
            if (CurrentTier == RatingBuffTier.Debuff) { return 1.15f; }
            return 1.0f;
        }
    }

    public float StaffSpeedMultiplier
    {
        get
        {
            if (CurrentTier == RatingBuffTier.Buff) { return 1.10f; }
            if (CurrentTier == RatingBuffTier.Debuff) { return 0.90f; }
            return 1.0f;
        }
    }

    public float TipMultiplier
    {
        get
        {
            if (CurrentTier == RatingBuffTier.Buff) { return 1.20f; }
            return 1.0f;
        }
    }

    // 손님이 식사를 완료하고 결제할 때 호출. 소수점 첫째 자리까지 표기.
    public void AddCustomerScore(float score)
    {
        float rounded = Mathf.Round(score * 10f) / 10f;
        _todayScores.Add(rounded);
        OnTodayAverageChanged?.Invoke(TodayAverage);
    }

    /// <summary>
    /// 해당 일차의 영업 평점을 이력에 한 번만 확정한다. 이미 확정한 일차면 무시한다.
    /// 영업이 없어 개인 평점이 없으면 이력에는 넣지 않고 확정 표시만 한다.
    /// </summary>
    public void CommitTodayRating(int day)
    {
        if (day <= _lastRecordedRatingDay)
            return;

        if (_todayScores.Count > 0)
        {
            _dailyHistory.Add(new DailyRatingEntry { day = day, value = Mathf.Max(0, TodayAverage - (RestaurantLitter.Count > 0 ? .2f : 0)) });
            while (_dailyHistory.Count > WindowSize) { _dailyHistory.RemoveAt(0); }
        }

        _lastRecordedRatingDay = day;
        _dailyHistory.RemoveAll(entry => entry.day < day - 6);
        _todayScores.Clear();
        UpdateRestaurantStatsStub();
    }

    public int LastRecordedRatingDay => _lastRecordedRatingDay;

    // 오늘 받은 개인 평가 건수
    public int TodayScoreCount => _todayScores.Count;

    // 시작 평판(day 0 시드값)이 아직 7일 평균에 포함되어 있는지
    public bool HasSeedRating => _dailyHistory.Exists(entry => entry.day == 0);

    private void OnDayAdvanced(int newDay)
    {
        // 날짜가 넘어가기 전 날의 평점이 아직 확정되지 않았다면 확정한다. (이미 확정했으면 무시)
        CommitTodayRating(newDay - 1);
        _todayScores.Clear();
    }

    private void UpdateRestaurantStatsStub()
    {
        if (RestaurantStatsStub.Instance == null) { return; }

        float rounded = Mathf.Round(RestaurantRating * 10f) / 10f;
        RestaurantStatsStub.Instance.SetRating(rounded);
    }

    // 저장된 이력이 없는 첫 실행: 1일차엔 4.0 고정, 이후 실제 일일 평점이 하나씩 쌓이며
    // (4.0 + 실제 평점들)의 평균으로 자연스럽게 옮겨가다가, 7일치 실제 데이터가 차면
    // WindowSize 초과로 이 시드값(day 0)이 자동으로 빠져 순수 7일 평균만 남는다.
    private void ResetHistoryToSeed()
    {
        _dailyHistory.Clear();
        _dailyHistory.Add(new DailyRatingEntry { day = 0, value = BaselineRating });
        _lastRecordedRatingDay = 0;
        _todayScores.Clear();
    }

    public void CaptureState(GameSaveData data)
    {
        data.dailyRatingHistory.Clear();
        foreach (DailyRatingEntry entry in _dailyHistory)
            data.dailyRatingHistory.Add(new DailyRatingEntry { day = entry.day, value = entry.value });

        data.lastRecordedRatingDay = _lastRecordedRatingDay;
    }

    // 로드는 날짜 경과가 아니므로 시드를 새로 넣거나 평점을 확정하지 않는다. 저장값 그대로 적용한다.
    public void RestoreState(GameSaveData data)
    {
        _dailyHistory.Clear();
        foreach (DailyRatingEntry entry in data.dailyRatingHistory)
        {
            if (entry != null)
                _dailyHistory.Add(new DailyRatingEntry { day = entry.day, value = entry.value });
        }

        _lastRecordedRatingDay = data.lastRecordedRatingDay;
        _todayScores.Clear();
        UpdateRestaurantStatsStub();
        OnTodayAverageChanged?.Invoke(TodayAverage);
    }
}
