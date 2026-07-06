using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStockManager : MonoBehaviour
{
    public static ShopStockManager Instance;

    [SerializeField] protected RecipeDatabaseSO _database;
    [SerializeField] protected Currency _gold;
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private ShopConfigSO _shopConfig;

    [SerializeField] private int defaultQuantity;


    private Dictionary<IslandType, List<StockData>> _islandStocks = new Dictionary<IslandType, List<StockData>>();
    private List<StockData> _recipeStocks = new List<StockData>();

    public Dictionary<IslandType,List<StockData>> IslandStocks => _islandStocks;
    public List<StockData> RecipeStocks => _recipeStocks;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitStocks();
    }

    public virtual void InitStocks()
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

       // 레시피 재고 초기화 (ShopConfig에 등록된 레시피만)
       _recipeStocks.Clear();
       if (_database != null && _shopConfig != null)
       {
           foreach (int recipeId in _shopConfig.ShopRecipeIds)
           {
               if (!_database.TryGetRecipe(recipeId, out _)) continue;
               _recipeStocks.Add(new StockData(recipeId, 1, 1, 0, isRecipe: true));
           }
       }
    }

    public StockData GetStock(IslandType islandType, int ingredientID)
    {
        _islandStocks.TryGetValue(islandType, out List<StockData> stocks);
        StockData stock = (StockData) from s in stocks where s.IngredientID == ingredientID select s;
        return stock;
    }

    public virtual bool Purchase(int ingredientId, int quantity, int unitPrice)
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

        if (target == null)
        {
            foreach (StockData s in _recipeStocks)
            {
                if (s.IngredientID == ingredientId)
                {
                    target = s;
                    break;
                }
            }
        }

        if (target == null || target.CurrentStock < quantity)
            return false;

        target.CurrentStock -= quantity;

        CurrencyManager.Instance.ProcessTransaction(
            new CurrencyTransaction(_gold, -totalCost, TransactionSource.ShopPurchase));

        if (target.IsRecipe)
        {
            RecipeBookState recipeBook = _recipeBookState != null
                ? _recipeBookState
                : FindFirstObjectByType<RecipeBookState>();
            recipeBook?.UnlockRecipe(ingredientId);
        }
        else
        {
            IngredientInventoryService.Instance.Add(ingredientId, quantity, "Shop");
        }

        return true;
    }

}
