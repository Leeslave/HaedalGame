using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ElfShopManager : MonoBehaviour
{
    public static ElfShopManager Instance { get; private set; }

    [Header("공통 설정")]
    [SerializeField]
    private RecipeDatabaseSO _database;

    [SerializeField]
    private Currency _gold;

    [Header("엘프 상점 설정")]
    [SerializeField]
    private ElfShopConfigSO _elfConfig;

    public const float ELF_PRICE_MULTIPLIER = 0.8f;

    private const float WEIGHT_PENALTY = 10f;
    private const float DEFAULT_WEIGHT_TWO = 50f;
    private const float DEFAULT_WEIGHT_THREE = 100f / 3f;

    private const string Key_Day = "ElfShop_Day";
    private const string Key_CurrentElf = "ElfShop_CurrentElf";
    private const string Key_WeightRed = "ElfShop_WRed";
    private const string Key_WeightBlue = "ElfShop_WBlue";
    private const string Key_WeightYellow = "ElfShop_WYellow";
    private const string Key_Stocks = "ElfShop_Stocks";

    private int _savedDay;
    private ElfType _currentElfType;
    private float _weightRed;
    private float _weightBlue;
    private float _weightYellow;

    private List<ElfShopStockData> _elfStocks = new List<ElfShopStockData>();

    public ElfType CurrentElfType => _currentElfType;
    public IReadOnlyList<ElfShopStockData> ElfStocks => _elfStocks;

    public Action OnElfShopRefreshed;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadElfState();
        CheckDailyRefresh();
    }

    private void Start()
    {
        if (InGameTimeManager.Instance != null)
            InGameTimeManager.Instance.OnDayAdvanced += OnDayAdvanced;

        ForceRefresh();
    }

    private void OnDestroy()
    {
        if (InGameTimeManager.Instance != null)
            InGameTimeManager.Instance.OnDayAdvanced -= OnDayAdvanced;
    }

    private void OnDayAdvanced(int day)
    {
        _savedDay = day;
        RefreshShop();
    }

    private void CheckDailyRefresh()
    {
        int today = InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 0;
        if (today != _savedDay)
        {
            _savedDay = today;
            RefreshShop();
        }
    }

    private void RefreshShop()
    {
        ElfType rolledElf = WeightedRandom();
        UpdateWeightsForNextDay(rolledElf);
        _currentElfType = rolledElf;

        GenerateElfStocks();
        SaveElfState();
        OnElfShopRefreshed?.Invoke();
    }

    public void ForceRefresh()
    {
        RefreshShop();
    }

    [ContextMenu("테스트: 재고 초기화")]
    public void ResetStockQuantities()
    {
        foreach (ElfShopStockData stock in _elfStocks)
            stock.CurrentStock = stock.MaxStock;

        SaveElfState();
        OnElfShopRefreshed?.Invoke();
    }

    // ───── 재고 생성 ─────

    private void GenerateElfStocks()
    {
        _elfStocks.Clear();

        int level =
            RestaurantLevelManager.Instance != null
                ? RestaurantLevelManager.Instance.CurrentLevel
                : 1;
        int slotCount = GetSlotCount(level);

        List<int> pool = GetItemPool(_currentElfType);
        if (pool == null || pool.Count == 0)
            return;

        List<int> shuffled = pool.OrderBy(_ => UnityEngine.Random.value).ToList();
        int count = Mathf.Min(slotCount, shuffled.Count);

        for (int i = 0; i < count; i++)
        {
            ElfShopStockData stock = CreateStock(_currentElfType, shuffled[i]);
            if (stock != null)
                _elfStocks.Add(stock);
        }
    }

    private int GetSlotCount(int level)
    {
        if (level >= 7) return 8;
        if (level >= 4) return 6;
        return 4;
    }

    private List<int> GetItemPool(ElfType elfType)
    {
        if (_elfConfig == null)
            return new List<int>();

        return elfType switch
        {
            ElfType.Red => _elfConfig.RedElfIngredientIds.ToList(),
            ElfType.Blue => _elfConfig.BlueElfRecipeIds.ToList(),
            ElfType.Yellow => _elfConfig.YellowElfIngredientIds.ToList(),
            _ => new List<int>(),
        };
    }

    private ElfShopStockData CreateStock(ElfType elfType, int itemId)
    {
        return elfType switch
        {
            ElfType.Red => new ElfShopStockData(ElfShopItemType.Ingredient, itemId, 50),
            ElfType.Blue => new ElfShopStockData(ElfShopItemType.Recipe, itemId, 1),
            ElfType.Yellow => new ElfShopStockData(ElfShopItemType.Ingredient, itemId, 10),
            _ => null,
        };
    }

    // ───── 구매 ─────

    public bool PurchaseFromElf(ElfShopStockData stock, int quantity)
    {
        if (stock == null || stock.IsSoldOut)
            return false;
        if (quantity <= 0 || quantity > stock.CurrentStock)
            return false;

        int unitPrice = GetElfUnitPrice(stock);
        int totalCost = unitPrice * quantity;

        if (CurrencyManager.Instance.GetCurrency(_gold) < totalCost)
            return false;

        stock.CurrentStock -= quantity;

        CurrencyManager.Instance.ProcessTransaction(
            new CurrencyTransaction(_gold, -totalCost, TransactionSource.ShopPurchase)
        );

        if (stock.ItemType == ElfShopItemType.Ingredient)
        {
            IngredientInventoryService.Instance.Add(stock.IngredientID, quantity, "ElfShop");
        }
        else
        {
            RecipeBookState recipeBook = FindFirstObjectByType<RecipeBookState>();
            if (recipeBook != null)
                recipeBook.UnlockRecipe(stock.IngredientID);
        }

        SaveElfState();
        OnElfShopRefreshed?.Invoke();
        return true;
    }

    public int GetElfUnitPrice(ElfShopStockData stock)
    {
        if (stock == null || _database == null)
            return 0;

        if (stock.ItemType == ElfShopItemType.Ingredient)
        {
            if (_database.TryGetIngredientById(stock.IngredientID, out IngredientData ingredient))
                return Mathf.RoundToInt(ingredient.Price * ELF_PRICE_MULTIPLIER);
        }
        else
        {
            if (_database.TryGetRecipe(stock.IngredientID, out RecipeData recipe))
                return Mathf.RoundToInt(recipe.Price * ELF_PRICE_MULTIPLIER);
        }

        return 0;
    }

    // ───── 가중치 ─────

    private void UpdateWeightsForNextDay(ElfType appearedElf)
    {
        bool yellowUnlocked = IsYellowUnlocked();
        bool consecutive = appearedElf == _currentElfType;

        if (!consecutive)
        {
            ResetWeights(yellowUnlocked);
            return;
        }

        int activeCount = yellowUnlocked ? 3 : 2;
        float bonus = WEIGHT_PENALTY / (activeCount - 1);

        switch (appearedElf)
        {
            case ElfType.Red:
                _weightRed = Mathf.Max(0f, _weightRed - WEIGHT_PENALTY);
                _weightBlue += bonus;
                if (yellowUnlocked) _weightYellow += bonus;
                break;
            case ElfType.Blue:
                _weightBlue = Mathf.Max(0f, _weightBlue - WEIGHT_PENALTY);
                _weightRed += bonus;
                if (yellowUnlocked) _weightYellow += bonus;
                break;
            case ElfType.Yellow:
                _weightYellow = Mathf.Max(0f, _weightYellow - WEIGHT_PENALTY);
                _weightRed += bonus;
                _weightBlue += bonus;
                break;
        }
    }

    private void ResetWeights(bool yellowUnlocked)
    {
        if (yellowUnlocked)
        {
            _weightRed = DEFAULT_WEIGHT_THREE;
            _weightBlue = DEFAULT_WEIGHT_THREE;
            _weightYellow = DEFAULT_WEIGHT_THREE;
        }
        else
        {
            _weightRed = DEFAULT_WEIGHT_TWO;
            _weightBlue = DEFAULT_WEIGHT_TWO;
            _weightYellow = 0f;
        }
    }

    private ElfType WeightedRandom()
    {
        bool yellowUnlocked = IsYellowUnlocked();
        float total = _weightRed + _weightBlue + (yellowUnlocked ? _weightYellow : 0f);

        if (total <= 0f)
            return ElfType.Red;

        float roll = UnityEngine.Random.Range(0f, total);
        if (roll < _weightRed)
            return ElfType.Red;
        roll -= _weightRed;
        if (roll < _weightBlue)
            return ElfType.Blue;
        return ElfType.Yellow;
    }

    private bool IsYellowUnlocked()
    {
        int level =
            RestaurantLevelManager.Instance != null
                ? RestaurantLevelManager.Instance.CurrentLevel
                : 1;
        return level >= 3;
    }

    // ───── 저장/불러오기 ─────

    private void LoadElfState()
    {
        _savedDay = PlayerPrefs.GetInt(Key_Day, -1);
        _currentElfType = (ElfType)PlayerPrefs.GetInt(Key_CurrentElf, (int)ElfType.Red);

        bool yellowUnlocked = IsYellowUnlocked();
        float def = yellowUnlocked ? DEFAULT_WEIGHT_THREE : DEFAULT_WEIGHT_TWO;

        _weightRed = PlayerPrefs.GetFloat(Key_WeightRed, def);
        _weightBlue = PlayerPrefs.GetFloat(Key_WeightBlue, def);
        _weightYellow = yellowUnlocked
            ? PlayerPrefs.GetFloat(Key_WeightYellow, DEFAULT_WEIGHT_THREE)
            : 0f;

        string json = PlayerPrefs.GetString(Key_Stocks, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                ElfShopSaveData data = JsonUtility.FromJson<ElfShopSaveData>(json);
                _elfStocks = data?.stocks ?? new List<ElfShopStockData>();
                RemoveStocksNotInConfig();
            }
            catch
            {
                _elfStocks = new List<ElfShopStockData>();
            }
        }
    }

    private void RemoveStocksNotInConfig()
    {
        if (_elfConfig == null)
            return;

        List<int> validPool = GetItemPool(_currentElfType);
        _elfStocks.RemoveAll(stock => !validPool.Contains(stock.IngredientID));
    }

    private void SaveElfState()
    {
        PlayerPrefs.SetInt(Key_Day, _savedDay);
        PlayerPrefs.SetInt(Key_CurrentElf, (int)_currentElfType);
        PlayerPrefs.SetFloat(Key_WeightRed, _weightRed);
        PlayerPrefs.SetFloat(Key_WeightBlue, _weightBlue);
        PlayerPrefs.SetFloat(Key_WeightYellow, _weightYellow);
        PlayerPrefs.SetString(
            Key_Stocks,
            JsonUtility.ToJson(new ElfShopSaveData { stocks = _elfStocks })
        );
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class ElfShopSaveData
{
    public List<ElfShopStockData> stocks = new List<ElfShopStockData>();
}
