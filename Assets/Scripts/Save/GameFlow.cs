using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameScenes
{
    public const string Title = "StartScene_TEST";
    public const string Island = "MainIsland_TEST";
    public const string Restaurant = "Restaurant_TEST";
}

public enum GamePhase
{
    Title,
    Intro,
    DayStart,
    Preparation,
    Operation,
    ClosingReport,
    FreeTime,
    EndingDay,
}

/// <summary>
/// 새로하기 / 이어하기 / 체크포인트 저장 / 하루 흐름 전환을 담당한다.
/// 새로하기는 인트로가 끝나고 첫 체크포인트 저장에 성공했을 때 비로소 기존 세이브를 교체한다.
/// </summary>
public static class GameFlow
{
    public static GamePhase Phase { get; private set; } = GamePhase.Title;

    public static event Action<GamePhase> OnPhaseChanged;

    /// <summary>
    /// 씬 이동 방식. 비어 있으면 SceneManager.LoadScene으로 바로 이동한다.
    /// 전환 가림막(SceneTransition)이 설치되면 여기에 연결되어, 다음 씬 초기화가 끝날 때까지 화면을 덮는다.
    /// </summary>
    public static Action<string> SceneLoader;

    private static void LoadScene(string sceneName)
    {
        if (SceneLoader != null)
            SceneLoader(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public static void SetPhase(GamePhase phase)
    {
        if (Phase == phase)
            return;

        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    // ───── 새로하기 ─────

    /// <summary>
    /// 새 세션을 메모리에만 만든다. 파일은 건드리지 않는다. (인트로 도중 종료하면 기존 세이브가 그대로 남는다)
    /// </summary>
    public static void BeginNewGame()
    {
        GameSaveData data = NewGameConfig.Load().CreateSaveData();
        data.introCompleted = false;

        GameSession.Begin(data);
        SetPhase(GamePhase.Intro);
    }

    /// <summary>
    /// 인트로 완료 직후 1일차 시작 체크포인트를 저장한다. 성공해야 true.
    /// 실패해도 현재 세션의 인트로 완료 상태는 유지되며, 같은 메서드로 다시 시도하면 된다.
    /// </summary>
    public static bool TrySaveIntroCheckpoint(out string error)
    {
        error = null;
        if (!GameSession.IsActive)
        {
            error = "진행 중인 게임이 없습니다.";
            return false;
        }

        GameSaveData current = GameSession.Current;
        current.introCompleted = true;
        current.day = 1;
        current.lastCompletedDay = 0;
        if (string.IsNullOrEmpty(current.nextGuideStep))
            current.nextGuideStep = GuideSteps.IslandStory;

        // 첫날 취침 전에는 기존 진행 파일을 교체하지 않는다.
        return true;
    }

    // ───── 이어하기 ─────

    /// <summary>세이브를 읽어 세션을 시작하고 섬으로 이동한다. 실패 시 세션·파일은 변하지 않는다.</summary>
    public static SaveLoadStatus TryContinue(out string error)
    {
        SaveLoadStatus status = SaveService.TryLoad(out GameSaveData data, out error);
        if (status != SaveLoadStatus.Ok)
            return status;

        GameSession.Begin(data);
        EnterDayStart();
        return SaveLoadStatus.Ok;
    }

    // ───── 하루 흐름 ─────

    /// <summary>체크포인트(DayStart)에서 하루를 시작한다. 섬 씬으로 이동한다.</summary>
    public static void EnterDayStart()
    {
        GameSession.OperationCompletedToday = GameSession.IsActive && GameSession.Current.progression.freeTime;
        SetPhase(GameSession.OperationCompletedToday ? GamePhase.FreeTime : GamePhase.DayStart);
        LoadScene(GameScenes.Island);
    }

    /// <summary>섬에서 식당으로 이동해 영업 준비를 시작한다. 같은 날 이미 영업을 마쳤으면 false.</summary>
    public static bool TryEnterRestaurant()
    {
        if (GameSession.OperationCompletedToday)
            return false;
        if (GameSession.IsActive && GameSession.Current.progression.constructionUntilDay > GameSession.Current.day)
            return false;

        SetPhase(GamePhase.Preparation);
        LoadScene(GameScenes.Restaurant);
        return true;
    }

    public static void ReturnToTitle()
    {
        GameSession.End();
        SetPhase(GamePhase.Title);
        LoadScene(GameScenes.Title);
    }
}
