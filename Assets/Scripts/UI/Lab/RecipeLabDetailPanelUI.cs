using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeLabDetailPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Unlocked Content")]
    [SerializeField] private GameObject _contentRoot;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Ingredients (개방 시)")]
    [SerializeField] private GameObject _ingredientSection;
    [SerializeField] private Transform _ingredientSlotRoot;
    [SerializeField] private RecipeLabIngredientSlotUI _ingredientSlotPrefab;

    [Header("Condition (잠김 시)")]
    [SerializeField] private GameObject _conditionSection;

    [Header("Locked")]
    [SerializeField] private GameObject _lockImage;

    private readonly List<RecipeLabIngredientSlotUI> _spawnedSlots = new List<RecipeLabIngredientSlotUI>();

    public void Bind(
        RecipeData recipe,
        bool locked,
        RecipeDatabaseSO database,
        IngredientInventoryService inventoryService)
    {
        ClearSlots();

        // 선택된 레시피가 없으면 패널 전체를 숨긴다.
        if (_root != null)
            _root.SetActive(recipe != null);

        if (recipe == null)
            return;

        // 이름/등급/설명은 잠금 여부와 상관없이 표시한다.
        if (_contentRoot != null)
            _contentRoot.SetActive(true);

        if (_nameText != null)
            _nameText.text = recipe.RecipeName;

        if (_gradeText != null)
            _gradeText.text = recipe.Grade;

        if (_descriptionText != null)
            _descriptionText.text = recipe.Description;

        // 이미지 슬롯만 스왑: 잠기면 음식 아이콘 대신 자물쇠 이미지.
        if (_iconImage != null)
        {
            _iconImage.sprite = recipe.Icon;
            _iconImage.enabled = !locked && recipe.Icon != null;
        }

        if (_lockImage != null)
            _lockImage.SetActive(locked);

        // 개방: 재료영역 / 잠김: 조건영역 으로 스위치.
        if (_ingredientSection != null)
            _ingredientSection.SetActive(!locked);

        if (_conditionSection != null)
            _conditionSection.SetActive(locked);

        if (!locked)
            BuildIngredientSlots(recipe, database, inventoryService);
    }

    private void BuildIngredientSlots(
        RecipeData recipe,
        RecipeDatabaseSO database,
        IngredientInventoryService inventoryService)
    {
        if (_ingredientSlotPrefab == null || _ingredientSlotRoot == null)
            return;

        for (int i = 0; i < recipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement requirement = recipe.Requirements[i];

            IngredientData ingredient = null;
            if (database != null)
                database.TryGetIngredientById(requirement.IngredientId, out ingredient);

            int ownedCount = inventoryService != null
                ? inventoryService.GetCount(requirement.IngredientId)
                : 0;

            RecipeLabIngredientSlotUI slot = Instantiate(_ingredientSlotPrefab, _ingredientSlotRoot);
            slot.Bind(ingredient, requirement.Amount, ownedCount);
            _spawnedSlots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
    }
}
