using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MenuData
{
    private HashSet<int> OwnedMenuIds = new HashSet<int>();

    public void AddMenu(FoodData foodData)
    {
        if (foodData == null) return;
        OwnedMenuIds.Add(foodData.id);
    }

    public bool HasMenu(FoodData foodData)
    {
        if (foodData == null) return false;
        return OwnedMenuIds.Contains(foodData.id);
    }

    public List<int> GetOwnedMenuIds()
    {
        return new List<int>(OwnedMenuIds);
    }
}