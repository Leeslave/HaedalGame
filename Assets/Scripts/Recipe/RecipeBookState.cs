using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeBookState : MonoBehaviour
{
    [SerializeField]
    private RecipeDatabaseSO _database;

    [SerializeField]
    private RecipeUnlockSaveService _saveService;

    private readonly HashSet<int> _unlockedRecipeIds = new HashSet<int>();
    private readonly List<int> _unlockedRecipeIdsInOrder = new List<int>();
    private readonly Dictionary<int, int> _acquireOrderByRecipeId = new Dictionary<int, int>();

    public event Action OnChanged;

    private void Awake()
    {
        Initialize();
        Debug.Log(Application.persistentDataPath);
    }

    public void Initialize()
    {
        _unlockedRecipeIds.Clear();
        _unlockedRecipeIdsInOrder.Clear();
        _acquireOrderByRecipeId.Clear();

        RecipeUnlockSaveData saveData = _saveService != null ? _saveService.Load() : null;

        if (
            saveData != null
            && saveData.unlockedRecipeIdsInOrder != null
            && saveData.unlockedRecipeIdsInOrder.Count > 0
        )
        {
            for (int i = 0; i < saveData.unlockedRecipeIdsInOrder.Count; i++)
            {
                int recipeId = saveData.unlockedRecipeIdsInOrder[i];

                if (_database == null || !_database.TryGetRecipe(recipeId, out _))
                    continue;

                AddUnlockedRecipeInternal(recipeId);
            }

            AddDefaultUnlockedRecipesIfMissing();
        }
        else
        {
            AddDefaultUnlockedRecipesIfMissing();
            Save();
        }

        OnChanged?.Invoke();
    }

    private void AddDefaultUnlockedRecipesIfMissing()
    {
        if (_database == null)
            return;

        for (int i = 0; i < _database.Recipes.Count; i++)
        {
            RecipeData recipe = _database.Recipes[i];

            if (recipe == null)
                continue;

            if (!recipe.DefaultUnlock)
                continue;

            AddUnlockedRecipeInternal(recipe.RecipeId);
        }
    }

    private void AddUnlockedRecipeInternal(int recipeId)
    {
        if (!_unlockedRecipeIds.Add(recipeId))
            return;

        _unlockedRecipeIdsInOrder.Add(recipeId);
        _acquireOrderByRecipeId[recipeId] = _unlockedRecipeIdsInOrder.Count - 1;
    }

    public bool IsUnlocked(int recipeId)
    {
        return _unlockedRecipeIds.Contains(recipeId);
    }

    public bool IsUnlocked(RecipeData recipe)
    {
        return recipe != null && IsUnlocked(recipe.RecipeId);
    }

    public void UnlockRecipe(int recipeId, bool saveImmediately = true)
    {
        if (_database == null || !_database.TryGetRecipe(recipeId, out _))
            return;

        if (_unlockedRecipeIds.Contains(recipeId))
            return;

        AddUnlockedRecipeInternal(recipeId);

        if (saveImmediately)
            Save();

        OnChanged?.Invoke();
    }

    public int GetAcquireOrder(int recipeId)
    {
        return _acquireOrderByRecipeId.TryGetValue(recipeId, out int order) ? order : int.MaxValue;
    }

    public List<RecipeData> GetUnlockedRecipes()
    {
        List<RecipeData> result = new List<RecipeData>();

        if (_database == null)
            return result;

        for (int i = 0; i < _unlockedRecipeIdsInOrder.Count; i++)
        {
            int recipeId = _unlockedRecipeIdsInOrder[i];

            if (_database.TryGetRecipe(recipeId, out RecipeData recipe))
                result.Add(recipe);
        }

        return result;
    }

    public List<RecipeData> GetRecipesWithIngredients(int ingredientId)
    {
        List<RecipeData> result = new List<RecipeData>();

        if (_database == null)
            return result;

        for (int i = 0; i < _database.Recipes.Count; i++)
        {
            RecipeData recipe = _database.Recipes[i];

            foreach (var ingredient in recipe.Requirements)
            {
                if (ingredient.IngredientId == ingredientId)
                {
                    result.Add(recipe);
                }
            }
        }

        return result;
    }

    public void Save()
    {
        if (_saveService == null)
            return;

        RecipeUnlockSaveData data = new RecipeUnlockSaveData();
        data.unlockedRecipeIdsInOrder.AddRange(_unlockedRecipeIdsInOrder);
        _saveService.Save(data);
    }
}
