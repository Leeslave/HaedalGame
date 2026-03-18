using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerOrderComponent : MonoBehaviour
{
    private FoodData curData;
    public FoodData GetOrderData()
    {
        if (curData != null) {return curData; }
        return null;
    }

    public void GenerateOrder()
    {
        List<int> ownedIds = RestaurantGameManager.instance.menuData.GetOwnedMenuIds();
        int randomId = ownedIds[Random.Range(0, ownedIds.Count)];
        curData = RestaurantGameManager.instance.foodDatabase.GetFoodById(randomId);
        return;
    }


    void Start()
    {
        curData = null;
    }
}