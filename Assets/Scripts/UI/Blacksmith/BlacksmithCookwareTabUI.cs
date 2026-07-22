using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대장간 "조리 도구 강화" 탭.
/// 좌측 도구 슬롯 목록 + 우측 상세(레벨업 효과 / 강화 조건 체크리스트 / 강화하기).
/// 강화 성공 시 골드 조건 금액을 차감하고 도구 레벨을 1 올린다.
/// </summary>
public class BlacksmithCookwareTabUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<CookwareUpgradeSO> _upgrades = new List<CookwareUpgradeSO>();
    [SerializeField] private Currency _goldCurrency;

    [Header("List")]
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private CookwareUpgradeSlotUI _slotPrefab;

    [Header("Detail - Header")]
    [SerializeField] private Image _detailIcon;
    [SerializeField] private TMP_Text _detailNameText;   // "후라이팬 Lv. 1"
    [SerializeField] private TMP_Text _maxLevelText;     // "최대 레벨 3"
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Detail - 레벨업 효과")]
    [SerializeField] private TMP_Text _currentHeaderText; // "현재 (Lv. 1)"
    [SerializeField] private TMP_Text _currentEffectText; // "한 요리에 사용할 수 있는 재료 개수: 2개"
    [SerializeField] private TMP_Text _nextHeaderText;    // "다음 레벨 (Lv. 2)"
    [SerializeField] private TMP_Text _nextEffectText;

    [Header("Detail - 강화 조건")]
    [SerializeField] private Transform _conditionRowRoot;
    [SerializeField] private UpgradeConditionRowUI _conditionRowPrefab;

    [Header("Detail - 강화")]
    [SerializeField] private TMP_Text _levelFromText;     // "Lv. 1"
    [SerializeField] private TMP_Text _levelToText;       // "Lv. 2"
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _upgradeButtonLabel;

    [Header("Detail - 강화 단계 선택 (- 1 + MAX)")]
    [SerializeField] private Button _stepMinusButton;
    [SerializeField] private Button _stepPlusButton;
    [SerializeField] private Button _stepMaxButton;
    [SerializeField] private TMP_Text _stepCountText;

    private readonly List<CookwareUpgradeSlotUI> _spawnedSlots = new List<CookwareUpgradeSlotUI>();
    private readonly List<UpgradeConditionRowUI> _spawnedRows = new List<UpgradeConditionRowUI>();

    private CookwareUpgradeSO _selected;
    private int _stepCount = 1; // 한 번에 올릴 레벨 수

    [Header("Debug")]
    // 버튼 비활성 원인 진단용 콘솔 로그 (확인 후 꺼도 됨)
    [SerializeField] private bool _debugLog = true;

    private void Awake()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnClickUpgrade);

        if (_stepMinusButton != null)
            _stepMinusButton.onClick.AddListener(OnClickStepMinus);

        if (_stepPlusButton != null)
            _stepPlusButton.onClick.AddListener(OnClickStepPlus);

        if (_stepMaxButton != null)
            _stepMaxButton.onClick.AddListener(OnClickStepMax);
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveListener(OnClickUpgrade);

        if (_stepMinusButton != null)
            _stepMinusButton.onClick.RemoveListener(OnClickStepMinus);

        if (_stepPlusButton != null)
            _stepPlusButton.onClick.RemoveListener(OnClickStepPlus);

        if (_stepMaxButton != null)
            _stepMaxButton.onClick.RemoveListener(OnClickStepMax);
    }

    private void OnEnable()
    {
        BuildSlots();

        if (_selected == null && _upgrades.Count > 0)
            _selected = _upgrades[0];

        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // 씬 로드 시 OnEnable이 매니저 Awake(Instance 세팅)보다 먼저 돌 수 있어
    // Start에서 구독을 한 번 더 시도한다 (Subscribe는 중복 안전).
    private void Start()
    {
        Subscribe();
        RefreshAll();
    }

    private void Subscribe()
    {
        // -= 후 += : 여러 번 호출돼도 중복 구독되지 않는다.
        if (CookwareLevelState.Instance != null)
        {
            CookwareLevelState.Instance.OnChanged -= RefreshAll;
            CookwareLevelState.Instance.OnChanged += RefreshAll;
        }

        if (BlacksmithLevelManager.Instance != null)
        {
            BlacksmithLevelManager.Instance.OnChanged -= RefreshAll;
            BlacksmithLevelManager.Instance.OnChanged += RefreshAll;
        }

        if (RestaurantLevelManager.Instance != null)
        {
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleRestaurantLevelChanged;
            RestaurantLevelManager.Instance.OnLevelChanged += HandleRestaurantLevelChanged;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }
    }

    private void Unsubscribe()
    {
        if (CookwareLevelState.Instance != null)
            CookwareLevelState.Instance.OnChanged -= RefreshAll;

        if (BlacksmithLevelManager.Instance != null)
            BlacksmithLevelManager.Instance.OnChanged -= RefreshAll;

        if (RestaurantLevelManager.Instance != null)
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleRestaurantLevelChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleRestaurantLevelChanged(int level) => RefreshAll();
    private void HandleCurrencyChanged(Currency currency, int amount) => RefreshAll();

    private void BuildSlots()
    {
        ClearSlots();

        if (_slotPrefab == null || _slotRoot == null)
            return;

        for (int i = 0; i < _upgrades.Count; i++)
        {
            if (_upgrades[i] == null)
                continue;

            CookwareUpgradeSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(_upgrades[i], _goldCurrency, SelectUpgrade);
            _spawnedSlots.Add(slot);
        }
    }

    private void SelectUpgrade(CookwareUpgradeSO upgrade)
    {
        _selected = upgrade;
        _stepCount = 1; // 도구 바꾸면 단계 선택 초기화
        RefreshAll();
    }

    private void RefreshAll()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] == null)
                continue;

            _spawnedSlots[i].Refresh();
            _spawnedSlots[i].SetSelected(_spawnedSlots[i].Upgrade == _selected);
        }

        RefreshDetail();
    }

    private void RefreshDetail()
    {
        ClearRows();

        if (_selected == null)
            return;

        int level = CookwareLevelState.Instance != null
            ? CookwareLevelState.Instance.GetLevel(_selected)
            : 1;

        bool isMax = level >= _selected.MaxLevel;

        // 헤더
        if (_detailIcon != null)
        {
            _detailIcon.sprite = _selected.Icon;
            _detailIcon.enabled = _selected.Icon != null;
        }

        if (_detailNameText != null)
            _detailNameText.text = $"{_selected.ToolName} Lv. {level}";

        if (_maxLevelText != null)
            _maxLevelText.text = $"최대 레벨 {_selected.MaxLevel}";

        if (_descriptionText != null)
            _descriptionText.text = _selected.Description;

        // 단계 선택 범위 보정 (레벨업/도구 변경 후에도 유효하도록)
        int maxSteps = isMax ? 0 : _selected.MaxLevel - level;
        _stepCount = Mathf.Clamp(_stepCount, 1, Mathf.Max(1, maxSteps));

        int targetLevel = level + _stepCount;

        // 레벨업 효과 (현재 → 선택한 목표 레벨)
        if (_currentHeaderText != null)
            _currentHeaderText.text = $"현재 (Lv. {level})";

        if (_currentEffectText != null)
            _currentEffectText.text = $"한 요리에 사용할 수 있는 재료 개수: {_selected.GetMaxIngredientCount(level)}개";

        if (_nextHeaderText != null)
            _nextHeaderText.text = isMax ? "최대 레벨" : $"다음 레벨 (Lv. {targetLevel})";

        if (_nextEffectText != null)
        {
            _nextEffectText.text = isMax
                ? "-"
                : $"한 요리에 사용할 수 있는 재료 개수: {_selected.GetMaxIngredientCount(targetLevel)}개";
        }

        // 강화 조건 체크리스트 (구간 합산: 골드=합계, 나머지=최고 요구치)
        bool allMet = false;

        if (!isMax)
        {
            List<UpgradeCondition> aggregated = BuildAggregatedConditions(level, _stepCount);
            allMet = BuildConditionRows(aggregated);
        }

        // Lv. X > Lv. Y
        if (_levelFromText != null)
            _levelFromText.text = $"Lv. {level}";

        if (_levelToText != null)
            _levelToText.text = isMax ? "-" : $"Lv. {targetLevel}";

        // 단계 선택 UI
        if (_stepCountText != null)
            _stepCountText.text = isMax ? "-" : _stepCount.ToString();

        if (_stepMinusButton != null)
            _stepMinusButton.interactable = !isMax && _stepCount > 1;

        if (_stepPlusButton != null)
            _stepPlusButton.interactable = !isMax && _stepCount < maxSteps;

        // MAX는 조건 충족 여부와 무관하게 최대 레벨까지 선택 가능
        if (_stepMaxButton != null)
            _stepMaxButton.interactable = !isMax;

        // 강화하기 버튼
        if (_upgradeButton != null)
            _upgradeButton.interactable = !isMax && allMet;

        if (_upgradeButtonLabel != null)
            _upgradeButtonLabel.text = isMax ? "최대 레벨" : "강화하기";

        if (_debugLog)
        {
            Debug.Log(
                $"[Blacksmith] {_selected.ToolName}: Lv={level}/{_selected.MaxLevel}, isMax={isMax}, " +
                $"maxSteps={maxSteps}, step={_stepCount}, allMet={allMet}\n" +
                $"  managers: LevelState={(CookwareLevelState.Instance != null)}, " +
                $"Blacksmith={(BlacksmithLevelManager.Instance != null)}, " +
                $"Restaurant={(RestaurantLevelManager.Instance != null)}, " +
                $"Currency={(CurrencyManager.Instance != null)}, gold할당={_goldCurrency != null}",
                this);
        }
    }

    /// <summary>
    /// 현재 레벨에서 steps 단계 올리는 데 필요한 조건을 구간 합산한다.
    /// 골드/사용횟수는 소모 자원이라 각 레벨 요구량의 합계, 식당레벨/대장간레벨은 구간 내 최고 요구치.
    /// </summary>
    private List<UpgradeCondition> BuildAggregatedConditions(int currentLevel, int steps)
    {
        List<UpgradeConditionType> order = new List<UpgradeConditionType>();
        Dictionary<UpgradeConditionType, int> amounts = new Dictionary<UpgradeConditionType, int>();

        for (int s = 1; s <= steps; s++)
        {
            if (!_selected.TryGetLevelData(currentLevel + s, out CookwareLevelData levelData))
                continue;

            for (int i = 0; i < levelData.UpgradeConditions.Count; i++)
            {
                UpgradeCondition condition = levelData.UpgradeConditions[i];

                bool isConsumable = condition.Type == UpgradeConditionType.Gold
                    || condition.Type == UpgradeConditionType.CookwareUseCount;

                if (!amounts.ContainsKey(condition.Type))
                {
                    order.Add(condition.Type);
                    amounts[condition.Type] = condition.Amount;
                }
                else if (isConsumable)
                {
                    amounts[condition.Type] += condition.Amount; // 소모 자원은 합산
                }
                else
                {
                    amounts[condition.Type] = Mathf.Max(amounts[condition.Type], condition.Amount);
                }
            }
        }

        List<UpgradeCondition> result = new List<UpgradeCondition>(order.Count);

        for (int i = 0; i < order.Count; i++)
            result.Add(new UpgradeCondition(order[i], amounts[order[i]]));

        return result;
    }

    /// <summary>조건 행을 생성하고 전체 충족 여부를 반환한다.</summary>
    private bool BuildConditionRows(List<UpgradeCondition> conditions)
    {
        bool allMet = true;

        for (int i = 0; i < conditions.Count; i++)
        {
            UpgradeConditionResult result = UpgradeConditionEvaluator.Evaluate(
                conditions[i],
                _selected,
                _goldCurrency);

            allMet &= result.Met;

            if (_debugLog)
                Debug.Log($"[Blacksmith]   조건: {result.Label} = {result.ValueText} → {(result.Met ? "충족" : "미충족")}", this);

            if (_conditionRowPrefab != null && _conditionRowRoot != null)
            {
                UpgradeConditionRowUI row = Instantiate(_conditionRowPrefab, _conditionRowRoot);
                row.Bind(result);
                _spawnedRows.Add(row);
            }
        }

        return allMet;
    }

    private bool AreAllConditionsMet(List<UpgradeCondition> conditions)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            UpgradeConditionResult result = UpgradeConditionEvaluator.Evaluate(
                conditions[i], _selected, _goldCurrency);

            if (!result.Met)
                return false;
        }

        return true;
    }

    private void OnClickStepMinus()
    {
        _stepCount = Mathf.Max(1, _stepCount - 1);
        RefreshAll();
    }

    private void OnClickStepPlus()
    {
        _stepCount++; // 상한은 RefreshDetail에서 클램프
        RefreshAll();
    }

    /// <summary>
    /// 최대 레벨까지의 단계를 선택한다 (조건 충족 여부와 무관 — 합산 요구치는 체크리스트가 보여주고,
    /// 실제 진행 가능 여부는 강화하기 버튼이 판단한다).
    /// </summary>
    private void OnClickStepMax()
    {
        if (_selected == null || CookwareLevelState.Instance == null)
            return;

        int level = CookwareLevelState.Instance.GetLevel(_selected);
        _stepCount = Mathf.Max(1, _selected.MaxLevel - level);

        RefreshAll();
    }

    private void OnClickUpgrade()
    {
        if (_selected == null || CookwareLevelState.Instance == null)
            return;

        int level = CookwareLevelState.Instance.GetLevel(_selected);

        if (level >= _selected.MaxLevel)
            return;

        int steps = Mathf.Clamp(_stepCount, 1, _selected.MaxLevel - level);

        // 클릭 시점에 구간 합산 조건 재검증 (버튼 활성화 이후 상태가 변했을 수 있음)
        List<UpgradeCondition> aggregated = BuildAggregatedConditions(level, steps);

        if (!AreAllConditionsMet(aggregated))
            return;

        // 소모 자원 차감: 골드 합산 금액 + 사용횟수 합산량
        for (int i = 0; i < aggregated.Count; i++)
        {
            if (aggregated[i].Type == UpgradeConditionType.Gold
                && CurrencyManager.Instance != null && _goldCurrency != null)
            {
                CurrencyTransaction tx = new CurrencyTransaction(
                    _goldCurrency,
                    -aggregated[i].Amount,
                    TransactionSource.BlacksmithUpgrade);

                CurrencyManager.Instance.ProcessTransaction(tx);
            }
            else if (aggregated[i].Type == UpgradeConditionType.CookwareUseCount)
            {
                CookwareLevelState.Instance.ConsumeUseCount(_selected, aggregated[i].Amount);
            }
        }

        _stepCount = 1;
        CookwareLevelState.Instance.SetLevel(_selected, level + steps);
        // SetLevel → OnChanged → RefreshAll 이 자동으로 돈다.
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
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
