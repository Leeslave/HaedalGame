using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] protected Image _iconImage;
    [SerializeField] protected TMP_Text _priceText;
    [SerializeField] protected GameObject _selectedObject;
    [SerializeField] protected GameObject _soldOutObject;

    protected StockData _stockData;

    protected Action<StockData> _onClick;

    public StockData StockData => _stockData;
    public virtual void Bind(RecipeDatabaseSO database, StockData stockData, Action<StockData> onClick, bool selected)
    {
        _stockData = stockData;
        _onClick = onClick;
        database.TryGetIngredientById(stockData.IngredientID, out IngredientData ingredientData);
        
        if (_iconImage != null)
        {
            _iconImage.sprite = ingredientData?.Icon;
            _iconImage.enabled = ingredientData != null && ingredientData.Icon != null;
        }

        if (_priceText != null)
            _priceText.text = ingredientData != null
                ? ingredientData.Price.ToString() + "G"
                : string.Empty;

        SetSelected(selected);

        if (_soldOutObject != null)
        {
            Debug.Log($"[ShopSlotUI] _soldOutObject.SetActive({stockData.IsSoldOut}) on {gameObject.name}");
            _soldOutObject.SetActive(stockData.IsSoldOut);
        }
        else
            Debug.LogWarning($"[ShopSlotUI] _soldOutObject is NULL on {gameObject.name}");

        gameObject.SetActive(true);
    }

    public virtual void SetEmpty()
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
