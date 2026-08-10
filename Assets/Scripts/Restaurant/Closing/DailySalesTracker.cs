using System.Collections.Generic;
using UnityEngine;

// 오늘 팔린 요리 1종에 대한 판매 기록.
public class SaleEntry
{
    public RecipeData Recipe;
    public int Count;
    public int Revenue;
}

// 오늘 하루치 판매(레시피별 판매 개수/매출) 집계.
public class DailySalesTracker : MonoBehaviour
{
    public static DailySalesTracker Instance { get; private set; }

    private Dictionary<int, SaleEntry> _sales = new Dictionary<int, SaleEntry>();

    public IReadOnlyCollection<SaleEntry> Sales => _sales.Values;

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (SaleEntry entry in _sales.Values) { total += entry.Count; }
            return total;
        }
    }

    public int TotalRevenue
    {
        get
        {
            int total = 0;
            foreach (SaleEntry entry in _sales.Values) { total += entry.Revenue; }
            return total;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordSale(RecipeData recipe, int revenue)
    {
        if (recipe == null) { return; }

        SaleEntry entry;
        if (!_sales.TryGetValue(recipe.RecipeId, out entry))
        {
            entry = new SaleEntry();
            entry.Recipe = recipe;
            entry.Count = 0;
            entry.Revenue = 0;
            _sales[recipe.RecipeId] = entry;
        }

        entry.Count += 1;
        entry.Revenue += revenue;
    }

    public void ResetDay()
    {
        _sales.Clear();
    }
}
