using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IngredientSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _selectedObject;

    [SerializeField] private TMP_Text _cntText;

    private Action<IngredientData> _onClick;
    private IngredientData _ingredient;


    public IngredientData Ingredient => _ingredient;

    void Bind(IngredientData ingredient, Action<IngredientData> onClick, bool selected)
    {
        _ingredient = ingredient;
        _onClick = onClick;
        
        _iconImage.sprite = ingredient != null ? ingredient.Icon : null;
        _iconImage.enabled = ingredient != null && ingredient.Icon != null;

        _cntText.text = IngredientInventoryService.Instance.GetCount(_ingredient.IngredientId).ToString();

        SetSelected(selected);
    }

    public void SetEmpty()
    {
        _ingredient = null;
        _iconImage.enabled = false;
        _cntText.text = string.Empty;
        _onClick = null;
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
