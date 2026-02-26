using System.Collections.Generic;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    // 현재 좌석들을 Inspector에서 확인할 수 있게 SerializeField로 선언
    [SerializeField] private List<Seat> seats = new List<Seat>();
    [SerializeField] private List<Seat> waitingSeats = new List<Seat>();

    [ReadOnly][SerializeField] private int indoorSeat;      // 현재 가게에 앉을 수 있는 좌석의 수
    [ReadOnly][SerializeField] private int seatedCount;     // 현재 가게에 손님이 앉아있는 좌석의 수
    [ReadOnly][SerializeField] private int outdoorSeat;     // 현재 웨이팅을 할 수 있는 좌석의 수
    [ReadOnly][SerializeField] private int waitingCount;    // 현재 웨이팅을 하고있는 좌석의 수

    // ============================================
    /* Getter and Setter */
    public void SetMaxSeat(int upgrade) { indoorSeat = upgrade; }       // 가게 업그레이드로 인한 내부 좌석 수 증감

    public void SetMaxWaiting(int upgrade) { outdoorSeat = upgrade; }    // 가게 업그레이드로 인한 외부 좌석 수 증감


    private void Awake()
    {
        seatedCount = 0;
        waitingCount = 0;
    }

    public void RegisterSeats(List<TableGroup> sortedTables)
    {
        seats.Clear();
        foreach (var table in sortedTables)
        {
            seats.AddRange(table.GetSeats());
        }
    }

    public void RegisterWaitingSeats(List<WaitingGroup> waiting)
    {
        waitingSeats.Clear();
        foreach (var seat in waiting)
        {
            waitingSeats.AddRange(seat.GetSeats());
        }
    }


    // 손님 오브젝트가 좌석을 앉으려고 시도하는 함수
    public Seat TryAssignSeat(CustomerAgent customer, out bool indoor)
    {
        indoor = false;
        for (int i = 0; i < seats.Count; i++) // 전체 좌석을 순회하면서
        {
            if (!seats[i].GetIsOccupied() && seats[i].TryOccupy(customer)) // 남는 좌석이 있다면
            {
                Debug.Log($"{i + 1}번째 자리를 고객이 차지하였습니다.");
                seatedCount++;
                indoor = true;
                return seats[i]; // 좌석 배정
            }
        }
        for (int i = 0; i < waitingSeats.Count; i++)
        {
            if (!waitingSeats[i].GetIsOccupied() && waitingSeats[i].TryOccupy(customer)) // 남는 좌석이 있다면
            {
                Debug.Log($"{i + 1}번째 웨이팅 자리를 고객이 차지하였습니다.");
                waitingCount++;
                return waitingSeats[i]; // 좌석 배정
            }
        }
        return null; // 그렇지 않다면 null를 리턴하여 자리가 없다는 것을 customer에게 전달
    }

    public void ReleaseSeat(Seat seat, bool isWaiting) // 자리를 비우게 하는 함수
    {
        if (seat == null) { return; } // 이미 공석이라면 리턴
        seat.Vacate(); // 공석이 아니라면 공석으로 만들라고 전달.
        if (!isWaiting) seatedCount--;
        else waitingCount--;
        TryAwakeWaitingCustomer();
    }

    private void TryAwakeWaitingCustomer()
    {
        Seat emptySeat = null;
        for (int i = 0; i < seats.Count; i++)
        {
            if (!seats[i].GetIsOccupied()) { emptySeat = seats[i]; break; } // 빈 자리가 있으면 해당 자리를 가져옴
        }
        if (emptySeat == null) return;

        for (int i = 0; i < waitingSeats.Count; i++)
        {
            if (waitingSeats[i].GetIsOccupied())
            {
                CustomerAgent customer = waitingSeats[i].GetOccupant();
                waitingSeats[i].Vacate();
                waitingCount--;
                emptySeat.TryOccupy(customer);
                seatedCount++;
                customer.PromoteToSeat(emptySeat);

                ShiftWaitingCustomersForward(i);
                return;
            }

        }

    }
    private void ShiftWaitingCustomersForward(int fromIndex)
    {
        for (int i = fromIndex + 1; i < waitingSeats.Count; i++)
        {
            if (!waitingSeats[i].GetIsOccupied()) break;

            CustomerAgent next = waitingSeats[i].GetOccupant();
            waitingSeats[i].Vacate();
            waitingSeats[i - 1].TryOccupy(next);
            next.MoveWaitingSeat(waitingSeats[i - 1]);
        }
    }


}
