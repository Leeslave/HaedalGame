using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [SerializeField]
    protected RecipeDatabaseSO _database;

    [SerializeField]
    protected Transform _slotRoot;

    [SerializeField]
    private ShopSlotUI _slotPrefab;

    [SerializeField]
    List<Button> _sortButtons;

    [SerializeField]
    private ShopDetailPannel _detailPanel;

    protected List<ShopSlotUI> _spawnedSlots = new List<ShopSlotUI>();
    protected IslandType _currentSortType = IslandType.All;
    protected StockData _selectedStock;
    public StockData SelectedStock => _selectedStock;

    private void Awake()
    {
        _spawnedSlots = _slotRoot.GetComponentsInChildren<ShopSlotUI>(true).ToList();
        RefreshList();
    }

    protected virtual void OnEnable()
    {
        if (_sortButtons != null)
        {
            for (int i = 0; i < _sortButtons.Count; i++)
            {
                int capturedIndex = i;
                _sortButtons[i].onClick.AddListener(() => OnChangedSort(capturedIndex));
            }
        }

        if (IngredientInventoryService.Instance != null)
            IngredientInventoryService.Instance.OnChanged += RefreshList;

        RefreshList();
    }

    private void OnChangedSort(int value)
    {
        _currentSortType = (IslandType)value;
        RefreshList();
    }

    public virtual void RefreshList()
    {
        ClearSlots();

        if (ShopStockManager.Instance == null)
            return;

        Dictionary<IslandType, List<StockData>> islandStocks = ShopStockManager
            .Instance
            .IslandStocks;
        if (islandStocks == null)
            return;

        List<StockData> displayStocks = new List<StockData>();

        if (_currentSortType == IslandType.All)
        {
            foreach (KeyValuePair<IslandType, List<StockData>> pair in islandStocks)
            {
                if (pair.Value == null)
                    continue;

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (pair.Value[i] != null)
                        displayStocks.Add(pair.Value[i]);
                }
            }
        }
        else
        {
            if (
                islandStocks.TryGetValue(_currentSortType, out List<StockData> stocks)
                && stocks != null
            )
            {
                for (int i = 0; i < stocks.Count; i++)
                {
                    if (stocks[i] != null)
                        displayStocks.Add(stocks[i]);
                }
            }
        }

        EnsureSlotCount(displayStocks.Count);

        bool stillHasSelectedStock = false;

        for (int i = 0; i < displayStocks.Count; i++)
        {
            if (_spawnedSlots[i] == null)
                continue;
            if (
                _selectedStock != null
                && displayStocks[i].IngredientID == _selectedStock.IngredientID
            )
                stillHasSelectedStock = true;

            _spawnedSlots[i]
                .Bind(_database, displayStocks[i], OnClickStock, IsSelected(displayStocks[i]));
        }

        for (int i = displayStocks.Count; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                _spawnedSlots[i].SetEmpty();
        }

        if (!stillHasSelectedStock)
            _selectedStock = null;
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                _spawnedSlots[i].SetEmpty();
        }
    }

    private void EnsureSlotCount(int requiredCount)
    {
        if (_slotPrefab == null || _slotRoot == null)
            return;

        while (_spawnedSlots.Count < requiredCount)
        {
            ShopSlotUI newSlot = Instantiate(_slotPrefab, _slotRoot);
            _spawnedSlots.Add(newSlot);
        }
    }

    private void OnClickStock(StockData stock)
    {
        if (stock == null)
            return;

        if (_selectedStock != null && _selectedStock.IngredientID == stock.IngredientID)
            _selectedStock = null;
        else
            _selectedStock = stock;

        RefreshSelectionVisual();
        RefreshDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (_detailPanel == null)
            return;

        if (_selectedStock == null)
            _detailPanel.gameObject.SetActive(false);
        else
            _detailPanel.Bind(_database, _selectedStock);
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            ShopSlotUI slot = _spawnedSlots[i];
            if (slot == null)
                continue;

            slot.SetSelected(IsSelected(slot.StockData));
        }
    }

    private bool IsSelected(StockData stock)
    {
        if (_selectedStock == null || stock == null)
            return false;

        return _selectedStock.IngredientID == stock.IngredientID;
    }
}
