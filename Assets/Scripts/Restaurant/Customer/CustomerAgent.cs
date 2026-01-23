using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class CustomerAgent : MonoBehaviour
{
    [ReadOnly][SerializeField] private RestaurantGameManager gm;

    [Header("Patience")] // 인내심 관련 변수
    private const float DefaultPatience = 25f;          // 기본 인내심을 이곳에서 정의
    [ReadOnly][SerializeField] private float curPatience = DefaultPatience;

    [SerializeField] private float waitingGraceSec = 3f;    // 웨이팅 룸으로 갔을 때 바로 인내심이 깎이는 것이 아니라 일부 대기 시간 부여
    [SerializeField] private float foodGraceSec = 5f;       // 음식을 주문 했을 때 바로 인내심이 깎이는 것이 아니라 일부 대기 시간 부여
    private float graceTimer;
    

    [Header("Patience Drain")]
    [ReadOnly][SerializeField] private float waitingDrainPerSec = 1.0f;
    [ReadOnly][SerializeField] private float orderDrainPerSec = 1.0f;
    [ReadOnly][SerializeField] private float foodDrainPerSec = 1.0f;
    

    [Header("Timings")]
    [SerializeField] private float eatDuration = 8f;
    [SerializeField] private float payDuration = 2f;
    [SerializeField] private float stateChangeDuration = 1.0f;

    [Header("Other")]
    [SerializeField] private bool autoTakeOrder = true;
    [SerializeField] private float autoTakeOrderDelay = 1.0f;
    
    // Destroy Event
    public Action<CustomerAgent> onExited;

    

    /* [ Runtime State ] */
    private CustomerState state;
    private float stateTimer;
    private RatingFlag ratingFlag = RatingFlag.None;

    private Seat currentSeat;
    private Order currentOrder;

    /* [ public getter ] */
    public CustomerState State => state;
    public float Patience => curPatience;
    public RatingFlag Rating => ratingFlag;
    public Order CurrentOrder => currentOrder;

    /* [ Lifecycle Methods ] */
    private void Awake()
    {
        gm = RestaurantGameManager.instance;
    }

    private void Update()
    {
         UpdateState(Time.deltaTime);
    }

// ========================================================================================
    /* [ State Machine ] */
    private void UpdateState(float dt)
    {
        switch(state)
        {
            case CustomerState.Enter:
                ChangeState(CustomerState.Seating);
                break;
            case CustomerState.Seating:
                TrySeat();
                break;
            case CustomerState.WaitingRoom:
                // 대기실로 이동하는 코드 추가
                break;
            case CustomerState.WaitingForOrder:
                // 기다리는 함수 추가
                break;
            case CustomerState.WaitingForFood:
                // 기다리는 함수 추가
                break;
            case CustomerState.Eating:
                // 먹는 것을 기다리는 함수 추가
                break;
            case CustomerState.Paying:
                // 결제하는 함수 추가
                break;
            case CustomerState.Exit:
                ExitRestaurant();
                break;
        }
    }


    private void ChangeState(CustomerState nextState)
    {
        state = nextState;
        stateTimer = 0f;
        
        switch(state)
        {
            case CustomerState.WaitingForOrder:
                Debug.Log($"{gameObject.name}이 주문을 대기하고 있습니다.");
                break;
            case CustomerState.WaitingForFood:
                Debug.Log($"{gameObject.name}이 음식을 대기하고 있습니다.");
                break;
        }
    }

// ========================================================================================
    /* [ State Logic ] */
    // CustomerState.Seating 
    private void TrySeat()
    {
        if (currentSeat != null) { return; } // 이미 자리를 점유하고 있다면 -> 근데 이 코드에 걸릴 일은 없을 듯
        currentSeat = gm.seatManager.TryAssignSeat(this);

        if (currentSeat == null) // 만약 입장했는데 자리가 없는 경우 <- 이 함수는 어느정도 개선이 필요함
        {
            ratingFlag = RatingFlag.Low;
            Debug.Log($"{gameObject.name}의 이번 식사의 평가는 {ratingFlag}입니다.");
            ChangeState(CustomerState.Exit);
            return;
        }

        transform.position = currentSeat.GetSeatPoint().position; // 이거는 나중에 길찾기 알고리즘 써서 이동하도록 만들기 A*
        //ChangeState(CustomerState.WaitingForOrder);
        StartCoroutine(WaitStateChange(CustomerState.WaitingForOrder));
    }

    // CustomerState.WaitingForOrder
    private void TryOrder()
    {
        
    }

    // CustomState.WaitingForFood
    private void WaitFood()
    {
        
    } 

    // CustomState.Eating
    private void Eating()
    {
        
    }

    // CustomState.Paying
    private void Paying()
    {
        
    }

    // 각 스테이트가 끝날때 마다 1초 정도 기다리고 다음 스테이트로 이동
    private IEnumerator WaitStateChange(CustomerState curState)
    {
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
            gm.seatManager.ReleaseSeat(currentSeat);
            currentSeat = null;
        }
        
        onExited.Invoke(this);
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
        StartCoroutine(WaitStateChange(CustomerState.Enter));
    }

    private void SetPatience(float value) { curPatience = value; } // 특정 이벤트에서 최대 인내심이 다른 경우

}
