using System.Collections.Generic;
using UnityEngine;

public class AutoAssignMenuByPriceButton : MonoBehaviour
{
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private MenuRecipeService _menuRecipeService;
    [SerializeField] private bool _clearExistingMenusFirst = true;
    [SerializeField] private bool _highestPriceFirst = true;

    public void AssignOwnedRecipesByPrice()
    {
        if (_recipeBookState == null || _menuRecipeService == null)
            return;

        List<RecipeData> unlockedRecipes = _recipeBookState.GetUnlockedRecipes();
        if (unlockedRecipes == null || unlockedRecipes.Count == 0)
            return;

        unlockedRecipes.Sort(CompareRecipe);

        if (_clearExistingMenusFirst)
            _menuRecipeService.ClearAllRecipes();

        int slotIndex = 0;

        for (int i = 0; i < unlockedRecipes.Count; i++)
        {
            if (slotIndex >= _menuRecipeService.SlotCount)
                break;

            RecipeData recipe = unlockedRecipes[i];
            if (recipe == null)
                continue;

            MenuRecipeSetResult result = _menuRecipeService.SetRecipe(slotIndex, recipe);

            if (result == MenuRecipeSetResult.Success)
                slotIndex++;
        }

    }

    private int CompareRecipe(RecipeData a, RecipeData b)
    {
        if (a == null && b == null)
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        int priceCompare = _highestPriceFirst
            ? b.Price.CompareTo(a.Price)
            : a.Price.CompareTo(b.Price);

        if (priceCompare != 0)
            return priceCompare;

        return a.RecipeId.CompareTo(b.RecipeId);
    }
}