using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IngredientSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private GameObject _selectedObject;

    private Action<Ingredient> _onClick;
    private Ingredient _ingredient;

    public Ingredient Ingredient => _ingredient;

    public void Bind(RecipeDatabaseSO database, Ingredient ingredient, Action<Ingredient> onClick, bool selected)
    {
        _ingredient = ingredient;
        _onClick = onClick;

        IngredientData ingredientData = null;
        if (database != null)
            database.TryGetIngredientById(ingredient.IngredientId, out ingredientData);

        if (_iconImage != null)
        {
            _iconImage.sprite = ingredientData != null ? ingredientData.Icon : null;
            _iconImage.enabled = ingredientData != null && ingredientData.Icon != null;
        }

        if (_countText != null)
            _countText.text = ingredient.Amount.ToString();

        SetSelected(selected);
        gameObject.SetActive(true);
    }

    public void SetEmpty()
    {
        _ingredient = null;
        _onClick = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        if (_countText != null)
            _countText.text = string.Empty;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedObject != null)
            _selectedObject.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_ingredient == null)
            return;

        _onClick?.Invoke(_ingredient);
    }
}