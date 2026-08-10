using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    // 현재 좌석들을 Inspector에서 확인할 수 있게 SerializeField로 선언
    [SerializeField] private List<Seat> seats = new List<Seat>();
    [SerializeField] private List<Seat> waitingBenchSeats = new List<Seat>();

    // 대기줄의 유일한 소유자. index 0이 맨 앞(다음에 승격될 손님)이고,
    // 리스트 인덱스가 곧 그 손님이 서 있는 웨이팅 벤치의 인덱스와 대응한다.
    // 웨이팅 벤치(Seat)에는 점유 플래그를 걸지 않으므로 한 벤치에 두 명이 배정되는 일이 구조적으로 불가능하다.
    [ReadOnly][SerializeField] private List<CustomerAgent> waitingLine = new List<CustomerAgent>();

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
        waitingBenchSeats.Clear();
        foreach (var group in waiting)
        {
            waitingBenchSeats.AddRange(group.GetSeats());
        }
    }

    public bool HasAvailableSeat()
    {
        if (seats.Any(s => !s.GetIsOccupied())) { return true; }
        return waitingLine.Count < waitingBenchSeats.Count;
    }

    // 손님이 좌석을 배정받으려고 시도하는 함수. 인도어 자리가 없으면 웨이팅 줄 맨 뒤에 등록한다.
    // excludeSeats: 그 손님이 이미 경로 탐색에 실패한 적 있는 좌석들. 같은(도달 불가능한) 좌석을
    // 무한히 재배정받는 것을 막기 위함 (만석+대기열 만석 상태에서 무한 루프로 이어지던 버그 수정).
    public Seat TryAssignSeat(CustomerAgent customer, out bool indoor, HashSet<Seat> excludeSeats = null)
    {
        indoor = false;
        for (int i = 0; i < seats.Count; i++)
        {
            if (excludeSeats != null && excludeSeats.Contains(seats[i])) { continue; }

            if (!seats[i].GetIsOccupied() && seats[i].TryOccupy(customer))
            {
                indoor = true;
                return seats[i];
            }
        }

        if (waitingLine.Count >= waitingBenchSeats.Count) { return null; } // 웨이팅 자리도 꽉 참

        waitingLine.Add(customer);
        return waitingBenchSeats[waitingLine.Count - 1];
    }

    // 자리를 비우게 하는 함수. isWaiting이면 웨이팅 줄에서 해당 손님을 제거하고, 아니면 인도어 좌석을 반납한다.
    public void ReleaseSeat(CustomerAgent customer, Seat seat, bool isWaiting)
    {
        if (isWaiting)
        {
            // 대기 벤치 타일도 착석 시 OnCustomerSeated()로 walkable=false 처리되므로,
            // 여기서 Vacate()로 복원하지 않으면 그 타일이 영구히 막힌 채로 남는다(타일 누수).
            if (seat != null) { seat.Vacate(); }

            if (waitingLine.Remove(customer))
            {
                RepositionWaitingLine();
            }
            return;
        }

        if (seat == null) { return; }
        seat.Vacate();
        PromoteNextFromWaitingLine();
    }

    // 인도어 자리가 하나 비면 웨이팅 줄 맨 앞 손님을 그 자리로 승격시킨다.
    private void PromoteNextFromWaitingLine()
    {
        if (waitingLine.Count == 0) { return; }

        Seat emptySeat = seats.FirstOrDefault(s => !s.GetIsOccupied());
        if (emptySeat == null) { return; }

        CustomerAgent customer = waitingLine[0];
        waitingLine.RemoveAt(0);

        // 승격 경로에서는 ReleaseSeat(isWaiting: true)를 거치지 않으므로, 여기서 직접
        // 대기 벤치 타일을 복원해야 한다 (안 그러면 그 타일이 영구히 막힌다 - 타일 누수).
        Seat oldBenchSeat = customer.GetCurrentSeat();
        if (oldBenchSeat != null) { oldBenchSeat.Vacate(); }

        emptySeat.TryOccupy(customer);
        customer.PromoteToSeat(emptySeat);

        RepositionWaitingLine();
    }

    // 대기줄 순서에 맞춰 남은 손님들을 각자의 웨이팅 벤치 위치로 재배치한다(점유 플래그 조작 없음).
    private void RepositionWaitingLine()
    {
        for (int i = 0; i < waitingLine.Count; i++)
        {
            waitingLine[i].MoveWaitingSeat(waitingBenchSeats[i]);
        }
    }
}
