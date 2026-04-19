using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecipeSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _goldText;

    [SerializeField] private GameObject _selectedObject;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Action<RecipeData> _onClick;
    private RecipeData _recipe;

    public RecipeData Recipe => _recipe;

    public void Bind(RecipeData recipe, Action<RecipeData> onClick, bool selected)
    {
        _recipe = recipe;
        _onClick = onClick;

        _iconImage.sprite = recipe != null ? recipe.Icon : null;
        _iconImage.enabled = recipe != null && recipe.Icon != null;
        _nameText.text = recipe != null ? recipe.RecipeName : string.Empty;
        _goldText.text = recipe != null ? recipe.Price.ToString() + "G" : string.Empty;
        _canvasGroup = transform.parent.GetComponent<CanvasGroup>();
        SetSelected(selected);
    }

    public void SetEmpty()
    { 
        _recipe = null;
        _iconImage.enabled = false;
        _nameText.text = string.Empty;
        _goldText.text = string.Empty;
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
        if (_recipe == null)
            return;

        _onClick?.Invoke(_recipe);
    }
}