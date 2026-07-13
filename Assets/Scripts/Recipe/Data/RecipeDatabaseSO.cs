using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabaseSO", menuName = "Game Data/Recipe/Recipe Database")]
public class RecipeDatabaseSO : ScriptableObject
{
    [SerializeField] private List<IngredientData> _ingredients = new();
    [SerializeField] private List<RecipeData> _recipes = new();

    private Dictionary<int, IngredientData> _ingredientById;
    private Dictionary<int, IngredientData> _ingredientByRecipeCode;
    private Dictionary<int, RecipeData> _recipeById;

    public IReadOnlyList<IngredientData> Ingredients => _ingredients;
    public IReadOnlyList<RecipeData> Recipes => _recipes;

    public void SetData(List<IngredientData> ingredients, List<RecipeData> recipes)
    {
        _ingredients = ingredients ?? new List<IngredientData>();
        _recipes = recipes ?? new List<RecipeData>();
        BuildCache();
    }

    public void BuildCache()
    {
        _ingredientById = new Dictionary<int, IngredientData>();
        _ingredientByRecipeCode = new Dictionary<int, IngredientData>();
        _recipeById = new Dictionary<int, RecipeData>();

        if (_ingredients == null)
            _ingredients = new List<IngredientData>();

        if (_recipes == null)
            _recipes = new List<RecipeData>();

        for (int i = 0; i < _ingredients.Count; i++)
        {
            IngredientData ingredient = _ingredients[i];

            if (ingredient == null)
                continue;

            if (!_ingredientById.ContainsKey(ingredient.IngredientId))
                _ingredientById.Add(ingredient.IngredientId, ingredient);
            else
                Debug.LogWarning($"Duplicate IngredientId: {ingredient.IngredientId}", this);

            if (!_ingredientByRecipeCode.ContainsKey(ingredient.RecipeCode))
                _ingredientByRecipeCode.Add(ingredient.RecipeCode, ingredient);
            else
                Debug.LogWarning($"Duplicate RecipeCode(rec_n): {ingredient.RecipeCode}", this);
        }

        for (int i = 0; i < _recipes.Count; i++)
        {
            RecipeData recipe = _recipes[i];

            if (recipe == null)
                continue;

            if (!_recipeById.ContainsKey(recipe.RecipeId))
                _recipeById.Add(recipe.RecipeId, recipe);
            else
                Debug.LogWarning($"Duplicate RecipeId: {recipe.RecipeId}", this);
        }
    }

    public bool TryGetIngredientById(int ingredientId, out IngredientData ingredient)
    {
        EnsureCache();
        return _ingredientById.TryGetValue(ingredientId, out ingredient);
    }

    public bool TryGetIngredientByRecipeCode(int recipeCode, out IngredientData ingredient)
    {
        EnsureCache();
        return _ingredientByRecipeCode.TryGetValue(recipeCode, out ingredient);
    }

    public bool TryGetRecipe(int recipeId, out RecipeData recipe)
    {
        EnsureCache();
        return _recipeById.TryGetValue(recipeId, out recipe);
    }

    public List<IngredientData> GetIngredientsByIslandType(IslandType islandType)
    {
        EnsureCache();

        List<IngredientData> result = new List<IngredientData>();

        if (_ingredients == null)
            return result;

        for (int i = 0; i < _ingredients.Count; i++)
        {
            IngredientData ingredient = _ingredients[i];

            if (ingredient == null)
                continue;

            if (islandType == IslandType.All || ingredient.BelongIsland == islandType)
                result.Add(ingredient);
        }

        return result;
    }

    public IReadOnlyList<IngredientData> GetIngredientsByIslandTypeReadonly(IslandType islandType)
    {
        return GetIngredientsByIslandType(islandType);
    }

    private void OnEnable()
    {
        if (_ingredients == null)
            _ingredients = new List<IngredientData>();

        if (_recipes == null)
            _recipes = new List<RecipeData>();

        BuildCache();
    }

    private void EnsureCache()
    {
        if (_ingredientById == null || _ingredientByRecipeCode == null || _recipeById == null)
            BuildCache();
    }
}