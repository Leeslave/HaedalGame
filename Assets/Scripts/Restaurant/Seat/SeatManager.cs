using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    // 현재 좌석들을 Inspector에서 확인할 수 있게 SerializeField로 선언
    [SerializeField] private List<Seat> seats = new List<Seat>();
    [SerializeField] private GameObject seatParent;

    private void Awake()
    {
        if (seats.Count == 0) // Inspector에서 직접 넣은 좌석이 아니라면
        {
            seats.AddRange(seatParent.GetComponentsInChildren<Seat>()); // 알아서 찾도록 하기
        }
    }

    // 손님 오브젝트가 좌석을 앉으려고 시도하는 함수
    public Seat TryAssignSeat(CustomerAgent customer) 
    {
        for (int i = 0; i < seats.Count; i++) // 전체 좌석을 순회하면서
        {
            if (!seats[i].GetIsOccupied() && seats[i].TryOccupy(customer)) // 남는 좌석이 있다면
            {
                Debug.Log($"{i}번째 자리를 고객이 차지하였습니다.");
                return seats[i]; // 좌석 배정
            }
        }
        return null; // 그렇지 않다면 null를 리턴하여 자리가 없다는 것을 customer에게 전달
    }

    public void ReleaseSeat(Seat seat) // 자리를 비우게 하는 함수
    {
        if (seat == null) { return; } // 이미 공석이라면 리턴
        seat.Vacate(); // 공석이 아니라면 공석으로 만들라고 전달.
    }
}
