using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대장간 "식당 레벨 강화" 탭 (목업 재현).
/// 좌측: 현재 → 다음 레벨 + 강화 조건 체크리스트 + "Lv. N로 강화" 버튼 + 공사 기간.
/// 우측: 레벨 혜택 표 6종 (현재/다음).
/// 식당 레벨은 순차 강화만 가능. 주간평점/판매금액/좌석 조건은 RestaurantStatsStub에서 읽는다(임시).
/// </summary>
public class BlacksmithRestaurantTabUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField]
    private RestaurantLevelTableSO _levelTable;

    [SerializeField]
    private Currency _goldCurrency;

    [Header("레벨 표시")]
    [SerializeField]
    private TMP_Text _currentLevelText; // "Lv. 3"

    [SerializeField]
    private TMP_Text _nextLevelText; // "Lv. 4" (최대면 "-")

    [Header("강화 조건 체크리스트")]
    [SerializeField]
    private Transform _conditionRowRoot;

    [SerializeField]
    private UpgradeConditionRowUI _conditionRowPrefab;

    [Header("강화 버튼")]
    [SerializeField]
    private Button _upgradeButton;

    [SerializeField]
    private TMP_Text _upgradeButtonLabel; // "Lv. 4로 강화"

    [SerializeField]
    private TMP_Text _constructionText; // "공사 기간 : 1일 (휴업)"

    [Header("레벨 혜택 표 (5행)")]
    [SerializeField]
    private RestaurantBenefitRowUI _seatRow; // 손님 좌석

    [SerializeField]
    private RestaurantBenefitRowUI _cookingSpaceRow; // 요리 공간

    [SerializeField]
    private RestaurantBenefitRowUI _waitingQueueRow; // 대기열

    [SerializeField]
    private RestaurantBenefitRowUI _recipeGradeRow; // 레시피 등급

    [SerializeField]
    private RestaurantBenefitRowUI _dailyVisitorRow; // 하루 방문 손님

    [Header("Debug")]
    [SerializeField]
    private bool _debugLog = true;

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

        if (_debugLog)
        {
            Debug.Log(
                $"[식당탭] levelTable={_levelTable != null}, conditionRoot={_conditionRowRoot != null}, " +
                $"conditionPrefab={_conditionRowPrefab != null}, RestaurantMgr={RestaurantLevelManager.Instance != null}, " +
                $"Currency={CurrencyManager.Instance != null}, gold할당={_goldCurrency != null}, Stub={RestaurantStatsStub.Instance != null}",
                this);
        }

        if (_levelTable == null)
        {
            if (_debugLog)
                Debug.LogWarning("[식당탭] _levelTable 미할당 → 아무것도 표시 안 됨", this);
            return;
        }

        int currentLevel =
            RestaurantLevelManager.Instance != null
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

        RefreshBenefits(current, next, isMax);

        bool allMet = !isMax && next != null && BuildConditionRows(currentLevel, next);

        if (_debugLog)
            Debug.Log($"[식당탭] Lv={currentLevel}/{_levelTable.MaxLevel}, isMax={isMax}, 조건행={_spawnedRows.Count}개 생성, allMet={allMet}", this);

        if (_upgradeButton != null)
            _upgradeButton.interactable = allMet;

        if (_upgradeButtonLabel != null)
            _upgradeButtonLabel.text = isMax ? "최대 레벨" : $"Lv. {nextLevel}로 강화";

        if (_constructionText != null)
            _constructionText.text =
                (isMax || next == null)
                    ? string.Empty
                    : $"공사 기간 : {next.ConstructionDays}일 (휴업)";
    }

    private void RefreshBenefits(
        RestaurantLevelEntry current,
        RestaurantLevelEntry next,
        bool isMax
    )
    {
        if (current == null)
            return;

        bool hasNext = !isMax && next != null;

        SetBenefit(
            _seatRow,
            $"{current.CustomerSeats}개",
            hasNext ? IntDelta(current.CustomerSeats, next.CustomerSeats, "개") : "-"
        );

        SetBenefit(
            _cookingSpaceRow,
            $"{current.CookingSpace}개",
            hasNext ? IntDelta(current.CookingSpace, next.CookingSpace, "개") : "-"
        );

        SetBenefit(
            _waitingQueueRow,
            $"{current.WaitingQueue}명",
            hasNext ? IntDelta(current.WaitingQueue, next.WaitingQueue, "명") : "-"
        );

        SetBenefit(
            _recipeGradeRow,
            $"{current.RecipeGradeMin} ~ {current.RecipeGradeMax}",
            hasNext ? $"{next.RecipeGradeMin} ~ {next.RecipeGradeMax}" : "-"
        );

        SetBenefit(
            _dailyVisitorRow,
            $"{current.DailyVisitors}명",
            hasNext ? IntDelta(current.DailyVisitors, next.DailyVisitors, "명") : "-"
        );
    }

    private static void SetBenefit(RestaurantBenefitRowUI row, string currentText, string nextText)
    {
        if (row != null)
            row.SetValues(currentText, nextText);
    }

    /// <summary>"16석 (+4석)" 형식. 증가분이 0 이하면 값만 표시.</summary>
    private static string IntDelta(int current, int next, string unit)
    {
        int delta = next - current;
        return delta > 0 ? $"{next}{unit} (+{delta}{unit})" : $"{next}{unit}";
    }

    /// <summary>다음 레벨 조건 행들을 생성하고 전체 충족 여부를 반환한다 (레벨/골드/평점/금액/개수).</summary>
    private bool BuildConditionRows(int currentLevel, RestaurantLevelEntry next)
    {
        bool allMet = true;

        // 1) 식당 레벨 (순차 강화 확인용 — 현재 레벨이 곧 요구치, 항상 충족)
        allMet &= AddRow("식당 레벨", $"Lv. {currentLevel} / Lv. {currentLevel}", true);

        // 2) 골드
        int gold =
            CurrencyManager.Instance != null && _goldCurrency != null
                ? CurrencyManager.Instance.GetCurrency(_goldCurrency)
                : 0;
        allMet &= AddRow("골드", $"{gold:N0} / {next.GoldCost:N0}", gold >= next.GoldCost);

        // 3) 평점
        if (next.RequiredRating > 0f)
        {
            float rating =
                RestaurantStatsStub.Instance != null ? RestaurantStatsStub.Instance.Rating : 0f;
            allMet &= AddRow("평점", $"{rating:0.0} / {next.RequiredRating:0.0}", rating >= next.RequiredRating);
        }

        // 4) 판매 금액
        if (next.RequiredSalesAmount > 0)
        {
            int sales =
                RestaurantStatsStub.Instance != null ? RestaurantStatsStub.Instance.SalesAmount : 0;
            allMet &= AddRow("판매 금액", $"{sales:N0} / {next.RequiredSalesAmount:N0}", sales >= next.RequiredSalesAmount);
        }

        // 5) 좌석 개수
        if (next.RequiredSeatCount > 0)
        {
            int seats =
                RestaurantStatsStub.Instance != null ? RestaurantStatsStub.Instance.SeatCount : 0;
            allMet &= AddRow("좌석 개수", $"{seats} / {next.RequiredSeatCount}", seats >= next.RequiredSeatCount);
        }

        return allMet;
    }

    private bool AddRow(string label, string valueText, bool met)
    {
        if (_conditionRowPrefab != null && _conditionRowRoot != null)
        {
            UpgradeConditionRowUI row = Instantiate(_conditionRowPrefab, _conditionRowRoot);
            row.Bind(
                new UpgradeConditionResult
                {
                    Label = label,
                    ValueText = valueText,
                    Met = met,
                }
            );
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
            CurrencyManager.Instance.ProcessTransaction(
                new CurrencyTransaction(
                    _goldCurrency,
                    -next.GoldCost,
                    TransactionSource.BlacksmithUpgrade
                )
            );
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
