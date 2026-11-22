using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class DayLoopSaveTests
{
    private string directory;
    private Action<string> loader;
    [SetUp] public void Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "HaedalDayLoopTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        SaveService.DirectoryOverride = directory;
        loader = GameFlow.SceneLoader; GameFlow.SceneLoader = _ => { };
        GameSession.End(); GameFlow.SetPhase(GamePhase.FreeTime);
    }
    [TearDown] public void Teardown()
    {
        GameSession.End(); GameFlow.SceneLoader = loader;
        SaveService.DirectoryOverride = null;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    private static GameSaveData Begin(int day = 1)
    {
        var data = NewGameConfig.Load().CreateSaveData();
        data.day = day; data.introCompleted = true;
        GameSession.Begin(data); return data;
    }
    private static DaySettlementRecord Record(int day = 1) => new DaySettlementRecord { day = day, revenue = 504, servedCount = 3, visitedCount = 3 };
    [Test] public void Settlement_EntersSameDayFreeTime_ExactlyOnce_WithoutWriting()
    {
        Begin();
        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(), out string error), error);
        Assert.IsTrue(DayLoopSave.TryCommitDay(Record(), out error), error);
        Assert.AreEqual(1, GameSession.Current.day);
        Assert.AreEqual(504, GameSession.Current.lifetimeRevenue);
        Assert.IsTrue(GameSession.Current.progression.freeTime);
        Assert.IsTrue(GameSession.OperationCompletedToday);
        Assert.IsFalse(SaveService.HasSaveFile());
    }
    [Test] public void Settlement_DoesNotPayRevenueTwice()
    {
        Begin(); int gold = RestaurantProgress.Gold(GameSession.Current);
        DayLoopSave.TryCommitDay(Record(), out _);
        Assert.AreEqual(gold, RestaurantProgress.Gold(GameSession.Current));
    }
    [Test] public void Sleep_WritesNextMorning_AndDoesNotChargeWeeklyWagesOnDayOne()
    {
        Begin(); int gold = RestaurantProgress.Gold(GameSession.Current);
        DayLoopSave.TryCommitDay(Record(), out _);
        GameSession.AddPlayTime(12);
        Assert.IsTrue(RestaurantProgress.TrySleep(out string error), error);
        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out var loaded, out error), error);
        Assert.AreEqual(2, loaded.day);
        Assert.AreEqual(gold, RestaurantProgress.Gold(loaded));
        Assert.IsFalse(loaded.progression.freeTime);
        Assert.IsFalse(loaded.settlementPendingReview);
        Assert.AreEqual(12, loaded.playTimeSeconds);
    }
    // 연구는 대기 일수 없이 재료를 예약하면 바로 실습한다 (5176ab1부터, GameSession.Begin이 대기 일수를 0으로 맞춘다).
    // 예전 테스트는 잠들 때마다 대기 일수가 1씩 줄어드는지 봤는데, 지금은 실패한 취침이 연구 예약을 건드리지 않는지만 본다.
    [Test] public void FailedSleep_DoesNotDeductWagesOrChangeResearch_AndCanRetry()
    {
        Begin(7);
        GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = 5, remainingDays = 0 });
        DayLoopSave.TryCommitDay(Record(7), out _);
        int gold = RestaurantProgress.Gold(GameSession.Current);
        string blocker = Path.Combine(directory, "blocker"); File.WriteAllText(blocker, "x");
        SaveService.DirectoryOverride = blocker;
        Assert.IsFalse(RestaurantProgress.TrySleep(out string error));
        Assert.IsNotEmpty(error);
        Assert.AreEqual(7, GameSession.Current.day);
        Assert.IsTrue(GameSession.Current.progression.freeTime, "저장에 실패하면 그날 밤(자유시간)에 그대로 머문다");
        Assert.AreEqual(gold, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(1, GameSession.Current.progression.research.Count);
        Assert.AreEqual(5, GameSession.Current.progression.research[0].recipeId);
        SaveService.DirectoryOverride = directory;
        Assert.IsTrue(RestaurantProgress.TrySleep(out error), error);
        Assert.AreEqual(8, GameSession.Current.day);
        Assert.AreEqual(gold - 700, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(1, GameSession.Current.progression.research.Count, "잠들어도 연구 예약은 남는다");
        Assert.AreEqual(0, GameSession.Current.progression.research[0].remainingDays, "연구는 대기 없이 바로 실습할 수 있다");
        Assert.IsFalse(RestaurantProgress.TrySleep(out _), "이미 저장한 아침에는 다시 잘 수 없다");
    }
    // 기다리다 지쳐 나간 손님도 평가(1점)를 남긴다. 결산 안내는 계산한 손님을 뺀 평가 수로 몇 명이 돌아갔는지 알려 준다.
    [Test] public void RatingNote_CountsCustomersWhoLeftAngry()
    {
        var record = new DaySettlementRecord { day = 2, servedCount = 3, visitedCount = 5, ratingCount = 5, todayRating = 3.2f };
        StringAssert.Contains("2명", DayLoopSave.RatingNote(record));
        record.ratingCount = 3;
        StringAssert.DoesNotContain("돌아간 손님", DayLoopSave.RatingNote(record));
    }
    [Test] public void IntroAndAcknowledgement_DoNotReplacePreviousSave()
    {
        var old = Begin(3); GameSession.StampCheckpoint(old, GameSaveData.ReasonDayEnded);
        Assert.IsTrue(SaveService.TryWrite(old, out _));
        string before = File.ReadAllText(SaveService.FilePath);
        GameFlow.BeginNewGame();
        Assert.IsTrue(GameFlow.TrySaveIntroCheckpoint(out _));
        DayLoopSave.TryCommitDay(Record(), out _); DayLoopSave.AcknowledgeSettlement();
        Assert.AreEqual(before, File.ReadAllText(SaveService.FilePath));
    }
}
