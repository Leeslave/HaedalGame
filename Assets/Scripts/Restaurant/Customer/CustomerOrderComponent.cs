using System.Collections.Generic;
using UnityEngine;

public class CustomerOrderComponent : MonoBehaviour
{
    private FoodData curData;
    public FoodData GetOrderData()
    {
        if (curData != null) { return curData; }
        return null;
    }

    public void GenerateOrder()
    {
        IReadOnlyList<FoodData> menu = MenuManager.Instance.UnlockedFoods;
        if (menu == null || menu.Count == 0)
        {
            Debug.LogWarning("오늘의 메뉴가 없습니다.");
            return;
        }

        int index = Random.Range(0, menu.Count);
        curData = menu[index];
    }


    void Start()
    {
        curData = null;
    }
}