using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조리 도구 목록의 슬롯 1칸. 아이콘 / 이름 / "Lv. 1 / 3" / 강화 조건 충족 게이지 "2 / 4".
/// </summary>
public class CookwareUpgradeSlotUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;          // "Lv. 1 / 3"
    [SerializeField] private Image _conditionGaugeFill;    // 다음 레벨 조건 충족 비율
    [SerializeField] private TMP_Text _conditionGaugeText; // "충족 2 / 4"
    [SerializeField] private GameObject _selectedFrame;

    private CookwareUpgradeSO _upgrade;
    private Currency _goldCurrency;
    private Action<CookwareUpgradeSO> _onClick;

    public CookwareUpgradeSO Upgrade => _upgrade;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(CookwareUpgradeSO upgrade, Currency goldCurrency, Action<CookwareUpgradeSO> onClick)
    {
        _upgrade = upgrade;
        _goldCurrency = goldCurrency;
        _onClick = onClick;
        Refresh();
    }

    /// <summary>레벨/사용횟수 상태를 다시 읽어 표시를 갱신한다.</summary>
    public void Refresh()
    {
        if (_upgrade == null)
            return;

        int level = CookwareLevelState.Instance != null
            ? CookwareLevelState.Instance.GetLevel(_upgrade)
            : 1;

        if (_iconImage != null)
        {
            _iconImage.sprite = _upgrade.Icon;
            _iconImage.enabled = _upgrade.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = _upgrade.ToolName;

        if (_levelText != null)
            _levelText.text = $"Lv. {level} / {_upgrade.MaxLevel}";

        RefreshConditionGauge(level);
    }

    /// <summary>게이지 = 다음 레벨 강화 조건 중 충족된 개수 비율. 최대 레벨이면 가득 + "-".</summary>
    private void RefreshConditionGauge(int level)
    {
        bool isMax = level >= _upgrade.MaxLevel;

        if (isMax || !_upgrade.TryGetLevelData(level + 1, out CookwareLevelData next)
                  || next.UpgradeConditions.Count == 0)
        {
            if (_conditionGaugeFill != null)
                _conditionGaugeFill.fillAmount = 1f;

            if (_conditionGaugeText != null)
                _conditionGaugeText.text = "-";

            return;
        }

        int total = next.UpgradeConditions.Count;
        int met = 0;

        for (int i = 0; i < total; i++)
        {
            UpgradeConditionResult result = UpgradeConditionEvaluator.Evaluate(
                next.UpgradeConditions[i], _upgrade, _goldCurrency);

            if (result.Met)
                met++;
        }

        if (_conditionGaugeFill != null)
            _conditionGaugeFill.fillAmount = (float)met / total;

        if (_conditionGaugeText != null)
            _conditionGaugeText.text = $"{met} / {total}";
    }

    public void SetSelected(bool selected)
    {
        if (_selectedFrame != null)
            _selectedFrame.SetActive(selected);
    }

    private void HandleClick()
    {
        _onClick?.Invoke(_upgrade);
    }
}
