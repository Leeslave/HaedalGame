using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 엘프 상점 슬롯 UI. ShopSlotUI를 상속해 재료/레시피를 모두 표시.
/// </summary>
public class ElfShopSlotUI : ShopSlotUI
{
    // ElfShopStockData를 받는 Bind 오버로드
    public void Bind(RecipeDatabaseSO database, ElfShopStockData stock, Action<StockData> onClick, bool selected, bool isUnlocked = false)
    {
        if (stock.ItemType == ElfShopItemType.Recipe)
            BindRecipe(database, stock, onClick, selected, isUnlocked);
        else
            base.Bind(database, stock, onClick, selected, isUnlocked); // 재료는 부모 그대로 활용
    }

    private void BindRecipe(RecipeDatabaseSO database, ElfShopStockData stock, Action<StockData> onClick, bool selected, bool isUnlocked)
    {
        database.TryGetRecipe(stock.IngredientID, out RecipeData recipe);

        // 부모 Bind에 stock을 넘겨 기본 처리 (soldOut, selected, unlocked 등) 처리 후
        // 아이콘/가격만 덮어씌움
        base.Bind(database, stock, onClick, selected, isUnlocked);

        if (_iconImage != null)
        {
            _iconImage.sprite = recipe?.Icon;
            _iconImage.enabled = recipe?.Icon != null;
        }

        int price = ElfShopManager.Instance != null ? ElfShopManager.Instance.GetElfUnitPrice(stock) : 0;
        if (_priceText != null)
            _priceText.text = $"{price}G";
    }
}
