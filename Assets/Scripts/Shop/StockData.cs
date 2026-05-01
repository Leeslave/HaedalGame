using System.Collections.Concurrent;
using UnityEngine;

[System.Serializable]
public class StockData
{
    public int IngredientID;
    public int CurrentStock;
    public int maxStock;
    public int LastRefillDay;

    public bool IsSoldOut => CurrentStock == 0;
}
