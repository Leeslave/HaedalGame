using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [TEST 하루 루프] TEST 씬이 열릴 때 하루 루프 기능을 자동으로 붙인다.
/// 씬 파일(.unity)은 건드리지 않으며, GameScenes에 등록된 TEST 씬 이름에서만 동작한다.
///  - StartScene_TEST : 인트로 중 타이틀 입력 잠금
///  - MainIsland_TEST : 일차·골드 표시, 결산 화면 복원
///  - Restaurant_TEST : 하루 단계·HUD·결산·첫날 안내·상태 표시·카메라 구도
/// 모든 씬 이동은 SceneTransition 가림막을 거친다.
/// </summary>
public static class DayLoopInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameFlow.SceneLoader = SceneTransition.Load;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // 첫 씬에서 sceneLoaded가 오지 않는 경우에 대비한다. (이미 설치되어 있으면 아무것도 하지 않는다)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallFirstScene()
    {
        Install(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        Time.timeScale = 1f;   // 일시정지 상태로 다음 씬에 넘어가지 않게
        Install(scene);
    }

    private static void Install(Scene scene)
    {
        if (!scene.IsValid())
            return;

        if (scene.name == GameScenes.Restaurant)
            Create<DayCycleController>("[DayLoop] Restaurant", scene);
        else if (scene.name == GameScenes.Island)
            Create<IslandDayHud>("[DayLoop] Island", scene);
        else if (scene.name == GameScenes.Title)
            Create<TitleTransitionGuard>("[DayLoop] Title", scene);
    }

    private static void Create<T>(string name, Scene scene) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null)
            return;

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<T>();
    }
}
