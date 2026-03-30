using UnityEngine;

// 서빙 알바가 현재 진행하고 있는 작업
public enum ServingTaskType
{
    TakeOrder,      // 주문을 받는 작업
    DeliverFood     // 요리를 전달하는 작업
}

// 현재 작업의 상태
public enum TaskState
{
    Waitng,     // 주문을 접수받길 기다리는 중 / 요리가 전달되기를 기다리는 중
    Claimed,    // 주문을 누군가 수주함 / 누군가 요리를 배달 중
    Done        // 끝
}

public class ServingTask
{
    public ServingTaskType TypeTask { get; private set; }
    public CustomerAgent Customer { get; private set; }
    public TaskState State { get; set; }
    public ServerAgent AssginedWorker { get; set; }
    public int Priority { get; set; }
    public float EnqueuedTime { get; private set; }


    public ServingTask(ServingTaskType type, CustomerAgent customer)
    {
        TypeTask = type;
        Customer = customer;
        State = TaskState.Waitng;
        EnqueuedTime = Time.time;
    }
}