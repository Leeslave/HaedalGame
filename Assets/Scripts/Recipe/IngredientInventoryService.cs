using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientInventoryService : MonoBehaviour
{
    [SerializeField] private List<Ingredient> _initialIngredients = new List<Ingredient>();

    private readonly Dictionary<int, int> _countsByIngredientId = new Dictionary<int, int>();

    public event Action OnChanged;

    public static IngredientInventoryService Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

            _countsByIngredientId.Clear();

        for (int i = 0; i < _initialIngredients.Count; i++)
        {
            Ingredient data = _initialIngredients[i];

            if (_countsByIngredientId.ContainsKey(data.IngredientId))
                _countsByIngredientId[data.IngredientId] += data.Amount;
            else
                _countsByIngredientId.Add(data.IngredientId, data.Amount);
        }
    }

    public int GetCount(int ingredientId)
    {
        return _countsByIngredientId.TryGetValue(ingredientId, out int count) ? count : 0;
    }

    public void Add(int ingredientId, int amount)
    {
        if (amount <= 0)
            return;

        if (_countsByIngredientId.ContainsKey(ingredientId))
            _countsByIngredientId[ingredientId] += amount;
        else
            _countsByIngredientId.Add(ingredientId, amount);

        OnChanged?.Invoke();
    }

    public void SetCount(int ingredientId, int amount)
    {
        _countsByIngredientId[ingredientId] = Mathf.Max(0, amount);
        OnChanged?.Invoke();
    }

    public bool HasEnough(int ingredientId, int amount)
    {
        if (amount <= 0)
            return true;

        return GetCount(ingredientId) >= amount;
    }

    public bool Consume(int ingredientId, int amount)
    {
        if (amount <= 0)
            return true;

        if (!HasEnough(ingredientId, amount))
            return false;

        _countsByIngredientId[ingredientId] -= amount;

        if (_countsByIngredientId[ingredientId] <= 0)
            _countsByIngredientId.Remove(ingredientId);

        OnChanged?.Invoke();
        return true;
    }

    public void AddSilently(int ingredientId, int amount)
    {
        if (amount <= 0)
            return;

        if (_countsByIngredientId.ContainsKey(ingredientId))
            _countsByIngredientId[ingredientId] += amount;
        else
            _countsByIngredientId.Add(ingredientId, amount);
    }

    public bool ConsumeSilently(int ingredientId, int amount)
    {
        if (amount <= 0)
            return true;

        if (!HasEnough(ingredientId, amount))
            return false;

        _countsByIngredientId[ingredientId] -= amount;

        if (_countsByIngredientId[ingredientId] <= 0)
            _countsByIngredientId.Remove(ingredientId);

        return true;
    }

    public void NotifyChanged()
    {
        OnChanged?.Invoke();
    }
}

[Serializable]
public class Ingredient
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private int _amount;
    [SerializeField] private string _source;


    public int IngredientId => _ingredientId;
    public int Amount => _amount;

    public string Source => _source;
}