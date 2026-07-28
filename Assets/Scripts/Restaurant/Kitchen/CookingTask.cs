using UnityEngine;

public enum CookingDiff
{
    Easy,
    Normal,
    Hard
}

public enum CookingType
{
    None    = 0,
    Pan     = 201,  // 프라이팬 요리
    Chop    = 202,  // 불을 쓰지 않는 요리
    Fry     = 203,  // 튀김 요리
    Pot     = 204   // 국, 죽 등의 냄비 요리
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

    public CookingTask( CustomerAgent customer, CookingType type = CookingType.None, float time = 10.0f)
    {
        State = TaskState.Waitng;
        Type = type;
        EnqueuedTime = Time.time;
        CookingTime = time;
        Customer = customer;
    }
}