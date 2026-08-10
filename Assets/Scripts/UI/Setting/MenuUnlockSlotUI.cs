using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Setting 씬 메뉴 해금 리스트의 한 줄.
// 아이콘 클릭: 해금 여부 토글 (저장은 적용 버튼에서 처리).
// 오늘의 메뉴 마크(선택): 클릭하면 오늘의 메뉴 등록 여부를 직접 토글 (해금된 메뉴에서만 동작).
public class MenuUnlockSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private GameObject _unlockedMark;

    [Header("오늘의 메뉴 (선택 사항)")]
    [SerializeField] private Button _dailyMenuButton;
    [SerializeField] private GameObject _dailyMenuMark;

    private RecipeData _recipe;
    private Action<RecipeData> _onClickIcon;
    private Action<RecipeData> _onClickDailyMenu;

    public RecipeData Recipe => _recipe;

    public void Bind(RecipeData recipe, bool isUnlocked, bool isDailyMenu, Action<RecipeData> onClickIcon, Action<RecipeData> onClickDailyMenu)
    {
        _recipe = recipe;
        _onClickIcon = onClickIcon;
        _onClickDailyMenu = onClickDailyMenu;

        if (_iconImage != null)
        {
            _iconImage.sprite = recipe != null ? recipe.Icon : null;
            _iconImage.enabled = recipe != null && recipe.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = recipe != null ? recipe.RecipeName : string.Empty;

        if (_priceText != null)
            _priceText.text = recipe != null ? recipe.Price.ToString("N0") : string.Empty;

        SetUnlocked(isUnlocked);
        SetDailyMenu(isDailyMenu);

        if (_dailyMenuButton != null)
        {
            _dailyMenuButton.onClick.RemoveAllListeners();
            _dailyMenuButton.onClick.AddListener(HandleDailyMenuClicked);
            _dailyMenuButton.interactable = isUnlocked;
        }
    }

    public void SetUnlocked(bool isUnlocked)
    {
        if (_unlockedMark != null)
            _unlockedMark.SetActive(isUnlocked);
    }

    public void SetDailyMenu(bool isDailyMenu)
    {
        if (_dailyMenuMark != null)
            _dailyMenuMark.SetActive(isDailyMenu);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_recipe == null)
            return;

        _onClickIcon?.Invoke(_recipe);
    }

    private void HandleDailyMenuClicked()
    {
        if (_recipe == null)
            return;

        _onClickDailyMenu?.Invoke(_recipe);
    }
}
