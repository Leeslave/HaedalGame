using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엘프 상점. 하루에 한 번 엘프와 재고를 정하고, 그 결과는 세이브(GameSaveData.elfShop)에 그대로 남는다.
/// 추첨은 runId + 일차로 시드를 고정한 결정적 난수를 사용한다. 따라서 이어하기·저장 재시도로 같은 날을
/// 다시 계산해도 엘프와 재고가 바뀌지 않는다. (로드 때 재추첨으로 결과가 바뀌는 일 방지)
/// </summary>
public class ElfShopManager : MonoBehaviour, ISaveParticipant
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

    private ElfShopSaveState _state = new ElfShopSaveState();
    private List<ElfShopStockData> _elfStocks = new List<ElfShopStockData>();

    public ElfType CurrentElfType => ParseElf(_state.currentElf);
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

        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    // ───── 세이브 ─────

    public void CaptureState(GameSaveData data)
    {
        _state.stocks.Clear();
        foreach (ElfShopStockData stock in _elfStocks)
        {
            _state.stocks.Add(new ElfShopStockEntry
            {
                itemType = stock.ItemType.ToString(),
                itemId = stock.IngredientID,
                currentStock = stock.CurrentStock,
                maxStock = stock.MaxStock,
            });
        }

        data.elfShop = CloneState(_state);
    }

    public void RestoreState(GameSaveData data)
    {
        // 해당 일차의 재고가 아직 정해지지 않았으면(새 게임, 또는 하루 종료 시 상점이 로드되지 않았던 경우) 정한다.
        // 시드가 고정되어 있어 몇 번을 계산해도 같은 결과다.
        if (data.elfShop == null || data.elfShop.targetDay != data.day)
            data.elfShop = CreateStateForDay(data.elfShop, data.day, data.restaurantLevel, data.runId);

        ApplyState(data.elfShop);
    }

    private void ApplyState(ElfShopSaveState state)
    {
        _state = CloneState(state);
        _elfStocks.Clear();

        foreach (ElfShopStockEntry entry in _state.stocks)
        {
            if (entry == null || !Enum.TryParse(entry.itemType, out ElfShopItemType itemType))
                continue;

            ElfShopStockData stock = new ElfShopStockData(itemType, entry.itemId, entry.maxStock);
            stock.CurrentStock = Mathf.Clamp(entry.currentStock, 0, entry.maxStock);
            _elfStocks.Add(stock);
        }

        OnElfShopRefreshed?.Invoke();
    }

    /// <summary>
    /// 이전 상태를 바탕으로 해당 일차의 엘프·재고를 정한다. 런타임 상태는 바꾸지 않는 순수 계산이다.
    /// 하루 종료 처리에서 다음 날 저장 후보를 만들 때도 사용한다.
    /// </summary>
    public ElfShopSaveState CreateStateForDay(ElfShopSaveState previous, int day, int restaurantLevel, string runId)
    {
        ElfShopSaveState next = previous != null ? CloneState(previous) : new ElfShopSaveState();
        System.Random rng = new System.Random(DeterministicSeed(runId, day, "ElfShop"));

        bool yellowUnlocked = IsYellowUnlocked(restaurantLevel);
        ElfType previousElf = ParseElf(next.currentElf);

        if (previous == null || previous.targetDay <= 0)
            ResetWeights(next, yellowUnlocked);

        ElfType rolledElf = WeightedRandom(next, yellowUnlocked, rng);
        UpdateWeightsForNextDay(next, previousElf, rolledElf, yellowUnlocked);

        next.currentElf = rolledElf.ToString();
        next.targetDay = day;
        next.stocks = GenerateElfStocks(rolledElf, restaurantLevel, rng);
        return next;
    }

    // ───── 테스트 ─────

    [ContextMenu("테스트: 오늘 상점 다시 추첨")]
    public void ForceRefresh()
    {
        string seedSource = Guid.NewGuid().ToString("N");
        int day = GameSession.IsActive ? GameSession.Current.day : 1;
        int level = RestaurantLevelManager.Instance != null ? RestaurantLevelManager.Instance.CurrentLevel : 1;

        ApplyState(CreateStateForDay(_state, day, level, seedSource));
        PushToSession();
    }

    [ContextMenu("테스트: 재고 초기화")]
    public void ResetStockQuantities()
    {
        foreach (ElfShopStockData stock in _elfStocks)
            stock.CurrentStock = stock.MaxStock;

        PushToSession();
        OnElfShopRefreshed?.Invoke();
    }

    // ───── 재고 생성 ─────

    private List<ElfShopStockEntry> GenerateElfStocks(ElfType elfType, int level, System.Random rng)
    {
        List<ElfShopStockEntry> result = new List<ElfShopStockEntry>();

        IReadOnlyList<ElfShopItemEntry> pool = GetItemPool(elfType);
        if (pool == null || pool.Count == 0)
            return result;

        // Fisher-Yates
        List<ElfShopItemEntry> shuffled = new List<ElfShopItemEntry>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int count = Mathf.Min(GetSlotCount(level), shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            int maxQty = shuffled[i].ItemType == ElfShopItemType.Recipe ? 1 : 50;
            result.Add(new ElfShopStockEntry
            {
                itemType = shuffled[i].ItemType.ToString(),
                itemId = shuffled[i].ItemId,
                currentStock = maxQty,
                maxStock = maxQty,
            });
        }

        return result;
    }

    private int GetSlotCount(int level)
    {
        if (level >= 7) return 8;
        if (level >= 4) return 6;
        return 4;
    }

    private IReadOnlyList<ElfShopItemEntry> GetItemPool(ElfType elfType)
    {
        if (_elfConfig == null)
            return new List<ElfShopItemEntry>();

        return _elfConfig.GetItems(elfType);
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
            RecipeBookState.UnlockRecipeAnywhere(stock.IngredientID);
        }

        PushToSession();
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

    private static void UpdateWeightsForNextDay(ElfShopSaveState state, ElfType previousElf, ElfType appearedElf, bool yellowUnlocked)
    {
        bool consecutive = appearedElf == previousElf;

        if (!consecutive)
        {
            ResetWeights(state, yellowUnlocked);
            return;
        }

        int activeCount = yellowUnlocked ? 3 : 2;
        float bonus = WEIGHT_PENALTY / (activeCount - 1);

        switch (appearedElf)
        {
            case ElfType.Red:
                state.weightRed = Mathf.Max(0f, state.weightRed - WEIGHT_PENALTY);
                state.weightBlue += bonus;
                if (yellowUnlocked) state.weightYellow += bonus;
                break;
            case ElfType.Blue:
                state.weightBlue = Mathf.Max(0f, state.weightBlue - WEIGHT_PENALTY);
                state.weightRed += bonus;
                if (yellowUnlocked) state.weightYellow += bonus;
                break;
            case ElfType.Yellow:
                state.weightYellow = Mathf.Max(0f, state.weightYellow - WEIGHT_PENALTY);
                state.weightRed += bonus;
                state.weightBlue += bonus;
                break;
        }
    }

    private static void ResetWeights(ElfShopSaveState state, bool yellowUnlocked)
    {
        if (yellowUnlocked)
        {
            state.weightRed = DEFAULT_WEIGHT_THREE;
            state.weightBlue = DEFAULT_WEIGHT_THREE;
            state.weightYellow = DEFAULT_WEIGHT_THREE;
        }
        else
        {
            state.weightRed = DEFAULT_WEIGHT_TWO;
            state.weightBlue = DEFAULT_WEIGHT_TWO;
            state.weightYellow = 0f;
        }
    }

    private static ElfType WeightedRandom(ElfShopSaveState state, bool yellowUnlocked, System.Random rng)
    {
        float total = state.weightRed + state.weightBlue + (yellowUnlocked ? state.weightYellow : 0f);

        if (total <= 0f)
            return ElfType.Red;

        float roll = (float)(rng.NextDouble() * total);
        if (roll < state.weightRed)
            return ElfType.Red;
        roll -= state.weightRed;
        if (roll < state.weightBlue)
            return ElfType.Blue;
        return yellowUnlocked ? ElfType.Yellow : ElfType.Blue;
    }

    private static bool IsYellowUnlocked(int restaurantLevel)
    {
        return restaurantLevel >= 3;
    }

    // ───── 유틸 ─────

    private void PushToSession()
    {
        if (GameSession.IsActive)
            CaptureState(GameSession.Current);
    }

    private static ElfType ParseElf(string value)
    {
        return Enum.TryParse(value, out ElfType elf) ? elf : ElfType.Red;
    }

    private static ElfShopSaveState CloneState(ElfShopSaveState source)
    {
        return JsonUtility.FromJson<ElfShopSaveState>(JsonUtility.ToJson(source));
    }

    /// <summary>플랫폼·실행마다 동일한 시드 (string.GetHashCode는 실행마다 달라질 수 있어 사용하지 않는다).</summary>
    public static int DeterministicSeed(string runId, int day, string salt)
    {
        unchecked
        {
            uint hash = 2166136261;
            string key = (runId ?? "") + "|" + day + "|" + salt;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= key[i];
                hash *= 16777619;
            }
            return (int)hash;
        }
    }
}
