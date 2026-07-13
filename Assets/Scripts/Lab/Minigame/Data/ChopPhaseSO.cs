using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재료별 절단 비주얼. 클릭 진행률에 따라 스프라이트가 순서대로 교체된다 (마지막 = 다 잘린 모습).
/// 스프라이트가 없으면 라벨 카운트 + 클릭 피드백만으로 진행된다.
/// </summary>
[Serializable]
public class ChopTarget
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private List<Sprite> _cutStageSprites = new List<Sprite>();

    public int IngredientId => _ingredientId;
    public IReadOnlyList<Sprite> CutStageSprites => _cutStageSprites;
}

/// <summary>
/// 썰기/다지기 페이즈. 재료를 도마에 드래그 후 클릭 연타로 자른다.
/// </summary>
[CreateAssetMenu(fileName = "ChopPhaseSO", menuName = "Game Data/Lab/Phase/Chop")]
public class ChopPhaseSO : PhaseSO
{
    [Header("Chop")]
    // 재료 1개를 자르는 데 필요한 클릭 횟수
    [SerializeField] private int _clickCountRequired = 5;

    // 재료별 절단 단계 스프라이트 (없는 재료는 라벨 카운트로만 표시)
    [SerializeField] private List<ChopTarget> _chopTargets = new List<ChopTarget>();

    public int ClickCountRequired => _clickCountRequired;
    public IReadOnlyList<ChopTarget> ChopTargets => _chopTargets;

    public override CookingActionType ActionType => CookingActionType.Chop;

    public bool TryGetChopTarget(int ingredientId, out ChopTarget target)
    {
        for (int i = 0; i < _chopTargets.Count; i++)
        {
            if (_chopTargets[i] != null && _chopTargets[i].IngredientId == ingredientId)
            {
                target = _chopTargets[i];
                return true;
            }
        }

        target = null;
        return false;
    }
}
