using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopDetailPannel : MonoBehaviour
{
    [SerializeField] protected Image _iconImage;
    [SerializeField] protected TMP_Text _titleText;
    [SerializeField] protected TMP_Text _nameText;
    [SerializeField] protected TMP_Text _detialText;
    [SerializeField] protected TMP_Text _pricteText;
    [SerializeField] protected TMP_Text _discountPriceText;
    [SerializeField] protected TMP_Text _totalPriceText;
    [SerializeField] protected TMP_Text _stockText;
    [SerializeField] protected TMP_Text _countText;


    [SerializeField] protected GameObject _originalPriceSection;
    [SerializeField] protected GameObject _discountPriceSection;
    [SerializeField] protected GameObject _Xobejct;


    [SerializeField] protected Button _minusButton;
    [SerializeField] protected Button _plusButton;
    [SerializeField] protected Button _maxButton;
    [SerializeField] protected Button _buyButton;
    [SerializeField] protected Button _cancelButton;

    [SerializeField] [Range(0f, 1f)] protected float _discountRate = 0f;

    protected StockData _currentStock;
    protected IngredientData _currentIngredient;
    protected int _currentBuyAmount;
    protected int _unitPrice;

    public int CurrentBuyAmount => _currentBuyAmount;

    public virtual void Bind(RecipeDatabaseSO database, StockData stock)
    {
        if (stock == null || database == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (stock.IsRecipe)
        {
            BindRecipe(database, stock);
            return;
        }

        if (!database.TryGetIngredientById(stock.IngredientID, out _currentIngredient))
        {
            gameObject.SetActive(false);
            return;
        }

        _currentStock = stock;
        _currentBuyAmount = stock.CurrentStock > 0 ? 1 : 0;

        gameObject.SetActive(true);

        _unitPrice = Mathf.RoundToInt(_currentIngredient.Price * (1f - _discountRate));

        if (_iconImage != null)
            _iconImage.sprite = _currentIngredient.Icon;

        _titleText.text = $"재료 : {_currentIngredient.IngredientName}";
        _nameText.text = $"재료 : {_currentIngredient.IngredientName}";
        _pricteText.text = $"{_currentIngredient.Price}G";

        bool hasDiscount = _discountRate > 0f;
        _Xobejct.SetActive(hasDiscount);
        _discountPriceSection.SetActive(hasDiscount);
        if (_discountPriceText != null)
            _discountPriceText.text = hasDiscount ? $"{_unitPrice}G" : string.Empty;

        _minusButton.onClick.RemoveAllListeners();
        _plusButton.onClick.RemoveAllListeners();
        _maxButton.onClick.RemoveAllListeners();
        _buyButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.RemoveAllListeners();

        _minusButton.onClick.AddListener(OnClickMinus);
        _plusButton.onClick.AddListener(OnClickPlus);
        _maxButton.onClick.AddListener(OnClickMax);
        _buyButton.onClick.AddListener(OnClickBuy);
        _cancelButton.onClick.AddListener(OnClickCancel);

        RefreshBuyAmount();
    }

    private void BindRecipe(RecipeDatabaseSO database, StockData stock)
    {
        if (!database.TryGetRecipe(stock.IngredientID, out RecipeData recipe))
        {
            gameObject.SetActive(false);
            return;
        }

        _currentStock     = stock;
        _currentBuyAmount = 1;
        _unitPrice        = (int)recipe.Price;

        if (_iconImage != null) _iconImage.sprite = recipe.Icon;
        if (_titleText != null) _titleText.text   = $"레시피 : {recipe.RecipeName}";
        if (_nameText  != null) _nameText.text    = recipe.RecipeName;
        if (_pricteText != null) _pricteText.text = $"{_unitPrice}G";
        if (_Xobejct != null) _Xobejct.SetActive(false);
        if (_discountPriceSection != null) _discountPriceSection.SetActive(false);

        _minusButton.interactable = false;
        _plusButton.interactable  = false;
        _maxButton.interactable   = false;

        _minusButton.onClick.RemoveAllListeners();
        _plusButton.onClick.RemoveAllListeners();
        _maxButton.onClick.RemoveAllListeners();
        _buyButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.RemoveAllListeners();

        _buyButton.onClick.AddListener(OnClickBuy);
        _cancelButton.onClick.AddListener(OnClickCancel);

        RefreshBuyAmount();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            gameObject.SetActive(false);
    }

    protected virtual void RefreshBuyAmount()
    {
        if (_currentStock != null && _currentStock.IsRecipe)
        {
            if (_totalPriceText != null) _totalPriceText.text = $"{_unitPrice}G";
            if (_stockText != null) _stockText.text = _currentStock.IsSoldOut ? "품절" : "구매 가능 (1개 한정)";
            if (_countText != null) _countText.text = "1";
            return;
        }

        _detialText.text = $"설명: 재헌입니다. 조재현아닙니다. 최재현도 아닙니다. 물론 정재현도 아닙니다. ";
        _totalPriceText.text = $"{_unitPrice * _currentBuyAmount}G";
        _stockText.text = $"구매 수량: {_currentBuyAmount} (재고 {_currentStock.CurrentStock}개 남음)";
        _countText.text = $"{_currentBuyAmount}";
        _minusButton.interactable = _currentBuyAmount > 1;
        _plusButton.interactable = _currentBuyAmount < _currentStock.CurrentStock;
        _maxButton.interactable = _currentStock.CurrentStock > 0;
    }

    private void OnClickMinus()
    {
        if (_currentBuyAmount <= 1) return;
        _currentBuyAmount--;
        RefreshBuyAmount();
    }

    private void OnClickPlus()
    {
        if (_currentBuyAmount >= _currentStock.CurrentStock) return;
        _currentBuyAmount++;
        RefreshBuyAmount();
    }

    private void OnClickMax()
    {
        _currentBuyAmount = _currentStock.CurrentStock;
        RefreshBuyAmount();
    }

    private void OnClickBuy()
    {
        if (ShopStockManager.Instance == null) return;

        bool success = ShopStockManager.Instance.Purchase(
            _currentStock.IngredientID, _currentBuyAmount, _unitPrice);

        if (success)
            gameObject.SetActive(false);
        else
            Debug.Log("[Shop] 구매 실패: 골드 부족 또는 재고 없음");
    }

    private void OnClickCancel()
    {
        gameObject.SetActive(false);
    }
}
