using UnityEngine;

public class WaitingGroup : MonoBehaviour
{
    [ReadOnly][SerializeField] private Seat[] seats;

    void Awake() { seats = GetComponentsInChildren<Seat>(); }
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
