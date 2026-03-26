using UnityEngine;

// 이 코드는 좌석 오브젝트에 붙어야 하는 코드
public class Seat : MonoBehaviour
{
    [SerializeField] private Transform seatPoint;               // 좌석의 정확한 위치
    private CustomerAgent occupant;                             // 점유하고 있는 손님 [EX] occupant ==  null => 아무도 없음. [But] occupant != null => 누군가 자리를 점유하고 있음
    public Transform GetSeatPoint() { return seatPoint; }       // 현재 자리의 위치가 어딘지 리턴

    public int seatNumber = 0;

    private bool isOccupied = false;                            // 현재 누군가 자리를 점유하고 있는지를 보여주는 flag
    public bool GetIsOccupied() { return isOccupied; }          // 현재 누군가 자리를 점유하고 있는지 리턴
    public CustomerAgent GetOccupant() { return occupant; }     // 그럼 누가 점유하고 있는지 리턴

    void Start()
    {
        isOccupied = false;                                     // 처음 시작할 때에는 아무도 점유를 하고 있어서는 안됨.
    }

    public bool TryOccupy(CustomerAgent customer)               // 누군가가 이 Seat를 점유하려고 시도하는 함수
    {
        if (isOccupied)                                         // 이미 누군가가 있다면
        { 
            Debug.Log("이미 누군가 이 자리를 점유하고 있습니다."); 
            return false;                                       // false 리턴
        }

        occupant = customer;                                    // 그렇지 않다면 현재 자리의 주인을 인수인 customer로 갱신하고
        isOccupied = true;                                      // 자리를 차지하고 있다는 flag를 갱신
        return true;                                            // 지금 인원이 자리를 잡았으니 잡았다고 SeatManager에게 알려줘야함.
    }

    public void Vacate()                                        // 자리를 빼는 함수
    {
        occupant = null;                                        // 그 고객은 이제 없고
        isOccupied = false;                                     // 다시 공석임을 나타내줌
    }



}