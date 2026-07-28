using System.Collections.Generic;
using UnityEngine;


// RestaurantGameManager에 선언
[System.Serializable]
public class MenuData
{
    private HashSet<int> OwnedMenuIds = new HashSet<int>();

    public void AddMenu(RecipeData foodData)
    {
        if (foodData == null) return;
        OwnedMenuIds.Add(foodData.RecipeId);
    }

    public bool HasMenu(RecipeData foodData)
    {
        if (foodData == null) return false;
        return OwnedMenuIds.Contains(foodData.RecipeId);
    }

    public List<int> GetOwnedMenuIds()
    {
        return new List<int>(OwnedMenuIds);
    }
}