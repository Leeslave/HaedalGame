using System.Collections.Generic;
using UnityEngine;

public class RecipeLabInventoryController : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private IngredientInventoryService _inventoryService;

    [Header("List")]
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private RecipeSlotUI _slotPrefab;

    [Header("Category Tabs")]
    [SerializeField] private RecipeCategoryTabUI[] _categoryTabs;

    [Header("Detail")]
    [SerializeField] private RecipeLabDetailPanelUI _detailPanel;


    private readonly List<RecipeSlotUI> _spawnedSlots = new List<RecipeSlotUI>();

    private RecipeCategory _currentCategory = RecipeCategory.All;
    private RecipeData _selectedRecipe;

    public RecipeData SelectedRecipe => _selectedRecipe;
    public bool HasSelectedRecipe => _selectedRecipe != null;

    private void OnEnable()
    {
        if (_recipeBookState != null)
            _recipeBookState.OnChanged += RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged += RefreshDetailOnly;

        if (_categoryTabs != null)
        {
            for (int i = 0; i < _categoryTabs.Length; i++)
            {
                if (_categoryTabs[i] != null)
                    _categoryTabs[i].Bind(OnClickCategory);
            }
        }
    }

    private void OnDisable()
    {
        if (_recipeBookState != null)
            _recipeBookState.OnChanged -= RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged -= RefreshDetailOnly;
    }

    private void Start()
    {
        RefreshList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            MoveTab(-1);
        else if (Input.GetKeyDown(KeyCode.E))
            MoveTab(1);
    }

    private void MoveTab(int direction)
    {
        if (_categoryTabs == null || _categoryTabs.Length == 0)
            return;

        int count = _categoryTabs.Length;
        int currentIndex = GetCurrentTabIndex();

        for (int step = 1; step <= count; step++)
        {
            int nextIndex = ((currentIndex + direction * step) % count + count) % count;
            RecipeCategoryTabUI tab = _categoryTabs[nextIndex];

            if (tab == null)
                continue;

            OnClickCategory(tab.Category);
            return;
        }
    }

    private int GetCurrentTabIndex()
    {
        if (_categoryTabs == null)
            return 0;

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            if (_categoryTabs[i] != null && _categoryTabs[i].Category == _currentCategory)
                return i;
        }

        return 0;
    }

    private void OnClickCategory(RecipeCategory category)
    {
        _currentCategory = category;
        RefreshList();
    }

    public void RefreshList()
    {
        ClearSlots();

        if (_database == null || _slotPrefab == null || _slotRoot == null)
        {
            RefreshTabSelection();
            RefreshDetailOnly();
            return;
        }

        IReadOnlyList<RecipeData> recipes = _database.Recipes;

        bool stillHasSelectedRecipe = false;

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];

            if (!MatchesCategory(recipe))
                continue;

            if (_selectedRecipe != null && recipe.RecipeId == _selectedRecipe.RecipeId)
                stillHasSelectedRecipe = true;

            bool locked = _recipeBookState != null && !_recipeBookState.IsUnlocked(recipe);

            RecipeSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(recipe, OnClickRecipe, IsSelected(recipe), locked);
            _spawnedSlots.Add(slot);
        }

        if (!stillHasSelectedRecipe)
            _selectedRecipe = null;

        RefreshTabSelection();
        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private bool MatchesCategory(RecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (_currentCategory == RecipeCategory.All)
            return true;

        return recipe.Categories != null && recipe.Categories.Contains(_currentCategory);
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

    private void RefreshTabSelection()
    {
        if (_categoryTabs == null)
            return;

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            RecipeCategoryTabUI tab = _categoryTabs[i];
            if (tab == null)
                continue;

            tab.SetSelected(tab.Category == _currentCategory);
        }
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

        bool locked = _selectedRecipe != null
            && _recipeBookState != null
            && !_recipeBookState.IsUnlocked(_selectedRecipe);

        _detailPanel.Bind(_selectedRecipe, locked, _database, _inventoryService);
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
