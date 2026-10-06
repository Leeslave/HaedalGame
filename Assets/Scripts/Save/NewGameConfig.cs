using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새 게임 1일차 초기 상태.
/// [임시값] 시작 골드·재료·직원·메뉴·테이블은 확정 기획이 없어 첫날 영업이 가능한 최소 구성으로 넣은 값이다.
/// 기획 확정 시 Resources/NewGameConfig 에셋 값만 바꾸면 된다. 에셋이 없으면 아래 필드 기본값을 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "NewGameConfig", menuName = "Game Data/Save/New Game Config")]
public class NewGameConfig : ScriptableObject
{
    private const string ResourcePath = "NewGameConfig";

    [Serializable]
    public class StartIngredient
    {
        public int ingredientId;
        public int amount;
    }

    [Serializable]
    public class StartEmployee
    {
        public string name = "해달";
        public string grade = "F";
        public float serving = 3f;
        public float cooking = 3f;
        public float handy = 1f;
        public float hp = 10f;
        public int wage = 350;
        public PartTimerRole role = PartTimerRole.None;
    }

    [Header("DB (비우면 Resources의 RecipeDatabaseSO 사용)")]
    [SerializeField] private RecipeDatabaseSO _database;

    [Header("재화")]
    [SerializeField] private int _startGold = 1000;

    [Header("재료 (보유 수량)")]
    [SerializeField] private List<StartIngredient> _startIngredients = new List<StartIngredient>
    {
        new StartIngredient { ingredientId = 1, amount = 10 },  // 새우
        new StartIngredient { ingredientId = 2, amount = 10 },  // 가리비
        new StartIngredient { ingredientId = 3, amount = 10 },  // 오징어
        new StartIngredient { ingredientId = 4, amount = 10 },  // 조개
    };

    [Header("레시피·메뉴")]
    [SerializeField] private List<int> _startUnlockedRecipeIds = new List<int> { 1, 2, 3, 4 };   // 기본 메뉴 4종
    [Tooltip("메뉴판 슬롯에 미리 올려 둘 레시피. 등록 규칙(MenuRecipeService)대로 재료 1세트씩 차감된다.")]
    [SerializeField] private List<int> _startMenuRecipeIds = new List<int> { 1, 2, 3, 4 };
    [SerializeField] private int _menuSlotCount = 4;

    [Header("직원")]
    [SerializeField] private List<StartEmployee> _startEmployees = new List<StartEmployee>
    {
        new StartEmployee { name = "주방 해달", role = PartTimerRole.Kitchen },
        new StartEmployee { name = "홀 해달", role = PartTimerRole.Serving },
    };

    [Header("식당")]
    [SerializeField] private int _restaurantLevel = 1;
    [SerializeField] private List<PlacedTableEntry> _startTables = new List<PlacedTableEntry>
    {
        new PlacedTableEntry { tableType = "TwoSeat", anchorX = -11, anchorY = 1 },
        new PlacedTableEntry { tableType = "TwoSeat", anchorX = -11, anchorY = -2 },
        new PlacedTableEntry { tableType = "TwoSeat", anchorX = 7, anchorY = -2 },
    };

    [Header("평점")]
    [Tooltip("실제 영업 평점이 쌓이기 전 7일 평균을 대신하는 시드값 (평점 문서 기준 4.0)")]
    [SerializeField] private float _seedRating = 4.0f;

    [Header("진행")]
    [SerializeField] private string _firstGuideStep = GuideSteps.IslandStory;

    public int MenuSlotCount => RestaurantRules.MenuLimit(_restaurantLevel);
    private RecipeDatabaseSO Database => _database != null ? _database : GameDatabase.Recipes;

    public static NewGameConfig Load()
    {
        NewGameConfig config = Resources.Load<NewGameConfig>(ResourcePath);
        return config != null ? config : CreateInstance<NewGameConfig>();
    }

