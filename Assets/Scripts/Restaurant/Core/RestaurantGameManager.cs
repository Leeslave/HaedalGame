using Unity.VisualScripting;
using UnityEngine;

public class RestaurantGameManager : MonoBehaviour
{
    public static RestaurantGameManager instance { get; private set; }
    

    // public SeatManager seatManager { get; private set; }
    // public OrderManager orderManager { get; private set; }
    // public RatingSystem ratingSystem { get; private set; }
    // public CustomerSpawner customerSpawner { get; private set; }
    // public KitchenSystem kitchenSystem { get; private set; }

    public SeatManager seatManager;
    public OrderManager orderManager;
    public RatingSystem ratingSystem;
    public CustomerSpawner customerSpawner;
    public KitchenSystem kitchenSystem;

    void Awake()
    {
        seatManager = GetComponentInChildren<SeatManager>();
        orderManager = GetComponentInChildren<OrderManager>();
        ratingSystem = GetComponentInChildren<RatingSystem>();
        customerSpawner = GetComponentInChildren<CustomerSpawner>();
        kitchenSystem = GetComponentInChildren<KitchenSystem>();
    }
}
