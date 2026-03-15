using System;
using UnityEngine;

public class KitchenSystem : MonoBehaviour
{
    public static KitchenSystem Instance;
    public event Action OnTaskAvailable;
    private CookingTaskQueue taskQueue = new CookingTaskQueue();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        taskQueue.OnTaskEnqueue += RelayTaskEnqueue;
    }

    private void RelayTaskEnqueue()
    {
        OnTaskAvailable?.Invoke();
    }

    public CookingTask ClaimTask(ChefAgent worker)
    {
        CookingTask task = taskQueue.GetNext();
        if (task == null) { return null; }
        if (taskQueue.TryClaim(task,worker)) { return task; }
        else return null;

    }

    public void RegisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived += HandleOrderReceived;
        customer.OnExited += UnregisterCustomer;
    }

    private void UnregisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived -= HandleOrderReceived;
        customer.OnExited -= UnregisterCustomer;
    }

    private void HandleOrderReceived(CustomerAgent customer)
    {
        OrderFoodData data = customer.GetOrderFoodData();
        if (data != null) { taskQueue.Enqueue(new CookingTask(data.cookingDiff,customer, data.cookingType, data.cookingTime));}
        else {Debug.Log("주문한 요리가 없습니다!");}
    }

    
}
