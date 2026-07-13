using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [디버그 전용] UI 없이 MinigameManager 척추를 돌려보는 임시 러너.
/// 빈 GameObject에 붙이고 Play → 스페이스(또는 컴포넌트 우클릭 "Run Minigame") → 콘솔 확인.
/// M4 이후 실제 UI/개방 팝업으로 교체 예정.
/// </summary>
public class MinigameTestRunner : MonoBehaviour
{
    [SerializeField] private MinigameManager _manager;
    [SerializeField] private RecipeDatabaseSO _recipeDatabase;
    [SerializeField] private int _testRecipeId = 11;

    [Header("언락 초기화용 (테스트)")]
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private RecipeUnlockSaveService _saveService;

    [Tooltip("실행 전에 필요한 재료를 인벤토리에 자동 충전 (재료 부족으로 막히지 않게)")]
    [SerializeField] private bool _autoFillIngredients = true;

    [SerializeField] private KeyCode _runKey = KeyCode.Space;

    // 미니게임 실행 없이 테스트 레시피의 재료만 인벤토리에 채운다 (버튼 흐름 테스트용)
    [SerializeField] private KeyCode _fillKey = KeyCode.F;

    private bool _subscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_runKey))
            Run();

        if (Input.GetKeyDown(_fillKey))
            FillOnly();
    }

    [ContextMenu("Fill Ingredients Only")]
    public void FillOnly()
    {
        FillIngredients(_testRecipeId);
        Debug.Log($"[Test] 재료 충전 완료 (recipeId={_testRecipeId})");
    }

    /// <summary>언락 세이브를 삭제하고 기본 개방 상태로 되돌린다 (defaultUnlock 레시피만 개방).</summary>
    [ContextMenu("Reset Unlocked Recipes")]
    public void ResetUnlockedRecipes()
    {
        if (_saveService != null)
            _saveService.DeleteSave();

        if (_recipeBookState != null)
            _recipeBookState.Initialize(); // 세이브 없음 → 기본 개방만 다시 구성 + OnChanged로 UI 갱신

        Debug.Log("[Test] 언락 레시피 초기화 완료 (기본 개방 상태)");
    }

    [ContextMenu("Run Minigame")]
    public void Run()
    {
        if (_manager == null)
        {
            Debug.LogError("[Test] MinigameManager 미할당");
            return;
        }

        if (_manager.IsRunning)
        {
            Debug.Log("[Test] 이미 진행 중");
            return;
        }

        Subscribe();

        if (!_manager.Initialize(_testRecipeId))
        {
            Debug.LogWarning($"[Test] Initialize 실패 (recipeId={_testRecipeId})");
            return;
        }

        if (_autoFillIngredients)
            FillIngredients(_testRecipeId);

        if (!_manager.StartMinigame())
            Debug.LogWarning("[Test] StartMinigame 실패 (재료 부족 등)");
    }

    /// <summary>레시피 필요 재료를 인벤토리에 부족분만큼 채워 차감이 성공하도록 한다.</summary>
    private void FillIngredients(int recipeId)
    {
        IngredientInventoryService inv = IngredientInventoryService.Instance;

        if (inv == null)
        {
            Debug.LogWarning("[Test] IngredientInventoryService.Instance 없음 - 재료 충전 스킵");
            return;
        }

        if (_recipeDatabase == null || !_recipeDatabase.TryGetRecipe(recipeId, out RecipeData recipe))
        {
            Debug.LogWarning("[Test] RecipeDatabase 미할당 또는 레시피 없음 - 재료 충전 스킵");
            return;
        }

        IReadOnlyList<RecipeIngredientRequirement> reqs = recipe.Requirements;

        for (int i = 0; i < reqs.Count; i++)
        {
            RecipeIngredientRequirement req = reqs[i];
            int missing = req.Amount - inv.GetCount(req.IngredientId);

            if (missing > 0)
                inv.Add(req.IngredientId, missing, "MinigameTest");
        }
    }

    private void Subscribe()
    {
        if (_subscribed || _manager == null)
            return;

        _manager.OnMinigameStarted += HandleStarted;
        _manager.OnPhaseStarted += HandlePhaseStarted;
        _manager.OnPhaseCompleted += HandlePhaseCompleted;
        _manager.OnScoreCalculated += HandleScoreCalculated;
        _manager.OnMinigameEnded += HandleEnded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _manager == null)
            return;

        _manager.OnMinigameStarted -= HandleStarted;
        _manager.OnPhaseStarted -= HandlePhaseStarted;
        _manager.OnPhaseCompleted -= HandlePhaseCompleted;
        _manager.OnScoreCalculated -= HandleScoreCalculated;
        _manager.OnMinigameEnded -= HandleEnded;
        _subscribed = false;
    }

    private void HandleStarted(RecipeData recipe)
    {
        Debug.Log($"[Test] ▶ 미니게임 시작: {recipe?.RecipeName}");
    }

    private void HandlePhaseStarted(PhaseSO phase, int index, int total)
    {
        Debug.Log($"[Test]   페이즈 {index + 1}/{total} 시작: {phase.PhaseName} ({phase.ActionType})");
    }

    private void HandlePhaseCompleted(PhaseSO phase, PhaseScoreResult result)
    {
        Debug.Log($"[Test]   페이즈 완료: {result.PhaseName} / 점수 {result.Score:0.0} / 통과 {(result.Passed ? "✓" : "✗")}");
    }

    private void HandleScoreCalculated(MinigameResult result)
    {
        Debug.Log($"[Test] ■ 채점: 평균 {result.AverageScore:0.0} / 성공 {(result.Success ? "✓" : "✗")}");
    }

    private void HandleEnded(MinigameResult result)
    {
        Debug.Log($"[Test] ◀ 미니게임 종료 (페이즈 {result.PhaseResults.Count}개)");
    }
}
