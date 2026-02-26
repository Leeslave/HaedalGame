using System.Collections;
using UnityEngine;
using System;

public class CustomerAgent : MonoBehaviour
{
    [ReadOnly][SerializeField] private RestaurantGameManager gm;

    [Header("Patience")] // 인내심 관련 변수
    private const float DefaultPatience = 25f;          // 기본 인내심을 이곳에서 정의
    [ReadOnly][SerializeField] private float curPatience = DefaultPatience;

    [SerializeField] private float waitingGraceSec = 3f;    // 웨이팅 룸으로 갔을 때 바로 인내심이 깎이는 것이 아니라 일부 대기 시간 부여
    [SerializeField] private float orderGraceSec = 5f;       // 음식을 주문 했을 때 바로 인내심이 깎이는 것이 아니라 일부 대기 시간 부여
    [SerializeField] private float foodGraceSec = 5f;       // 음식을 주문 했을 때 바로 인내심이 깎이는 것이 아니라 일부 대기 시간 부여
    private float graceTimer;


    [Header("Patience Drain")]
    [SerializeField] private float drainPerSec = 0.1f;


    [Header("Timings")]
    [SerializeField] private float eatDuration = 8f;
    private float eatTimer;
    [SerializeField] private float payDuration = 2f;
    private float payTimer;
    [SerializeField] private float stateChangeDuration = 1.0f;

    [Header("Other")]
    private bool indoor = false;
    private bool isPatience = false;
    private bool isWaiting = false;
    [ReadOnly][SerializeField] private bool isOrder = false;
    [ReadOnly][SerializeField] private bool isWaitingFood = false;
    private bool isEating = false;
    private bool isPaying = false;

    private bool canBoost;
    private float boostLoadingTime = 5f;

    public event Action<CustomerAgent> OnOrderReceived;
    public event Action<CustomerAgent> OnFoodReceived;

    // Destroy Event
    public Action<CustomerAgent> OnExited;



    /* [ Runtime State ] */
    private CustomerState state;
    private RatingFlag ratingFlag = RatingFlag.None;

    private Seat currentSeat;
    private Order currentOrder;

    /* [ public getter ] */
    public CustomerState State => state;
    public float Patience => curPatience;
    public RatingFlag Rating => ratingFlag;
    public Order CurrentOrder => currentOrder;
    public bool IsWaiting => isWaiting;

    /* [ public Setter ]*/
    public void SetDrainRate(float value) { drainPerSec = value; }

    /* [ Lifecycle Methods ] */
    private void Awake()
    {
        gm = RestaurantGameManager.instance;
    }

    private void Start()
    {
        canBoost = true;
    }

    private void OnMouseDown()
    {
        if (canBoost)
        {
            canBoost = false;
            TaskManager.Instance.BoostCustomer(this);
            StartCoroutine(BoostLoading());
        }
    }

    private IEnumerator BoostLoading()
    {
        yield return new WaitForSeconds(boostLoadingTime);
        canBoost = true;
    }

    private void Update()
    {
        if (isPatience)
        {
            if (graceTimer > 0)
            {
                graceTimer -= Time.deltaTime;
            }
            else
            {
                curPatience -= drainPerSec * Time.deltaTime;

                if (curPatience <= 0)
                {
                    curPatience = 0;
                    isPatience = false;
                    ratingFlag = RatingFlag.Low;
                    StartCoroutine(WaitStateChange(CustomerState.Exit));
                }
            }
        }

        if (isEating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0)
            {
                eatTimer = 0;
                isEating = false;
                StartCoroutine(WaitStateChange(CustomerState.Paying));
            }
        }

        if (isPaying)
        {
            payTimer -= Time.deltaTime;
            if (payTimer <= 0)
            {
                payTimer = 0;
                isPaying = false;

                StartCoroutine(WaitStateChange(CustomerState.Exit));
            }
        }


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
                TrySeat(); ;
                break;
            case CustomerState.WaitingRoom:
                WaitingRoom();
                break;
            case CustomerState.WaitingForOrder:
                //Debug.Log($"{gameObject.name}이 주문을 대기하고 있습니다.");
                TryOrder();
                break;
            case CustomerState.WaitingForFood:
                //Debug.Log($"{gameObject.name}이 음식을 대기하고 있습니다.");
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



    // void OnMouseDown()
    // {
    //     ActivateCondition();
    // }

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
            //Debug.Log($"{gameObject.name}의 이번 식사의 평가는 {ratingFlag}입니다.");
            ChangeState(CustomerState.Exit);
            return;
        }

        transform.position = currentSeat.GetSeatPoint().position; // 이거는 나중에 길찾기 알고리즘 써서 이동하도록 만들기 A*

        if (indoor) { StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder)); }
        else { isWaiting = true; StartCoroutine(WaitStateChange(CustomerState.WaitingRoom)); }
    }

    // CustomerState.WaitingRoom
    private void WaitingRoom()
    {
        graceTimer = waitingGraceSec;
        isPatience = true;
    }

    public void PromoteToSeat(Seat newSeat)
    {
        currentSeat = newSeat;
        indoor = true;
        isPatience = false;
        isWaiting = false;
        transform.position = newSeat.GetSeatPoint().position;
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
        yield return new WaitForSeconds(3f); // 이 값은 랜덤으로 줘도 됨
        // 여기다 어떤 메뉴를 골랐는지 UI에 띄우기
        Debug.Log("메뉴를 골랐습니다!");

        yield return new WaitForSeconds(1f);
        graceTimer = orderGraceSec;
        isPatience = true;
        isOrder = true;

        OnOrderReceived?.Invoke(this);
    }

    public void ReceiveOrder()
    {
        if (!isOrder) { return; }
        Debug.Log("주문을 받았습니다!");
        isOrder = false;
        StartCoroutine(WaitStateChange(CustomerState.WaitingForFood));
    }



    // CustomState.WaitingForFood
    private void WaitFood()
    {
        graceTimer = foodGraceSec;
        isPatience = true;
        isWaitingFood = true;

        OnFoodReceived?.Invoke(this);
    }

    public void ReceiveFood()
    {
        if (!isWaitingFood) { return; }
        Debug.Log("손님이 음식을 받았습니다!");
        isWaitingFood = false;
        StartCoroutine(WaitStateChange(CustomerState.Eating));
    }

    // CustomState.Eating
    private void Eating()
    {
        eatTimer = eatDuration;
        isEating = true;
    }

    // CustomState.Paying
    private void Paying()
    {
        payTimer = payDuration;
        isPaying = true;
    }

    // 각 스테이트가 끝날때 마다 1초 정도 기다리고 다음 스테이트로 이동
    private IEnumerator WaitStateChange(CustomerState curState)
    {
        isPatience = false;
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
        Debug.Log("고객이 만족하고 퇴장하였습니다!");
        OnExited?.Invoke(this);
        StopAllCoroutines();
        Destroy(gameObject);
    }

    // ========================================================================================
    /* [ Event Logic ] */
    public void SpawnCustomer(float patienceValue = DefaultPatience)
    {
        InitPatience(patienceValue);
    }

    private void InitPatience(float patienceValue)
    {
        if (null == gm) { gm = RestaurantGameManager.instance; }

        state = CustomerState.None;
        currentSeat = null;

        SetPatience(patienceValue);
        TaskManager.Instance.RegisterCustomer(this);
        StartCoroutine(WaitStateChange(CustomerState.Enter));
    }

    private void SetPatience(float value) { curPatience = value; } // 특정 이벤트에서 최대 인내심이 다른 경우

}
