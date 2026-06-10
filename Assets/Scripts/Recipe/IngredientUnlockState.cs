using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientUnlockState : MonoBehaviour
{
    [SerializeField]
    private RecipeDatabaseSO _database;

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

        IngredientUnlockSaveData saveData = _saveService != null ? _saveService.Load() : null;

        if (
            saveData != null
            && saveData.unlockedIngredientIdsInOrder != null
            && saveData.unlockedIngredientIdsInOrder.Count > 0
        )
        {
            for (int i = 0; i < saveData.unlockedIngredientIdsInOrder.Count; i++)
            {
                int ingredientId = saveData.unlockedIngredientIdsInOrder[i];

                if (_database == null || !_database.TryGetIngredientById(ingredientId, out _))
                    continue;

                AddUnlockedIngredientInternal(ingredientId);
            }

            AddDefaultUnlockedIngredientsIfMissing();
        }
        else
        {
            AddDefaultUnlockedIngredientsIfMissing();
            Save();
        }

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

    public void Save()
    {
        if (_saveService == null)
            return;

        IngredientUnlockSaveData data = new IngredientUnlockSaveData();
        data.unlockedIngredientIdsInOrder.AddRange(_unlockedIngredientIdsInOrder);
        _saveService.Save(data);
    }
}
