using System.Collections;
using UnityEngine;

// 주방 알바. KitchenSystem이 배정한 조리 작업을 상태 머신으로 순서대로 처리한다.
public class ChefAgent : PartTimerAgent
{
    private enum ChefState
    {
        Idle,
        WaitingForTool,
        ApproachingStation,
        Cooking,
        ApproachingDropoff,
        DroppingOffFood,
        Returning
    }

    [SerializeField] private PartTimerStatus status;
    [ReadOnly][SerializeField] private ChefState state;

    private CookingTask curTask;
    private CookingType curType;
    private Transform stationTransform;
    private Coroutine stateCoroutine;

    public void Initialize(int index)
    {
        positionNumber = index;
        initPosition = ChefManager.Instance.GetInitPosition(index);
        state = ChefState.Idle;
        KitchenSystem.Instance.RequestTask(this);
    }

    // KitchenSystem이 유휴 등록 직후 호출한다. 대기 위치에서 멀면 복귀 이동을 시작한다.
    public void GoIdle()
    {
        if (Vector2.Distance(transform.position, initPosition) > arrivalThreshold)
        {
            Interrupt(ChefState.Returning, ReturningRoutine());
        }
        else
        {
            Interrupt(ChefState.Idle, null);
        }
    }

    // KitchenSystem이 작업을 배정할 때 호출한다.
    public void AssignTask(CookingTask task)
    {
        curTask = task;
        curType = task.GetCookingType();
        stationTransform = curType != CookingType.None ? ChefManager.Instance.GetCookingToolTransform(curType) : null;

        if (stationTransform == null)
        {
            FinishTask();
            return;
        }

        if (ChefManager.Instance.RequestTool(curType, this))
        {
            Interrupt(ChefState.ApproachingStation, ApproachingStationRoutine());
        }
        else
        {
            Interrupt(ChefState.WaitingForTool, WaitingForToolRoutine());
        }
    }

    // ChefManager가 대기 중이던 도구를 배정해줄 때 호출하는 콜백.
    public void OnToolGranted(CookingType type)
    {
        Interrupt(ChefState.ApproachingStation, ApproachingStationRoutine());
    }

    // 외부(KitchenSystem/ChefManager)에서 지금 진행 중인 걸 끊고 새 상태로 전환할 때 사용.
    // WaitingForTool/Returning처럼 독립적으로 돌고 있는 코루틴을 중단시켜야 하므로 StopCoroutine이 필요하다.
    private void Interrupt(ChefState next, IEnumerator routine)
    {
        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
            stateCoroutine = null;
        }
        Advance(next, routine);
    }

    // 상태 코루틴 "자기 자신 안"에서 다음 상태로 넘어갈 때 사용.
    // 지금 실행 중인 코루틴을 스스로 StopCoroutine 하면 Unity 코루틴 스케줄러가 꼬여
    // 네이티브 할당자 오류/프레임 드랍을 유발할 수 있으므로 여기서는 절대 멈추지 않는다.
    private void Advance(ChefState next, IEnumerator routine)
    {
        state = next;
        stateCoroutine = routine != null ? StartCoroutine(routine) : null;
    }

    private IEnumerator ReturningRoutine()
    {
        yield return MoveTo(initPosition, status.serving);
    }

    // 도구가 비워질 때까지 대기(폴링 없음). 대기 중엔 A*로 대기 위치 쪽에 머무른다.
    private IEnumerator WaitingForToolRoutine()
    {
        yield return MoveTo(initPosition, status.serving);
    }

    private IEnumerator ApproachingStationRoutine()
    {
        yield return new WaitForSeconds(1f); // 조리 시작 준비 연출
        if (curTask.Customer != null)
        {
            TaskLogger.Instance.LogCooking($"현재 {positionNumber}번째 주방 직원이 {curTask.Customer.coc.GetOrderData().RecipeName}주문을 받았습니다.");
        }

        yield return MoveTo(stationTransform.position, status.serving);

        if (curTask.Customer == null)
        {
            ChefManager.Instance.ReleaseTool(curType);
            FinishTask();
            yield break;
        }

        Advance(ChefState.Cooking, CookingRoutine());
    }

    private IEnumerator CookingRoutine()
    {
        yield return new WaitForSeconds(curTask.CookingTime);
        ChefManager.Instance.ReleaseTool(curType);
        Advance(ChefState.ApproachingDropoff, ApproachingDropoffRoutine());
    }

    private IEnumerator ApproachingDropoffRoutine()
    {
        yield return MoveTo(ChefManager.Instance.GetKitchenPosition(), status.serving);

        state = ChefState.DroppingOffFood;
        if (curTask.Customer == null)
        {
            Debug.Log("요리가 완성되었지만 손님이 가셔서 폐기처분 했습니다.");
        }
        else
        {
            KitchenSystem.Instance.CompleteFood(curTask);
        }
        FinishTask();
    }

    private void FinishTask()
    {
        curTask = null;
        stationTransform = null;
        KitchenSystem.Instance.RequestTask(this);
    }
}
