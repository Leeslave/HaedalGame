using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대장간 "식당 레벨 강화" 탭.
/// 좌측 현재→다음 레벨 + 혜택 표, 우측 강화 조건 체크리스트 + "Lv. N로 강화" 버튼.
/// 식당 레벨은 순차 강화만 가능 (단계 선택 없음).
/// 좌석/주간평점/직원 조건은 RestaurantStatsStub에서 읽는다 (시스템 연결 전 임시).
/// </summary>
public class BlacksmithRestaurantTabUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RestaurantLevelTableSO _levelTable;
    [SerializeField] private Currency _goldCurrency;

    [Header("좌측 - 레벨 표시")]
    [SerializeField] private TMP_Text _currentLevelText;  // "Lv. 2"
    [SerializeField] private TMP_Text _nextLevelText;     // "Lv. 3" (최대면 "-")

    [Header("좌측 - 레벨 혜택 (현재 / 다음)")]
    [SerializeField] private TMP_Text _seatsCurrentText;  // "12석"
    [SerializeField] private TMP_Text _seatsNextText;     // "16석 (+4석)"
    [SerializeField] private TMP_Text _ratingCurrentText; // "3.0점"
    [SerializeField] private TMP_Text _ratingNextText;    // "3.5점 (+0.5)"
    [SerializeField] private TMP_Text _staffCurrentText;  // "8명"
    [SerializeField] private TMP_Text _staffNextText;     // "10명 (+2명)"
    [SerializeField] private TMP_Text _gradeCurrentText;  // "C"
    [SerializeField] private TMP_Text _gradeNextText;     // "B"

    [Header("우측 - 상세")]
    [SerializeField] private TMP_Text _detailTitleText;   // "식당 레벨 Lv. 3"
    [SerializeField] private Transform _conditionRowRoot;
    [SerializeField] private UpgradeConditionRowUI _conditionRowPrefab;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _upgradeButtonLabel; // "Lv. 3로 강화"

    private readonly List<UpgradeConditionRowUI> _spawnedRows = new List<UpgradeConditionRowUI>();

    private void Awake()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnClickUpgrade);
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveListener(OnClickUpgrade);
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (RestaurantLevelManager.Instance != null)
        {
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
            RestaurantLevelManager.Instance.OnLevelChanged += HandleLevelChanged;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }

        if (RestaurantStatsStub.Instance != null)
        {
            RestaurantStatsStub.Instance.OnChanged -= Refresh;
            RestaurantStatsStub.Instance.OnChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (RestaurantLevelManager.Instance != null)
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;

        if (RestaurantStatsStub.Instance != null)
            RestaurantStatsStub.Instance.OnChanged -= Refresh;
    }

    private void HandleLevelChanged(int level) => Refresh();
    private void HandleCurrencyChanged(Currency currency, int amount) => Refresh();

    private void Refresh()
    {
        ClearRows();

        if (_levelTable == null)
            return;

        int currentLevel = RestaurantLevelManager.Instance != null
            ? RestaurantLevelManager.Instance.CurrentLevel
            : 1;

        int nextLevel = currentLevel + 1;
        bool isMax = nextLevel > _levelTable.MaxLevel;

        _levelTable.TryGetEntry(currentLevel, out RestaurantLevelEntry current);
        RestaurantLevelEntry next = null;

        if (!isMax)
            _levelTable.TryGetEntry(nextLevel, out next);

        // 레벨 표시
        if (_currentLevelText != null)
            _currentLevelText.text = $"Lv. {currentLevel}";

        if (_nextLevelText != null)
            _nextLevelText.text = isMax ? "-" : $"Lv. {nextLevel}";

        if (_detailTitleText != null)
            _detailTitleText.text = isMax ? $"식당 레벨 Lv. {currentLevel} (최대)" : $"식당 레벨 Lv. {nextLevel}";

        // 혜택 표
        RefreshBenefits(current, next, isMax);

        // 조건 체크리스트
        bool allMet = !isMax && next != null && BuildConditionRows(currentLevel, next);

        // 강화 버튼
        if (_upgradeButton != null)
            _upgradeButton.interactable = allMet;

        if (_upgradeButtonLabel != null)
            _upgradeButtonLabel.text = isMax ? "최대 레벨" : $"Lv. {nextLevel}로 강화";
    }

    private void RefreshBenefits(RestaurantLevelEntry current, RestaurantLevelEntry next, bool isMax)
    {
        if (current == null)
            return;

        if (_seatsCurrentText != null)
            _seatsCurrentText.text = $"{current.MaxSeatCount}석";

        if (_ratingCurrentText != null)
            _ratingCurrentText.text = $"{current.WeeklyRatingCap:0.0}점";

        if (_staffCurrentText != null)
            _staffCurrentText.text = $"{current.MaxStaffCount}명";

        if (_gradeCurrentText != null)
            _gradeCurrentText.text = current.GradeCap;

        if (isMax || next == null)
        {
            if (_seatsNextText != null) _seatsNextText.text = "-";
            if (_ratingNextText != null) _ratingNextText.text = "-";
            if (_staffNextText != null) _staffNextText.text = "-";
            if (_gradeNextText != null) _gradeNextText.text = "-";
            return;
        }

        if (_seatsNextText != null)
            _seatsNextText.text = $"{next.MaxSeatCount}석 (+{next.MaxSeatCount - current.MaxSeatCount}석)";

        if (_ratingNextText != null)
            _ratingNextText.text = $"{next.WeeklyRatingCap:0.0}점 (+{next.WeeklyRatingCap - current.WeeklyRatingCap:0.0})";

        if (_staffNextText != null)
            _staffNextText.text = $"{next.MaxStaffCount}명 (+{next.MaxStaffCount - current.MaxStaffCount}명)";

        if (_gradeNextText != null)
            _gradeNextText.text = next.GradeCap;
    }

    /// <summary>다음 레벨 조건 행들을 생성하고 전체 충족 여부를 반환한다.</summary>
    private bool BuildConditionRows(int currentLevel, RestaurantLevelEntry next)
    {
        bool allMet = true;

        // 골드
        int gold = CurrencyManager.Instance != null && _goldCurrency != null
            ? CurrencyManager.Instance.GetCurrency(_goldCurrency)
            : 0;

        allMet &= AddRow("골드", $"{gold:N0} / {next.GoldCost:N0}", gold >= next.GoldCost);

        // 식당 좌석 수
        if (next.RequiredSeatCount > 0)
        {
            int seats = RestaurantStatsStub.Instance != null
                ? RestaurantStatsStub.Instance.CurrentSeatCount
                : 0;

            allMet &= AddRow("식당 좌석 수", $"{seats} / {next.RequiredSeatCount}", seats >= next.RequiredSeatCount);
        }

        // 주간 평점
        if (next.RequiredWeeklyRating > 0f)
        {
            float rating = RestaurantStatsStub.Instance != null
                ? RestaurantStatsStub.Instance.WeeklyRating
                : 0f;

            allMet &= AddRow("주간 평점", $"{rating:0.0} / {next.RequiredWeeklyRating:0.0}", rating >= next.RequiredWeeklyRating);
        }

        // 보유 직원
        if (!string.IsNullOrEmpty(next.RequiredStaffGrade))
        {
            bool hasStaff = RestaurantStatsStub.Instance != null
                && RestaurantStatsStub.Instance.HasStaff(next.RequiredStaffGrade, next.RequiredStaffCount);

            allMet &= AddRow(
                "보유 직원",
                $"{next.RequiredStaffGrade} 등급 이상 직원 {next.RequiredStaffCount}명 보유",
                hasStaff);
        }

        // 식당 레벨 (순차 강화 확인용 — 항상 충족 상태로 표시)
        allMet &= AddRow("식당 레벨", $"Lv. {currentLevel} / Lv. {currentLevel}", true);

        return allMet;
    }

    private bool AddRow(string label, string valueText, bool met)
    {
        if (_conditionRowPrefab != null && _conditionRowRoot != null)
        {
            UpgradeConditionRowUI row = Instantiate(_conditionRowPrefab, _conditionRowRoot);

            row.Bind(new UpgradeConditionResult
            {
                Label = label,
                ValueText = valueText,
                Met = met
            });

            _spawnedRows.Add(row);
        }

        return met;
    }

    private void OnClickUpgrade()
    {
        if (_levelTable == null || RestaurantLevelManager.Instance == null)
            return;

        int currentLevel = RestaurantLevelManager.Instance.CurrentLevel;
        int nextLevel = currentLevel + 1;

        if (nextLevel > _levelTable.MaxLevel)
            return;

        if (!_levelTable.TryGetEntry(nextLevel, out RestaurantLevelEntry next))
            return;

        // 클릭 시점 재검증
        ClearRows();
        bool allMet = BuildConditionRows(currentLevel, next);

        if (!allMet)
        {
            Refresh();
            return;
        }

        // 골드 차감
        if (CurrencyManager.Instance != null && _goldCurrency != null && next.GoldCost > 0)
        {
            CurrencyManager.Instance.ProcessTransaction(new CurrencyTransaction(
                _goldCurrency, -next.GoldCost, TransactionSource.BlacksmithUpgrade));
        }

        RestaurantLevelManager.Instance.LevelUp();
        // OnLevelChanged → Refresh 가 자동으로 돈다.
    }

    private void ClearRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }

        _spawnedRows.Clear();
    }
}
