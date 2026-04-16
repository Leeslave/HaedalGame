using System;
using System.Collections;
using UnityEngine;

public class UIPopupMotion : MonoBehaviour
{
    [SerializeField] private RectTransform _target;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Open")]
    [SerializeField] private float _openStartScale = 0.85f;
    [SerializeField] private float _openOvershootScale = 1.05f;
    [SerializeField] private float _openEndScale = 1.0f;
    [SerializeField] private float _openFirstDuration = 0.12f;
    [SerializeField] private float _openSecondDuration = 0.08f;

    [Header("Close")]
    [SerializeField] private float _closeEndScale = 0.92f;
    [SerializeField] private float _closeDuration = 0.12f;

    [Header("Move")]
    [SerializeField] private bool _useMoveY = false;
    [SerializeField] private float _openStartOffsetY = -20f;
    [SerializeField] private float _closeEndOffsetY = -10f;

    private Coroutine _playRoutine;
    private Vector2 _baseAnchoredPosition;

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
    }

    public void PlayOpen()
    {
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        if (_target == null)
            _target = transform as RectTransform;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_target != null)
            _baseAnchoredPosition = _target.anchoredPosition;

        _playRoutine = StartCoroutine(CoPlayOpen());
    }

    public void PlayClose(Action onComplete)
    {
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(CoPlayClose(onComplete));
    }

    private IEnumerator CoPlayOpen()
    {
        if (_target == null)
            yield break;

        Vector3 startScale = Vector3.one * _openStartScale;
        Vector3 overshootScale = Vector3.one * _openOvershootScale;
        Vector3 endScale = Vector3.one * _openEndScale;

        Vector2 startPos = _baseAnchoredPosition;
        if (_useMoveY)
            startPos += new Vector2(0f, _openStartOffsetY);

        _target.localScale = startScale;

        if (_useMoveY)
            _target.anchoredPosition = startPos;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        float time = 0f;

        while (time < _openFirstDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / _openFirstDuration);
            float eased = EaseOutBackLite(t);

            _target.localScale = Vector3.LerpUnclamped(startScale, overshootScale, eased);

            if (_useMoveY)
                _target.anchoredPosition = Vector2.Lerp(startPos, _baseAnchoredPosition, t);

            if (_canvasGroup != null)
                _canvasGroup.alpha = t;

            yield return null;
        }

        time = 0f;

        while (time < _openSecondDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / _openSecondDuration);
            float eased = EaseOutCubic(t);

            _target.localScale = Vector3.LerpUnclamped(overshootScale, endScale, eased);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            yield return null;
        }

        _target.localScale = endScale;

        if (_useMoveY)
            _target.anchoredPosition = _baseAnchoredPosition;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        _playRoutine = null;
    }

    private IEnumerator CoPlayClose(Action onComplete)
    {
        if (_target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startScale = _target.localScale;
        Vector3 endScale = Vector3.one * _closeEndScale;

        Vector2 startPos = _target.anchoredPosition;
        Vector2 endPos = _baseAnchoredPosition;

        if (_useMoveY)
            endPos += new Vector2(0f, _closeEndOffsetY);

        float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;

        float time = 0f;

        while (time < _closeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / _closeDuration);
            float eased = EaseInCubic(t);

            _target.localScale = Vector3.Lerp(startScale, endScale, eased);

            if (_useMoveY)
                _target.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            yield return null;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        onComplete?.Invoke();
        _playRoutine = null;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private float EaseOutBackLite(float t)
    {
        float c1 = 1.2f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}