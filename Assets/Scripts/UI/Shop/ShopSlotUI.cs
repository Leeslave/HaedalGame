using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private GameObject _selectedObject;
    [SerializeField] private GameObject _soldOutObject;

    private StockData _stockData;

    private Action<StockData> _onClick;

    public StockData StockData => _stockData;
    public void Bind(RecipeDatabaseSO database, StockData stockData, Action<StockData> onClick, bool selected)
    {
        _stockData = stockData;
        _onClick = onClick;
        database.TryGetIngredientById(stockData.IngredientID, out IngredientData ingredientData);
        
        if (_iconImage != null)
        {
            _iconImage.sprite = ingredientData != null ? ingredientData.Icon : null;
            _iconImage.enabled = ingredientData != null && ingredientData.Icon != null;
        }

        if (_priceText != null)
            _priceText.text = ingredientData.Price.ToString() + "G";

        SetSelected(selected);

        if (_soldOutObject != null)
            _soldOutObject.SetActive(stockData.IsSoldOut);

        gameObject.SetActive(true);
    }

    public void SetEmpty()
    {
        _stockData = null;
        _onClick = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        if (_priceText != null)
            _priceText.text = string.Empty;

        if (_soldOutObject != null)
            _soldOutObject.SetActive(false);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedObject != null)
            _selectedObject.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stockData == null || _stockData.IsSoldOut)
            return;

        _onClick?.Invoke(_stockData);
    }
}
