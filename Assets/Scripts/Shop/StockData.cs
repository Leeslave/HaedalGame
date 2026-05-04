using System.Collections.Concurrent;
using UnityEngine;

[System.Serializable]
public class StockData
{
    public int IngredientID;
    public int CurrentStock;
    public int MaxStock;
    public int LastRefillDay;

    public bool IsSoldOut => CurrentStock == 0;

    public StockData(int ingredientId, int currentStock,int maxStock, int lastRefillDay)
    {
        IngredientID = ingredientId;
        CurrentStock = currentStock;
        MaxStock = maxStock;
        LastRefillDay = lastRefillDay;
    }
}
