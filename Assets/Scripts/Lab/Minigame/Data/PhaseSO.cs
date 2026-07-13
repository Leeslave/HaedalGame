using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 페이즈 화면에 표시되는 아이템. 원재료가 아닌 중간 결과물(예: "연어 스테이크", "크림 소스")을
/// 표현할 때 사용한다. 스프라이트가 없으면 흰 박스 + 이름으로 표시된다.
/// </summary>
[Serializable]
public class PhaseDisplayItem
{
    [SerializeField] private string _itemName;
    [SerializeField] private Sprite _sprite;

    public string ItemName => _itemName;
    public Sprite Sprite => _sprite;
}

/// <summary>
/// 미니게임 페이즈 공통 데이터 (추상).
/// 점수 계산/인터랙션 로직은 페이즈별 PhaseController(MonoBehaviour)가 담당하고,
/// SO는 순수 데이터만 보관한다.
/// </summary>
public abstract class PhaseSO : ScriptableObject
{
    [Header("Common")]
    [SerializeField] private string _phaseName;
    [SerializeField] private CookwareSO _cookware;

    // 이 페이즈에서 사용하는 재료 (RecipeDatabaseSO의 IngredientId 기준)
    [SerializeField] private List<int> _requiredIngredientIds = new List<int>();

    // 화면에 원재료 대신 표시할 중간 결과물 (예: Assembly의 "연어 스테이크").
    // 비어 있으면 requiredIngredientIds의 원재료가 표시된다.
    [SerializeField] private List<PhaseDisplayItem> _displayItems = new List<PhaseDisplayItem>();

    [Header("Score")]
    [SerializeField] private float _baseScore = 5f;
    [SerializeField] private float _wrongIngredientPenalty = 0.5f;

    public string PhaseName => _phaseName;
    public CookwareSO Cookware => _cookware;
    public IReadOnlyList<int> RequiredIngredientIds => _requiredIngredientIds;
    public IReadOnlyList<PhaseDisplayItem> DisplayItems => _displayItems;
    public float BaseScore => _baseScore;
    public float WrongIngredientPenalty => _wrongIngredientPenalty;

    /// <summary>
    /// 페이즈의 인터랙션 타입. MinigameManager가 대응하는 PhaseController를 고르는 데 사용한다.
    /// </summary>
    public abstract CookingActionType ActionType { get; }

    public bool IsRequiredIngredient(int ingredientId)
    {
        return _requiredIngredientIds != null && _requiredIngredientIds.Contains(ingredientId);
    }
}
