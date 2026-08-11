using System.Collections;
using UnityEngine;

/// <summary>
/// 코드를 건드리지 않고 튜토리얼 시퀀스를 시작시키는 컴포넌트.
/// 화면(팝업) 오브젝트에 붙여두면 그 화면이 처음 열릴 때 자동으로 재생된다.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerTiming
    {
        OnEnable, // 오브젝트가 켜질 때마다 시도 (이미 완료한 시퀀스는 자동으로 무시된다)
        OnStart,  // 최초 1회
        Manual    // Play()를 직접 호출
    }

    [SerializeField] private TutorialSequenceSO _sequence;
    [SerializeField] private TriggerTiming _timing = TriggerTiming.OnStart;
    [Tooltip("UI 레이아웃이 잡힌 뒤 시작하도록 약간 늦춘다.")]
    [SerializeField] private float _delay = 0.2f;
    [Tooltip("TutorialManager가 아직 없을 때 몇 초까지 기다릴지.")]
    [SerializeField] private float _managerWaitTimeout = 3f;

    private Coroutine _playRoutine;

    private void Start()
    {
        if (_timing == TriggerTiming.OnStart)
            Play();
    }

    private void OnEnable()
    {
        // Start보다 먼저 도는 첫 OnEnable은 Start에 맡긴다.
        if (_timing == TriggerTiming.OnEnable && didStart)
            Play();
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
    }

    /// <summary>버튼 OnClick 등에서 직접 호출할 수 있다.</summary>
    public void Play()
    {
        if (_sequence == null)
        {
            Debug.LogWarning($"[Tutorial] {name}의 TutorialTrigger에 시퀀스가 비어 있다.");
            return;
        }

        if (_sequence.PlayOnce && TutorialManager.IsCompleted(_sequence.SequenceId))
            return;

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        float waited = 0f;

        while (TutorialManager.Instance == null)
        {
            if (waited >= _managerWaitTimeout)
            {
                Debug.LogWarning($"[Tutorial] TutorialManager를 찾지 못해 '{_sequence.SequenceId}'를 재생하지 못했다.");
                _playRoutine = null;
                yield break;
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        float time = 0f;

        while (time < _delay)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        TutorialManager.Instance.Play(_sequence);
        _playRoutine = null;
    }
}
