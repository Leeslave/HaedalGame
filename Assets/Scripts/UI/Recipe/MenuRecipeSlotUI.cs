using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuRecipeSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MenuRecipeService _menuRecipeService;
    [SerializeField] private RecipeBookController _recipeBookController;
    [SerializeField] private int _slotIndex;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _emptyObject;

    public int SlotIndex => _slotIndex;

    public RecipeData CurrentRecipe
    {
        get
        {
            if (_menuRecipeService == null)
                return null;

            return _menuRecipeService.GetRecipe(_slotIndex);
        }
    }

    private void OnEnable()
    {
        if (_menuRecipeService != null)
            _menuRecipeService.OnChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (_menuRecipeService != null)
            _menuRecipeService.OnChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        RecipeData recipe = CurrentRecipe;

        if (_emptyObject != null)
            _emptyObject.SetActive(recipe == null);

        if (_iconImage != null)
        {
            _iconImage.sprite = recipe != null ? recipe.Icon : null;
            _iconImage.enabled = recipe != null && recipe.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = recipe != null ? recipe.RecipeName : string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_menuRecipeService == null)
            return;

        RecipeData selectedRecipe = _recipeBookController != null
            ? _recipeBookController.SelectedRecipe
            : null;

        // 레시피 선택 상태: 추가 / 교체 시도
        if (selectedRecipe != null)
        {
            MenuRecipeSetResult result = _menuRecipeService.SetRecipe(_slotIndex, selectedRecipe);

            if (result == MenuRecipeSetResult.Success)
            {
                if (_recipeBookController != null)
                    _recipeBookController.ClearSelection();
            }
            else
            {
                switch (result)
                {
                    case MenuRecipeSetResult.DuplicateRecipeInOtherSlot:
                        Debug.Log("이미 다른 슬롯에 같은 메뉴가 배치되어 있습니다.");
                        break;

                    case MenuRecipeSetResult.SameRecipeAlreadyAssigned:
                        Debug.Log("이미 이 슬롯에 같은 메뉴가 배치되어 있습니다.");
                        break;

                    case MenuRecipeSetResult.NotEnoughIngredients:
                        Debug.Log("재료가 부족해서 메뉴를 추가할 수 없습니다.");
                        break;

                    default:
                        Debug.Log($"메뉴 추가 실패: {result}");
                        break;
                }
            }

            return;
        }

        // 선택된 레시피가 없고 현재 메뉴가 비어있으면 아무것도 안 함
        if (CurrentRecipe == null)
            return;

        // 선택된 레시피가 없고 현재 메뉴가 있으면 해제 confirm
        if (PopupManager.Instance == null)
        {
            ClearAssignedRecipe();
            return;
        }

        PopupManager.Instance.ShowConfirmPopup(
            $"{CurrentRecipe.RecipeName} 메뉴를 해제할까요?",
            "해제",
            "취소",
            () =>
            {
                ClearAssignedRecipe();
            });
    }

    public void ClearAssignedRecipe()
    {
        if (_menuRecipeService == null)
            return;

        _menuRecipeService.ClearRecipe(_slotIndex);
    }
}