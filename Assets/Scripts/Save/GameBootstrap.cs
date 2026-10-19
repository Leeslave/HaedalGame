using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 배치 없이 자동 생성되는 상주 오브젝트.
/// - 플레이 시간 누적 (타이틀 화면 시간 제외, 배속과 무관한 실제 시간)
/// - 에디터에서 게임 씬을 바로 실행했을 때 임시 세션 생성
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private static GameBootstrap _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("[GameBootstrap]");
        _instance = go.AddComponent<GameBootstrap>();
        DontDestroyOnLoad(go);

        // 첫 씬의 Awake 이후, Start 이전에 실행된다. 이미 등록된 매니저는 Begin에서 복원된다.
        string firstScene = SceneManager.GetActiveScene().name;
        if (Application.isEditor && firstScene != GameScenes.Title && !GameSession.IsActive)
        {
            GameSession.BeginDevSession();
            GameFlow.SetPhase(firstScene == GameScenes.Restaurant ? GamePhase.Preparation : GamePhase.DayStart);
        }
    }

    private void Update()
    {
        if (GameFlow.Phase != GamePhase.Title)
            GameSession.AddPlayTime(Time.unscaledDeltaTime);
    }
}
