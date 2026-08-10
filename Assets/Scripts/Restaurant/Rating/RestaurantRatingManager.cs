using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public enum RatingBuffTier
{
    Neutral,
    Buff,
    Debuff
}

// 식당 평점(개인 평점 누적 + 7일 이동평균) 및 평점 구간별 실시간 버프/디버프 배수를 관리하는 싱글턴.
public class RestaurantRatingManager : MonoBehaviour
{
    public static RestaurantRatingManager Instance { get; private set; }

    private const string SaveKey = "RestaurantDailyRatings";
    private const int WindowSize = 7;
    private const float BaselineRating = 4.0f; // 7일치 데이터가 다 쌓이기 전, 아직 없는 날짜를 대신하는 기본값

    [SerializeField] private RestaurantLevelTableSO _levelTable;

    private List<float> _todayScores = new List<float>();
    private Queue<float> _dailyHistory = new Queue<float>();

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

        LoadDailyHistory();
    }

    private void Start()
    {
        if (InGameTimeManager.Instance != null)
        {
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
            foreach (float value in _dailyHistory) { sum += value; }
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

    private void OnDayAdvanced(int newDay)
    {
        if (_todayScores.Count > 0)
        {
            _dailyHistory.Enqueue(TodayAverage);
            while (_dailyHistory.Count > WindowSize) { _dailyHistory.Dequeue(); }
            SaveDailyHistory();
        }

        _todayScores.Clear();
        UpdateRestaurantStatsStub();
    }

    private void UpdateRestaurantStatsStub()
    {
        if (RestaurantStatsStub.Instance == null) { return; }

        float rounded = Mathf.Round(RestaurantRating * 10f) / 10f;
        RestaurantStatsStub.Instance.SetRating(rounded);
    }

    private void LoadDailyHistory()
    {
        _dailyHistory.Clear();
        string raw = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            // 저장된 이력이 없는 첫 실행: 1일차엔 4.0 고정, 이후 실제 일일 평점이 하나씩 큐에 쌓이며
            // (4.0 + 실제 평점들)의 평균으로 자연스럽게 옮겨가다가, 7일치 실제 데이터가 차면
            // WindowSize 초과로 이 시드값이 자동으로 dequeue되어 순수 7일 평균만 남는다.
            _dailyHistory.Enqueue(BaselineRating);
            return;
        }

        string[] parts = raw.Split(',');
        foreach (string part in parts)
        {
            float value;
            if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                _dailyHistory.Enqueue(value);
            }
        }
    }

    private void SaveDailyHistory()
    {
        StringBuilder sb = new StringBuilder();
        bool first = true;
        foreach (float value in _dailyHistory)
        {
            if (!first) { sb.Append(','); }
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            first = false;
        }

        PlayerPrefs.SetString(SaveKey, sb.ToString());
        PlayerPrefs.Save();
    }
}
