using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomerAgent : MonoBehaviour
{
    public bool IngredientsReserved;
    public int FoodQuality;
    public float ServingLuck;
    public bool LeftAngry { get; private set; }
    public float LastRating { get; private set; }

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
    private Coroutine shiftRoutine;   // 웨이팅 줄이 당겨져 옆 의자로 옮겨 앉는 중

    private const float BaseMoveSpeed = 3f;
    private float MoveSpeed => BaseMoveSpeed * RestaurantSpeedController.SpeedMultiplier;

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
        // 좌석은 InitPatience에서 스폰과 동시에 이미 예약됨. 혹시 비어 있으면 다시 잡아본다.
        if (currentSeat == null)
        {
            currentSeat = gm.seatManager.TryAssignSeat(this, out indoor);
            if (currentSeat == null) // 만약 재시도했는데도 자리가 없는 경우
            {
                ratingFlag = RatingFlag.Low;
                ChangeState(CustomerState.Exit);
                return;
            }
        }

        StartCoroutine(MoveToSeat());
    }

    // 배치도의 길을 따라 자리까지 헤엄친다. 가는 도중에 웨이팅 줄이 당겨져 자리가 바뀌면 새 자리로 다시 간다.
    private IEnumerator MoveToSeat()
    {
        nm.SetMoving(true);
        Seat goal = null;
        while (goal != currentSeat && currentSeat != null)
        {
            goal = currentSeat;
            yield return Swim(goal.GetSeatPoint().position);
        }
        nm.SetMoving(false);
        if (goal == null) { yield break; }

        nm.ArriveDirection(goal.GetFacingDirection());
        goal.OnCustomerSeated();
        // 좌석별로 인스펙터에서 조절 가능한 정렬 순서 적용 (테이블과 겹침 방지)
        nm.ApplySeatSortingOrder(goal.GetSeatedSortingOrderOffset());

        if (indoor) { cpc.ChangeState(); cbc.SetCanBoost(true); StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder)); }
        else { isWaiting = true; cuc.ShowBubble(1); StartCoroutine(WaitStateChange(CustomerState.WaitingRoom)); }
    }

    private IEnumerator Swim(Vector2 target)
    {
        yield return RestaurantNav.Swim(transform, RestaurantNav.Route(transform.position, target), () => MoveSpeed, nm.SetDirection);
    }

    public void PromoteToSeat(Seat newSeat)
    {
        currentSeat = newSeat;
        indoor = true;
        isWaiting = false;
        cpc.SetIsWaiting(false);
        nm.SetMoving(false);
        nm.ResetSortingOrder();
        cuc.CloseBubble();
        StopAllCoroutines();
        shiftRoutine = null;
        TaskLogger.Instance.LogServing("현재 손님이 좌석에 앉았습니다.");
        StartCoroutine(MoveToSeat());
    }

    // 웨이팅 줄이 당겨질 때 SeatManager가 부른다. 이미 앉아 있으면 옆 의자로 헤엄쳐 옮겨 앉는다.
    public void MoveWaitingSeat(Seat newSeat)
    {
        Seat oldSeat = currentSeat;
        if (oldSeat == newSeat) { return; } // 대기열 앞당김으로 위치가 바뀌지 않은 손님은 처리할 필요 없음

        if (oldSeat != null) { oldSeat.Vacate(); }
        currentSeat = newSeat;
        if (!isWaiting) { return; } // 아직 웨이팅석으로 가는 중이면 MoveToSeat이 새 자리로 이어서 간다.

        newSeat.OnCustomerSeated();
        if (shiftRoutine != null) { StopCoroutine(shiftRoutine); }
        shiftRoutine = StartCoroutine(ShiftToSeat(newSeat));
    }

    private IEnumerator ShiftToSeat(Seat seat)
    {
        nm.SetMoving(true);
        yield return RestaurantNav.Swim(transform, new List<Vector3> { seat.GetSeatPoint().position }, () => MoveSpeed, nm.SetDirection);
        nm.SetMoving(false);
        nm.ArriveDirection(seat.GetFacingDirection());
        nm.ApplySeatSortingOrder(seat.GetSeatedSortingOrderOffset());
        shiftRoutine = null;
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
        if (state == CustomerState.Exit) return;
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
            LastRating = score;
            RestaurantRatingManager.Instance.AddCustomerScore(score);
            tipMultiplier = RestaurantRatingManager.Instance.TipMultiplier;
        }

        if (gold == null) { return; }

        int amount = Mathf.RoundToInt(order.Price);
        int tip = UnityEngine.Random.value < Mathf.Clamp01(.2f + ServingLuck / 100f)
            ? Mathf.RoundToInt(amount * .1f * tipMultiplier * RestaurantRules.TrashTipMultiplier(RestaurantLitter.Count)) : 0;
        CurrencyTransaction tx = new CurrencyTransaction(gold, amount + tip, TransactionSource.CustomerPayment);
        CurrencyManager.Instance.ProcessTransaction(tx);

        if (DailySalesTracker.Instance != null)
        {
            int finalAmount = amount + tip;
            DailySalesTracker.Instance.RecordSale(order, finalAmount);
        }

        OnAnyPaid?.Invoke(this, order, tx.FinalAmount);
        RestaurantProgress.RecordSaleForMission(tx.FinalAmount);
    }

    // Score = min(5.0, Taste Score + Favorite Bonus)
    private float ComputePersonalRating(RecipeData order)
    {
        int recipeGrade = FoodQuality;
        int expectation = RestaurantRatingManager.Instance.CurrentExpectation;
        float taste = gm.ratingSystem.PersonalRating(recipeGrade, expectation);

        float favoriteBonus = 0f;
        if (coc.IsFavorite(order)) { favoriteBonus = 0.5f; }

        return Mathf.Min(coc.FavoriteOnMenu ? 5.0f : 4.5f, taste + favoriteBonus);
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
        nm.SetMoving(true);
        yield return Swim(gm.exitPoint.position);
        nm.SetMoving(false);
        OnExited?.Invoke(this);
        Destroy(gameObject);
    }



    // Event

    private void PatienceExhausted()
    {
        LeftAngry = true;
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
