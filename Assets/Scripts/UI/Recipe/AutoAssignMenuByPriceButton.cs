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
        if (!RestaurantProgress.TryAutoMenu(out string error)) Debug.Log(error);
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