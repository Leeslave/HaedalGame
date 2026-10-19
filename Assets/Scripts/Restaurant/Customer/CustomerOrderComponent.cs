using System.Collections.Generic;
using UnityEngine;

public class CustomerOrderComponent : MonoBehaviour
{
    // 손님 유형별로 인스펙터에서 직접 배정하는 최애 메뉴 레시피 ID. -1이면 미설정(보너스 없음).
    [SerializeField] private int favoriteRecipeId = -1;
    public int FavoriteRecipeId => favoriteRecipeId;
    public bool FavoriteOnMenu { get; private set; }
    public int Species { get; set; }

    private RecipeData curData;
    public RecipeData GetOrderData()
    {
        if (curData != null) { return curData; }
        return null;
    }

    public void GenerateOrder()
    {
        IReadOnlyList<RecipeData> menu = MenuManager.Instance.DailyFoods;
        if (GameSession.IsActive && GameDatabase.Recipes.Recipes.Count > 0)
            favoriteRecipeId = GameDatabase.Recipes.Recipes[(GameSession.Current.day + Species) % GameDatabase.Recipes.Recipes.Count].RecipeId;
        if (menu == null || menu.Count == 0)
        {
            Debug.LogWarning("오늘의 메뉴가 없습니다.");
            return;
        }

        var available = new List<RecipeData>();
        foreach (var recipe in menu)
            if (IngredientInventoryService.Instance != null && IngredientInventoryService.Instance.CraftableCount(recipe) > 0)
                available.Add(recipe);
        curData = available.Count == 0 ? null : available[Random.Range(0, available.Count)];
        FavoriteOnMenu = false;
        foreach (var item in menu) if (item.RecipeId == favoriteRecipeId) FavoriteOnMenu = true;
        var favorite = available.Find(r => r.RecipeId == favoriteRecipeId);
        if (favorite != null)
        {
            var others = available.FindAll(r => r.RecipeId != favoriteRecipeId);
            curData = others.Count == 0 || Random.value < .5f ? favorite : others[Random.Range(0, others.Count)];
        }
    }


    void Start()
    {
        curData = null;
    }
}
