using System;
using UnityEngine;

public class RestaurantGameManager : MonoBehaviour
{
    public static RestaurantGameManager instance { get; private set; }
    
    public SeatManager seatManager;
    public OrderManager orderManager;
    public RatingSystem ratingSystem;
    public CustomerSpawner customerSpawner;
    public KitchenSystem kitchenSystem;

    [SerializeField] public FoodDatabase foodDatabase;
    public MenuData menuData = new MenuData();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        seatManager = GetComponentInChildren<SeatManager>();
        orderManager = GetComponentInChildren<OrderManager>();
        ratingSystem = GetComponentInChildren<RatingSystem>();
        customerSpawner = GetComponentInChildren<CustomerSpawner>();
        kitchenSystem = GetComponentInChildren<KitchenSystem>();
    }
}
