using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 진행 세이브 한 개(단일 슬롯)의 전체 데이터.
/// 로드 가능한 지점은 DayStart 하나뿐이며, day는 "이어서 시작할 일차"다. (첫 저장 1, 첫날 밤 저장 2)
/// JsonUtility로 직렬화하므로 Dictionary 대신 리스트 DTO만 사용하고, Unity 오브젝트 참조는 넣지 않는다.
/// </summary>
[Serializable]
public class GameSaveData
{
    public const int CurrentSchemaVersion = 1;
    public const string CheckpointDayStart = "DayStart";
    public const string ReasonIntroCompleted = "IntroCompleted";
    public const string ReasonDayEnded = "DayEnded";

    // ───── 메타데이터 ─────
    public int schemaVersion = CurrentSchemaVersion;
    public string runId = "";
    public int saveRevision;
    public string savedAtUtc = "";          // ISO 8601 (UTC)
    public double playTimeSeconds;          // 마지막 체크포인트까지의 누적 플레이 시간

    // ───── 진행 ─────
    public int day = 1;
    public string checkpoint = CheckpointDayStart;
    public string checkpointReason = "";
    public bool introCompleted;
    public string nextGuideStep = "";
    public List<string> completedTutorialIds = new List<string>();
    public List<string> completedDialogueIds = new List<string>();
    public List<string> storyFlags = new List<string>();
    public int lastCompletedDay;

    // ───── 재화·재료·레시피·메뉴 ─────
    public List<WalletEntry> wallets = new List<WalletEntry>();
    public List<IngredientStackEntry> ingredients = new List<IngredientStackEntry>();
    public List<int> unlockedIngredientIds = new List<int>();   // 해금 순서 유지
    public List<int> unlockedRecipeIds = new List<int>();       // 해금 순서 유지
    public List<int> menuSlots = new List<int>();               // 슬롯 순서대로 레시피 ID, 빈 슬롯은 -1

    // ───── 직원 ─────
    public List<EmployeeEntry> employees = new List<EmployeeEntry>();
    public int nextEmployeeSerial = 1;

    // ───── 식당·가구·도구 ─────
    public int restaurantLevel = 1;
    public List<PlacedTableEntry> placedTables = new List<PlacedTableEntry>();
    public List<CookwareEntry> cookware = new List<CookwareEntry>();
    public int blacksmithLevel = 1;
    public int blacksmithExp;

    // ───── 평점·경제 이력·패널티 ─────
    public List<DailyRatingEntry> dailyRatingHistory = new List<DailyRatingEntry>();
    public int lastRecordedRatingDay;
    public long lifetimeRevenue;
    public int unpaidDismissalCount;
    public bool scoutPenaltyPending;

    // ───── 상점 ─────
    public ElfShopSaveState elfShop = new ElfShopSaveState();
    public List<ShopStockEntry> regularShopStocks = new List<ShopStockEntry>();

    public GameSaveData Clone()
    {
        return JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(this));
    }

    public bool HasTutorialCompleted(string id) => !string.IsNullOrEmpty(id) && completedTutorialIds.Contains(id);
    public bool HasDialogueCompleted(string id) => !string.IsNullOrEmpty(id) && completedDialogueIds.Contains(id);
    public bool HasStoryFlag(string flag) => !string.IsNullOrEmpty(flag) && storyFlags.Contains(flag);

    public static void AddUnique(List<string> list, string value)
    {
        if (list != null && !string.IsNullOrEmpty(value) && !list.Contains(value))
            list.Add(value);
    }
}

[Serializable]
public class WalletEntry
{
    public string currencyId;
    public int amount;
}

[Serializable]
public class IngredientStackEntry
{
    public int ingredientId;
    public int amount;
    public string source = "";
    public long acquiredTime;
}

[Serializable]
public class EmployeeEntry
{
    public string instanceId;       // 같은 이름·등급이어도 구분되는 개체 ID
    public string name;
    public string grade;
    public float serving;
    public float cooking;
    public float handy;
    public float hp;
    public int wage;
    public string role = "None";    // PartTimerRole 이름
    public int slotIndex = -1;      // 역할 안에서의 배치 순서, 미배치 -1
}

[Serializable]
public class PlacedTableEntry
{
    public string tableType;        // TableType 이름
    public int anchorX;
    public int anchorY;
}

[Serializable]
public class CookwareEntry
{
    public string toolId;           // CookwareUpgradeSO 에셋 이름
    public int level = 1;
    public int useCount;
}

[Serializable]
public class DailyRatingEntry
{
    public int day;                 // 0 = 신규 게임 시드값(실제 영업일 아님)
    public float value;
}

[Serializable]
public class ElfShopSaveState
{
    public int targetDay;           // 이 재고가 유효한 일차 (0이면 아직 추첨 전)
    public string currentElf = "Red";
    public float weightRed = 50f;
    public float weightBlue = 50f;
    public float weightYellow;
    public List<ElfShopStockEntry> stocks = new List<ElfShopStockEntry>();
}

[Serializable]
public class ElfShopStockEntry
{
    public string itemType;         // ElfShopItemType 이름
    public int itemId;
    public int currentStock;
    public int maxStock;
}

[Serializable]
public class ShopStockEntry
{
    public int itemId;
    public bool isRecipe;
    public int currentStock;
    public int maxStock;
}

/// <summary>타이틀 화면 요약 표시용 정보.</summary>
public struct SaveSummary
{
    public int Day;
    public DateTime SavedAtLocal;
    public double PlayTimeSeconds;
    public int Gold;
}
