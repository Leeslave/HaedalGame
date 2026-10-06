using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이브에 포함되는 상태를 가진 매니저가 구현한다.
/// RestoreState는 저장값을 그대로 적용만 해야 한다. 구매·강화·하루 경과 함수 호출, 비용·보상 처리, 랜덤 추첨, 파일 저장 금지.
/// </summary>
public interface ISaveParticipant
{
    void CaptureState(GameSaveData data);
    void RestoreState(GameSaveData data);
}

/// <summary>
/// 현재 플레이 세션의 기준 데이터.
/// 파일에서 읽은(또는 새 게임으로 만든) GameSaveData를 메모리에 두고, 매니저들은 등록 시 여기서 복원한다.
/// 씬이 바뀌어 매니저가 파괴될 때는 자기 상태를 여기로 되돌려 놓으므로, 하루 동안의 변화는 파일 저장 없이 메모리에만 쌓인다.
/// 파일에는 체크포인트(인트로 완료, 하루 종료)에서만 기록한다.
/// </summary>
public static class GameSession
{
    public const string GoldCurrencyId = "cur_gold";

    private static readonly List<ISaveParticipant> _participants = new List<ISaveParticipant>();

    public static GameSaveData Current { get; private set; }
    public static bool IsActive => Current != null;

    /// <summary>오늘 영업을 이미 마쳤는지. 체크포인트는 항상 영업 전이므로 파일에는 저장하지 않는다.</summary>
    public static bool OperationCompletedToday { get; set; }

    /// <summary>에디터에서 게임 씬을 직접 실행해 임시로 만든 세션인지. 이 세션은 파일에 저장하지 않는다.</summary>
    public static bool IsDevSession { get; private set; }

    /// <summary>현재 세션 시작(또는 마지막 체크포인트 적용) 이후 흐른 실제 플레이 시간.</summary>
    public static double UnsavedPlayTime { get; private set; }

    /// <summary>세션 데이터가 통째로 교체되어 모든 참여자가 복원된 뒤 발행. UI 갱신용.</summary>
    public static event Action OnSessionRestored;

    public static void Register(ISaveParticipant participant)
    {
        if (participant == null || _participants.Contains(participant))
            return;

        _participants.Add(participant);

        if (IsActive)
            participant.RestoreState(Current);
    }

    /// <summary>파괴되는 매니저가 호출한다. 세션이 살아 있으면 현재 상태를 세션에 남긴다.</summary>
    public static void Unregister(ISaveParticipant participant)
    {
        if (participant == null || !_participants.Remove(participant))
            return;

        if (IsActive)
            participant.CaptureState(Current);
    }

    /// <summary>세션 데이터를 교체하고 살아 있는 모든 참여자를 새 데이터로 복원한다.</summary>
    public static void Begin(GameSaveData data, bool isDevSession = false)
    {
        Current = data ?? throw new ArgumentNullException(nameof(data));
        if (Current.progression == null) Current.progression = new ProgressionState();
        IsDevSession = isDevSession;
        OperationCompletedToday = Current.progression.freeTime;
        UnsavedPlayTime = 0;
        RestoreAll();
    }

    /// <summary>체크포인트 저장 성공 후, 저장한 후보 데이터를 현재 세션으로 적용한다.</summary>
    public static void ApplyCheckpoint(GameSaveData savedData)
    {
        Begin(savedData, IsDevSession);
    }

    public static void End()
    {
        Current = null;
        IsDevSession = false;
        OperationCompletedToday = false;
        UnsavedPlayTime = 0;
    }

    /// <summary>살아 있는 참여자의 상태를 세션에 모으고, 그 사본을 돌려준다. 세션 자체는 그대로다.</summary>
    public static GameSaveData CaptureSnapshot()
    {
        if (!IsActive)
            return null;

        // 캡처 도중 참여자 목록이 바뀌어도 안전하도록 복사본으로 순회한다.
        foreach (ISaveParticipant participant in _participants.ToArray())
            participant.CaptureState(Current);

        GameSaveData snapshot = Current.Clone();
        snapshot.playTimeSeconds = Current.playTimeSeconds + UnsavedPlayTime;
        return snapshot;
    }

    public static void AddPlayTime(double seconds)
    {
        if (IsActive && seconds > 0)
            UnsavedPlayTime += seconds;
    }

    // 메모리 내 거래: 저장된 플레이 시간을 중복 가산하지 않는다.
    public static void ApplyRuntime(GameSaveData data)
    {
        double elapsed = UnsavedPlayTime;
        data.playTimeSeconds = Current.playTimeSeconds;
        Begin(data, IsDevSession);
        UnsavedPlayTime = elapsed;
    }

    private static void RestoreAll()
    {
        foreach (ISaveParticipant participant in _participants.ToArray())
            participant.RestoreState(Current);

        OnSessionRestored?.Invoke();
    }

    /// <summary>체크포인트 파일에 쓸 메타데이터를 채운다.</summary>
    public static void StampCheckpoint(GameSaveData data, string reason)
    {
        data.schemaVersion = GameSaveData.CurrentSchemaVersion;
        data.checkpoint = GameSaveData.CheckpointDayStart;
        data.checkpointReason = reason;
        data.saveRevision++;
        data.savedAtUtc = DateTime.UtcNow.ToString("o");
    }

    // ───── 튜토리얼·대화 완료 기록 (세션이 없으면 호출자가 기존 방식을 사용) ─────

    public static bool TryGetTutorialCompleted(string id, out bool completed)
    {
        completed = IsActive && Current.HasTutorialCompleted(id);
        return IsActive;
    }

    public static bool TryMarkTutorialCompleted(string id)
    {
        if (!IsActive) return false;
        GameSaveData.AddUnique(Current.completedTutorialIds, id);
        return true;
    }

    public static bool TryResetTutorial(string id)
    {
        if (!IsActive) return false;
        Current.completedTutorialIds.Remove(id);
        return true;
    }

    public static bool TryGetDialogueCompleted(string id, out bool completed)
    {
        completed = IsActive && Current.HasDialogueCompleted(id);
        return IsActive;
    }

    public static bool TryMarkDialogueCompleted(string id)
    {
        if (!IsActive) return false;
        GameSaveData.AddUnique(Current.completedDialogueIds, id);
        return true;
    }

    public static bool TryResetDialogue(string id)
    {
        if (!IsActive) return false;
        Current.completedDialogueIds.Remove(id);
        return true;
    }

    // ───── 에디터 전용: 게임 씬 직접 실행 ─────

    /// <summary>
    /// 타이틀을 거치지 않고 게임 씬을 바로 실행했을 때 임시 세션을 만든다.
    /// 정상 세이브가 있으면 그 내용으로, 없으면 새 게임 초기값으로 시작한다. 이 세션은 파일에 저장하지 않는다.
    /// </summary>
    public static void BeginDevSession()
    {
        if (IsActive)
            return;

        if (SaveService.TryLoad(out GameSaveData saved, out _) == SaveLoadStatus.Ok)
        {
            Debug.Log("[GameSession] 에디터 직접 실행: 기존 세이브로 임시 세션을 시작합니다. (파일에 저장되지 않음)");
            Begin(saved, true);
            return;
        }

        GameSaveData data = NewGameConfig.Load().CreateSaveData();
        data.introCompleted = true;
        Debug.Log("[GameSession] 에디터 직접 실행: 새 게임 초기값으로 임시 세션을 시작합니다. (파일에 저장되지 않음)");
        Begin(data, true);
    }
}