    /// <summary>1일차 시작 상태를 만든다. 인트로 완료 여부·저장 메타데이터는 호출자가 채운다.</summary>
    public GameSaveData CreateSaveData()
    {
        GameSaveData data = new GameSaveData();
        data.runId = Guid.NewGuid().ToString("N");
        data.day = 1;
        data.lastCompletedDay = 0;
        data.nextGuideStep = _firstGuideStep;
        data.restaurantLevel = Mathf.Max(1, _restaurantLevel);

        data.wallets.Add(new WalletEntry { currencyId = GameSession.GoldCurrencyId, amount = _startGold });

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (StartIngredient start in _startIngredients)
        {
            if (start == null || start.amount <= 0) continue;
            AddIngredient(data, start.ingredientId, start.amount, now);
            AddUniqueInt(data.unlockedIngredientIds, start.ingredientId);
        }

        RecipeDatabaseSO database = Database;
        if (database != null)
        {
            foreach (IngredientData ingredient in database.Ingredients)
            {
                if (ingredient != null && ingredient.DefaultUnlock)
                    AddUniqueInt(data.unlockedIngredientIds, ingredient.IngredientId);
            }

            foreach (RecipeData recipe in database.Recipes)
            {
                if (recipe != null && recipe.DefaultUnlock)
                    AddUniqueInt(data.unlockedRecipeIds, recipe.RecipeId);
            }
        }

        foreach (int recipeId in _startUnlockedRecipeIds)
            AddUniqueInt(data.unlockedRecipeIds, recipeId);

        for (int i = 0; i < MenuSlotCount; i++)
            data.menuSlots.Add(-1);

        Dictionary<PartTimerRole, int> nextSlotByRole = new Dictionary<PartTimerRole, int>();
        foreach (StartEmployee start in _startEmployees)
        {
            if (start == null) continue;

            int slotIndex = -1;
            if (start.role != PartTimerRole.None)
            {
                nextSlotByRole.TryGetValue(start.role, out slotIndex);
                nextSlotByRole[start.role] = slotIndex + 1;
            }

            data.employees.Add(new EmployeeEntry
            {
                instanceId = "emp-" + data.nextEmployeeSerial++,
                name = start.name,
                grade = start.grade,
                serving = start.serving,
                cooking = start.cooking,
                handy = start.handy,
                hp = start.hp,
                wage = start.wage,
                role = start.role.ToString(),
                slotIndex = slotIndex,
            });
        }

        foreach (PlacedTableEntry table in _startTables)
        {
            if (table == null) continue;
            data.placedTables.Add(new PlacedTableEntry { tableType = table.tableType, anchorX = table.anchorX, anchorY = table.anchorY });
        }

        data.dailyRatingHistory.Add(new DailyRatingEntry { day = 0, value = _seedRating });
        data.lastRecordedRatingDay = 0;

        // 첫 조리도구는 관리 화면에서 무료 선택한다. 선택 전에는 영업할 수 없다.
        data.menuSlots.Clear();
        for (int i = 0; i < RestaurantRules.MenuLimit(data.restaurantLevel); i++) data.menuSlots.Add(-1);
        if (data.placedTables.Count > 1) data.placedTables.RemoveRange(1, data.placedTables.Count - 1);
        for (int id = 1; id <= 10; id++) if (!data.ingredients.Exists(x => x.ingredientId == id)) AddIngredient(data, id, 30, now);
        foreach (var ingredient in data.ingredients) ingredient.amount = 30;
        return data;
    }

    private static void AddIngredient(GameSaveData data, int ingredientId, int amount, long acquiredTime)
    {
        IngredientStackEntry stack = data.ingredients.Find(s => s.ingredientId == ingredientId);
        if (stack != null)
        {
            stack.amount += amount;
            return;
        }

        data.ingredients.Add(new IngredientStackEntry
        {
            ingredientId = ingredientId,
            amount = amount,
            source = "NewGame",
            acquiredTime = acquiredTime,
        });
    }

    private static void AddUniqueInt(List<int> list, int value)
    {
        if (!list.Contains(value))
            list.Add(value);
    }
}

/// <summary>체크포인트에서 재개할 안내 단계 ID.</summary>
public static class GuideSteps
{
    public const string IslandStory = "IslandStory";          // 1일차: 인트로 직후 섬 도입 대화
    public const string RestaurantGuide = "RestaurantGuide";  // 식당 영업 준비 안내
    public const string None = "";
}
