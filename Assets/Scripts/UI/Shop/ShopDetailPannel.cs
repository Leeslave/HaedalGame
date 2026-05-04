using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopDetailPannel : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _detialText;
    [SerializeField] private TMP_Text _pricteText;
    [SerializeField] private TMP_Text _discountPriceText;
    [SerializeField] private TMP_Text _totalPriceText;
    [SerializeField] private TMP_Text _stockText;
    [SerializeField] private TMP_Text _countText;


    [SerializeField] private GameObject _originalPriceSection;
    [SerializeField] private GameObject _discountPriceSection;
    [SerializeField] private GameObject _Xobejct;


    [SerializeField] private Button _minusButton;
    [SerializeField] private Button _plusButton;
    [SerializeField] private Button _maxButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _cancelButton;

    [SerializeField] [Range(0f, 1f)] private float _discountRate = 0f;

    private StockData _currentStock;
    private IngredientData _currentIngredient;
    private int _currentBuyAmount;
    private int _unitPrice;

    public int CurrentBuyAmount => _currentBuyAmount;

    public void Bind(RecipeDatabaseSO database, StockData stock)
    {
        if (stock == null || database == null)
        {
            gameObject.SetActive(false);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            gameObject.SetActive(false);
    }

    private void RefreshBuyAmount()
    {
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
