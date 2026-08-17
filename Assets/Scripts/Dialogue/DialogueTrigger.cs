using System.Collections;
using UnityEngine;

/// <summary>
/// 코드를 건드리지 않고 대사를 시작시키는 컴포넌트.
/// JSON 파일을 직접 물리거나 Resources 경로만 적어두고, 오브젝트가 켜질 때 자동으로 재생시킬 수 있다.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerTiming
    {
        OnEnable, // 오브젝트가 켜질 때마다 시도 (한 번만 볼 대사는 알아서 무시된다)
        OnStart,  // 최초 1회
        Manual    // Play()를 직접 호출
    }

    [Tooltip("재생할 JSON 파일. 비우면 아래 Resource Path를 쓴다.")]
    [SerializeField] private TextAsset _scriptAsset;

    [Tooltip("Resources 아래의 JSON 경로. 확장자는 빼고 적는다. 예) Dialogue/intro_day1")]
    [SerializeField] private string _resourcePath = "";

    [SerializeField] private TriggerTiming _timing = TriggerTiming.OnStart;

    [Tooltip("UI 레이아웃이 잡힌 뒤 시작하도록 약간 늦춘다.")]
    [SerializeField] private float _delay = 0.2f;

    [Tooltip("DialogueManager가 아직 없을 때 몇 초까지 기다릴지.")]
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
        if (_scriptAsset == null && string.IsNullOrWhiteSpace(_resourcePath))
        {
            Debug.LogWarning($"[Dialogue] {name}의 DialogueTrigger에 재생할 대사가 지정되지 않았다.");
            return;
        }

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        float waited = 0f;

        while (DialogueManager.Instance == null)
        {
            if (waited >= _managerWaitTimeout)
            {
                Debug.LogWarning($"[Dialogue] DialogueManager를 찾지 못해 {name}의 대사를 재생하지 못했다.");
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

        if (_scriptAsset != null)
            DialogueManager.Instance.Play(_scriptAsset);
        else
            DialogueManager.Instance.PlayFromResources(_resourcePath);

        _playRoutine = null;
    }
}
