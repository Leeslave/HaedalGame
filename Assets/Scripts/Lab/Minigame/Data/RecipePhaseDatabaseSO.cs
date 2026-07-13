using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// recipeId → RecipePhaseSetSO 조회용 데이터베이스.
/// RecipeDatabaseSO와 동일한 캐시 패턴을 사용한다.
/// </summary>
[CreateAssetMenu(
    fileName = "RecipePhaseDatabaseSO",
    menuName = "Game Data/Lab/Recipe Phase Database"
)]
public class RecipePhaseDatabaseSO : ScriptableObject
{
    [SerializeField]
    private List<RecipePhaseSetSO> _phaseSets = new List<RecipePhaseSetSO>();

    private Dictionary<int, RecipePhaseSetSO> _phaseSetByRecipeId;

    public IReadOnlyList<RecipePhaseSetSO> PhaseSets => _phaseSets;

    public void BuildCache()
    {
        _phaseSetByRecipeId = new Dictionary<int, RecipePhaseSetSO>();

        if (_phaseSets == null)
            _phaseSets = new List<RecipePhaseSetSO>();

        for (int i = 0; i < _phaseSets.Count; i++)
        {
            RecipePhaseSetSO phaseSet = _phaseSets[i];

            if (phaseSet == null)
                continue;

            if (!_phaseSetByRecipeId.ContainsKey(phaseSet.RecipeId))
                _phaseSetByRecipeId.Add(phaseSet.RecipeId, phaseSet);
            else
                Debug.LogWarning($"Duplicate RecipeId in phase sets: {phaseSet.RecipeId}", this);
        }
    }

    public bool TryGetPhaseSet(int recipeId, out RecipePhaseSetSO phaseSet)
    {
        EnsureCache();
        return _phaseSetByRecipeId.TryGetValue(recipeId, out phaseSet);
    }

    /// <summary>
    /// 해당 레시피에 미니게임 페이즈 시퀀스가 정의되어 있는지 여부.
    /// </summary>
    public bool HasPhaseSet(int recipeId)
    {
        EnsureCache();
        return _phaseSetByRecipeId.ContainsKey(recipeId);
    }

    private void OnEnable()
    {
        if (_phaseSets == null)
            _phaseSets = new List<RecipePhaseSetSO>();

        BuildCache();
    }

    private void EnsureCache()
    {
        if (_phaseSetByRecipeId == null)
            BuildCache();
    }
}
