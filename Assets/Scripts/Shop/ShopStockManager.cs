using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopStockManager : MonoBehaviour
{
    public static ShopStockManager Instance;

    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private Currency _gold;

    private Dictionary<IslandType, List<StockData>> _islandStocks;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public StockData GetStock(IslandType islandType, int ingredientID)
    {
        _islandStocks.TryGetValue(islandType, out List<StockData> stocks);
        StockData stock = (StockData) from s in stocks where s.IngredientID == ingredientID select s;
        return stock;
    }

    public bool TryPurchase(IslandType islandType, int ingredientId, int quantity)
    {
       _database.TryGetIngredientById(ingredientId, out IngredientData data);
        int price = data.Pirce;

        if (price > CurrencyManager.Instance.GetCurrency(_gold))
        {
            return true;

        }
        else
            return false;
    }

}
