using UnityEngine;

/// <summary>
/// 엘프 상점 구매 상세 패널. ShopDetailPannel을 상속해
/// 레시피 아이템과 20% 고정 할인 표시를 추가.
/// </summary>
public class ElfShopDetailPanel : ShopDetailPannel
{
    private ElfShopStockData _elfStock;
    private System.Action _onAfterPurchase;

    public void Bind(RecipeDatabaseSO database, ElfShopStockData stock, System.Action onAfterPurchase = null)
    {
        if (stock == null || database == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _elfStock = stock;
        _onAfterPurchase = onAfterPurchase;

        if (stock.ItemType == ElfShopItemType.Ingredient)
        {
            _discountRate = 1f - ElfShopManager.ELF_PRICE_MULTIPLIER;
            base.Bind(database, stock);
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(OnBuyIngredient);
        }
        else
        {
            BindRecipe(database, stock);
        }
    }

    private void BindRecipe(RecipeDatabaseSO database, ElfShopStockData stock)
    {
        if (!database.TryGetRecipe(stock.IngredientID, out RecipeData recipe))
        {
            gameObject.SetActive(false);
            return;
        }

        _currentStock     = stock;
        _currentBuyAmount = 1;
        _unitPrice        = ElfShopManager.Instance != null ? ElfShopManager.Instance.GetElfUnitPrice(stock) : 0;

        if (_iconImage != null) _iconImage.sprite = recipe.Icon;
        if (_titleText != null) _titleText.text   = $"레시피 : {recipe.RecipeName}";
        if (_nameText  != null) _nameText.text    = recipe.RecipeName;

        int basePrice = Mathf.RoundToInt(_unitPrice / ElfShopManager.ELF_PRICE_MULTIPLIER);
        if (_pricteText           != null) _pricteText.text           = $"{basePrice}G";
        if (_discountPriceText    != null) _discountPriceText.text    = $"{_unitPrice}G";
        if (_Xobejct              != null) _Xobejct.SetActive(true);
        if (_discountPriceSection != null) _discountPriceSection.SetActive(true);

        // 레시피는 수량 조작 불필요
        _minusButton.interactable = false;
        _plusButton.interactable  = false;
        _maxButton.interactable   = false;

        _minusButton.onClick.RemoveAllListeners();
        _plusButton.onClick.RemoveAllListeners();
        _maxButton.onClick.RemoveAllListeners();
        _buyButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.RemoveAllListeners();

        _buyButton.onClick.AddListener(OnBuyRecipe);
        _cancelButton.onClick.AddListener(() => gameObject.SetActive(false));

        RefreshBuyAmount();
        gameObject.SetActive(true);
    }

    protected override void RefreshBuyAmount()
    {
        if (_elfStock != null && _elfStock.ItemType == ElfShopItemType.Recipe)
        {
            if (_totalPriceText != null) _totalPriceText.text = $"{_unitPrice}G";
            if (_stockText      != null) _stockText.text      = _elfStock.IsSoldOut ? "품절" : "구매 가능 (1개 한정)";
            if (_countText      != null) _countText.text      = "1";
            return;
        }

        base.RefreshBuyAmount();
    }

    private void OnBuyIngredient()
    {
        if (ElfShopManager.Instance == null || _elfStock == null) return;

        bool success = ElfShopManager.Instance.PurchaseFromElf(_elfStock, _currentBuyAmount);
        if (success)
        {
            _onAfterPurchase?.Invoke();
            gameObject.SetActive(false);
        }
        else
            Debug.Log("[ElfShop] 구매 실패: 골드 부족 또는 재고 없음");
    }

    private void OnBuyRecipe()
    {
        if (ElfShopManager.Instance == null || _elfStock == null) return;

        bool success = ElfShopManager.Instance.PurchaseFromElf(_elfStock, 1);
        if (success)
        {
            _onAfterPurchase?.Invoke();
            gameObject.SetActive(false);
        }
        else
            Debug.Log("[ElfShop] 구매 실패: 골드 부족 또는 품절");
    }
}
