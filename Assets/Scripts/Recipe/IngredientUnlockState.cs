using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientUnlockState : MonoBehaviour, ISaveParticipant
{
    [SerializeField]
    private RecipeDatabaseSO _database;

    [Tooltip("사용 안 함. 해금 상태는 통합 세이브(GameSession)로 저장한다. 씬 참조 유지를 위해 필드만 남겨 둔다.")]
    [SerializeField]
    private IngredientUnlockSaveService _saveService;

    private readonly HashSet<int> _unlockedIngredientIds = new HashSet<int>();
    private readonly List<int> _unlockedIngredientIdsInOrder = new List<int>();
    private readonly Dictionary<int, int> _acquireOrderByIngredientId = new Dictionary<int, int>();

    public event Action OnChanged;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        _unlockedIngredientIds.Clear();
        _unlockedIngredientIdsInOrder.Clear();
        _acquireOrderByIngredientId.Clear();

        // 세션이 있으면 Register에서 세션 데이터로 다시 복원된다. 없으면 DB 기본 해금만 적용한다.
        AddDefaultUnlockedIngredientsIfMissing();

        GameSession.Register(this);
        OnChanged?.Invoke();
    }

    private void OnDestroy()
    {
        GameSession.Unregister(this);
    }

    public void CaptureState(GameSaveData data)
    {
        data.unlockedIngredientIds.Clear();
        data.unlockedIngredientIds.AddRange(_unlockedIngredientIdsInOrder);
    }

    public void RestoreState(GameSaveData data)
    {
        _unlockedIngredientIds.Clear();
        _unlockedIngredientIdsInOrder.Clear();
        _acquireOrderByIngredientId.Clear();

        foreach (int ingredientId in data.unlockedIngredientIds)
        {
            if (_database != null && !_database.TryGetIngredientById(ingredientId, out _))
            {
                Debug.LogWarning($"[IngredientUnlockState] 세이브의 ID {ingredientId}가 DB에 없어 건너뜁니다.");
                continue;
            }

            AddUnlockedIngredientInternal(ingredientId);
        }

        AddDefaultUnlockedIngredientsIfMissing();
        OnChanged?.Invoke();
    }

    private void AddDefaultUnlockedIngredientsIfMissing()
    {
        if (_database == null)
            return;

        for (int i = 0; i < _database.Ingredients.Count; i++)
        {
            IngredientData ingredient = _database.Ingredients[i];

            if (ingredient == null)
                continue;

            if (!ingredient.DefaultUnlock)
                continue;

            AddUnlockedIngredientInternal(ingredient.IngredientId);
        }
    }

    private void AddUnlockedIngredientInternal(int ingredientId)
    {
        if (!_unlockedIngredientIds.Add(ingredientId))
            return;

        _unlockedIngredientIdsInOrder.Add(ingredientId);
        _acquireOrderByIngredientId[ingredientId] = _unlockedIngredientIdsInOrder.Count - 1;
    }

    public bool IsUnlocked(int ingredientId)
    {
        return _unlockedIngredientIds.Contains(ingredientId);
    }

    public bool IsUnlocked(IngredientData ingredient)
    {
        return ingredient != null && IsUnlocked(ingredient.IngredientId);
    }

    public void UnlockIngredient(int ingredientId, bool saveImmediately = true)
    {
        if (_database == null || !_database.TryGetIngredientById(ingredientId, out _))
            return;

        if (_unlockedIngredientIds.Contains(ingredientId))
            return;

        AddUnlockedIngredientInternal(ingredientId);

        if (saveImmediately)
            Save();

        OnChanged?.Invoke();
    }

    public int GetAcquireOrder(int ingredientId)
    {
        return _acquireOrderByIngredientId.TryGetValue(ingredientId, out int order) ? order : int.MaxValue;
    }

    public List<IngredientData> GetUnlockedIngredients()
    {
        List<IngredientData> result = new List<IngredientData>();

        if (_database == null)
            return result;

        for (int i = 0; i < _unlockedIngredientIdsInOrder.Count; i++)
        {
            int ingredientId = _unlockedIngredientIdsInOrder[i];

            if (_database.TryGetIngredientById(ingredientId, out IngredientData ingredient))
                result.Add(ingredient);
        }

        return result;
    }

    /// <summary>현재 해금 상태를 세션에 반영한다. 파일 저장은 체크포인트에서만 한다.</summary>
    public void Save()
    {
        if (GameSession.IsActive)
            CaptureState(GameSession.Current);
    }
}
