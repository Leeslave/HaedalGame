using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public enum IngredientSortType
{
    AcquireOrder = 0,
    Name = 1,
    Count = 2
}

public class IngredientUIController : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private TMP_Dropdown _sortDropdown;
    [SerializeField] private IngredientDetailPanelUI _detailPanel;


    [SerializeField] private Transform _root;
    [SerializeField] private bool _descendingByDefault = true;

    private IngredientSortType _currentSortType = IngredientSortType.AcquireOrder;
    private Ingredient _selectedIngredient;
    private List<IngredientSlotUI> _slots = new List<IngredientSlotUI>();

    public bool HasSelectedIngredient => _selectedIngredient != null;

    private void Awake()
    {
        _slots = _root.GetComponentsInChildren<IngredientSlotUI>(true).ToList();
    }

    private void OnEnable()
    {
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.AddListener(OnChangedSort);

        if (IngredientInventoryService.Instance != null)
            IngredientInventoryService.Instance.OnChanged += RefreshList;

        RefreshList();
    }

    private void OnDisable()
    {
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.RemoveListener(OnChangedSort);

        if (IngredientInventoryService.Instance != null)
            IngredientInventoryService.Instance.OnChanged -= RefreshList;
    }

    private void OnChangedSort(int value)
    {
        _currentSortType = (IngredientSortType)value;
        RefreshList();
    }

    public void RefreshList()
    {
        ClearSlots();

        if (IngredientInventoryService.Instance == null)
        {
            RefreshDetailOnly();
            return;
        }

        List<Ingredient> ingredients = IngredientInventoryService.Instance.GetIngredientList();
        SortIngredients(ingredients);

        bool stillHasSelected = false;

        for (int i = 0; i < ingredients.Count && i < _slots.Count; i++)
        {
            Ingredient ingredient = ingredients[i];

            if (_selectedIngredient != null && ingredient.IngredientId == _selectedIngredient.IngredientId)
                stillHasSelected = true;

            IngredientSlotUI slot = _slots[i];
            slot.Bind(_database, ingredient, OnClickIngredient, IsSelected(ingredient));
        }

        if (!stillHasSelected)
            _selectedIngredient = null;

        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private void SortIngredients(List<Ingredient> ingredients)
    {
        switch (_currentSortType)
        {
            case IngredientSortType.AcquireOrder:
                ingredients.Sort((a, b) =>
                {
                    int compare = a.AcquiredTime.CompareTo(b.AcquiredTime);
                    return _descendingByDefault ? -compare : compare;
                });
                break;

            case IngredientSortType.Name:
                ingredients.Sort((a, b) =>
                {
                    string aName = GetIngredientName(a.IngredientId);
                    string bName = GetIngredientName(b.IngredientId);
                    int compare = string.Compare(aName, bName, StringComparison.Ordinal);
                    return _descendingByDefault ? -compare : compare;
                });
                break;

            case IngredientSortType.Count:
                ingredients.Sort((a, b) =>
                {
                    int compare = a.Amount.CompareTo(b.Amount);
                    return _descendingByDefault ? -compare : compare;
                });
                break;
        }
    }

    private string GetIngredientName(int ingredientId)
    {
        if (_database != null && _database.TryGetIngredientById(ingredientId, out IngredientData ingredientData) && ingredientData != null)
            return ingredientData.IngredientName;

        return string.Empty;
    }

    private void OnClickIngredient(Ingredient ingredient)
    {
        if (ingredient == null)
            return;

        if (_selectedIngredient != null && _selectedIngredient.IngredientId == ingredient.IngredientId)
            _selectedIngredient = null;
        else
            _selectedIngredient = ingredient;

        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    public void ClearSelection()
    {
        _selectedIngredient = null;
        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            IngredientSlotUI slot = _slots[i];
            if (slot == null)
                continue;

            slot.SetSelected(IsSelected(slot.Ingredient));
        }
    }

    private bool IsSelected(Ingredient ingredient)
    {
        if (_selectedIngredient == null || ingredient == null)
            return false;

        return _selectedIngredient.IngredientId == ingredient.IngredientId;
    }

    private void RefreshDetailOnly()
    {
        if (_detailPanel == null)
            return;

        _detailPanel.Bind(_database, _selectedIngredient);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
                _slots[i].SetEmpty();
        }
    }
}