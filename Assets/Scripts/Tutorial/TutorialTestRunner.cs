using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용] 튜토리얼 시퀀스를 단축키로 돌려보는 디버그 헬퍼.
/// 빈 오브젝트에 붙이고 Play → F5(재생) / F6(진행도 초기화) / F7(건너뛰기) / F8(이벤트 발사) → 콘솔 확인.
/// 출시 전 씬에서 제거.
/// </summary>
public class TutorialTestRunner : MonoBehaviour
{
    [SerializeField] private TutorialSequenceSO _sequence;

    [Tooltip("Event 모드 스텝을 넘길 때 발사할 이벤트 키. 스텝의 Complete Event Key와 맞춘다.")]
    [SerializeField] private string _testEventKey = "";

    [Header("키")]
    [Tooltip("완료 기록을 무시하고 바로 재생한다.")]
    [SerializeField] private KeyCode _playKey = KeyCode.F5;
    [SerializeField] private KeyCode _resetKey = KeyCode.F6;
    [SerializeField] private KeyCode _skipKey = KeyCode.F7;
    [SerializeField] private KeyCode _fireEventKey = KeyCode.F8;
    [Tooltip("현재 등록된 타깃 키 목록을 출력한다.")]
    [SerializeField] private KeyCode _dumpTargetsKey = KeyCode.F9;

    private bool _subscribed;

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_playKey))
            PlaySequence();

        if (Input.GetKeyDown(_resetKey))
            ResetProgress();

        if (Input.GetKeyDown(_skipKey))
            SkipSequence();

        if (Input.GetKeyDown(_fireEventKey))
            FireTestEvent();

        if (Input.GetKeyDown(_dumpTargetsKey))
            DumpActiveTargets();
    }

    /// <summary>완료 기록을 지우고 처음부터 재생한다.</summary>
    [ContextMenu("Play Sequence")]
    public void PlaySequence()
    {
        if (!HasManager() || !HasSequence())
            return;

        Subscribe();

        // 테스트는 항상 처음부터 볼 수 있어야 하므로 완료 기록을 먼저 지운다.
        TutorialManager.ResetProgress(_sequence.SequenceId);
        TutorialManager.Instance.Play(_sequence);
    }

    /// <summary>완료 기록만 지운다. (다음 자동 재생 조건을 다시 보고 싶을 때)</summary>
    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        if (!HasSequence())
            return;

        TutorialManager.ResetProgress(_sequence.SequenceId);
        Debug.Log($"[TutorialTest] '{_sequence.SequenceId}' 완료 기록 삭제 — F6");
    }

    [ContextMenu("Skip Sequence")]
    public void SkipSequence()
    {
        if (!HasManager())
            return;

        TutorialManager.Instance.SkipSequence();
        Debug.Log("[TutorialTest] 시퀀스 건너뛰기 요청 — F7");
    }

    /// <summary>Event 모드 스텝을 진행시킨다.</summary>
    [ContextMenu("Fire Test Event")]
    public void FireTestEvent()
    {
        if (!HasManager())
            return;

        if (string.IsNullOrEmpty(_testEventKey))
        {
            Debug.LogWarning("[TutorialTest] Test Event Key가 비어 있다.");
            return;
        }

        TutorialManager.Instance.NotifyEvent(_testEventKey);
        Debug.Log($"[TutorialTest] 이벤트 발사: '{_testEventKey}' — F8");
    }

    /// <summary>지금 화면에 등록되어 있는 타깃 키를 모두 출력한다. (키 오타 확인용)</summary>
    [ContextMenu("Dump Active Targets")]
    public void DumpActiveTargets()
    {
        List<string> keys = TutorialTargetRegistry.GetActiveKeys();

        if (keys.Count == 0)
        {
            Debug.Log("[TutorialTest] 등록된 타깃이 없다. (TutorialTarget이 붙어 있는지 / Key가 비어 있지 않은지 확인)");
            return;
        }

        Debug.Log($"[TutorialTest] 등록된 타깃 {keys.Count}개:\n - {string.Join("\n - ", keys)}");
    }

    /// <summary>시퀀스의 모든 스텝 타깃 키가 지금 화면에서 찾아지는지 점검한다.</summary>
    [ContextMenu("Validate Sequence Keys")]
    public void ValidateSequenceKeys()
    {
        if (!HasSequence())
            return;

        IReadOnlyList<TutorialStepSO> steps = _sequence.Steps;
        int missing = 0;

        for (int i = 0; i < steps.Count; i++)
        {
            TutorialStepSO step = steps[i];

            if (step == null)
            {
                Debug.LogWarning($"[TutorialTest] {i + 1}번 스텝이 비어 있다.");
                missing++;
                continue;
            }

            if (!step.HasTarget)
                continue;

            if (!TutorialTargetRegistry.Contains(step.TargetKey))
            {
                Debug.LogWarning($"[TutorialTest] {i + 1}번 스텝({step.name})의 타깃 '{step.TargetKey}'를 지금 화면에서 찾지 못했다.");
                missing++;
            }
        }

        if (missing == 0)
            Debug.Log($"[TutorialTest] '{_sequence.SequenceId}' 타깃 키 {steps.Count}개 모두 정상.");
    }

    private bool HasManager()
    {
        if (TutorialManager.Instance != null)
            return true;

        Debug.LogError("[TutorialTest] TutorialManager가 씬에 없다.");
        return false;
    }

    private bool HasSequence()
    {
        if (_sequence != null)
            return true;

        Debug.LogError("[TutorialTest] Sequence 미할당");
        return false;
    }

    private void Subscribe()
    {
        if (_subscribed || TutorialManager.Instance == null)
            return;

        TutorialManager.Instance.OnSequenceStarted += HandleSequenceStarted;
        TutorialManager.Instance.OnStepStarted += HandleStepStarted;
        TutorialManager.Instance.OnSequenceFinished += HandleSequenceFinished;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || TutorialManager.Instance == null)
        {
            _subscribed = false;
            return;
        }

        TutorialManager.Instance.OnSequenceStarted -= HandleSequenceStarted;
        TutorialManager.Instance.OnStepStarted -= HandleStepStarted;
        TutorialManager.Instance.OnSequenceFinished -= HandleSequenceFinished;
        _subscribed = false;
    }

    private void HandleSequenceStarted(TutorialSequenceSO sequence)
    {
        Debug.Log($"[TutorialTest] ▶ 시작: {sequence.SequenceId} (스텝 {sequence.StepCount}개)");
    }

    private void HandleStepStarted(TutorialStepSO step)
    {
        int index = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStepIndex + 1 : 0;
        string target = step.HasTarget ? step.TargetKey : "(타깃 없음)";
        Debug.Log($"[TutorialTest]   스텝 {index}: {step.name} / 타깃 {target} / 진행조건 {step.AdvanceMode}");
    }

    private void HandleSequenceFinished(TutorialSequenceSO sequence)
    {
        Debug.Log($"[TutorialTest] ◀ 종료: {sequence.SequenceId}");
    }
}
