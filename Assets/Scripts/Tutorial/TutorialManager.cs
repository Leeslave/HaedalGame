using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 시퀀스를 순서대로 재생하는 매니저.
/// 스텝 데이터(TutorialStepSO)를 읽어 타깃을 찾고, 표현은 TutorialHighlightView에 맡기고,
/// 진행 조건(클릭/시간/이벤트)만 판정한다. 완료 여부는 PlayerPrefs에 시퀀스 아이디로 저장한다.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string CompletedKeyPrefix = "Tutorial_Completed_";

    [SerializeField] private TutorialHighlightView _view;
    [Tooltip("아이디로 재생하기 위해 미리 등록해두는 시퀀스 목록.")]
    [SerializeField] private List<TutorialSequenceSO> _sequences = new List<TutorialSequenceSO>();
    [SerializeField] private bool _dontDestroyOnLoad = true;
    [Tooltip("테스트용. 켜면 저장된 완료 기록을 무시하고 항상 재생한다.")]
    [SerializeField] private bool _ignoreSavedProgress = false;

    public event Action<TutorialSequenceSO> OnSequenceStarted;
    public event Action<TutorialStepSO> OnStepStarted;
    public event Action<TutorialSequenceSO> OnSequenceFinished;

    private Coroutine _playRoutine;
    private Action _onFinished;

    private string _waitingEventKey;
    private bool _eventFired;
    private bool _skipStepRequested;
    private bool _skipSequenceRequested;

    private float _cachedTimeScale = 1f;

    public bool IsPlaying => _playRoutine != null;
    public TutorialSequenceSO CurrentSequence { get; private set; }
    public TutorialStepSO CurrentStep { get; private set; }
    public int CurrentStepIndex { get; private set; } = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (_view != null)
        {
            _view.OnSkipRequested += SkipSequence;
            _view.Hide();
        }
    }

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnSkipRequested -= SkipSequence;

        if (Instance == this)
            Instance = null;
    }

    #region 재생

    /// <summary>시퀀스를 재생한다. 이미 완료됐거나 재생 중이면 false를 반환한다.</summary>
    public bool Play(TutorialSequenceSO sequence, Action onFinished = null)
    {
        if (sequence == null)
        {
            Debug.LogWarning("[Tutorial] 재생할 시퀀스가 비어 있다.");
            return false;
        }

        if (IsPlaying)
        {
            Debug.LogWarning($"[Tutorial] 이미 '{(CurrentSequence != null ? CurrentSequence.SequenceId : "?")}' 재생 중이라 '{sequence.SequenceId}'를 건너뛴다.");
            return false;
        }

        if (_view == null)
        {
            Debug.LogError("[Tutorial] TutorialHighlightView가 연결되지 않았다.");
            return false;
        }

        if (sequence.StepCount == 0)
        {
            Debug.LogWarning($"[Tutorial] '{sequence.SequenceId}'에 스텝이 없다.");
            return false;
        }

        if (sequence.PlayOnce && IsCompleted(sequence.SequenceId))
            return false;

        _onFinished = onFinished;
        _playRoutine = StartCoroutine(CoPlaySequence(sequence));
        return true;
    }

    /// <summary>등록된 시퀀스 목록에서 아이디로 찾아 재생한다.</summary>
    public bool Play(string sequenceId, Action onFinished = null)
    {
        TutorialSequenceSO sequence = FindSequence(sequenceId);

        if (sequence == null)
        {
            Debug.LogWarning($"[Tutorial] '{sequenceId}' 시퀀스를 Sequences 목록에서 찾지 못했다.");
            return false;
        }

        return Play(sequence, onFinished);
    }

    public TutorialSequenceSO FindSequence(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            return null;

        for (int i = 0; i < _sequences.Count; i++)
        {
            TutorialSequenceSO sequence = _sequences[i];

            if (sequence != null && sequence.SequenceId == sequenceId)
                return sequence;
        }

        return null;
    }

    private IEnumerator CoPlaySequence(TutorialSequenceSO sequence)
    {
        CurrentSequence = sequence;
        _skipSequenceRequested = false;

        if (sequence.PauseGameTime)
        {
            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        _view.Show();
        OnSequenceStarted?.Invoke(sequence);

        IReadOnlyList<TutorialStepSO> steps = sequence.Steps;

        for (int i = 0; i < steps.Count; i++)
        {
            if (_skipSequenceRequested)
                break;

            TutorialStepSO step = steps[i];

            if (step == null)
                continue;

            CurrentStepIndex = i;
            yield return CoPlayStep(step);
        }

        CurrentStep = null;
        CurrentStepIndex = -1;
        _view.Hide();

        if (sequence.PauseGameTime)
            Time.timeScale = _cachedTimeScale;

        if (sequence.PlayOnce)
            MarkCompleted(sequence.SequenceId);

        _playRoutine = null;
        CurrentSequence = null;

        OnSequenceFinished?.Invoke(sequence);

        Action finished = _onFinished;
        _onFinished = null;
        finished?.Invoke();
    }

    private IEnumerator CoPlayStep(TutorialStepSO step)
    {
        CurrentStep = step;
        _skipStepRequested = false;
        _eventFired = false;
        _waitingEventKey = step.CompleteEventKey;

        if (step.StartDelay > 0f)
            yield return WaitUnscaled(step.StartDelay);

        TutorialTarget target = null;

        if (step.HasTarget)
        {
            // 타깃이 아직 안 만들어졌을 수 있으니(팝업이 열리는 중 등) 잠시 기다린다.
            yield return CoWaitForTarget(step);

            target = TutorialTargetRegistry.Find(step.TargetKey);

            if (target == null)
                Debug.LogWarning($"[Tutorial] 타깃 '{step.TargetKey}'를 찾지 못해 문구만 표시한다.");
        }

        // 진행 조건을 만족시킬 수단이 없으면(타깃 없는 TargetClick, 키 없는 Event) 영영 넘어가지 못한다.
        // 이런 스텝은 클릭으로 넘어갈 수 있게 낮춰서 튜토리얼이 막히지 않도록 한다.
        TutorialAdvanceMode advanceMode = step.AdvanceMode;

        if (advanceMode == TutorialAdvanceMode.TargetClick && target == null)
        {
            Debug.LogWarning($"[Tutorial] '{step.name}'은 TargetClick인데 타깃이 없어 AnyClick으로 대체한다.");
            advanceMode = TutorialAdvanceMode.AnyClick;
        }
        else if (advanceMode == TutorialAdvanceMode.Event && string.IsNullOrEmpty(step.CompleteEventKey))
        {
            Debug.LogWarning($"[Tutorial] '{step.name}'은 Event 모드인데 Complete Event Key가 비어 AnyClick으로 대체한다.");
            advanceMode = TutorialAdvanceMode.AnyClick;
        }

        _view.SetStep(step);
        _view.SetHoleBlocking(!step.AllowTargetInteraction);
        _view.UpdateHole(GetTargetRect(target), step);

        OnStepStarted?.Invoke(step);

        // TargetClick 스텝에서 타깃이 버튼이면 실제 클릭 이벤트를 그대로 받는다.
        Button targetButton = null;
        bool targetButtonClicked = false;

        if (advanceMode == TutorialAdvanceMode.TargetClick && target != null)
        {
            targetButton = target.GetComponent<Button>();

            if (targetButton == null)
                targetButton = target.GetComponentInChildren<Button>();

            if (targetButton != null)
            {
                targetButton.onClick.AddListener(HandleTargetButtonClicked);

                // 비활성 버튼은 onClick이 오지 않는다. 아래 위치 판정으로 넘어가므로 막히지는 않지만,
                // 의도한 상황이 아닐 수 있어 알려둔다.
                if (!targetButton.IsInteractable())
                    Debug.LogWarning($"[Tutorial] '{step.name}'의 타깃 버튼({targetButton.name})이 비활성 상태라 " +
                                     "onClick이 오지 않는다. 구멍 안쪽 클릭으로 진행한다.");
            }
        }

        void HandleTargetButtonClicked() => targetButtonClicked = true;

        float elapsed = 0f;

        while (true)
        {
            // 타깃이 스크롤/애니메이션으로 움직여도 따라가도록 매 프레임 갱신한다.
            _view.UpdateHole(GetTargetRect(target), step);

            if (_skipStepRequested || _skipSequenceRequested)
                break;

            if (_eventFired)
                break;

            elapsed += Time.unscaledDeltaTime;
            bool inputReady = elapsed >= step.InputDelay;

            if (advanceMode == TutorialAdvanceMode.Delay)
            {
                if (elapsed >= step.AutoAdvanceDelay)
                    break;
            }
            else if (advanceMode == TutorialAdvanceMode.AnyClick)
            {
                if (inputReady && Input.GetMouseButtonDown(0))
                    break;
            }
            else if (advanceMode == TutorialAdvanceMode.TargetClick)
            {
                if (targetButtonClicked)
                    break;

                // 버튼이 없는 타깃(슬롯·월드 오브젝트)뿐 아니라, 버튼이 비활성이라 onClick이
                // 오지 않는 경우에도 튜토리얼이 막히면 안 되므로 구멍 안쪽 클릭을 함께 인정한다.
                if (inputReady && Input.GetMouseButtonDown(0) && _view.IsInsideHole(Input.mousePosition))
                    break;
            }

            yield return null;
        }

        if (targetButton != null)
            targetButton.onClick.RemoveListener(HandleTargetButtonClicked);

        _waitingEventKey = null;
        _eventFired = false;
    }

    /// <summary>타깃이 화면에 나타날 때까지(타임아웃까지) 기다린다.</summary>
    private IEnumerator CoWaitForTarget(TutorialStepSO step)
    {
        float waited = 0f;

        while (!TutorialTargetRegistry.Contains(step.TargetKey))
        {
            if (waited >= step.TargetWaitTimeout)
                yield break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static Rect? GetTargetRect(TutorialTarget target)
    {
        if (target == null || !target.isActiveAndEnabled)
            return null;

        return target.GetScreenRect();
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float time = 0f;

        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    #endregion

    #region 외부 제어

    /// <summary>
    /// Event 모드 스텝을 진행시킨다. 게임 로직에서 원하는 시점에 호출하면 된다.
    /// 예) 상점 팝업이 열린 뒤 TutorialManager.Instance.NotifyEvent("shop.opened");
    /// </summary>
    public void NotifyEvent(string eventKey)
    {
        if (!IsPlaying || string.IsNullOrEmpty(eventKey))
            return;

        if (_waitingEventKey == eventKey)
            _eventFired = true;
    }

    /// <summary>현재 스텝만 건너뛴다.</summary>
    public void SkipStep()
    {
        if (IsPlaying)
            _skipStepRequested = true;
    }

    /// <summary>시퀀스 전체를 건너뛴다. (완료로 기록된다)</summary>
    public void SkipSequence()
    {
        if (IsPlaying)
            _skipSequenceRequested = true;
    }

    #endregion

    #region 진행도 저장

    public static bool IsCompleted(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            return false;

        if (Instance != null && Instance._ignoreSavedProgress)
            return false;

        return PlayerPrefs.GetInt(CompletedKeyPrefix + sequenceId, 0) == 1;
    }

    public static void MarkCompleted(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            return;

        PlayerPrefs.SetInt(CompletedKeyPrefix + sequenceId, 1);
        PlayerPrefs.Save();
    }

    public static void ResetProgress(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
            return;

        PlayerPrefs.DeleteKey(CompletedKeyPrefix + sequenceId);
        PlayerPrefs.Save();
    }

    /// <summary>테스트용: 등록된 모든 시퀀스의 완료 기록을 지운다.</summary>
    [ContextMenu("모든 튜토리얼 진행도 초기화")]
    public void ResetAllProgress()
    {
        for (int i = 0; i < _sequences.Count; i++)
        {
            if (_sequences[i] != null)
                ResetProgress(_sequences[i].SequenceId);
        }

        Debug.Log("[Tutorial] 등록된 시퀀스의 완료 기록을 모두 지웠다.");
    }

    #endregion
}
