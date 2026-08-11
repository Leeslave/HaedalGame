using System.Collections.Generic;
using UnityEngine;

/// <summary>하이라이트 테두리 모양.</summary>
public enum TutorialHighlightShape
{
    Circle,     // 원형 (타깃을 감싸는 정사각 크기)
    Rectangle,  // 타깃 사각형에 맞춤
    None        // 테두리 없이 구멍만 뚫는다
}

/// <summary>다음 스텝으로 넘어가는 조건.</summary>
public enum TutorialAdvanceMode
{
    AnyClick,    // 화면 아무 곳이나 클릭
    TargetClick, // 하이라이트된 타깃을 클릭해야 진행 (타깃과 실제 상호작용 가능)
    Delay,       // 지정한 시간이 지나면 자동 진행
    Event        // 코드에서 TutorialManager.Instance.NotifyEvent(키) 호출 시 진행
}

/// <summary>안내 문구 묶음을 화면 어디에 배치할지.</summary>
public enum TutorialTextPlacement
{
    Auto,   // 타깃 기준으로 여백이 넓은 쪽(위/아래)에 자동 배치
    Above,  // 타깃 위
    Below,  // 타깃 아래
    Fixed   // 프리팹에 배치해둔 위치 그대로 사용
}

/// <summary>
/// 튜토리얼 한 스텝(팝업 한 장)의 데이터.
/// "무엇을 강조할지 / 무슨 문구를 띄울지 / 언제 넘어갈지"만 정의하고, 표현은 TutorialHighlightView가 담당한다.
/// </summary>
[CreateAssetMenu(fileName = "TutorialStepSO", menuName = "Game Data/Tutorial/Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [Header("타깃")]
    [Tooltip("강조할 대상의 키. TutorialTarget 컴포넌트의 Key와 맞춘다. 비우면 타깃 없이 전체 딤 + 문구만 표시한다.")]
    [SerializeField] private string _targetKey;
    [Tooltip("타깃이 아직 화면에 없을 때 몇 초까지 기다릴지. 시간이 지나면 타깃 없이 문구만 표시한다.")]
    [SerializeField] private float _targetWaitTimeout = 3f;
    [Tooltip("하이라이트 영역을 얼마나 여유 있게 잡을지(캔버스 단위).")]
    [SerializeField] private Vector2 _highlightPadding = new Vector2(24f, 24f);
    [SerializeField] private TutorialHighlightShape _shape = TutorialHighlightShape.Circle;

    [Header("문구")]
    [SerializeField] private string _titleText = "";
    [SerializeField, TextArea(2, 5)] private string _descriptionText = "";
    [Tooltip("진행 방법 안내. 예) 아무 곳이나 클릭하여 다음으로")]
    [SerializeField] private string _instructionText = "아무 곳이나 클릭하여 다음으로";
    [Tooltip("안내문 아래에 원하는 만큼 붙일 부연 설명 줄. 프리팹의 Extra Line Prefab으로 생성된다.")]
    [SerializeField, TextArea(1, 3)] private List<string> _extraLines = new List<string>();
    [SerializeField] private TutorialTextPlacement _textPlacement = TutorialTextPlacement.Auto;
    [Tooltip("배치 계산 후 추가로 밀어줄 오프셋(캔버스 단위).")]
    [SerializeField] private Vector2 _textOffset = Vector2.zero;

    [Header("진행 조건")]
    [SerializeField] private TutorialAdvanceMode _advanceMode = TutorialAdvanceMode.AnyClick;
    [Tooltip("스텝이 뜬 뒤 이 시간이 지나야 클릭을 입력으로 받는다. (뜨자마자 넘어가는 것 방지)")]
    [SerializeField] private float _inputDelay = 0.35f;
    [Tooltip("Delay 모드일 때 자동으로 넘어가기까지의 시간.")]
    [SerializeField] private float _autoAdvanceDelay = 2f;
    [Tooltip("Event 모드에서 기다릴 이벤트 키. 다른 모드에서도 이 이벤트가 오면 함께 진행된다.")]
    [SerializeField] private string _completeEventKey = "";
    [Tooltip("타깃을 실제로 조작할 수 있게 할지. TargetClick 모드는 항상 켜진 것으로 취급한다.")]
    [SerializeField] private bool _allowTargetInteraction = false;
    [Tooltip("이 스텝에서 건너뛰기 버튼을 보여줄지.")]
    [SerializeField] private bool _showSkipButton = true;

    [Header("연출")]
    [Tooltip("이전 스텝이 끝나고 이 스텝이 뜨기까지의 대기 시간.")]
    [SerializeField] private float _startDelay = 0f;

    public string TargetKey => _targetKey;
    public float TargetWaitTimeout => Mathf.Max(0f, _targetWaitTimeout);
    public Vector2 HighlightPadding => _highlightPadding;
    public TutorialHighlightShape Shape => _shape;

    public string TitleText => _titleText;
    public string DescriptionText => _descriptionText;
    public string InstructionText => _instructionText;
    public IReadOnlyList<string> ExtraLines => _extraLines;
    public TutorialTextPlacement TextPlacement => _textPlacement;
    public Vector2 TextOffset => _textOffset;

    public TutorialAdvanceMode AdvanceMode => _advanceMode;
    public float InputDelay => Mathf.Max(0f, _inputDelay);
    public float AutoAdvanceDelay => Mathf.Max(0f, _autoAdvanceDelay);
    public string CompleteEventKey => _completeEventKey;
    public bool ShowSkipButton => _showSkipButton;
    public float StartDelay => Mathf.Max(0f, _startDelay);

    /// <summary>타깃을 실제로 클릭할 수 있어야 하는 스텝인지.</summary>
    public bool AllowTargetInteraction => _allowTargetInteraction || _advanceMode == TutorialAdvanceMode.TargetClick;

    public bool HasTarget => !string.IsNullOrEmpty(_targetKey);
}
