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
    [SerializeField] private GameObject _lockedObject;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Action<RecipeData> _onClick;
    private RecipeData _recipe;

    public RecipeData Recipe => _recipe;

    public void Bind(RecipeData recipe, Action<RecipeData> onClick, bool selected, bool locked = false)
    {
        _recipe = recipe;
        _onClick = onClick;

        if (_iconImage != null)
        {
            _iconImage.sprite = recipe != null ? recipe.Icon : null;
            _iconImage.enabled = recipe != null && recipe.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = recipe != null ? recipe.RecipeName : string.Empty;

        if (_goldText != null)
            _goldText.text = recipe != null ? recipe.Price.ToString() + "G" : string.Empty;

        if (transform.parent != null)
            _canvasGroup = transform.parent.GetComponent<CanvasGroup>();

        SetSelected(selected);
        SetLocked(locked);
    }

    public void SetLocked(bool locked)
    {
        if (_lockedObject != null)
            _lockedObject.SetActive(locked);
    }

    public void SetEmpty()
    { 
        _recipe = null;

        if (_iconImage != null)
            _iconImage.enabled = false;

        if (_nameText != null)
            _nameText.text = string.Empty;

        if (_goldText != null)
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