using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 새 게임 시작 연출. 실제 재생 완료 이벤트로 끝을 판단한다. (시간을 재서 추정하지 않는다)
///  1) PlayableDirector가 연결되어 있으면 Timeline 재생 → stopped 이벤트
///  2) 없으면 대화 시스템으로 임시 인트로 스크립트 재생 → 재생 완료 콜백
/// 스킵을 넣게 되면 Skip()을 호출하면 된다. 완료 처리는 같은 경로로 한 번만 실행된다.
/// </summary>
public class IntroController : MonoBehaviour
{
    [Header("Timeline (실제 연출이 준비되면 연결)")]
    [SerializeField] private PlayableDirector _director;
    [Tooltip("인트로 재생 중에만 켜 둘 오브젝트 (연출 캔버스 등)")]
    [SerializeField] private GameObject _root;

    [Header("임시 인트로 (Timeline이 없을 때)")]
    [Tooltip("Resources 기준 대화 스크립트 경로. 씬(또는 DDOL)에 DialogueManager가 있어야 한다.")]
    [SerializeField] private string _fallbackDialoguePath = "Dialogue/intro_day1";

    private Action _onFinished;
    private bool _playing;
    private bool _finished;

    public bool IsPlaying => _playing;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void Play(Action onFinished)
    {
        if (_playing || _finished)
            return;

        _playing = true;
        _onFinished = onFinished;
        GameFlow.SetPhase(GamePhase.Intro);

        if (_root != null)
            _root.SetActive(true);

        if (_director != null)
        {
            if (_director.extrapolationMode == DirectorWrapMode.Hold || _director.extrapolationMode == DirectorWrapMode.Loop)
                Debug.LogWarning("[Intro] PlayableDirector의 Wrap Mode가 None이어야 재생 종료(stopped) 이벤트가 발생합니다. 재생 시간이 끝나면 종료로 처리합니다.");

            _director.stopped += HandleDirectorStopped;
            _director.time = 0;
            _director.Play();
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.PlayFromResources(_fallbackDialoguePath, Finish))
            return;

        Debug.LogWarning("[Intro] 재생할 Timeline이나 대화 시스템(DialogueManager)이 없어 인트로를 건너뜁니다.");
        Finish();
    }

    public void Skip()
    {
        if (!_playing || _finished)
            return;

        if (_director != null)
        {
            _director.Stop(); // stopped 이벤트로 Finish가 호출된다.
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
        {
            DialogueManager.Instance.Stop(); // 재생 완료 콜백으로 Finish가 호출된다.
            return;
        }

        Finish();
    }

    private void Update()
    {
        // Wrap Mode가 Hold/Loop이면 stopped가 오지 않으므로 재생 위치가 끝에 닿았을 때 종료로 본다.
        if (_playing && !_finished && _director != null
            && _director.extrapolationMode != DirectorWrapMode.None
            && _director.time >= _director.duration)
        {
            _director.Stop();
            Finish();
        }
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        Finish();
    }

    private void Finish()
    {
        if (_finished)
            return; // 완료 이벤트 재진입 차단

        _finished = true;
        _playing = false;

        if (_director != null)
            _director.stopped -= HandleDirectorStopped;

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (_director != null)
            _director.stopped -= HandleDirectorStopped;
    }
}
