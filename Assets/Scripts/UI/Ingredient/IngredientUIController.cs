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

    private IngredientSortType _currentSortType = IngredientSortType.AcquireOrder;
    private Ingredient _selectedIngredient;


    [SerializeField] private Transform _root;
    private  List<IngredientSlotUI> _slots = new List<IngredientSlotUI>();


    public bool HasSelectedIngredient => _selectedIngredient != null;

    private void Awake()
    {
       _slots = _root.GetComponentsInChildren<IngredientSlotUI>().ToList();
    }

    private void OnEnable()
    {

    }

    public void RefreshList()
    {
        ClearSlots();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null)
                _slots[i].SetEmpty();
        }

    }
    private void SortRecipes(List<Ingredient> ingredients)
    {
        switch(_currentSortType)
        {

        }
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


    private bool IsSelected(IngredientData ingredient)
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
}
