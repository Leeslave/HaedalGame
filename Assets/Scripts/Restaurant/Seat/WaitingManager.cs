using System.Collections.Generic;
using UnityEngine;

public class WaitingManager : MonoBehaviour
{
    public static WaitingManager Instance;
    [ReadOnly] public List<WaitingGroup> waitingSeats;

    void Awake()
    {
        Instance = this;
        waitingSeats = new();
    }

    void Start()
    {
        foreach (var waiting in FindObjectsByType<WaitingGroup>(FindObjectsSortMode.None))
        {
            RegisterSeat(waiting);
        }
    }

    public void RegisterSeat(WaitingGroup waiting)
    {
        if (!waitingSeats.Contains(waiting)) { waitingSeats.Add(waiting); }
        if (RestaurantGameManager.instance?.seatManager == null) return;
        RestaurantGameManager.instance.seatManager.RegisterWaitingSeats(waitingSeats);
    }

    public void UnregisterSeat(WaitingGroup waiting)
    {
        if (waitingSeats.Contains(waiting)) { waitingSeats.Remove(waiting); }
        if (RestaurantGameManager.instance?.seatManager == null) return;
        RestaurantGameManager.instance.seatManager.RegisterWaitingSeats(waitingSeats);
    }
}
