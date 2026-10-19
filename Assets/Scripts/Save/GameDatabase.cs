using UnityEngine;

/// <summary>
/// 세이브 복원 시 ID → 정의 데이터를 찾기 위한 공용 DB 접근.
/// 씬마다 인스펙터 참조가 없어도 복원할 수 있도록 Resources에서 읽는다.
/// </summary>
public static class GameDatabase
{
    private const string RecipeDatabasePath = "Data/RecipeDatabaseSO";

    private static RecipeDatabaseSO _recipes;

    public static RecipeDatabaseSO Recipes
    {
        get
        {
            if (_recipes == null)
            {
                _recipes = Resources.Load<RecipeDatabaseSO>(RecipeDatabasePath);
                if (_recipes == null)
                    Debug.LogError($"[GameDatabase] Resources/{RecipeDatabasePath} 를 찾을 수 없습니다.");
            }
            return _recipes;
        }
    }

    public static bool TryGetRecipe(int recipeId, out RecipeData recipe)
    {
        recipe = null;
        return Recipes != null && Recipes.TryGetRecipe(recipeId, out recipe);
    }
}
