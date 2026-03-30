using UnityEngine;

public enum CookingDiff
{
    Easy,
    Normal,
    Hard
}

public enum CookingType
{
    Grill,
    Mix,
    Pot,
    raw,
    dessert,
    none
}


public class CookingTask
{
    public TaskState State { get; set; }
    public CookingDiff Diffcult { get; private set; }
    
    public ChefAgent AssignedWorker { get; set; }
    public CustomerAgent Customer { get; private set; }
    public float CookingTime;
    public CookingType Type;
    public float EnqueuedTime { get; private set; }

    public CookingType GetCookingType() { return Type; }
    public CustomerAgent GetCustomerAgent() { return Customer; }

    public CookingTask( CustomerAgent customer, CookingType type = CookingType.none, float time = 10.0f)
    {
        State = TaskState.Waitng;
        Type = type;
        EnqueuedTime = Time.time;
        CookingTime = time;
        Customer = customer;
    }
}