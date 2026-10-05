using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomerAgent : MonoBehaviour
{
    [ReadOnly][SerializeField] private RestaurantGameManager gm;

    public CustomerPatienceComponent cpc;
    public CustomerOrderComponent coc;
    public CustomerUIComponent cuc;
    public CustomerBoostComponent cbc;
    public NPCMovement nm;

    [Header("Timings")]
    [SerializeField] private float eatDuration = 8f;
    [SerializeField] private float payDuration = 2f;
    [SerializeField] private float stateChangeDuration = 1.0f;

    [Header("Payment")]
    [SerializeField] private Currency gold;

    [Header("Other")]
    private bool indoor = false;
    private bool isWaiting = false;


    // Action
    public event Action<CustomerAgent> OnOrderReceived;
    public event Action<CustomerAgent> OnOrderTaken;
    public Action<CustomerAgent> OnExited;


    // 화면 표시·집계용 관찰 이벤트. 손님의 행동에는 관여하지 않는다.
    public static event Action<CustomerAgent> OnAnySpawned;
    public static event Action<CustomerAgent, CustomerState> OnAnyStateChanged;
    public static event Action<CustomerAgent, RecipeData, int> OnAnyPaid;   // 실제 지급된 금액(팁 포함)

    private CustomerState state;
    private RatingFlag ratingFlag = RatingFlag.None;

    public CustomerState State => state;
    public bool IsInWaitingLine => isWaiting;

    // 식사를 마치고 결제까지 완료했는지(= 대접받았는지) 여부. 일일 결산의 손님 수 집계에 쓰인다.
    public bool WasServed { get; private set; }

    private Seat currentSeat;
    public Seat GetCurrentSeat() { return currentSeat; }

    private const float BaseMoveSpeed = 3f;
    private float MoveSpeed => BaseMoveSpeed * RestaurantSpeedController.SpeedMultiplier;

    // MoveToSeat에서 경로 탐색에 실패한 좌석들. 만석+대기열 만석 상태에서 유일하게 빈 좌석이
    // 도달 불가능할 경우, 같은 좌석을 무한히 재배정받아 TrySeat<->MoveToSeat이 무한 반복되는 것을 막기 위함.
    private readonly HashSet<Seat> failedSeats = new HashSet<Seat>();

    /* [ Awake & Start ] */
    private void Awake()
    {
        gm = RestaurantGameManager.instance;
    }

    /* [ Spawn ] */
    public void SpawnCustomer(float patienceValue)
    {
        cpc = GetComponent<CustomerPatienceComponent>();
        coc = GetComponent<CustomerOrderComponent>();
        cuc = GetComponent<CustomerUIComponent>();
        cbc = GetComponent<CustomerBoostComponent>();
        nm = GetComponent<NPCMovement>();

        cpc.OnPatienceExhausted += PatienceExhausted;
        cpc.OnWaitingProgress += cuc.ChangeEmotion;
        OnAnySpawned?.Invoke(this);
        InitPatience(patienceValue);
    }

    // 이 손님의 식사 시간만 바꾼다. (첫 손님 안내 등 개별 조정용)
    public void OverrideEatDuration(float seconds)
    {
        if (seconds > 0f) { eatDuration = seconds; }
    }

    // 마감이 끝나지 않는 상황(도달 불가 등)을 풀기 위해 손님을 미대접으로 내보낸다. 이미 계산했으면 대접으로 남는다.
    public void ForceLeave()
    {
        if (state == CustomerState.Exit) { return; }
        Debug.LogWarning($"[{name}] 진행이 멈춘 손님을 퇴장시킵니다. (상태: {state})");
        ratingFlag = RatingFlag.Low;
        StopAllCoroutines();
        ChangeState(CustomerState.Exit);
    }

    private void InitPatience(float patienceValue)
    {
        if (null == gm) { gm = RestaurantGameManager.instance; }
        state = CustomerState.None;
        cpc.SetPatience(patienceValue);
        HallSystem.Instance.RegisterCustomer(this);

        // 스폰 시점에 즉시 좌석을 예약함 (2초 뒤 Seating 단계에서 잡으면 그 사이 다른 손님에게 마지막 자리를 뺏겨
        // 생성되었다가 바로 퇴장하는 문제가 생김). 스포너가 미리 자리 유무를 확인하므로 여기서 실패하는 것은
        // 정상 케이스가 아니지만, 안전장치로 처리해둠.
        currentSeat = gm.seatManager.TryAssignSeat(this, out indoor);
        if (currentSeat == null)
        {
            ratingFlag = RatingFlag.Low;
            ChangeState(CustomerState.Exit);
            return;
        }

        StartCoroutine(WaitStateChange(CustomerState.Enter));
    }

    // ========================================================================================
    /* [ State Machine ] */

    private void ChangeState(CustomerState nextState)
    {
        state = nextState;
        OnAnyStateChanged?.Invoke(this, state);
        switch (state)
        {
            case CustomerState.Enter:
                StartCoroutine(WaitStateChange(CustomerState.Seating));
                break;
            case CustomerState.Seating:
                TrySeat();
                break;
            case CustomerState.WaitingRoom:
                cpc.ResetGraceTimer();
                cpc.SetIsWaiting(true);
                break;
            case CustomerState.WaitingForOrder:
                TryOrder();
                break;
            case CustomerState.WaitingForFood:
                WaitFood();
                break;
            case CustomerState.Eating:
                StartCoroutine(Eating());
                break;
            case CustomerState.Paying:
                StartCoroutine(Paying());
                break;
            case CustomerState.Exit:
                ExitRestaurant();
                break;

        }
    }

    // ========================================================================================
    /* [ State Logic ] */
    // CustomerState.Seating 
    private void TrySeat()
    {
        // 좌석은 InitPatience에서 스폰과 동시에 이미 예약됨. 여기서 null이라면 MoveToSeat 중
        // 경로 탐색 실패로 자리를 반납하고 재시도하는 경우이므로 다시 잡아본다.
        if (currentSeat == null)
        {
            currentSeat = gm.seatManager.TryAssignSeat(this, out indoor, failedSeats);
            if (currentSeat == null) // 만약 재시도했는데도 자리가 없는 경우
            {
                ratingFlag = RatingFlag.Low;
                ChangeState(CustomerState.Exit);
                return;
            }
        }

        //transform.position = currentSeat.GetSeatPoint().position; // 이거는 나중에 길찾기 알고리즘 써서 이동하도록 만들기 A*
        StartCoroutine(MoveToSeat(currentSeat));
    }

    private IEnumerator MoveToSeat(Seat seat)
    {
        Vector3 target = seat.GetSeatPoint().position;
        PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        PathNode endNode = PathfindingGrid.Instance.GetNodeFromWorld(target);

        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("MoveToSeat: 시작 또는 도착 노드가 그리드 밖입니다. 다른 자리를 탐색합니다.");
            failedSeats.Add(seat);
            gm.seatManager.ReleaseSeat(this, seat, !indoor);
            currentSeat = null;
            TrySeat();
            yield break;
        }

        List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);

        if (path == null)
        {
            Debug.LogWarning("MoveToSeat: 경로를 찾을 수 없습니다. 다른 자리를 탐색합니다.");
            failedSeats.Add(seat);
            gm.seatManager.ReleaseSeat(this, seat, !indoor);
            currentSeat = null;
            TrySeat();
            yield break;
        }

        nm.SetMoving(true);

        foreach (Vector3 waypoint in path)
        {
            while (Vector2.Distance(transform.position, waypoint) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, waypoint, MoveSpeed * Time.deltaTime);
                Vector2 dir = (waypoint - transform.position).normalized;
                nm.SetDirection(dir);
                yield return null;
            }
        }

        // A* 경로는 타일 중심까지만 안내하므로, 테이블 크기에 비례해 옮겨진 실제 좌석 좌표까지 마지막으로 미세 이동
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);
            Vector2 dir = (target - transform.position).normalized;
            nm.SetDirection(dir);
            yield return null;
        }

        nm.SetMoving(false);
        nm.ArriveDirection(currentSeat.GetFacingDirection());
        // 도착 후 해당 타일을 장애물로 전환
        seat.OnCustomerSeated();
        // 좌석별로 인스펙터에서 조절 가능한 정렬 순서 적용 (테이블과 겹침 방지)
        nm.ApplySeatSortingOrder(seat.GetSeatedSortingOrderOffset());

        if (indoor) { cpc.ChangeState(); cbc.SetCanBoost(true); StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder)); }
        else { isWaiting = true; cuc.ShowBubble(1); StartCoroutine(WaitStateChange(CustomerState.WaitingRoom)); }
    }

    public void PromoteToSeat(Seat newSeat)
    {
        currentSeat = newSeat;
        indoor = true;
        isWaiting = false;
        cpc.SetIsWaiting(false);
        nm.SetMoving(false);
        cuc.CloseBubble();
        StopAllCoroutines();
        TaskLogger.Instance.LogServing("현재 손님이 좌석에 앉았습니다.");
        StartCoroutine(MoveToSeat(newSeat));
    }

    public void MoveWaitingSeat(Seat newSeat)
    {
        Seat oldSeat = currentSeat;
        if (oldSeat == newSeat) { return; } // 대기열 앞당김으로 위치가 바뀌지 않은 손님은 처리할 필요 없음

        // 대기 벤치 슬롯 이동도 착석/퇴장과 동일하게 타일 walkable을 갱신해야 한다.
        // (그렇지 않으면 옮겨간 벤치는 통행 가능으로, 비워둔 벤치는 통행 불가로 남는 버그가 생긴다)
        if (oldSeat != null) { oldSeat.Vacate(); }
        newSeat.OnCustomerSeated();

        currentSeat = newSeat;
        transform.position = newSeat.GetSeatPoint().position;
        nm.ApplySeatSortingOrder(newSeat.GetSeatedSortingOrderOffset());
    }

    // CustomerState.WaitingForOrder
    private void TryOrder()
    {
        if (!indoor) { return; }
        StartCoroutine(ChoosingMenu());
    }


    private IEnumerator ChoosingMenu()
    {
        cuc.ShowBubble(2);
        yield return new WaitForSeconds(3f); // 이 값은 랜덤으로 줘도 됨
        coc.GenerateOrder();
        if (coc.GetOrderData() == null)
        {
            // 오늘의 메뉴가 비어 주문할 수 없으면 기다리지 않고 미대접으로 퇴장한다. (마감이 끝나지 않는 상황 방지)
            ratingFlag = RatingFlag.Low;
            cuc.CloseBubble();
            StartCoroutine(WaitStateChange(CustomerState.Exit));
            yield break;
        }
        Debug.Log("메뉴를 골랏습니다");
        cuc.ShowBubble(0, coc.GetOrderData().Icon);
        cpc.ResetGraceTimer();
        OnOrderReceived?.Invoke(this);
    }



    public void ReceiveOrder()
    {
        if (coc.GetOrderData() == null) { return; }
        cpc.ChangeState();
        OnOrderTaken?.Invoke(this);
        StartCoroutine(WaitStateChange(CustomerState.WaitingForFood));
    }



    // CustomState.WaitingForFood
    private void WaitFood()
    {
        cpc.ResetGraceTimer();
    }

    public void ReceiveFood()
    {
        if (state != CustomerState.WaitingForFood) { return; }
        cpc.ChangeState();
        cuc.CloseBubble();
        StartCoroutine(WaitStateChange(CustomerState.Eating));
    }

    // CustomState.Eating
    private IEnumerator Eating()
    {
        yield return new WaitForSeconds(eatDuration);
        StartCoroutine(WaitStateChange(CustomerState.Paying));
    }

    // CustomState.Paying
    private IEnumerator Paying()
    {
        ProcessPayment();
        yield return new WaitForSeconds(payDuration);
        StartCoroutine(WaitStateChange(CustomerState.Exit));
    }

    private void ProcessPayment()
    {
        RecipeData order = coc.GetOrderData();
        if (order == null) { return; }

        WasServed = true;

        float tipMultiplier = 1f;

        if (RestaurantRatingManager.Instance != null && gm != null && gm.ratingSystem != null)
        {
            float score = ComputePersonalRating(order);
            RestaurantRatingManager.Instance.AddCustomerScore(score);
            tipMultiplier = RestaurantRatingManager.Instance.TipMultiplier;
        }

        if (gold == null) { return; }

        int amount = Mathf.RoundToInt(order.Price);
        CurrencyTransaction tx = new CurrencyTransaction(gold, amount, TransactionSource.CustomerPayment, tipMultiplier);
        CurrencyManager.Instance.ProcessTransaction(tx);

        if (DailySalesTracker.Instance != null)
        {
            int finalAmount = Mathf.RoundToInt(amount * tipMultiplier);
            DailySalesTracker.Instance.RecordSale(order, finalAmount);
        }

        OnAnyPaid?.Invoke(this, order, tx.FinalAmount);
    }

    // Score = min(5.0, Taste Score + Favorite Bonus)
    private float ComputePersonalRating(RecipeData order)
    {
        int recipeGrade = RatingSystem.GradeToInt(order.Grade);
        int expectation = RestaurantRatingManager.Instance.CurrentExpectation;
        float taste = gm.ratingSystem.PersonalRating(recipeGrade, expectation);

        float favoriteBonus = 0f;
        if (coc.FavoriteRecipeId >= 0 && coc.FavoriteRecipeId == order.RecipeId) { favoriteBonus = 0.5f; }

        return Mathf.Min(5.0f, taste + favoriteBonus);
    }

    // 각 스테이트가 끝날때 마다 1초 정도 기다리고 다음 스테이트로 이동
    private IEnumerator WaitStateChange(CustomerState curState)
    {
        //isPatience = false;
        yield return new WaitForSeconds(stateChangeDuration);
        ChangeState(curState);
    }

    // CustomState.Exit
    private void ExitRestaurant()
    {
        if (ratingFlag == RatingFlag.None)
        {
            ratingFlag = RatingFlag.Perfect; // 이건 나중에 RatingSysyem 구현 후 수정
        }

        if (currentSeat != null)
        {
            gm.seatManager.ReleaseSeat(this, currentSeat, isWaiting);
            currentSeat = null;
        }
        //Debug.Log("고객이 만족하고 퇴장하였습니다!");
        nm.ResetSortingOrder();
        nm.SetMoving(false);
        cpc.OnPatienceExhausted -= PatienceExhausted;
        cpc.OnWaitingProgress -= cuc.ChangeEmotion;
        StopAllCoroutines();
        StartCoroutine(MoveToExit());
    }

    private IEnumerator MoveToExit()
    {
        Vector3 target = gm.exitPoint.position;

        PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        PathNode endNode = PathfindingGrid.Instance.GetNodeFromWorld(target);

        if (startNode == null || endNode == null)
        {
            transform.position = target;
            OnExited?.Invoke(this);
            Destroy(gameObject);
            yield break;
        }

        List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);

        if (path == null)
        {
            transform.position = target;
            OnExited?.Invoke(this);
            Destroy(gameObject);
            yield break;
        }

        nm.SetMoving(true);

        foreach (Vector3 waypoint in path)
        {
            while (Vector2.Distance(transform.position, waypoint) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, waypoint, MoveSpeed * Time.deltaTime);
                Vector2 dir = (waypoint - transform.position).normalized;
                nm.SetDirection(dir);
                yield return null;
            }
        }

        nm.SetMoving(false);
        OnExited?.Invoke(this); // ← OnCustomerSeated() 대신 퇴장 이벤트
        Destroy(gameObject);    // ← 상태 전환 대신 오브젝트 제거
    }



    // Event

    private void PatienceExhausted()
    {
        Debug.Log("손님이 지쳐서 나갔습니다.");
        ratingFlag = RatingFlag.Low;
        nm.SetMoving(false);
        StopAllCoroutines();
        StartCoroutine(WaitStateChange(CustomerState.Exit));
    }


    private void OnMouseDown()
    {
        cbc.Boost(this);
    }




}
