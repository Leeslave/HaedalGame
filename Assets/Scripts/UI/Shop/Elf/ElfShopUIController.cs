using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엘프 상점 UI 컨트롤러. ShopUIController를 상속해
/// 엘프 타입 표시 및 ElfShopSlotUI/ElfShopDetailPanel과 연동.
/// </summary>
public class ElfShopUIController : ShopUIController
{
    [Header("엘프 상점 전용")]
    [SerializeField]
    private ElfShopSlotUI _elfSlotPrefab;

    [SerializeField]
    private ElfShopDetailPanel _elfDetailPanel;

    [SerializeField]
    private Image _elfColorIndicator;

    [SerializeField]
    private TMP_Text _elfTypeText;

    [SerializeField]
    private Color _redElfColor = Color.red;

    [SerializeField]
    private Color _blueElfColor = Color.blue;

    [SerializeField]
    private Color _yellowElfColor = Color.yellow;

    private List<ElfShopSlotUI> _elfSlots = new List<ElfShopSlotUI>();
    private ElfShopStockData _selectedElfStock;
    private bool _started = false;

    // OnEnable은 Awake 이전에도 호출될 수 있으므로 최초 초기화는 Start에서 처리
    protected override void OnEnable()
    {
        if (!_started)
            return;

        if (ElfShopManager.Instance != null)
        {
            ElfShopManager.Instance.OnElfShopRefreshed += RefreshElfShop;
            RefreshElfShop();
        }
    }

    private void Start()
    {
        _started = true;
        if (ElfShopManager.Instance != null)
        {
            ElfShopManager.Instance.OnElfShopRefreshed += RefreshElfShop;
            RefreshElfShop();
        }
    }

    private void OnDisable()
    {
        if (ElfShopManager.Instance != null)
            ElfShopManager.Instance.OnElfShopRefreshed -= RefreshElfShop;
    }

    public void RefreshElfShop()
    {
        RefreshElfVisual();
        RefreshElfSlots();
    }

    private void RefreshElfVisual()
    {
        if (ElfShopManager.Instance == null)
            return;

        ElfType elfType = ElfShopManager.Instance.CurrentElfType;

        if (_elfColorIndicator != null)
        {
            _elfColorIndicator.color = elfType switch
            {
                ElfType.Red => _redElfColor,
                ElfType.Blue => _blueElfColor,
                ElfType.Yellow => _yellowElfColor,
                _ => Color.white,
            };
        }

        if (_elfTypeText != null)
        {
            _elfTypeText.text = elfType switch
            {
                ElfType.Red => "붉은 엘프 상점",
                ElfType.Blue => "푸른 엘프 상점",
                ElfType.Yellow => "노란 엘프 상점",
                _ => string.Empty,
            };
        }
    }

    private void RefreshElfSlots()
    {
        if (ElfShopManager.Instance == null || _database == null)
            return;

        IReadOnlyList<ElfShopStockData> stocks = ElfShopManager.Instance.ElfStocks;

        EnsureElfSlotCount(stocks.Count);

        bool selectedExists = false;

        for (int i = 0; i < stocks.Count; i++)
        {
            if (_elfSlots[i] == null)
                continue;

            bool isSelected =
                _selectedElfStock != null
                && stocks[i].ItemType == _selectedElfStock.ItemType
                && stocks[i].IngredientID == _selectedElfStock.IngredientID;

            if (isSelected)
                selectedExists = true;

            _elfSlots[i].Bind(_database, stocks[i], OnClickElfSlot, isSelected);
        }

        for (int i = stocks.Count; i < _elfSlots.Count; i++)
        {
            if (_elfSlots[i] != null)
                _elfSlots[i].SetEmpty();
        }

        if (!selectedExists)
        {
            _selectedElfStock = null;
            if (_elfDetailPanel != null)
                _elfDetailPanel.gameObject.SetActive(false);
        }
    }

    private void EnsureElfSlotCount(int required)
    {
        if (_elfSlotPrefab == null || _slotRoot == null)
            return;

        while (_elfSlots.Count < required)
        {
            ElfShopSlotUI newSlot = Instantiate(_elfSlotPrefab, _slotRoot);
            _elfSlots.Add(newSlot);
        }
    }

    private void OnClickElfSlot(StockData stock)
    {
        ElfShopStockData elfStock = stock as ElfShopStockData;
        if (elfStock == null)
            return;

        bool same =
            _selectedElfStock != null
            && _selectedElfStock.ItemType == elfStock.ItemType
            && _selectedElfStock.IngredientID == elfStock.IngredientID;

        _selectedElfStock = same ? null : elfStock;

        RefreshElfSelectionVisuals();

        if (_elfDetailPanel == null)
            return;
        if (_selectedElfStock == null)
            _elfDetailPanel.gameObject.SetActive(false);
        else
            _elfDetailPanel.Bind(_database, _selectedElfStock);
    }

    private void RefreshElfSelectionVisuals()
    {
        foreach (ElfShopSlotUI slot in _elfSlots)
        {
            if (slot == null || slot.StockData == null)
                continue;

            ElfShopStockData elfStock = slot.StockData as ElfShopStockData;
            bool isSelected =
                _selectedElfStock != null
                && elfStock != null
                && elfStock.ItemType == _selectedElfStock.ItemType
                && elfStock.IngredientID == _selectedElfStock.IngredientID;

            slot.SetSelected(isSelected);
        }
    }
}
