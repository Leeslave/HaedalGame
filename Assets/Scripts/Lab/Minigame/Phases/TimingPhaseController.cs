using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이밍 페이즈 컨트롤러 (Grill/Boil/StirFry/Fry 공용).
/// 흐름: 필요 재료를 팬(드롭존)에 드래그 → [요리시작] 활성화 → 클릭 시 익힘 진행
///  → 5단계 판정 구간 중 현재 익힘 단계가 포커스됨 → [꺼내기] 클릭 시 그 단계로 판정
///  → gaugeCount 만큼 반복 → 평균 점수로 자동 완료. 끝까지 안 꺼내면 오버쿡(1점).
/// 어떤 액션 타입을 담당할지는 인스펙터에서 지정한다 (로직 동일, 도구 그림만 다름).
/// </summary>
public class TimingPhaseController : DragDropPhaseController
{
    [Header("담당 액션 타입 (Grill/Boil/StirFry/Fry)")]
    [SerializeField] private CookingActionType _timingActionType = CookingActionType.Grill;

    [Header("Cook UI")]
    // 요리시작/꺼내기 겸용 버튼. 재료 전부 배치 시 활성화된다.
    [SerializeField] private Button _cookButton;
    [SerializeField] private TMP_Text _cookButtonLabel;

    // 5단계 판정 구간 칸 (설익음~오버쿡 순서로 5개). 항상 모두 보이고,
    // 현재 익힘 단계 칸만 확대되어 포커스를 표현한다.
    [SerializeField] private RectTransform[] _zoneSegments = new RectTransform[5];

    // 포커스된 칸의 확대 배율
    [SerializeField] private float _focusedScale = 1.2f;

    [SerializeField] private Image _foodImage;        // 익힘 단계 이미지 (선택)
    [SerializeField] private TMP_Text _judgementText; // 판정 결과 표시 (선택)

    [Header("판정 구간 경계 (0~1, 오름차순 4개)")]
    // [0]설익음끝 [1]약간익음끝 [2]익음끝 [3]살짝오버쿡끝. 기본: 익음 = 45~55%
    [SerializeField] private float[] _zoneBoundaries = { 0.35f, 0.45f, 0.55f, 0.65f };

    // 마지막 판정을 보여주고 다음으로 넘어가기까지의 대기 시간
    [SerializeField] private float _judgementShowTime = 0.8f;

    private const string StartLabel = "요리시작";
    private const string TakeOutLabel = "꺼내기";

    public override CookingActionType ActionType => _timingActionType;

    private readonly List<TimingJudgement> _roundJudgements = new List<TimingJudgement>();
    private readonly List<float> _roundScores = new List<float>();

    private Coroutine _cookRoutine;
    private bool _cooking;       // 현재 게이지 진행 중인가
    private bool _takeOut;       // 꺼내기 요청
    private bool _roundsDone;    // 전 라운드 종료
    private int _completedRounds;

    protected override void OnBegin()
    {
        _roundJudgements.Clear();
        _roundScores.Clear();
        _cooking = false;
        _takeOut = false;
        _roundsDone = false;
        _completedRounds = 0;

        if (_cookButton != null)
        {
            _cookButton.onClick.AddListener(OnClickCook);
            _cookButton.interactable = false; // 재료를 전부 배치해야 활성화
        }

        SetCookLabel(StartLabel);
        SetZoneFocus(-1);

        if (_judgementText != null)
            _judgementText.text = string.Empty;

        base.OnBegin(); // 재료 스폰 + 드롭존/완료버튼 셋업
    }

    /// <summary>재료를 전부 팬에 올리면 요리시작 버튼이 켜진다.</summary>
    protected override void OnAllItemsPlaced()
    {
        if (_cookButton != null)
            _cookButton.interactable = true;
    }

    /// <summary>전부 배치 + 전 라운드 판정 완료가 페이즈 목표.</summary>
    protected override bool IsGoalMet()
    {
        return base.IsGoalMet() && _roundsDone;
    }

    /// <summary>페이즈 점수 = 라운드 판정 점수의 평균 (baseScore 미사용).</summary>
    protected override PhaseScoreResult BuildResult()
    {
        float sum = 0f;
        for (int i = 0; i < _roundScores.Count; i++)
            sum += _roundScores[i];

        float average = _roundScores.Count > 0 ? sum / _roundScores.Count : 0f;

        PhaseScoreResult result = new PhaseScoreResult(
            Phase != null ? Phase.PhaseName : ActionType.ToString(),
            average);

        result.TimingResults.AddRange(_roundJudgements);
        return result;
    }

    private void OnClickCook()
    {
        if (_roundsDone)
            return;

        if (!_cooking)
            _cookRoutine = StartCoroutine(RunCookRound()); // 요리시작
        else
            _takeOut = true; // 꺼내기
    }

