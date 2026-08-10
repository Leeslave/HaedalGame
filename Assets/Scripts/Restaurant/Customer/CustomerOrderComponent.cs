using System.Collections.Generic;
using UnityEngine;

public class CustomerOrderComponent : MonoBehaviour
{
    // 손님 유형별로 인스펙터에서 직접 배정하는 최애 메뉴 레시피 ID. -1이면 미설정(보너스 없음).
    [SerializeField] private int favoriteRecipeId = -1;
    public int FavoriteRecipeId => favoriteRecipeId;

    private RecipeData curData;
    public RecipeData GetOrderData()
    {
        if (curData != null) { return curData; }
        return null;
    }

    public void GenerateOrder()
    {
        IReadOnlyList<RecipeData> menu = MenuManager.Instance.DailyFoods;
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