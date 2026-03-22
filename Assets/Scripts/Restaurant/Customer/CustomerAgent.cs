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

    [Header("Timings")]
    [SerializeField] private float eatDuration = 8f;
    [SerializeField] private float payDuration = 2f;
    [SerializeField] private float stateChangeDuration = 1.0f;

    [Header("Other")]
    private bool indoor = false;
    private int seatNumber;
    private bool isWaiting = false;
    [ReadOnly][SerializeField] private bool isOrder = false;
    [ReadOnly][SerializeField] private bool isWaitingFood = false;


    // Action
    public event Action<CustomerAgent> OnOrderReceived;

    // Destroy Event
    public Action<CustomerAgent> OnExited;


    private CustomerState state;
    private RatingFlag ratingFlag = RatingFlag.None;

    private Seat currentSeat;

    /* [ Awake & Start ] */
    private void Awake()
    {
        gm = RestaurantGameManager.instance;
    }

    private void Start()
    {
        cpc = GetComponent<CustomerPatienceComponent>();
        coc = GetComponent<CustomerOrderComponent>();
        cuc = GetComponent<CustomerUIComponent>();
        cbc = GetComponent<CustomerBoostComponent>();

        cpc.OnPatienceExhausted += PatienceExhausted;
        cpc.OnWaitingProgress += cuc.ChangeEmotion;
    }


    /* [ Spawn ] */
    public void SpawnCustomer(float patienceValue)
    {
        cpc = GetComponent<CustomerPatienceComponent>();
        coc = GetComponent<CustomerOrderComponent>();
        cuc = GetComponent<CustomerUIComponent>();
        cbc = GetComponent<CustomerBoostComponent>();
        InitPatience(patienceValue);
    }

    private void InitPatience(float patienceValue)
    {
        if (null == gm) { gm = RestaurantGameManager.instance; }
        state = CustomerState.None;
        cpc.SetPatience(patienceValue);
        currentSeat = null;
        HallSystem.Instance.RegisterCustomer(this);
        StartCoroutine(WaitStateChange(CustomerState.Enter));
    }







    // ========================================================================================
    /* [ State Machine ] */

    private void ChangeState(CustomerState nextState)
    {
        state = nextState;
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
                break;
            case CustomerState.WaitingForOrder:
                TryOrder();
                break;
            case CustomerState.WaitingForFood:
                WaitFood();
                break;
            case CustomerState.Eating:
                Eating();
                break;
            case CustomerState.Paying:
                Paying();
                break;
            case CustomerState.Exit:
                ExitRestaurant();
                break;

        }
    }


    public void ActivateCondition()
    {
        if (isOrder) { ReceiveOrder(); }
        if (isWaitingFood) { ReceiveFood(); }
    }

    // ========================================================================================
    /* [ State Logic ] */
    // CustomerState.Seating 
    private void TrySeat()
    {
        if (currentSeat != null) { return; } // 이미 자리를 점유하고 있다면 -> 근데 이 코드에 걸릴 일은 없을 듯
        currentSeat = gm.seatManager.TryAssignSeat(this, out indoor);

        if (currentSeat == null) // 만약 입장했는데 자리가 없는 경우 <- 이 함수는 어느정도 개선이 필요함
        {
            ratingFlag = RatingFlag.Low;
            ChangeState(CustomerState.Exit);
            return;
        }

        transform.position = currentSeat.GetSeatPoint().position; // 이거는 나중에 길찾기 알고리즘 써서 이동하도록 만들기 A*
        seatNumber = currentSeat.seatNumber;
        if (indoor) { cpc.ChangeState(); cbc.SetCanBoost(true); StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder)); }
        else { isWaiting = true; cuc.ShowBubble(1); StartCoroutine(WaitStateChange(CustomerState.WaitingRoom)); }
    }


    public void PromoteToSeat(Seat newSeat)
    {
        currentSeat = newSeat;
        indoor = true;
        //isPatience = false;
        isWaiting = false;
        cuc.CloseBubble();
        transform.position = newSeat.GetSeatPoint().position;
        TaskLogger.Instance.LogServing($"현재 손님이 {seatNumber}번 좌석에 앉았습니다.");
        StopAllCoroutines();
        StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder));
    }

    public void MoveWaitingSeat(Seat newSeat)
    {
        currentSeat = newSeat;
        transform.position = newSeat.GetSeatPoint().position;
    }

    // CustomerState.WaitingForOrder
    private void TryOrder()
    {
        StartCoroutine(ChoosingMenu());
    }


    private IEnumerator ChoosingMenu()
    {
        cuc.ShowBubble(2);
        yield return new WaitForSeconds(3f); // 이 값은 랜덤으로 줘도 됨
        coc.GenerateOrder();
        Debug.Log("메뉴를 골랏습니다");
        cuc.ShowBubble(0, coc.GetOrderData().image);
        cpc.ResetGraceTimer();
        isOrder = true;

        OnOrderReceived?.Invoke(this);
    }



    public void ReceiveOrder()
    {
        if (!isOrder) { return; }
        //cuc.CloseBubble();
        KitchenSystem.Instance.HandleOrderReceived(this);
        isOrder = false;
        StartCoroutine(WaitStateChange(CustomerState.WaitingForFood));
    }



    // CustomState.WaitingForFood
    private void WaitFood()
    {
        cpc.ResetGraceTimer();
        isWaitingFood = true;

        //OnFoodReceived?.Invoke(this);
    }

    public void ReceiveFood()
    {
        if (!isWaitingFood) { return; }
        isWaitingFood = false;
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
        yield return new WaitForSeconds(payDuration);
        StartCoroutine(WaitStateChange(CustomerState.Exit));
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
            gm.seatManager.ReleaseSeat(currentSeat, isWaiting);
            currentSeat = null;
        }
        //Debug.Log("고객이 만족하고 퇴장하였습니다!");
        OnExited?.Invoke(this);
        StopAllCoroutines();
        Destroy(gameObject);
    }



    // Event

    private void PatienceExhausted()
    {
        ratingFlag = RatingFlag.Low;
        StartCoroutine(WaitStateChange(CustomerState.Exit));
    }

    private void ChangeWaitingEmotion()
    {
        cuc.ChangeEmotion();
    }

    private void OnMouseDown()
    {
        cbc.Boost(this);
    }




}
