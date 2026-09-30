using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 스텝을 순서대로 묶은 단위. (예: 첫 접속 튜토리얼, 대장간 첫 진입 안내)
/// SequenceId 기준으로 완료 여부를 저장해서 한 번만 재생할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "TutorialSequenceSO", menuName = "Game Data/Tutorial/Tutorial Sequence")]
public class TutorialSequenceSO : ScriptableObject
{
    [Tooltip("저장/조회에 쓰이는 고유 아이디. 비우면 에셋 이름을 사용한다.")]
    [SerializeField] private string _sequenceId = "";
    [Tooltip("한 번 완료하면 다시 재생하지 않는다.")]
    [SerializeField] private bool _playOnce = true;
    [Tooltip("재생 중 게임 시간을 멈출지. (연출은 unscaledTime으로 돌아가므로 안전하다)")]
    [SerializeField] private bool _pauseGameTime = false;
    [SerializeField] private List<TutorialStepSO> _steps = new List<TutorialStepSO>();

    public string SequenceId => string.IsNullOrEmpty(_sequenceId) ? name : _sequenceId;
    public bool PlayOnce => _playOnce;
    public bool PauseGameTime => _pauseGameTime;
    public IReadOnlyList<TutorialStepSO> Steps => _steps;
    public int StepCount => _steps.Count;
}
