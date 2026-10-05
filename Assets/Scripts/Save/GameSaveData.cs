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

    // ───── 하루 결산 (DayEnded 체크포인트와 함께 확정) ─────
    // 이전 버전 세이브에는 없는 필드라 기본값(결산 없음)으로 읽힌다.
    public DaySettlementRecord lastSettlement = new DaySettlementRecord();
    public bool settlementPendingReview;    // 결산 확정 후 플레이어가 결산 화면을 닫기 전에 종료했는지

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

/// <summary>
/// 하루 영업 결산 스냅샷. 결산 확정 시점의 값을 그대로 담으며, 다시 계산하지 않고 표시만 한다.
/// day가 0이면 아직 결산한 날이 없다는 뜻이다.
/// </summary>
[Serializable]
public class DaySettlementRecord
{
    public int day;

    // 장부 (모두 실제 거래 이벤트에서 집계)
    public List<DaySaleEntry> sales = new List<DaySaleEntry>();
    public int revenue;             // 손님 결제 합계 (팁 포함)
    public int otherIncome;         // 영업 중 손님 결제 외 수입
    public int expense;             // 영업 중 지출
    public int netProfit;           // revenue + otherIncome - expense
    public int goldAtOpen;
    public int goldAtClose;

    // 손님
    public int visitedCount;        // 입장(생성) 기준
    public int servedCount;         // 계산 완료 후 퇴장
    public int notServedCount;      // 계산 없이 퇴장 (대기 포기·주문 불가 등)
    public int forcedLeaveCount;    // 마감 정지 방지로 내보낸 손님 (notServedCount에 포함)

    // 평점
    public float todayRating;       // 오늘 개인 평점 평균 (평가 0건이면 0)
    public int ratingCount;
    public float previousRating;    // 결산 전 식당 평점
    public float totalRating;       // 결산 후 식당 평점
    public bool seedRatingIncluded; // 시작 평판(시드)이 아직 평균에 포함되어 있는지

    // 운영 측정값 (평균 = 합계 / 표본 수)
    public float seatWaitSum;
    public int seatWaitSamples;
    public float orderTakeSum;
    public int orderTakeSamples;
    public float foodWaitSum;
    public int foodWaitSamples;
    public float firstRevenueSeconds = -1f;   // 영업 시작부터 첫 결제까지 (측정 못 했으면 -1)
}

[Serializable]
public class DaySaleEntry
{
    public int recipeId;
    public string recipeName;
    public int count;
    public int unitPrice;           // 메뉴 가격
    public int revenue;             // 실제 받은 금액 합계 (팁 포함)
}

/// <summary>타이틀 화면 요약 표시용 정보.</summary>
public struct SaveSummary
{
    public int Day;
    public DateTime SavedAtLocal;
    public double PlayTimeSeconds;
    public int Gold;
}
