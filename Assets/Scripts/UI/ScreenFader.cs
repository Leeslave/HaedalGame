using System;
using System.Collections;
using UnityEngine;

// 화면 전체를 덮는 CanvasGroup의 alpha를 Lerp해 페이드 인/아웃한다.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void FadeOut(Action onComplete)
    {
        StartFade(1f, onComplete);
    }

    public void FadeIn(Action onComplete)
    {
        StartFade(0f, onComplete);
    }

    private void StartFade(float targetAlpha, Action onComplete)
    {
        if (activeRoutine != null) { StopCoroutine(activeRoutine); }
        activeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        canvasGroup.blocksRaycasts = true;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        activeRoutine = null;
        onComplete?.Invoke();
    }
}
