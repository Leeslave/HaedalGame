using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum RecipeSortType
{
    AcquireOrder = 0,
    Name = 1,
    Price = 2
}

public class RecipeBookController : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private IngredientInventoryService _inventoryService;

    [Header("List")]
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private RecipeSlotUI _slotPrefab;
    [SerializeField] private TMP_Dropdown _sortDropdown;

    [Header("Detail")]
    [SerializeField] private RecipeDetailPanelUI _detailPanel;

    private readonly List<RecipeSlotUI> _spawnedSlots = new List<RecipeSlotUI>();

    private RecipeData _selectedRecipe;
    private RecipeSortType _currentSortType = RecipeSortType.AcquireOrder;
    private bool _isAscending = false;

    public RecipeData SelectedRecipe => _selectedRecipe;
    public bool HasSelectedRecipe => _selectedRecipe != null;

    private void OnEnable()
    {
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.AddListener(OnChangedSort);

        if (_recipeBookState != null)
            _recipeBookState.OnChanged += RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged += RefreshDetailOnly;
    }

    private void Start()
    {
        RefreshList();
    }

    private void OnDisable()
    {
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.RemoveListener(OnChangedSort);

        if (_recipeBookState != null)
            _recipeBookState.OnChanged -= RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged -= RefreshDetailOnly;
    }

    private void OnChangedSort(int value)
    {
        _currentSortType = (RecipeSortType)value;
        RefreshList();
    }

    public void RefreshList()
    {
        ClearSlots();

        if (_recipeBookState == null || _slotPrefab == null || _slotRoot == null)
        {
            RefreshDetailOnly();
            return;
        }

        List<RecipeData> recipes = _recipeBookState.GetUnlockedRecipes();
        SortRecipes(recipes);

        bool stillHasSelectedRecipe = false;

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];

            if (_selectedRecipe != null && recipe.RecipeId == _selectedRecipe.RecipeId)
                stillHasSelectedRecipe = true;

            RecipeSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(recipe, OnClickRecipe, IsSelected(recipe));
            _spawnedSlots.Add(slot);
        }

        if (!stillHasSelectedRecipe)
            _selectedRecipe = null;

        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private void SortRecipes(List<RecipeData> recipes)
    {
        switch (_currentSortType)
        {
            case RecipeSortType.AcquireOrder:
                recipes.Sort((a, b) =>
                {
                    int compare = _recipeBookState.GetAcquireOrder(a.RecipeId)
                        .CompareTo(_recipeBookState.GetAcquireOrder(b.RecipeId));

                    return _isAscending ? compare : -compare;
                });
                break;

            case RecipeSortType.Name:
                recipes.Sort((a, b) =>
                {
                    int compare = string.Compare(a.RecipeName, b.RecipeName, StringComparison.Ordinal);
                    return _isAscending ? compare : -compare;
                });
                break;

            case RecipeSortType.Price:
                recipes.Sort((a, b) =>
                {
                    int compare = a.Price.CompareTo(b.Price);
                    return _isAscending ? compare : -compare;
                });
                break;
        }
    }

    public void ToggleSortOrder()
    {
        _isAscending = !_isAscending;

        RefreshList();
    }

    private void OnClickRecipe(RecipeData recipe)
    {
        if (recipe == null)
            return;

        if (_selectedRecipe != null && _selectedRecipe.RecipeId == recipe.RecipeId)
            _selectedRecipe = null;
        else
            _selectedRecipe = recipe;

        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    public void ClearSelection()
    {
        _selectedRecipe = null;
        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            RecipeSlotUI slot = _spawnedSlots[i];
            if (slot == null)
                continue;

            slot.SetSelected(IsSelected(slot.Recipe));
        }
    }

    private bool IsSelected(RecipeData recipe)
    {
        if (_selectedRecipe == null || recipe == null)
            return false;

        return _selectedRecipe.RecipeId == recipe.RecipeId;
    }

    private void RefreshDetailOnly()
    {
        if (_detailPanel == null)
            return;

        _detailPanel.Bind(_selectedRecipe, _database, _inventoryService);
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