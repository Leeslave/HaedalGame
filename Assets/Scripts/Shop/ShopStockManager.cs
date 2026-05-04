using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStockManager : MonoBehaviour
{
    public static ShopStockManager Instance;

    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private Currency _gold;

    [SerializeField] private int defaultQuantity;


    private Dictionary<IslandType, List<StockData>> _islandStocks = new Dictionary<IslandType, List<StockData>>();

    public Dictionary<IslandType,List<StockData>> IslandStocks => _islandStocks;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        InitStocks();
    }

    public void InitStocks()
    {
       foreach(IslandType island in Enum.GetValues(typeof(IslandType)))
       {
            if (island == IslandType.All) continue;

            List<IngredientData> ingredients = _database.GetIngredientsByIslandType(island);      
            List<StockData> stocks = new List<StockData>();
            foreach(var ingredient in ingredients)
            {
                StockData stock = new StockData(ingredient.IngredientId, defaultQuantity, 100, 0);

                if (stock != null)
                    stocks.Add(stock);
            }

            if (stocks.Count > 0)
            _islandStocks.Add(island, stocks);   
       }
    }

    public StockData GetStock(IslandType islandType, int ingredientID)
    {
        _islandStocks.TryGetValue(islandType, out List<StockData> stocks);
        StockData stock = (StockData) from s in stocks where s.IngredientID == ingredientID select s;
        return stock;
    }

    public bool Purchase(int ingredientId, int quantity, int unitPrice)
    {
        int totalCost = unitPrice * quantity;

        if (CurrencyManager.Instance.GetCurrency(_gold) < totalCost)
            return false;

        StockData target = null;
        foreach (List<StockData> stocks in _islandStocks.Values)
        {
            foreach (StockData s in stocks)
            {
                if (s.IngredientID == ingredientId)
                {
                    target = s;
                    break;
                }
            }
            if (target != null) break;
        }

        if (target == null || target.CurrentStock < quantity)
            return false;

        target.CurrentStock -= quantity;

        CurrencyManager.Instance.ProcessTransaction(
            new CurrencyTransaction(_gold, -totalCost, TransactionSource.ShopPurchase));

        IngredientInventoryService.Instance.Add(ingredientId, quantity, "Shop");

        return true;
    }

}
