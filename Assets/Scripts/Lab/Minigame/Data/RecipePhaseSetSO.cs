using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레시피 1개의 미니게임 페이즈 시퀀스.
/// CSV 기반 RecipeData(재료/수량 등 밸런싱 데이터)와는 recipeId로만 연결한다.
/// 예) 연어 스테이크: Chop → Mix → Grill(gaugeCount=2) → Assembly
/// </summary>
[CreateAssetMenu(fileName = "RecipePhaseSetSO", menuName = "Game Data/Lab/Recipe Phase Set")]
public class RecipePhaseSetSO : ScriptableObject
{
    // RecipeDatabaseSO의 RecipeData.RecipeId와 매칭
    [SerializeField]
    private int _recipeId;

    // 순서대로 진행되는 페이즈 시퀀스 (이전 페이즈로 되돌아가기 불가)
    [SerializeField]
    private List<PhaseSO> _phases = new List<PhaseSO>();

    public int RecipeId => _recipeId;
    public IReadOnlyList<PhaseSO> Phases => _phases;

    public int PhaseCount => _phases != null ? _phases.Count : 0;
}
