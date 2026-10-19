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
    [Test] public void FailedSleep_DoesNotDeductWagesOrAdvanceResearch_AndCanRetry()
    {
        Begin(7);
        GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = 5, remainingDays = 2 });
        DayLoopSave.TryCommitDay(Record(7), out _);
        int gold = RestaurantProgress.Gold(GameSession.Current);
        string blocker = Path.Combine(directory, "blocker"); File.WriteAllText(blocker, "x");
        SaveService.DirectoryOverride = blocker;
        Assert.IsFalse(RestaurantProgress.TrySleep(out string error));
        Assert.IsNotEmpty(error);
        Assert.AreEqual(7, GameSession.Current.day);
        Assert.AreEqual(gold, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(2, GameSession.Current.progression.research[0].remainingDays);
        SaveService.DirectoryOverride = directory;
        Assert.IsTrue(RestaurantProgress.TrySleep(out error), error);
        Assert.AreEqual(8, GameSession.Current.day);
        Assert.AreEqual(gold - 700, RestaurantProgress.Gold(GameSession.Current));
        Assert.AreEqual(1, GameSession.Current.progression.research[0].remainingDays);
        Assert.IsFalse(RestaurantProgress.TrySleep(out _), "이미 저장한 아침에는 다시 잘 수 없다");
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
