/// <summary>
/// 엘프 상점용 재고 데이터. StockData를 확장해 레시피 슬롯도 지원.
/// IngredientID는 아이템 종류에 따라 재료 ID 또는 레시피 ID로 사용됨.
/// </summary>
[System.Serializable]
public class ElfShopStockData : StockData
{
    public ElfShopItemType ItemType;

    public ElfShopStockData(ElfShopItemType itemType, int itemId, int maxQty)
        : base(itemId, maxQty, maxQty, 0, isRecipe: itemType == ElfShopItemType.Recipe)
    {
        ItemType = itemType;
    }
}
