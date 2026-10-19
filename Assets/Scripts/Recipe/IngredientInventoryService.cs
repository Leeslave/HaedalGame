using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IngredientInventoryService : MonoBehaviour, ISaveParticipant
{
    [SerializeField] private List<Ingredient> _initialIngredients = new List<Ingredient>();
    private readonly List<IngredientGradeEntry> _stacks = new List<IngredientGradeEntry>();
    private readonly Dictionary<int, int> _dailyGrades = new Dictionary<int, int>();
    private readonly Dictionary<int, Ingredient> _metadata = new Dictionary<int, Ingredient>();
    public static IngredientInventoryService Instance { get; private set; }
    public event Action OnChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        foreach (var item in _initialIngredients) AddSilently(item.IngredientId, item.Amount);
        GameSession.Register(this);
    }
    private void OnDestroy() { if (Instance == this) { GameSession.Unregister(this); Instance = null; } }
    public static bool IsUnlimited(int id) => GameDatabase.Recipes != null && GameDatabase.Recipes.TryGetIngredientById(id, out var item) && item.IsBasicSeasoning;
    public int GetCount(int id) => IsUnlimited(id) ? 999999 : _stacks.Where(s => s.ingredientId == id).Sum(s => s.amount);
    public int HighestGrade(int id) => _stacks.Where(s => s.ingredientId == id && s.amount > 0).Select(s => s.grade).DefaultIfEmpty(0).Max();
    public int ActiveGrade(int id) => _dailyGrades.TryGetValue(id, out var grade) ? grade : HighestGrade(id);
    public int UsableCount(int id) => IsUnlimited(id) ? 999999 : _stacks.Where(s => s.ingredientId == id && s.grade == ActiveGrade(id)).Sum(s => s.amount);
    public void BeginOperation()
    {
        _dailyGrades.Clear();
        foreach (var s in _stacks) _dailyGrades[s.ingredientId] = HighestGrade(s.ingredientId);
    }
    public void EndOperation() => _dailyGrades.Clear();
    public Dictionary<int, int> GetIngrdients() => _stacks.Select(s => s.ingredientId).Distinct().ToDictionary(id => id, GetCount);
    public List<Ingredient> GetIngredientList() => GetIngrdients().Select(p => new Ingredient(p.Key, p.Value,
        _metadata.TryGetValue(p.Key, out var m) ? m.Source : "", _metadata.TryGetValue(p.Key, out m) ? m.AcquiredTime : 0)).ToList();
    public void Add(int id, int amount, string source = "") { AddGrade(id, 0, amount, source); }
    public void AddGrade(int id, int grade, int amount, string source = "")
    {
        AddStack(id, grade, amount, source);
        NotifyChanged();
    }
    private void AddStack(int id, int grade, int amount, string source = "")
    {
        if (amount <= 0 || IsUnlimited(id)) return;
        grade = Mathf.Clamp(grade, 0, 5);
        var stack = _stacks.Find(s => s.ingredientId == id && s.grade == grade);
        if (stack == null) _stacks.Add(new IngredientGradeEntry { ingredientId = id, grade = grade, amount = amount });
        else stack.amount += amount;
        if (!_metadata.ContainsKey(id)) _metadata[id] = new Ingredient(id, 0, source, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
    public void AddSilently(int id, int amount) => AddStack(id, 0, amount);
    public void SetCount(int id, int amount) { _stacks.RemoveAll(s => s.ingredientId == id); AddStack(id, 0, amount); NotifyChanged(); }
    public bool HasEnough(int id, int amount) => amount <= 0 || UsableCount(id) >= amount;
    public bool ConsumeSilently(int id, int amount)
    {
        if (amount <= 0 || IsUnlimited(id)) return true;
        if (!HasEnough(id, amount)) return false;
        _stacks.Find(s => s.ingredientId == id && s.grade == ActiveGrade(id)).amount -= amount;
        return true;
    }
    public bool Consume(int id, int amount) { if (!ConsumeSilently(id, amount)) return false; NotifyChanged(); return true; }
    public int CraftableCount(RecipeData recipe)
    {
        if (recipe == null) return 0;
        return recipe.Requirements.GroupBy(r => r.IngredientId).Where(g => g.Sum(r => r.Amount) > 0)
            .Select(g => UsableCount(g.Key) / g.Sum(r => r.Amount)).DefaultIfEmpty(999999).Min();
    }
    // All requirements are checked together before any stack is changed (including repeated IDs).
    public bool TryConsumeRecipe(RecipeData recipe, out int quality)
    {
        quality = 0;
        if (recipe == null || CraftableCount(recipe) < 1) return false;
        float sum = 0; int count = 0;
        var needs = recipe.Requirements.GroupBy(r => r.IngredientId).Select(g => new { Id = g.Key, Amount = g.Sum(r => r.Amount) }).ToArray();
        foreach (var req in needs)
        {
            if (!IsUnlimited(req.Id)) { sum += ActiveGrade(req.Id) * req.Amount; count += req.Amount; }
            ConsumeSilently(req.Id, req.Amount);
        }
        quality = count > 0 ? Mathf.FloorToInt(sum / count) : 0;
        NotifyChanged(); return true;
    }
    public bool PromoteAll(int id)
    {
        if (GameFlow.Phase == GamePhase.Operation || IsUnlimited(id)) return false;
        bool changed = false;
        for (int grade = 0; grade < 5; grade++)
        {
            var s = _stacks.Find(x => x.ingredientId == id && x.grade == grade);
            int n = s == null ? 0 : s.amount / 10;
            if (n == 0) continue;
            s.amount %= 10; AddStack(id, grade + 1, n); changed = true;
        }
        if (changed) NotifyChanged();
        return changed;
    }
    public void NotifyChanged() => OnChanged?.Invoke();
    public void CaptureState(GameSaveData data)
    {
        data.ingredients = GetIngredientList().Select(i => new IngredientStackEntry { ingredientId = i.IngredientId, amount = i.Amount, source = i.Source, acquiredTime = i.AcquiredTime }).ToList();
        data.progression.ingredientGrades = _stacks.Where(s => s.amount > 0).Select(s => new IngredientGradeEntry { ingredientId = s.ingredientId, grade = s.grade, amount = s.amount }).ToList();
    }
    public void RestoreState(GameSaveData data)
    {
        _stacks.Clear(); _metadata.Clear(); _dailyGrades.Clear();
        if (data.progression.ingredientGrades.Count > 0)
            foreach (var s in data.progression.ingredientGrades) AddStack(s.ingredientId, s.grade, s.amount);
        else foreach (var s in data.ingredients) AddStack(s.ingredientId, 0, s.amount, s.source);
        foreach (var s in data.ingredients) _metadata[s.ingredientId] = new Ingredient(s.ingredientId, s.amount, s.source, s.acquiredTime);
        NotifyChanged();
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
    { _ingredientId = ingredientId; _amount = amount; _source = source; _acquiredTime = acquiredTime; }
}
