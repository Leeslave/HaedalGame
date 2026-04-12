using System;
using UnityEngine;

public class HallSystem : MonoBehaviour
{
    public static HallSystem Instance;
    public event Action OnTaskAvailable;
    private ServingTaskQueue taskQueue = new ServingTaskQueue();

    void Awake()
    {
        Instance = this;
    }

    void Start() { }

    public void Initialize()
    {
        taskQueue.OnTaskEnqueue += RelayTaskEnqueue;
    }

    private void RelayTaskEnqueue()
    {
        OnTaskAvailable?.Invoke();
    }

    public ServingTask ClaimTask(ServerAgent worker)
    {
        ServingTask task = taskQueue.GetHighestPriority();
        if (task == null) { return null; }
        if (taskQueue.TryClaim(task, worker)) return task;
        else return null;
    }

    public void RegisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived += HandleOrderReceived;
        customer.OnOrderTaken += HandleOrderTaken;
        customer.OnExited += UnregisterCustomer;
    }

    private void UnregisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived -= HandleOrderReceived;
        customer.OnOrderTaken -= HandleOrderTaken;
        customer.OnExited -= UnregisterCustomer;
    }

    private void HandleOrderReceived(CustomerAgent customer)
    {
        taskQueue.Enqueue(new ServingTask(ServingTaskType.TakeOrder, customer));
    }

    private void HandleOrderTaken(CustomerAgent customer)
    {
        KitchenSystem.Instance.HandleOrderReceived(customer);
    }

    public void HandleFoodReceived(CookingTask task)
    {
        taskQueue.Enqueue(new ServingTask(ServingTaskType.DeliverFood, task.GetCustomerAgent()));
    }

    public void BoostCustomer(CustomerAgent customer)
    {
        taskQueue.Boost(customer);
    }
}