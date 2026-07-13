using System;
using UnityEngine;

/// <summary>
/// 미니게임 진행 중 페이즈 컨트롤러에 전달되는 공용 컨텍스트.
/// </summary>
public class MinigameContext
{
    public RecipeData Recipe;
    public RecipeDatabaseSO Database;
    public IngredientInventoryService Inventory;
}

/// <summary>
/// 페이즈 컨트롤러 추상 베이스 (기획서 7.4).
/// 실제 인터랙션/채점 로직은 상속 클래스(Chop/Mix/Grill/... M4·M5)가 구현하고,
/// MinigameManager는 ActionType으로 컨트롤러를 찾아 Begin() → OnCompleted 대기만 한다.
/// </summary>
public abstract class MinigamePhaseController : MonoBehaviour
{
    /// <summary>이 컨트롤러가 담당하는 액션 타입. 매니저의 페이즈-컨트롤러 매칭 키.</summary>
    public abstract CookingActionType ActionType { get; }

    /// <summary>페이즈 완료 시 채점 결과와 함께 발행.</summary>
    public event Action<PhaseScoreResult> OnCompleted;

    /// <summary>재료 배치 이벤트 (ingredientId, 정상 재료 여부). 아트/사운드 훅용(기획서 7.1).</summary>
    public event Action<int, bool> OnIngredientPlaced;

    /// <summary>타이밍 게이지 판정 이벤트. 타이밍 페이즈만 발행.</summary>
    public event Action<TimingJudgement> OnTimingResult;

    protected PhaseSO Phase { get; private set; }
    protected MinigameContext Context { get; private set; }

    /// <summary>매니저가 페이즈 진입 시 호출. 컨트롤러를 활성화하고 OnBegin()으로 위임한다.</summary>
    public void Begin(PhaseSO phase, MinigameContext context)
    {
        Phase = phase;
        Context = context;
        gameObject.SetActive(true);
        OnBegin();
    }

    /// <summary>상속 클래스가 실제 페이즈 준비/시작 로직을 구현.</summary>
    protected abstract void OnBegin();

    /// <summary>포기/중단 시 매니저가 호출.</summary>
    public virtual void Abort()
    {
        gameObject.SetActive(false);
    }

    protected void RaiseIngredientPlaced(int ingredientId, bool isCorrect)
        => OnIngredientPlaced?.Invoke(ingredientId, isCorrect);

    protected void RaiseTimingResult(TimingJudgement judgement)
        => OnTimingResult?.Invoke(judgement);

    /// <summary>상속 클래스가 페이즈 완료 시 호출. 컨트롤러를 비활성화하고 결과를 발행한다.</summary>
    protected void Complete(PhaseScoreResult result)
    {
        gameObject.SetActive(false);
        OnCompleted?.Invoke(result);
    }
}
