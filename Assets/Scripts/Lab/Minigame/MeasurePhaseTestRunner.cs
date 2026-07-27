using UnityEngine;

/// <summary>
/// [디버그 전용] MeasurePhaseController를 매니저/레시피 없이 단독으로 돌려보는 러너.
/// 빈 오브젝트에 붙이고 Play → 자동 시작(또는 스페이스/우클릭 Run) → 계량 페이즈 진행.
/// 연출·판정을 빠르게 반복 확인하는 용도. 실제 연결은 MinigameManager가 담당.
/// </summary>
public class MeasurePhaseTestRunner : MonoBehaviour
{
    [SerializeField] private MeasurePhaseController _controller;
    [SerializeField] private MeasurePhaseSO _phase;
    [SerializeField] private RecipeDatabaseSO _database; // 선택: MeasureTarget이 ingredientId로 이름 참조할 때

    [SerializeField] private KeyCode _runKey = KeyCode.Space;
    [SerializeField] private bool _runOnStart = true;

    private bool _subscribed;

    private void Start()
    {
        if (_runOnStart)
            Run();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_runKey))
            Run();
    }

    [ContextMenu("Run Measure Phase")]
    public void Run()
    {
        if (_controller == null || _phase == null)
        {
            Debug.LogError("[MeasureTest] _controller 또는 _phase 미할당", this);
            return;
        }

        Subscribe();

        MinigameContext context = new MinigameContext
        {
            Database = _database
        };

        int count = _phase.Measures != null ? _phase.Measures.Count : 0;
        Debug.Log($"[MeasureTest] ▶ 계량 페이즈 시작: {_phase.PhaseName} (재료 {count}개)");

        _controller.Begin(_phase, context);
    }

    private void Subscribe()
    {
        if (_subscribed || _controller == null)
            return;

        _controller.OnCompleted += HandleCompleted;
        _subscribed = true;
    }

    private void HandleCompleted(PhaseScoreResult result)
    {
        Debug.Log($"[MeasureTest] ◀ 완료: {result.PhaseName} / 점수 {result.Score:0.0} / 통과 {(result.Score >= 4f ? "✓" : "✗")}");
    }

    private void OnDestroy()
    {
        if (_controller != null && _subscribed)
            _controller.OnCompleted -= HandleCompleted;
    }
}
