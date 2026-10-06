using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 미니게임 오케스트레이터 (기획서 7.3).
/// Initialize(레시피 조회) → StartMinigame(재료 차감 + 페이즈 순회) → Evaluate(평균) → 결과 발행.
/// 모든 페이즈 연결을 확인한 뒤 재료를 원자적으로 소비한다.
/// </summary>
public class MinigameManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RecipeDatabaseSO _recipeDatabase;
    [SerializeField] private RecipePhaseDatabaseSO _phaseDatabase;
    [SerializeField] private IngredientInventoryService _inventory; // 비우면 싱글톤 사용

    [Header("Phase Controllers (씬 인스턴스, ActionType별 1개)")]
    [SerializeField] private List<MinigamePhaseController> _phaseControllers = new List<MinigamePhaseController>();

    [Header("HUD")]
    // 상단 페이즈 표시. 예: "Phase 1/4 - 재료 손질"
    [SerializeField] private TMP_Text _phaseTitleText;

    [Header("Rules")]
    [SerializeField] private float _successThreshold = 4f;          // 평균 이 값 이상이면 성공(기획서 4.11)

    // 이벤트 (기획서 7.1)
    public event Action<RecipeData> OnMinigameStarted;
    public event Action<MinigameResult> OnMinigameEnded;
    public event Action<PhaseSO, int, int> OnPhaseStarted;        // (phase, index, total)
    public event Action<PhaseSO, PhaseScoreResult> OnPhaseCompleted;
    public event Action<MinigameResult> OnScoreCalculated;

    private RecipeData _recipe;
    private RecipePhaseSetSO _phaseSet;
    private MinigameContext _context;
    private readonly List<PhaseScoreResult> _results = new List<PhaseScoreResult>();

    private bool _phaseDone;
    private PhaseScoreResult _pendingResult;
    private bool _running;

    public bool IsRunning => _running;

    /// <summary>
    /// 레시피 + 페이즈 시퀀스를 조회한다. 재료 차감은 하지 않는다.
    /// </summary>
    public bool Initialize(int recipeId)
    {
        _recipe = null;
        _phaseSet = null;

        if (_recipeDatabase == null || !_recipeDatabase.TryGetRecipe(recipeId, out _recipe))
        {
            Debug.LogWarning($"[Minigame] Recipe not found. recipeId={recipeId}");
            return false;
        }

        if (_phaseDatabase == null || !_phaseDatabase.TryGetPhaseSet(recipeId, out _phaseSet))
        {
            Debug.LogWarning($"[Minigame] PhaseSet not found. recipeId={recipeId}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 개방 확인 팝업 "네" 시점에 호출. 재료를 일괄 차감하고 미니게임을 시작한다.
    /// 재료 부족 시 차감 없이 false 반환(기획서 6.5).
    /// </summary>
    public bool StartMinigame()
    {
        if (_running)
            return false;

        if (_recipe == null || _phaseSet == null)
        {
            Debug.LogWarning("[Minigame] Not initialized.");
            return false;
        }

        IngredientInventoryService inv = ResolveInventory();

        // 잘못 연결된 실습은 재료를 차감하기 전에 거절한다.
        if (_phaseSet.Phases.Count == 0) return false;
        foreach (var phase in _phaseSet.Phases)
            if (phase == null || ResolveController(phase.ActionType) == null) return false;
        if (inv == null || !inv.TryConsumeRecipe(_recipe, out _)) return false;

        _context = new MinigameContext
        {
            Recipe = _recipe,
            Database = _recipeDatabase,
            Inventory = inv
        };

        _results.Clear();
        _running = true;
        OnMinigameStarted?.Invoke(_recipe);

        StartCoroutine(RunPhases());
        return true;
    }

    private IEnumerator RunPhases()
    {
        IReadOnlyList<PhaseSO> phases = _phaseSet.Phases;
        int total = phases.Count;

        for (int i = 0; i < total; i++)
        {
            PhaseSO phase = phases[i];
            if (phase == null)
                continue;

            if (_phaseTitleText != null)
                _phaseTitleText.text = $"Phase {i + 1}/{total} - {phase.PhaseName}";

            OnPhaseStarted?.Invoke(phase, i, total);

            MinigamePhaseController controller = ResolveController(phase.ActionType);

            if (controller == null) { _running = false; yield break; }
            _phaseDone = false;
            _pendingResult = null;
            controller.OnCompleted += HandlePhaseCompleted;
            controller.Begin(phase, _context);
            while (!_phaseDone) yield return null;
            controller.OnCompleted -= HandlePhaseCompleted;

            PhaseScoreResult result = _pendingResult ?? new PhaseScoreResult(phase.PhaseName, 0f);
            result.Passed = result.Score >= _successThreshold;
            _results.Add(result);

            OnPhaseCompleted?.Invoke(phase, result);
        }

        if (_phaseTitleText != null)
            _phaseTitleText.text = string.Empty;

        MinigameResult final = Evaluate();
        _running = false;

        OnScoreCalculated?.Invoke(final);
        OnMinigameEnded?.Invoke(final);
    }

    private void HandlePhaseCompleted(PhaseScoreResult result)
    {
        _pendingResult = result;
        _phaseDone = true;
    }

    private MinigameResult Evaluate()
    {
        float sum = 0f;
        for (int i = 0; i < _results.Count; i++)
            sum += _results[i].Score;

        float average = _results.Count > 0 ? sum / _results.Count : 0f;

        return new MinigameResult
        {
            Recipe = _recipe,
            PhaseResults = new List<PhaseScoreResult>(_results),
            AverageScore = average,
            Success = _results.Count == _phaseSet.Phases.Count && average >= _successThreshold
        };
    }

    private MinigamePhaseController ResolveController(CookingActionType actionType)
    {
        for (int i = 0; i < _phaseControllers.Count; i++)
        {
            if (_phaseControllers[i] != null && _phaseControllers[i].ActionType == actionType)
                return _phaseControllers[i];
        }

        return null;
    }

    private IngredientInventoryService ResolveInventory()
    {
        return _inventory != null ? _inventory : IngredientInventoryService.Instance;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (var controller in _phaseControllers)
        {
            if (controller == null) continue;
            controller.OnCompleted -= HandlePhaseCompleted;
            if (_running) controller.Abort();
        }
        _running = false;
    }
}
