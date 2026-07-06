using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecipeCategoryTabUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RecipeCategory _category;
    [SerializeField] private TextMeshProUGUI _selectedObject;
    [SerializeField] private Color32 _selectedColor;
    [SerializeField] private Color32 _unSelectedColor;



    private Action<RecipeCategory> _onClick;

    public RecipeCategory Category => _category;

    public void Bind(Action<RecipeCategory> onClick)
    {
        _onClick = onClick;
    }

    public void SetSelected(bool selected)
    {
        if (_selectedObject != null)
            _selectedObject.color = selected ? _selectedColor : _unSelectedColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick?.Invoke(_category);
    }
}
