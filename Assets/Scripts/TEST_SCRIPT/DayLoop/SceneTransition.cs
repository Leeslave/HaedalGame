using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 씬 전환 가림막. (FLOW-01)
/// 이동 요청 즉시 화면을 불투명하게 덮고, 다음 씬의 Awake·Start·첫 프레임 갱신(세션 복원·HUD·카메라 배치)이
/// 끝난 뒤에 걷는다. 그래서 이전 씬(타이틀 등)이나 초기화 전 HUD(0원 표시 등)가 한 프레임도 비치지 않는다.
/// 전환 중 들어온 중복 이동 요청은 무시한다. (버튼 연타로 씬·Canvas가 중복 생성되는 것 방지)
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const float RevealDuration = 0.3f;
    private const int SettleFrames = 3;   // 새 씬의 Start와 첫 Update/LateUpdate가 돌 시간

    private static SceneTransition _instance;

    private CanvasGroup _group;
    private bool _loading;

    public static bool IsTransitioning => _instance != null && _instance._loading;

    public static void Load(string sceneName)
    {
        Ensure().BeginLoad(sceneName);
    }

    /// <summary>다음 동작(저장 등) 전에 화면만 즉시 덮는다. Load가 이어지지 않으면 Reveal로 걷어야 한다.</summary>
    public static void CoverNow()
    {
        Ensure().SetAlpha(1f);
    }

    public static void Reveal()
    {
        if (_instance != null && !_instance._loading)
            _instance.SetAlpha(0f);
    }

    private static SceneTransition Ensure()
    {
        if (_instance != null)
            return _instance;

        GameObject go = new GameObject("[SceneTransition]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        DontDestroyOnLoad(go);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        Image cover = DayLoopUI.Panel(go.transform, DayLoopUI.Curtain, "Cover", false);
        DayLoopUI.Stretch(cover.rectTransform);
        cover.raycastTarget = true;

        _instance = go.AddComponent<SceneTransition>();
        _instance._group = go.GetComponent<CanvasGroup>();
        _instance.SetAlpha(0f);
        return _instance;
    }

    private void SetAlpha(float alpha)
    {
        _group.alpha = alpha;
        _group.blocksRaycasts = alpha > 0.01f;
        _group.interactable = false;
    }

    private void BeginLoad(string sceneName)
    {
        if (_loading)
        {
            Debug.LogWarning($"[SceneTransition] 전환 중이라 '{sceneName}' 이동 요청을 무시합니다.");
            return;
        }

        _loading = true;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // 요청한 프레임에 바로 덮는다. (인트로가 끝난 직후 타이틀이 다시 보이던 문제)
        SetAlpha(1f);
        Time.timeScale = 1f;
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"[SceneTransition] '{sceneName}' 씬을 불러올 수 없습니다. Build Settings를 확인하세요.");
            _loading = false;
            SetAlpha(0f);
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        for (int i = 0; i < SettleFrames; i++)
            yield return null;

        float elapsed = 0f;
        while (elapsed < RevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / RevealDuration));
            yield return null;
        }

        SetAlpha(0f);
        _loading = false;
    }
}
