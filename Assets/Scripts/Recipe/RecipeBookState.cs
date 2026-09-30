using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeBookState : MonoBehaviour, ISaveParticipant
{
    [SerializeField]
    private RecipeDatabaseSO _database;

    [Tooltip("사용 안 함. 해금 상태는 통합 세이브(GameSession)로 저장한다. 씬 참조 유지를 위해 필드만 남겨 둔다.")]
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

        // 세션이 있으면 Register에서 세션 데이터로 다시 복원된다. 없으면 DB 기본 해금만 적용한다.
        AddDefaultUnlockedRecipesIfMissing();

        GameSession.Register(this);
        OnChanged?.Invoke();
    }

    private void OnDestroy()
    {
        GameSession.Unregister(this);
    }

    public void CaptureState(GameSaveData data)
    {
        data.unlockedRecipeIds.Clear();
        data.unlockedRecipeIds.AddRange(_unlockedRecipeIdsInOrder);
    }

    public void RestoreState(GameSaveData data)
    {
        _unlockedRecipeIds.Clear();
        _unlockedRecipeIdsInOrder.Clear();
        _acquireOrderByRecipeId.Clear();

        foreach (int recipeId in data.unlockedRecipeIds)
        {
            if (_database != null && !_database.TryGetRecipe(recipeId, out _))
            {
                Debug.LogWarning($"[RecipeBookState] 세이브의 ID {recipeId}가 DB에 없어 건너뜁니다.");
                continue;
            }

            AddUnlockedRecipeInternal(recipeId);
        }

        AddDefaultUnlockedRecipesIfMissing();
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

    /// <summary>
    /// 씬에 RecipeBookState가 없어도(상점만 열린 씬 등) 해금이 유실되지 않도록 세션에 직접 반영한다.
    /// </summary>
    public static void UnlockRecipeAnywhere(int recipeId)
    {
        RecipeBookState recipeBook = FindFirstObjectByType<RecipeBookState>();
        if (recipeBook != null)
        {
            recipeBook.UnlockRecipe(recipeId);
            return;
        }

        if (GameSession.IsActive && !GameSession.Current.unlockedRecipeIds.Contains(recipeId))
            GameSession.Current.unlockedRecipeIds.Add(recipeId);
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

    /// <summary>현재 해금 상태를 세션에 반영한다. 파일 저장은 체크포인트에서만 한다.</summary>
    public void Save()
    {
        if (GameSession.IsActive)
            CaptureState(GameSession.Current);
    }
}
