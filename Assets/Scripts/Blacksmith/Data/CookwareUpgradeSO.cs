using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 강화 조건 종류 (대장간 UI의 조건 행에 대응).
/// </summary>
public enum UpgradeConditionType
{
    Gold,             // 골드 N 이상 보유 (강화 시 차감)
    RestaurantLevel,  // 식당 레벨 N 이상
    CookwareUseCount, // 해당 도구 사용 요리 횟수 N 이상
    BlacksmithLevel   // 대장간 레벨 N 이상
}

[Serializable]
public class UpgradeCondition
{
    [SerializeField] private UpgradeConditionType _type;
    [SerializeField] private int _amount;

    public UpgradeConditionType Type => _type;
    public int Amount => _amount;

    public UpgradeCondition() { }

    /// <summary>다단계 강화 시 구간 조건 합산용 런타임 생성자.</summary>
    public UpgradeCondition(UpgradeConditionType type, int amount)
    {
        _type = type;
        _amount = amount;
    }
}

/// <summary>
/// 도구의 레벨 1개 분량 데이터.
/// 효과(그 레벨에서 한 요리에 사용 가능한 재료 개수)와
/// 그 레벨로 올라가기 위한 강화 조건(이전 레벨 → 이 레벨)을 담는다. Lv.1은 조건 없음.
/// </summary>
[Serializable]
public class CookwareLevelData
{
    [SerializeField] private int _maxIngredientCount = 2;
    [SerializeField] private List<UpgradeCondition> _upgradeConditions = new List<UpgradeCondition>();

    public int MaxIngredientCount => _maxIngredientCount;
    public IReadOnlyList<UpgradeCondition> UpgradeConditions => _upgradeConditions;
}

/// <summary>
/// 식당 조리 도구 1종의 대장간 강화 데이터 (후라이팬/튀김기/도마/솥).
/// 미니게임(Lab)의 CookwareSO와는 별개 시스템 — 이 SO 자체가 도구의 정체성이다.
/// levels[0] = Lv.1 (기본, 조건 없음), levels[1] = Lv.2 (Lv.1→2 조건), ...
/// </summary>
[CreateAssetMenu(fileName = "CookwareUpgradeSO", menuName = "Game Data/Blacksmith/Cookware Upgrade")]
public class CookwareUpgradeSO : ScriptableObject
{
    [SerializeField] private string _toolName; // 예: "후라이팬"

    [TextArea]
    [SerializeField] private string _description; // 예: "열 전달이 뛰어난 팬으로..."

    [SerializeField] private Sprite _icon;

    [SerializeField] private List<CookwareLevelData> _levels = new List<CookwareLevelData>();

    public string ToolName => _toolName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int MaxLevel => _levels.Count;

    /// <summary>level은 1-based. 범위 밖이면 false.</summary>
    public bool TryGetLevelData(int level, out CookwareLevelData data)
    {
        int index = level - 1;

        if (index < 0 || index >= _levels.Count)
        {
            data = null;
            return false;
        }

        data = _levels[index];
        return data != null;
    }

    /// <summary>해당 레벨의 재료 개수 효과. 없으면 0.</summary>
    public int GetMaxIngredientCount(int level)
    {
        return TryGetLevelData(level, out CookwareLevelData data) ? data.MaxIngredientCount : 0;
    }
}
