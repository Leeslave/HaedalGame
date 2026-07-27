using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 계량 페이즈 컨트롤러. 계량컵에 액체를 꾹 눌러 따르다가 목표량에서 손을 뗀다.
/// 목표 ±허용오차 안이면 완벽, 벗어나면 감점, 컵을 넘기면(100% 도달) 쏟음 처리.
/// MeasurePhaseSO.Measures를 순서대로 계량하고 평균 점수로 완료한다.
/// </summary>
public class MeasurePhaseController : MinigamePhaseController
{
    public override CookingActionType ActionType => CookingActionType.Measure;

    [Header("입력")]
    // 꾹 누르기 버튼. 미지정 시 마우스 왼쪽 버튼을 사용한다.
    [SerializeField] private PourHoldButton _pourButton;

    [Header("계량컵")]
    [SerializeField] private Image _liquidFill;        // Image Type=Filled, Fill Method=Vertical, Origin=Bottom
    [SerializeField] private RectTransform _targetLine; // 목표선 (fill 영역 자식, 세로 앵커로 위치)
    [SerializeField] private RectTransform _toleranceZone; // 허용 구간 밴드 (선택)
    [SerializeField] private Image _sourceImage;       // 따르는 병/주전자 (선택)

    [Header("텍스트")]
    [SerializeField] private TMP_Text _labelText;      // "간장 2/3 컵"
    [SerializeField] private TMP_Text _judgementText;  // "딱 맞음!" 등 (선택)
    [SerializeField] private TMP_Text _progressText;   // "1 / 3" (선택)

    [Header("버튼")]
    [SerializeField] private Button _completeButton;   // 전 재료 계량 후 활성. 미지정 시 자동 완료

    [Header("Rule")]
    [SerializeField] private float _judgementShowTime = 0.7f; // 판정 표시 후 다음 재료로

    [Header("연출 (선택)")]
    [SerializeField] private RectTransform _sourceTransform;   // 따르는 병/주전자 (누르면 기울어짐)
    [SerializeField] private float _pourTiltAngle = -30f;      // 따를 때 기울기(도). 부호로 방향 조절
    [SerializeField] private float _tiltLerpSpeed = 12f;
    // 회전 축을 source의 로컬 좌표로 지정(주둥이 위치 등). Pivot을 안 건드리고 이 점을 중심으로 기울인다.
    [SerializeField] private bool _useCustomPivot = false;
    [SerializeField] private Vector2 _tiltPivotOffset = Vector2.zero; // source 로컬 기준 축 오프셋(px)
    [SerializeField] private GameObject _pourStream;           // 물줄기 (누르는 동안 표시)
    [SerializeField] private RectTransform _liquidSurface;     // 액체 표면 (따르는 동안 찰랑임)
    [SerializeField] private float _surfaceWobbleAngle = 4f;
    [SerializeField] private float _surfaceWobbleSpeed = 18f;
    [SerializeField] private Image _targetLineImage;           // 목표선 (스위트스폿 하이라이트, 없으면 _targetLine에서 탐색)
    [SerializeField] private Color _targetNormalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color _targetHitColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private RectTransform _shakeTarget;       // 넘칠 때 흔들 대상 (보통 컵)
    [SerializeField] private float _shakeAmount = 12f;
    [SerializeField] private float _shakeDuration = 0.3f;

    private readonly List<float> _scores = new List<float>();
    private Coroutine _runRoutine;
    private Coroutine _shakeRoutine;
    private Coroutine _popRoutine;

    // 연출용 상태
    private bool _measureActive;
    private float _currentFill;
    private float _currentTarget;
    private float _currentTolerance;
    private Vector3 _judgementBaseScale = Vector3.one;

    // 병 기울임: 직립 기준 위치 + 현재 각도 (커스텀 축 회전용)
    private Vector2 _sourceBasePos;
    private bool _sourceBaseCaptured;
    private float _currentTilt;

    protected override void OnBegin()
    {
        _scores.Clear();
        _measureActive = false;

        if (_completeButton != null)
        {
            _completeButton.onClick.AddListener(OnClickComplete);
            _completeButton.interactable = false;
        }

        if (_judgementText != null)
        {
            _judgementText.text = string.Empty;
            _judgementBaseScale = _judgementText.rectTransform.localScale;
        }

        // 연출 초기화
        if (_targetLineImage == null && _targetLine != null)
            _targetLineImage = _targetLine.GetComponent<Image>();

        if (_sourceTransform != null)
        {
            // 직립 기준 위치는 아직 안 기울인 최초 상태에서 1회만 캡처
            if (!_sourceBaseCaptured)
            {
                _sourceBasePos = _sourceTransform.anchoredPosition;
                _sourceBaseCaptured = true;
            }

            _currentTilt = 0f;
            _sourceTransform.localRotation = Quaternion.identity;

            if (_useCustomPivot)
                _sourceTransform.anchoredPosition = _sourceBasePos;
        }

        if (_pourStream != null)
            _pourStream.SetActive(false);

        _runRoutine = StartCoroutine(RunMeasures());
    }

