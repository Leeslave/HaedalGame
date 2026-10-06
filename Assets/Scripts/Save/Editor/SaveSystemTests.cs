using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 통합 세이브 EditMode 테스트. (Window > General > Test Runner > EditMode)
/// 실제 세이브 파일을 건드리지 않도록 임시 폴더를 사용한다.
/// </summary>
public class SaveSystemTests
{
    private string _dir;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "HaedalSaveTests_" + System.Guid.NewGuid().ToString("N"));
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

    private static GameSaveData CreateCheckpointData()
    {
        GameSaveData data = NewGameConfig.Load().CreateSaveData();
        data.introCompleted = true;
        GameSession.StampCheckpoint(data, GameSaveData.ReasonIntroCompleted);
        return data;
    }

    // ───── 새 게임 초기값 ─────

    [Test]
    public void NewGame_CreatesPlayableDayOne()
    {
        GameSaveData data = NewGameConfig.Load().CreateSaveData();

        Assert.AreEqual(1, data.day);
        Assert.AreEqual(0, data.lastCompletedDay);
        Assert.IsFalse(string.IsNullOrEmpty(data.runId));
        Assert.IsTrue(data.wallets.Exists(w => w.currencyId == GameSession.GoldCurrencyId && w.amount > 0));
        Assert.IsTrue(data.employees.Exists(e => e.role == "Kitchen" && e.slotIndex >= 0), "주방 인력");
        Assert.IsTrue(data.employees.Exists(e => e.role == "Serving" && e.slotIndex >= 0), "홀 인력");
        Assert.IsTrue(data.placedTables.Count > 0, "좌석");
        Assert.IsFalse(data.menuSlots.Exists(id => id >= 0), "첫 조리도구를 고른 뒤 메뉴 등록");
        Assert.AreEqual(1, data.dailyRatingHistory.Count, "평점 시드 1개");
        Assert.AreEqual(0, data.dailyRatingHistory[0].day);

        HashSet<string> ids = new HashSet<string>();
        foreach (EmployeeEntry employee in data.employees)
            Assert.IsTrue(ids.Add(employee.instanceId), "직원 ID 중복: " + employee.instanceId);
    }

    [Test]
    public void NewGame_KeepsIngredientsUntilAnOrderIsAccepted()
    {
        var data = NewGameConfig.Load().CreateSaveData();
        Assert.AreEqual(3, data.menuSlots.Count);
        Assert.IsTrue(data.menuSlots.TrueForAll(id => id == -1));
        for (int id = 1; id <= 10; id++)
            Assert.AreEqual(30, data.ingredients.Find(s => s.ingredientId == id).amount);
    }

    [Test]
    public void NewGame_EachRunHasNewRunId()
    {
        Assert.AreNotEqual(NewGameConfig.Load().CreateSaveData().runId, NewGameConfig.Load().CreateSaveData().runId);
    }

    // ───── 파일 입출력 ─────

    [Test]
    public void WriteThenLoad_RoundTripsExactly()
    {
        GameSaveData data = CreateCheckpointData();
        data.storyFlags.Add("met_elf");
        data.completedTutorialIds.Add("HouseTutorial");

        Assert.IsTrue(SaveService.TryWrite(data, out string error), error);
        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out error), error);

        Assert.AreEqual(JsonUtility.ToJson(data), JsonUtility.ToJson(loaded));
        Assert.IsFalse(File.Exists(SaveService.FilePath + ".tmp"), "임시 파일이 남으면 안 됨");
        Assert.IsFalse(File.Exists(SaveService.FilePath + ".bak"), "백업 파일이 남으면 안 됨");
    }

    [Test]
    public void Overwrite_KeepsOnlyLatest()
    {
        GameSaveData first = CreateCheckpointData();
        Assert.IsTrue(SaveService.TryWrite(first, out _));

        GameSaveData second = first.Clone();
        second.day = 2;
        Assert.IsTrue(SaveService.TryWrite(second, out string error), error);

        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out _));
        Assert.AreEqual(2, loaded.day);
    }

    [Test]
    public void FailedWrite_KeepsPreviousFile()
    {
        GameSaveData first = CreateCheckpointData();
        Assert.IsTrue(SaveService.TryWrite(first, out _));
        string before = File.ReadAllText(SaveService.FilePath);

        GameSaveData second = first.Clone();
        second.day = 2;

        // 기존 파일을 다른 프로세스가 잡고 있는 상황 → 교체 실패
        using (new FileStream(SaveService.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.IsFalse(SaveService.TryWrite(second, out string error));
            Assert.IsNotNull(error);
        }

        Assert.AreEqual(before, File.ReadAllText(SaveService.FilePath));
        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out _));
        Assert.AreEqual(1, loaded.day);
    }

    [Test]
    public void NoFile_IsNotFound()
    {
        Assert.IsFalse(SaveService.HasSaveFile());
        Assert.AreEqual(SaveLoadStatus.NotFound, SaveService.TryLoad(out _, out _));
    }

    [Test]
    public void BackupOnly_IsRecovered()
    {
        GameSaveData data = CreateCheckpointData();
        Assert.IsTrue(SaveService.TryWrite(data, out _));
        File.Move(SaveService.FilePath, SaveService.FilePath + ".bak"); // 교체 도중 종료된 상황

        Assert.AreEqual(SaveLoadStatus.Ok, SaveService.TryLoad(out GameSaveData loaded, out _));
        Assert.AreEqual(data.runId, loaded.runId);
    }

    [TestCase("")]
    [TestCase("{ not json")]
    [TestCase("{\"schemaVersion\":1,\"day\":1,\"checkpoint\":\"DayStart\",\"introCompleted\":true}")] // runId 없음
    public void CorruptedJson_IsRejected(string json)
    {
        Assert.AreEqual(SaveLoadStatus.Corrupted, SaveService.TryParse(json, out GameSaveData data, out string error));
        Assert.IsNull(data);
        Assert.IsNotNull(error);
    }

    [Test]
    public void FutureVersion_IsRejected()
    {
        GameSaveData data = CreateCheckpointData();
        data.schemaVersion = GameSaveData.CurrentSchemaVersion + 1;
        Assert.AreEqual(SaveLoadStatus.UnsupportedVersion, SaveService.TryParse(JsonUtility.ToJson(data), out _, out _));
    }

    [Test]
    public void BeforeIntroCompleted_IsRejected()
    {
        GameSaveData data = CreateCheckpointData();
        data.introCompleted = false;
        Assert.AreEqual(SaveLoadStatus.Corrupted, SaveService.TryParse(JsonUtility.ToJson(data), out _, out _));
    }

    // ───── 세션 ─────

    private class FakeParticipant : ISaveParticipant
    {
        public int Value;
        public int RestoreCount;
        public void CaptureState(GameSaveData data) { data.lifetimeRevenue = Value; }
        public void RestoreState(GameSaveData data) { Value = (int)data.lifetimeRevenue; RestoreCount++; }
    }

    [Test]
    public void Session_RegisterRestores_UnregisterCaptures()
    {
        GameSaveData data = CreateCheckpointData();
        data.lifetimeRevenue = 50;
        GameSession.Begin(data);

        FakeParticipant participant = new FakeParticipant();
        GameSession.Register(participant);
        Assert.AreEqual(50, participant.Value);

        participant.Value = 80;
        GameSession.Unregister(participant); // 씬 전환으로 파괴
        Assert.AreEqual(80, GameSession.Current.lifetimeRevenue);
    }

    [Test]
    public void Session_SnapshotIsIndependentCopy()
    {
        GameSession.Begin(CreateCheckpointData());
        FakeParticipant participant = new FakeParticipant { Value = 10 };
        GameSession.Register(participant);
        participant.Value = 10;

        GameSaveData snapshot = GameSession.CaptureSnapshot();
        snapshot.lifetimeRevenue = 999;

        Assert.AreEqual(10, GameSession.Current.lifetimeRevenue);
        GameSession.Unregister(participant);
    }

    [Test]
    public void Session_EndStopsCapture()
    {
        GameSession.Begin(CreateCheckpointData());
        FakeParticipant participant = new FakeParticipant();
        GameSession.Register(participant);
        GameSession.End();

        participant.Value = 123;
        Assert.DoesNotThrow(() => GameSession.Unregister(participant));
        Assert.IsFalse(GameSession.IsActive);
    }

    [Test]
    public void Session_TutorialAndDialogueProgressLivesInSession()
    {
        GameSession.Begin(CreateCheckpointData());
        Assert.IsTrue(GameSession.TryMarkTutorialCompleted("seq"));
        Assert.IsTrue(GameSession.TryGetTutorialCompleted("seq", out bool done) && done);

        // 체크포인트로 되돌리면 이후 본 기록도 함께 되돌아간다.
        GameSession.Begin(CreateCheckpointData());
        Assert.IsTrue(GameSession.TryGetTutorialCompleted("seq", out done));
        Assert.IsFalse(done);
    }

    // ───── 엘프 상점 추첨 ─────

    [Test]
    public void ElfShopRoll_IsDeterministicPerRunAndDay()
    {
        GameObject go = new GameObject("ElfShopTest");
        try
        {
            ElfShopManager shop = go.AddComponent<ElfShopManager>();
            ElfShopSaveState a = shop.CreateStateForDay(null, 2, 1, "run-A");
            ElfShopSaveState b = shop.CreateStateForDay(null, 2, 1, "run-A");

            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b), "같은 run/일차는 같은 결과");
            Assert.AreEqual(2, a.targetDay);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
