using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeDetailPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _recipeIconImage;
    [SerializeField] private TMP_Text _recipeNameText;
    [SerializeField] private Transform _ingredientSlotRoot;
    [SerializeField] private RecipeIngredientRequirementSlotUI _ingredientSlotPrefab;

    private readonly List<RecipeIngredientRequirementSlotUI> _spawnedSlots = new List<RecipeIngredientRequirementSlotUI>();

    public void Bind(RecipeData recipe, RecipeDatabaseSO database, IngredientInventoryService inventoryService)
    {
        ClearSlots();

        if (_root != null)
            _root.SetActive(recipe != null);

        if (recipe == null)
            return;

        _recipeNameText.text = recipe.RecipeName;
        _recipeIconImage.sprite = recipe.Icon;
        _recipeIconImage.enabled = recipe.Icon != null;

        for (int i = 0; i < recipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement requirement = recipe.Requirements[i];

            RecipeIngredientRequirementSlotUI slot =
                Instantiate(_ingredientSlotPrefab, _ingredientSlotRoot);

            IngredientData ingredient = null;
            if (database != null)
                database.TryGetIngredientById(requirement.IngredientId, out ingredient);

            int ownedCount = inventoryService != null
                ? inventoryService.GetCount(requirement.IngredientId)
                : 0;

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