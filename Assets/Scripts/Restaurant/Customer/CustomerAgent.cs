using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerAgent : MonoBehaviour
{
    [ReadOnly][SerializeField] private RestaurantGameManager gm;

    [Header("Patience")] // 인내심 관련 변수
    private const float DefaultPatience = 25f;          // 기본 인내심을 이곳에서 정의
    [ReadOnly][SerializeField] private float curPatience = 25f;

    [Header("Timings")]
    [SerializeField] private float eatDuration = 8f;
    [SerializeField] private float payDuration = 2f;

    [Header("Other")]
    [SerializeField] private bool autoTakeOrder = true;
    [SerializeField] private float autoTakeOrderDelay = 1.0f;

    

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
    private void Start()
    {
        curPatience = DefaultPatience;
        currentSeat = null;
        gm = RestaurantGameManager.instance;
        // ChangeState(CustomerState.Enter);
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
    private void TrySeat()
    {
        if (currentSeat != null) { return; } // 이미 자리를 점유하고 있다면 -> 근데 이 코드에 걸릴 일은 없을 듯
        gm = RestaurantGameManager.instance;
        currentSeat = RestaurantGameManager.instance.seatManager.TryAssignSeat(this);

        if (currentSeat == null) // 만약 입장했는데 자리가 없는 경우 <- 이 함수는 어느정도 개선이 필요함
        {
            ratingFlag = RatingFlag.Low;
            Debug.Log($"{gameObject.name}의 이번 식사의 평가는 {ratingFlag}입니다.");
            ChangeState(CustomerState.Exit);
            return;
        }

        transform.position = currentSeat.GetSeatPoint().position; // 이거는 나중에 길찾기 알고리즘 써서 이동하도록 만들기 A*
        ChangeState(CustomerState.WaitingForOrder);

    }

    
    
    
    /* 남은 작업은 order, Patience */

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
        
        Destroy(gameObject);
    }

    // ========================================================================================
    /* [ Event Logic ] */
    private void ResetPatience(bool isEvent = false, float patienceValue = DefaultPatience)
    {
        if (isEvent) { SetMaxPatience(patienceValue); }
    }
    private void SetMaxPatience(float value) { curPatience = value; } // 특정 이벤트에서 최대 인내심이 다른 경우

}
