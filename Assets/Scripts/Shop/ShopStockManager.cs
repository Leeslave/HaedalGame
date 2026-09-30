using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStockManager : MonoBehaviour, ISaveParticipant
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
        GameSession.Register(this);
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    // 품목 구성은 DB/설정에서 다시 만들고, 남은 재고 수량만 저장한다. (현재 일일 재입고 규칙은 없음)
    public void CaptureState(GameSaveData data)
    {
        data.regularShopStocks.Clear();

        foreach (List<StockData> stocks in _islandStocks.Values)
        {
            foreach (StockData stock in stocks)
                data.regularShopStocks.Add(ToEntry(stock));
        }

        foreach (StockData stock in _recipeStocks)
            data.regularShopStocks.Add(ToEntry(stock));
    }

    public void RestoreState(GameSaveData data)
    {
        _islandStocks.Clear();
        InitStocks();

        foreach (ShopStockEntry entry in data.regularShopStocks)
        {
            StockData stock = FindStock(entry.itemId, entry.isRecipe);
            if (stock != null)
                stock.CurrentStock = Mathf.Clamp(entry.currentStock, 0, stock.MaxStock);
        }
    }

    private static ShopStockEntry ToEntry(StockData stock)
    {
        return new ShopStockEntry
        {
            itemId = stock.IngredientID,
            isRecipe = stock.IsRecipe,
            currentStock = stock.CurrentStock,
            maxStock = stock.MaxStock,
        };
    }

    private StockData FindStock(int itemId, bool isRecipe)
    {
        if (isRecipe)
            return _recipeStocks.Find(s => s.IngredientID == itemId);

        foreach (List<StockData> stocks in _islandStocks.Values)
        {
            StockData found = stocks.Find(s => s.IngredientID == itemId);
            if (found != null)
                return found;
        }
        return null;
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

        if (GameSession.IsActive)
            CaptureState(GameSession.Current);

        if (target.IsRecipe)
        {
            if (_recipeBookState != null)
                _recipeBookState.UnlockRecipe(ingredientId);
            else
                RecipeBookState.UnlockRecipeAnywhere(ingredientId);
        }
        else
        {
            IngredientInventoryService.Instance.Add(ingredientId, quantity, "Shop");
        }

        return true;
    }

}