    private IEnumerator RunMeasures()
    {
        MeasurePhaseSO so = Phase as MeasurePhaseSO;

        IReadOnlyList<MeasureTarget> measures = so != null ? so.Measures : null;
        int total = measures != null ? measures.Count : 0;
        float speed = so != null ? Mathf.Max(0.01f, so.FillSpeed) : 0.4f;

        for (int i = 0; i < total; i++)
        {
            MeasureTarget target = measures[i];
            if (target == null)
                continue;

            SetupVisual(target, i, total);

            // 이전 재료에서 누르고 있던 입력을 놓을 때까지 대기 (연속 홀드 방지)
            while (IsPouring())
                yield return null;

            float fill = 0f;
            bool poured = false;
            _measureActive = true;

            while (true)
            {
                bool held = IsPouring();

                if (held)
                {
                    fill += speed * Time.deltaTime;
                    poured = true;
                }

                _currentFill = fill;
                SetFill(Mathf.Min(fill, 1f));

                // 컵을 넘김 → 쏟음
                if (fill >= 1f)
                {
                    CommitScore(1f, "넘쳤어요!");
                    StartShake();
                    break;
                }

                // 따르다가 손 뗌 → 그 지점으로 판정
                if (poured && !held)
                {
                    float score = ScorePour(fill, target.TargetAmount, target.Tolerance, out string feedback);
                    CommitScore(score, feedback);
                    break;
                }

                yield return null;
            }

            _measureActive = false;

            yield return new WaitForSeconds(_judgementShowTime);
        }

        _runRoutine = null;

        // 전 재료 종료: 완료 버튼이 있으면 대기, 없으면 자동 완료.
        if (_completeButton != null)
            _completeButton.interactable = true;
        else
            CompletePhase();
    }

    private bool IsPouring()
    {
        return _pourButton != null ? _pourButton.IsHeld : Input.GetMouseButton(0);
    }

    private void Update()
    {
        bool pouring = _measureActive && IsPouring();

        // 병 기울이기 (누르면 기울고, 떼면 원위치로 부드럽게)
        if (_sourceTransform != null)
        {
            float targetTilt = pouring ? _pourTiltAngle : 0f;
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * _tiltLerpSpeed);

            Quaternion rot = Quaternion.Euler(0f, 0f, _currentTilt);
            _sourceTransform.localRotation = rot;

            // 커스텀 축: (직립위치 + 오프셋) 지점을 고정한 채 그 점을 중심으로 회전
            // → 직립일 땐 원래 자리, 기울일 땐 주둥이가 고정되고 몸통만 회전.
            if (_useCustomPivot && _sourceBaseCaptured)
            {
                Vector2 rotatedOffset = rot * (Vector3)_tiltPivotOffset;
                _sourceTransform.anchoredPosition = _sourceBasePos + _tiltPivotOffset - rotatedOffset;
            }
        }

        // 물줄기 on/off
        if (_pourStream != null && _pourStream.activeSelf != pouring)
            _pourStream.SetActive(pouring);

        // 액체 표면 찰랑임
        if (_liquidSurface != null)
        {
            float wobble = pouring ? Mathf.Sin(Time.time * _surfaceWobbleSpeed) * _surfaceWobbleAngle : 0f;
            _liquidSurface.localRotation = Quaternion.Euler(0f, 0f, wobble);
        }

