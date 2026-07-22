using UnityEngine;

/// <summary>
/// 강화 조건 1개의 평가 결과 (조건 체크리스트 한 줄).
/// </summary>
public struct UpgradeConditionResult
{
    public UpgradeConditionType Type;
    public string Label;     // "골드", "식당 레벨", "후라이팬 사용 요리 횟수", "대장간 레벨"
    public string ValueText; // "12,450 / 1,000", "Lv. 2 이상", "15 / 10"
    public bool Met;
}

/// <summary>
/// 강화 조건을 현재 게임 상태(각 매니저)와 대조해 평가한다.
/// 매니저가 씬에 없으면 미충족 처리한다.
/// </summary>
public static class UpgradeConditionEvaluator
{
    /// <param name="goldCurrency">Gold 조건 검사/차감에 쓸 재화 SO</param>
    public static UpgradeConditionResult Evaluate(
        UpgradeCondition condition,
        CookwareUpgradeSO tool,
        Currency goldCurrency)
    {
        UpgradeConditionResult result = new UpgradeConditionResult
        {
            Type = condition.Type,
            Met = false
        };

        switch (condition.Type)
        {
            case UpgradeConditionType.Gold:
            {
                int owned = CurrencyManager.Instance != null && goldCurrency != null
                    ? CurrencyManager.Instance.GetCurrency(goldCurrency)
                    : 0;

                result.Label = "골드";
                result.ValueText = $"{owned:N0} / {condition.Amount:N0}";
                result.Met = owned >= condition.Amount;
                break;
            }

            case UpgradeConditionType.RestaurantLevel:
            {
                int level = RestaurantLevelManager.Instance != null
                    ? RestaurantLevelManager.Instance.CurrentLevel
                    : 0;

                result.Label = "식당 레벨";
                result.ValueText = $"Lv. {condition.Amount} 이상";
                result.Met = level >= condition.Amount;
                break;
            }

            case UpgradeConditionType.CookwareUseCount:
            {
                int count = CookwareLevelState.Instance != null
                    ? CookwareLevelState.Instance.GetUseCount(tool)
                    : 0;

                string toolName = tool != null ? tool.ToolName : "도구";

                result.Label = $"{toolName} 사용 요리 횟수";
                result.ValueText = $"{count} / {condition.Amount}";
                result.Met = count >= condition.Amount;
                break;
            }

            case UpgradeConditionType.BlacksmithLevel:
            {
                int level = BlacksmithLevelManager.Instance != null
                    ? BlacksmithLevelManager.Instance.CurrentLevel
                    : 0;

                result.Label = "대장간 레벨";
                result.ValueText = $"Lv. {condition.Amount} 이상";
                result.Met = level >= condition.Amount;
                break;
            }
        }

        return result;
    }
}
