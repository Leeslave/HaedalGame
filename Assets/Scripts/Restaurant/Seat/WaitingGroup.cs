using UnityEngine;

public class WaitingGroup : MonoBehaviour
{
    [ReadOnly][SerializeField] private Seat[] seats;

    void Awake() { seats = GetComponentsInChildren<Seat>(); }

    void Start()
    {
        // 웨이팅 좌석도 실제 타일을 등록해야 착석 시 그 타일이 장애물로 막힌다.
        // (PathfindingGrid.Instance는 Awake에서 초기화되므로 Start에서 안전하게 접근 가능)
        foreach (Seat seat in seats)
        {
            Vector2Int gridPos = PathfindingGrid.Instance.WorldToGridPos(seat.GetSeatPoint().position);
            seat.SetGridPos(gridPos);
        }
    }

    public Seat[] GetSeats() { return seats; }

    private void OnEnable()
    {
        if (WaitingManager.Instance == null) return;
        WaitingManager.Instance.RegisterSeat(this);    
    }

    private void OnDisable()
    {
        if (WaitingManager.Instance == null) return;
        WaitingManager.Instance.UnregisterSeat(this);
    }

}
