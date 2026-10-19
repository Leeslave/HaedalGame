using System.Collections;
using UnityEngine;

// 서빙 알바. HallSystem이 배정한 작업(손님 응대)을 상태 머신으로 순서대로 처리한다.
public class ServerAgent : PartTimerAgent
{
    private enum ServerState
    {
        Idle,
        ApproachingCustomerForOrder,
        TakingOrder,
        ApproachingKitchen,
        PickingUpFood,
        ApproachingCustomerWithFood,
        DeliveringFood,
        Returning
    }

    [SerializeField] private PartTimerStatus status;
    [ReadOnly][SerializeField] private ServerState state;

    private ServingTask curTask;
    private Coroutine stateCoroutine;

    // 화면 표시용: 현재 상태 이름, 응대 중인 손님, 들고 있는 음식(픽업 후 서빙 전까지만)
    public string StateLabel
    {
        get
        {
            if (IsSleeping) return "잠듦 · 눌러서 깨우기";
            switch (state)
            {
                case ServerState.ApproachingCustomerForOrder: return "주문 받으러 이동";
                case ServerState.TakingOrder: return "주문 받는 중";
                case ServerState.ApproachingKitchen: return "음식 받으러 이동";
                case ServerState.PickingUpFood: return "음식 받는 중";
                case ServerState.ApproachingCustomerWithFood: return "음식 운반 중";
                case ServerState.DeliveringFood: return "서빙 중";
                case ServerState.Returning: return "대기 위치로";
                default: return "대기 중";
            }
        }
    }
    public CustomerAgent CurrentCustomer => curTask != null ? curTask.Customer : null;
    public RecipeData CarriedFood =>
        state == ServerState.ApproachingCustomerWithFood && curTask != null && curTask.Customer != null
            ? curTask.Customer.coc.GetOrderData()
            : null;

    public void Initialize(int index)
    {
        status = SavedStatus(status);
        positionNumber = index;
        initPosition = ServerManager.Instance.GetInitPosition(index);
        state = ServerState.Idle;
        HallSystem.Instance.RequestTask(this);
    }

    public void InitialServerSetting(PartTimerData data, int number)
    {
        partTimerName = data.serverName;
        level = data.level;
        status = data.status;
        Initialize(number);
    }

    // HallSystem이 유휴 등록 직후 호출한다. 대기 위치에서 멀면 복귀 이동을 시작한다.
    public void GoIdle()
    {
        if (Vector2.Distance(transform.position, initPosition) > arrivalThreshold)
        {
            Interrupt(ServerState.Returning, ReturningRoutine());
        }
        else
        {
            Interrupt(ServerState.Idle, null);
        }
    }

    // HallSystem이 작업을 배정할 때 호출한다.
    public void AssignTask(ServingTask task)
    {
        curTask = task;
        if (task.Customer.cbc.GetCheckBoost())
        {
            task.Customer.cbc.SetCheckBoost(false);
        }

        if (task.TypeTask == ServingTaskType.TakeOrder)
        {
            Interrupt(ServerState.ApproachingCustomerForOrder, ApproachingCustomerForOrderRoutine());
        }
        else
        {
            Interrupt(ServerState.ApproachingKitchen, ApproachingKitchenRoutine());
        }
    }

    // 외부(HallSystem)에서 지금 진행 중인 걸 끊고 새 상태로 전환할 때 사용.
    // Returning처럼 독립적으로 돌고 있는 코루틴을 중단시켜야 하므로 StopCoroutine이 필요하다.
    private void Interrupt(ServerState next, IEnumerator routine)
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
    private void Advance(ServerState next, IEnumerator routine)
    {
        state = next;
        stateCoroutine = routine != null ? StartCoroutine(routine) : null;
    }

    private IEnumerator ReturningRoutine()
    {
        yield return MoveTo(initPosition, status.serving);
    }

    // ── TakeOrder ──────────────────────────────────────────
    private IEnumerator ApproachingCustomerForOrderRoutine()
    {
        MoveResult moveResult = new MoveResult();
        if (curTask.Customer != null)
        {
            PathNode customerNode = PathfindingGrid.Instance.GetNodeFromWorld(curTask.Customer.transform.position);
            PathNode approachNode = FindApproachNode(customerNode);
            if (approachNode != null)
            {
                yield return MoveToWithRetry(approachNode.worldPos, status.serving, moveResult);
            }
        }

        if (curTask.Customer != null && !moveResult.Success)
        {
            Debug.LogWarning($"[{name}] 손님에게 접근하지 못해 주문 접수를 포기합니다.");
            FinishTask();
            yield break;
        }

        Advance(ServerState.TakingOrder, TakingOrderRoutine());
    }

    private IEnumerator TakingOrderRoutine()
    {
        if (curTask.Customer != null) { curTask.Customer.ReceiveOrder(); }
        yield return Work(1f);
        FinishTask();
    }

    // ── DeliverFood ────────────────────────────────────────
    private IEnumerator ApproachingKitchenRoutine()
    {
        MoveResult moveResult = new MoveResult();
        yield return MoveToWithRetry(ServerManager.Instance.GetKitchenPosition(), status.serving, moveResult);

        if (!moveResult.Success)
        {
            Debug.LogWarning($"[{name}] 주방에 도달하지 못해 배달 작업을 포기합니다.");
            FinishTask();
            yield break;
        }

        Advance(ServerState.PickingUpFood, PickingUpFoodRoutine());
    }

    private IEnumerator PickingUpFoodRoutine()
    {
        yield return new WaitForSeconds(2f); // 음식 픽업 연출

        if (curTask.Customer == null) { FinishTask(); yield break; }
        Advance(ServerState.ApproachingCustomerWithFood, ApproachingCustomerWithFoodRoutine());
    }

    private IEnumerator ApproachingCustomerWithFoodRoutine()
    {
        MoveResult moveResult = new MoveResult();
        if (curTask.Customer != null)
        {
            PathNode customerNode = PathfindingGrid.Instance.GetNodeFromWorld(curTask.Customer.transform.position);
            PathNode approachNode = FindApproachNode(customerNode);
            if (approachNode != null)
            {
                yield return MoveToWithRetry(approachNode.worldPos, status.serving, moveResult);
            }
        }

        if (curTask.Customer != null && !moveResult.Success)
        {
            Debug.LogWarning($"[{name}] 손님에게 접근하지 못해 배달 작업을 포기합니다.");
            FinishTask();
            yield break;
        }

        Advance(ServerState.DeliveringFood, DeliveringFoodRoutine());
    }

    private IEnumerator DeliveringFoodRoutine()
    {
        if (curTask.Customer != null) { curTask.Customer.ServingLuck = status.handy; curTask.Customer.ReceiveFood(); }
        yield return Work(1f);
        FinishTask();
    }

    private void FinishTask()
    {
        curTask = null;
        HallSystem.Instance.RequestTask(this);
    }
}