    private IEnumerator RunCookRound()
    {
        TimingPhaseSO timingPhase = Phase as TimingPhaseSO;

        float speed = timingPhase != null ? Mathf.Max(0.01f, timingPhase.GaugeSpeed) : 0.5f;
        int totalRounds = timingPhase != null ? Mathf.Max(1, timingPhase.GaugeCount) : 1;

        _cooking = true;
        _takeOut = false;
        SetCookLabel(TakeOutLabel);

        if (_judgementText != null)
            _judgementText.text = string.Empty;

        float value = 0f;
        SetZoneFocus(GetZoneIndex(0f));
        UpdateCookSprite(timingPhase, 0f);

        // 익힘 진행: 꺼내거나 끝까지 가면 정지.
        while (!_takeOut && value < 1f)
        {
            value += speed * Time.deltaTime;
            value = Mathf.Min(value, 1f);

            SetZoneFocus(GetZoneIndex(value));
            UpdateCookSprite(timingPhase, value);

            yield return null;
        }

        // 끝까지 안 꺼냈으면 오버쿡 취급.
        TimingJudgement judgement = _takeOut ? Judge(value) : TimingJudgement.Overcooked;

        _roundJudgements.Add(judgement);
        _roundScores.Add(GetScore(judgement));

        if (_judgementText != null)
            _judgementText.text = GetJudgementLabel(judgement);

        RaiseTimingResult(judgement);

        _cooking = false;
        _completedRounds++;

        if (_completedRounds < totalRounds)
        {
            // 다음 라운드(예: 뒷면)는 다시 요리시작 클릭으로.
            SetCookLabel(StartLabel);
        }
        else
        {
            _roundsDone = true;

            if (_cookButton != null)
                _cookButton.interactable = false;

            // 마지막 판정을 잠깐 보여준 뒤 완료 (완료 버튼이 연결돼 있으면 버튼 대기).
            yield return new WaitForSeconds(_judgementShowTime);

            _cookRoutine = null;
            CheckGoal();
            yield break;
        }

        _cookRoutine = null;
    }

    /// <summary>현재 익힘 정도에 해당하는 판정 구간 칸을 확대한다. index -1이면 전부 원래 크기.</summary>
    private void SetZoneFocus(int index)
    {
        if (_zoneSegments == null)
            return;

        for (int i = 0; i < _zoneSegments.Length; i++)
        {
            if (_zoneSegments[i] == null)
                continue;

            float scale = i == index ? _focusedScale : 1f;
            _zoneSegments[i].localScale = new Vector3(scale, scale, 1f);
        }
    }

    private int GetZoneIndex(float value)
    {
        if (_zoneBoundaries == null || _zoneBoundaries.Length < 4)
            return 0;

        if (value < _zoneBoundaries[0]) return 0;
        if (value < _zoneBoundaries[1]) return 1;
        if (value < _zoneBoundaries[2]) return 2;
        if (value < _zoneBoundaries[3]) return 3;
        return 4;
    }

    private TimingJudgement Judge(float value)
    {
        switch (GetZoneIndex(value))
        {
            case 0: return TimingJudgement.Undercooked;
            case 1: return TimingJudgement.SlightlyUnder;
            case 2: return TimingJudgement.Perfect;
            case 3: return TimingJudgement.SlightlyOver;
            default: return TimingJudgement.Overcooked;
        }
    }

    private static float GetScore(TimingJudgement judgement)
    {
        switch (judgement)
        {
            case TimingJudgement.Perfect: return 5f;
            case TimingJudgement.SlightlyUnder:
            case TimingJudgement.SlightlyOver: return 3f;
            default: return 1f;
        }
    }

    private static string GetJudgementLabel(TimingJudgement judgement)
    {
        switch (judgement)
        {
            case TimingJudgement.Undercooked: return "설익음";
            case TimingJudgement.SlightlyUnder: return "약간 익음";
            case TimingJudgement.Perfect: return "익음!";
            case TimingJudgement.SlightlyOver: return "살짝 오버쿡";
            case TimingJudgement.Overcooked: return "오버쿡";
            default: return string.Empty;
        }
    }

    /// <summary>익힘 진행률에 맞춰 음식 이미지를 교체한다 (예: 생연어 → 노릇 → 탄).</summary>
    private void UpdateCookSprite(TimingPhaseSO timingPhase, float value)
    {
        if (_foodImage == null || timingPhase == null)
            return;

        IReadOnlyList<Sprite> stages = timingPhase.CookStageSprites;

        if (stages == null || stages.Count == 0)
            return;

        int index = Mathf.Clamp(Mathf.FloorToInt(value * stages.Count), 0, stages.Count - 1);

        if (stages[index] != null && _foodImage.sprite != stages[index])
            _foodImage.sprite = stages[index];
    }

    private void SetCookLabel(string label)
    {
        if (_cookButtonLabel != null)
            _cookButtonLabel.text = label;
    }

    protected override void OnCleanup()
    {
        if (_cookRoutine != null)
        {
            StopCoroutine(_cookRoutine);
            _cookRoutine = null;
        }

        if (_cookButton != null)
            _cookButton.onClick.RemoveListener(OnClickCook);
    }
}
