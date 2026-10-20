using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>타이틀/인트로/씬 전환에서 직렬화된 구형 폰트 머티리얼이 남아 글자가 사라지는 것을 방지한다.</summary>
public sealed class DesignFontBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("Design fonts");
        DontDestroyOnLoad(root); root.AddComponent<DesignFontBootstrap>();
    }
    private void Awake() { SceneManager.sceneLoaded += Loaded; }
    private void OnDestroy() { SceneManager.sceneLoaded -= Loaded; }
    private void Loaded(Scene scene, LoadSceneMode mode) { StartCoroutine(RepairAfterStart()); }
    private IEnumerator RepairAfterStart()
    {
        yield return null;
        PopupManager.Instance?.SetNavigatorVisible(false);
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.GetComponent<DesignCanvasMarker>() != null || canvas.name.Contains("Popup") || canvas.name.Contains("SceneTransition") || canvas.name.Contains("Dialogue")) continue;
            var group = canvas.GetComponent<CanvasGroup>();
            if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        }
        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.font == DesignFonts.Heading || text.font == DesignFonts.Body) continue;
            DesignFonts.Apply(text, text.fontSize >= 28);
        }
    }
}

public sealed class DesignCanvasMarker : MonoBehaviour { }
