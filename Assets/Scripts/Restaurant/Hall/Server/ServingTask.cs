using UnityEngine;

public enum ServingTaskType
{
    TakeOrder, DeliverFood
}

public enum TaskState
{
    Wating, Claimed, Done
}

public class ServingTask
{
    public ServingTaskType TypeTask { get; private set; }
    public CustomerAgent Customer { get; private set; }
    public TaskState State { get; set; }
    public ServerAgent AssginedWorkder { get; set; }
    public float EnqueuedTime { get; private set; }

    public ServingTask(ServingTaskType type, CustomerAgent customer)
    {
        TypeTask = type;
        Customer = customer;
        State = TaskState.Wating;
        EnqueuedTime = Time.time;
    }
}