        // 스위트스폿 하이라이트 (목표 구간에 들어오면 목표선이 빛나고 맥동)
        if (_targetLineImage != null)
        {
            bool inSpot = _measureActive && Mathf.Abs(_currentFill - _currentTarget) <= _currentTolerance;
            _targetLineImage.color = inSpot ? _targetHitColor : _targetNormalColor;

            if (_targetLine != null)
            {
                float pulse = inSpot ? 1f + Mathf.Sin(Time.time * 12f) * 0.12f : 1f;
                _targetLine.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    private void StartShake()
    {
        if (_shakeTarget == null)
            return;

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector2 basePos = _shakeTarget.anchoredPosition;
        float t = 0f;

        while (t < _shakeDuration)
        {
            t += Time.deltaTime;
            float damp = 1f - (t / _shakeDuration);
            _shakeTarget.anchoredPosition = basePos +
                new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * _shakeAmount * damp;
            yield return null;
        }

        _shakeTarget.anchoredPosition = basePos;
        _shakeRoutine = null;
    }

    private IEnumerator PopText()
    {
        RectTransform rt = _judgementText.rectTransform;
        float duration = 0.18f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float r = t / duration;
            float scale = Mathf.Lerp(1.4f, 1f, r); // 크게 튀어나왔다가 원래대로
            rt.localScale = _judgementBaseScale * scale;
            yield return null;
        }

        rt.localScale = _judgementBaseScale;
        _popRoutine = null;
    }

    private void SetupVisual(MeasureTarget target, int index, int total)
    {
        _currentTarget = target.TargetAmount;
        _currentTolerance = target.Tolerance;
        _currentFill = 0f;

        // 라벨: "간장 2/3 컵"
        if (_labelText != null)
        {
            string name = ResolveName(target);
            _labelText.text = string.IsNullOrEmpty(target.TargetLabel)
                ? name
                : $"{name} {target.TargetLabel}";
        }

        if (_progressText != null)
            _progressText.text = $"{index + 1} / {total}";

        // 액체 색/스프라이트
        if (_liquidFill != null)
        {
            _liquidFill.color = target.LiquidColor;
            if (target.LiquidSprite != null)
                _liquidFill.sprite = target.LiquidSprite;
            _liquidFill.fillAmount = 0f;
        }

        // 따르는 병/주전자
        if (_sourceImage != null)
        {
            _sourceImage.sprite = target.SourceSprite;
            _sourceImage.enabled = target.SourceSprite != null;
        }

        // 목표선 위치 (fill 영역 기준 세로 비율)
        SetAnchorY(_targetLine, target.TargetAmount);

        // 허용 구간 밴드 (target ± tolerance)
        SetToleranceZone(target.TargetAmount, target.Tolerance);

        if (_judgementText != null)
            _judgementText.text = string.Empty;
    }

    private string ResolveName(MeasureTarget target)
    {
        if (!string.IsNullOrEmpty(target.DisplayName))
            return target.DisplayName;

        if (target.IngredientId >= 0 && Context != null && Context.Database != null
            && Context.Database.TryGetIngredientById(target.IngredientId, out IngredientData data))
        {
            return data.IngredientName;
        }

        return string.Empty;
    }

    private void SetFill(float value)
    {
        if (_liquidFill != null)
            _liquidFill.fillAmount = value;
    }

    /// <summary>RectTransform을 부모 세로 비율 y 위치에 놓는다 (앵커 기준).</summary>
    private static void SetAnchorY(RectTransform rect, float y)
    {
        if (rect == null)
            return;

        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        min.y = y;
        max.y = y;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
    }

    private void SetToleranceZone(float target, float tolerance)
    {
        if (_toleranceZone == null)
            return;

        float bottom = Mathf.Clamp01(target - tolerance);
        float top = Mathf.Clamp01(target + tolerance);

        Vector2 min = _toleranceZone.anchorMin;
        Vector2 max = _toleranceZone.anchorMax;
        min.y = bottom;
        max.y = top;
        _toleranceZone.anchorMin = min;
        _toleranceZone.anchorMax = max;
        _toleranceZone.offsetMin = new Vector2(_toleranceZone.offsetMin.x, 0f);
        _toleranceZone.offsetMax = new Vector2(_toleranceZone.offsetMax.x, 0f);
    }

    /// <summary>목표량 대비 채운 양으로 점수(1/3/5)와 피드백 문구를 낸다.</summary>
    private static float ScorePour(float fill, float target, float tolerance, out string feedback)
    {
        float diff = fill - target;      // 양수=많음, 음수=부족
        float abs = Mathf.Abs(diff);

        if (abs <= tolerance)
        {
            feedback = "딱 맞음!";
            return 5f;
        }

        if (abs <= tolerance * 2f)
        {
            feedback = diff < 0 ? "조금 부족" : "조금 많음";
            return 3f;
        }

        feedback = diff < 0 ? "너무 부족" : "너무 많음";
        return 1f;
    }

    private void CommitScore(float score, string feedback)
    {
        _scores.Add(score);

        if (_judgementText != null)
        {
            _judgementText.text = feedback;

            if (_popRoutine != null)
                StopCoroutine(_popRoutine);

            _popRoutine = StartCoroutine(PopText());
        }
    }

    private void OnClickComplete()
    {
        if (_runRoutine == null)
            CompletePhase();
    }

    private void CompletePhase()
    {
        Cleanup();

        float sum = 0f;
        for (int i = 0; i < _scores.Count; i++)
            sum += _scores[i];

        float average = _scores.Count > 0 ? sum / _scores.Count : 0f;

        Complete(new PhaseScoreResult(Phase != null ? Phase.PhaseName : "Measure", average));
    }

    public override void Abort()
    {
        if (_runRoutine != null)
        {
            StopCoroutine(_runRoutine);
            _runRoutine = null;
        }

        Cleanup();
        base.Abort();
    }

    private void Cleanup()
    {
        if (_completeButton != null)
            _completeButton.onClick.RemoveListener(OnClickComplete);

        _measureActive = false;

        if (_pourStream != null)
            _pourStream.SetActive(false);

        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            _shakeRoutine = null;
        }
    }
}
