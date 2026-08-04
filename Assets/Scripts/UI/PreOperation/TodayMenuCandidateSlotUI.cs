using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 메뉴판 리스트의 한 줄. 클릭하면 "오늘의 메뉴" 등록/해제를 토글한다.
public class TodayMenuCandidateSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private GameObject _registeredMark;

    private RecipeData _recipe;
    private Action<RecipeData> _onClick;

    public RecipeData Recipe => _recipe;

    public void Bind(RecipeData recipe, bool isRegistered, Action<RecipeData> onClick)
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

        if (_priceText != null)
            _priceText.text = recipe != null ? recipe.Price.ToString("N0") : string.Empty;

        if (_registeredMark != null)
            _registeredMark.SetActive(isRegistered);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_recipe == null)
            return;

        _onClick?.Invoke(_recipe);
    }
}
