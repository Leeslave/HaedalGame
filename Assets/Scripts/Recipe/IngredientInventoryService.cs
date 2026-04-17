using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientInventoryService : MonoBehaviour
{
    [SerializeField] private List<Ingredient> _initialIngredients = new List<Ingredient>();

    private readonly Dictionary<int, int> _countsByIngredientId = new Dictionary<int, int>();
    private readonly Dictionary<int, long> _acquiredTimeByIngredientId = new Dictionary<int, long>();
    private readonly Dictionary<int, string> _sourceByIngredientId = new Dictionary<int, string>();

    public event Action OnChanged;

    public static IngredientInventoryService Instance { get; private set; }

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
            return;
        }

        _countsByIngredientId.Clear();
        _acquiredTimeByIngredientId.Clear();
        _sourceByIngredientId.Clear();

        for (int i = 0; i < _initialIngredients.Count; i++)
        {
            Ingredient data = _initialIngredients[i];

            if (_countsByIngredientId.ContainsKey(data.IngredientId))
                _countsByIngredientId[data.IngredientId] += data.Amount;
            else
                _countsByIngredientId.Add(data.IngredientId, data.Amount);

            if (!_acquiredTimeByIngredientId.ContainsKey(data.IngredientId))
                _acquiredTimeByIngredientId.Add(data.IngredientId, data.AcquiredTime > 0 ? data.AcquiredTime : GetNow());

            if (!_sourceByIngredientId.ContainsKey(data.IngredientId))
                _sourceByIngredientId.Add(data.IngredientId, data.Source);
        }
    }

    public int GetCount(int ingredientId)
    {
        return _countsByIngredientId.TryGetValue(ingredientId, out int count) ? count : 0;
    }

    public Dictionary<int, int> GetIngrdients()
    {
        return new Dictionary<int, int>(_countsByIngredientId);
    }

    public List<Ingredient> GetIngredientList()
    {
        List<Ingredient> result = new List<Ingredient>();

        foreach (KeyValuePair<int, int> pair in _countsByIngredientId)
        {
            int ingredientId = pair.Key;
            int amount = pair.Value;

            _acquiredTimeByIngredientId.TryGetValue(ingredientId, out long acquiredTime);
            _sourceByIngredientId.TryGetValue(ingredientId, out string source);

            result.Add(new Ingredient(ingredientId, amount, source, acquiredTime));
        }

        return result;
    }

    public void Add(int ingredientId, int amount, string source = "")
    {
        if (amount <= 0)
            return;

        bool isNewIngredient = !_countsByIngredientId.ContainsKey(ingredientId);

        if (_countsByIngredientId.ContainsKey(ingredientId))
            _countsByIngredientId[ingredientId] += amount;
        else
            _countsByIngredientId.Add(ingredientId, amount);

        if (isNewIngredient)
            _acquiredTimeByIngredientId[ingredientId] = GetNow();

        if (!_sourceByIngredientId.ContainsKey(ingredientId) || string.IsNullOrWhiteSpace(_sourceByIngredientId[ingredientId]))
            _sourceByIngredientId[ingredientId] = source;

        OnChanged?.Invoke();
    }

    public void SetCount(int ingredientId, int amount)
    {
        amount = Mathf.Max(0, amount);

        if (amount == 0)
        {
            _countsByIngredientId.Remove(ingredientId);
            OnChanged?.Invoke();
            return;
        }

        bool isNewIngredient = !_countsByIngredientId.ContainsKey(ingredientId);

        _countsByIngredientId[ingredientId] = amount;

        if (isNewIngredient)
            _acquiredTimeByIngredientId[ingredientId] = GetNow();

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

        bool isNewIngredient = !_countsByIngredientId.ContainsKey(ingredientId);

        if (_countsByIngredientId.ContainsKey(ingredientId))
            _countsByIngredientId[ingredientId] += amount;
        else
            _countsByIngredientId.Add(ingredientId, amount);

        if (isNewIngredient)
            _acquiredTimeByIngredientId[ingredientId] = GetNow();
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

    private long GetNow()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

[Serializable]
public class Ingredient
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private int _amount;
    [SerializeField] private string _source;
    [SerializeField] private long _acquiredTime;

    public int IngredientId => _ingredientId;
    public int Amount => _amount;
    public string Source => _source;
    public long AcquiredTime => _acquiredTime;

    public Ingredient(int ingredientId, int amount, string source = "", long acquiredTime = 0)
    {
        _ingredientId = ingredientId;
        _amount = amount;
        _source = source;
        _acquiredTime = acquiredTime;
    }
}