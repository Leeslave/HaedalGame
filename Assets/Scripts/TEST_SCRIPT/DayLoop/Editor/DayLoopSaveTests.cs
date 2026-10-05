using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 결산 확정 체크포인트 EditMode 테스트. (Window > General > Test Runner > EditMode)
/// 실제 세이브 파일을 건드리지 않도록 임시 폴더를 사용한다.
/// </summary>
public class DayLoopSaveTests
{
    private string _dir;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "HaedalDayLoopTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        SaveService.DirectoryOverride = _dir;
        GameSession.End();
    }

    [TearDown]
    public void TearDown()
    {
        GameSession.End();
        SaveService.DirectoryOverride = null;
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    private static void BeginDayOne()
    {
        GameSaveData data = NewGameConfig.Load().CreateSaveData();
        data.introCompleted = true;
        GameSession.StampCheckpoint(data, GameSaveData.ReasonIntroCompleted);
        GameSession.Begin(data);
    }

    private static DaySettlementRecord Record(int day, int revenue)
    {
        DaySettlementRecord record = new DaySettlementRecord { day = day, revenue = revenue, netProfit = revenue, servedCount = 3, visitedCount = 3 };
        record.sales.Add(new DaySaleEntry { recipeId = 1, recipeName = "초밥", count = 3, unitPrice = revenue / 3, revenue = revenue });
        return record;
    }

    [Test]
    public void CommitDay_AdvancesDayExactlyOnce()
    {
        BeginDayOne();

        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(1, 504), out string error), error);
        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(1, 504), out error), error);   // 연타·재시도

        Assert.AreEqual(2, GameSession.Current.day);
        Assert.AreEqual(1, GameSession.Current.lastCompletedDay);
        Assert.AreEqual(504, GameSession.Current.lifetimeRevenue, "같은 날 매출이 두 번 누적되면 안 됨");

        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out error), error);
        Assert.AreEqual(2, loaded.day);
        Assert.AreEqual(GameSaveData.ReasonDayEnded, loaded.checkpointReason);
        Assert.IsTrue(loaded.settlementPendingReview);
        Assert.AreEqual(1, loaded.lastSettlement.day);
        Assert.AreEqual(504, loaded.lastSettlement.revenue);
        Assert.AreEqual("초밥", loaded.lastSettlement.sales[0].recipeName);
    }

    [Test]
    public void CommitDay_KeepsGold()
    {
        BeginDayOne();
        int gold = GameSession.Current.wallets.Find(w => w.currencyId == GameSession.GoldCurrencyId).amount;

        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(1, 100), out string error), error);

        // 결산은 골드를 다시 지급하지 않는다. (지급은 손님 결제 때 이미 끝남)
        Assert.AreEqual(gold, GameSession.Current.wallets.Find(w => w.currencyId == GameSession.GoldCurrencyId).amount);
    }

    [Test]
    public void CommitDay_WriteFailure_DoesNotAdvance()
    {
        BeginDayOne();

        // 저장 폴더 자리에 파일을 두어 쓰기를 실패시킨다.
        string blocker = Path.Combine(_dir, "blocker");
        File.WriteAllText(blocker, "x");
        SaveService.DirectoryOverride = blocker;

        Assert.IsFalse(DayLoopSave.TryCommitDay(Record(1, 100), out string error));
        Assert.IsFalse(string.IsNullOrEmpty(error));
        Assert.AreEqual(1, GameSession.Current.day, "저장 실패 시 날짜가 넘어가면 안 됨");
        Assert.AreEqual(0, GameSession.Current.lastCompletedDay);

        // 원인이 해결되면 같은 결산으로 다시 시도할 수 있다.
        SaveService.DirectoryOverride = _dir;
        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(1, 100), out error), error);
        Assert.AreEqual(2, GameSession.Current.day);
    }

    [Test]
    public void Acknowledge_ClearsPendingReviewInFile()
    {
        BeginDayOne();
        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(1, 100), out string error), error);

        DayLoopSave.AcknowledgeSettlement();

        Assert.IsFalse(GameSession.Current.settlementPendingReview);
        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out error), error);
        Assert.IsFalse(loaded.settlementPendingReview);
        Assert.AreEqual(2, loaded.day, "확인 표시만 바뀌고 날짜는 그대로");
    }

    [Test]
    public void SaveWithoutSettlementFields_StillLoads()
    {
        GameSaveData data = NewGameConfig.Load().CreateSaveData();
        data.introCompleted = true;
        GameSession.StampCheckpoint(data, GameSaveData.ReasonIntroCompleted);

        // 결산 필드가 생기기 전 세이브를 흉내 낸다.
        string json = JsonUtility.ToJson(data);
        int start = json.IndexOf(",\"lastSettlement\":");
        int end = json.IndexOf(",\"settlementPendingReview\":false");
        Assume.That(start > 0 && end > start);
        json = json.Remove(start, end - start).Replace(",\"settlementPendingReview\":false", "");

        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryParse(json, out GameSaveData loaded, out string error), error);
        Assert.IsFalse(loaded.settlementPendingReview);
        Assert.IsTrue(loaded.lastSettlement == null || loaded.lastSettlement.day == 0);
    }
}
