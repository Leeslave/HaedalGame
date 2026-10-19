using System.Collections.Generic;
using UnityEngine;

public class HallSystem : MonoBehaviour
{
    public static HallSystem Instance;
    private ServingTaskQueue taskQueue = new ServingTaskQueue();
    private readonly Queue<ServerAgent> idleAgents = new Queue<ServerAgent>();


    // 씬이 켜졌을 때 이벤트를 등록하고, 꺼질 때 이벤트 해제한다.
    void Awake()
    {
        Instance = this;
        taskQueue.OnTaskEnqueue += DispatchPending;
    }

    void OnDestroy()
    {
        taskQueue.OnTaskEnqueue -= DispatchPending;
    }

    // 알바가 유휴 상태가 될 때(작업 완료 직후, 초기화 시 포함) 호출한다.
    // 먼저 대기열 맨 뒤에 줄을 세운 뒤 배정을 시도하므로, 방금 유휴가 된 알바가
    // 더 오래 기다린 다른 알바를 제치고 다음 작업을 새치기하는 일이 없다.
    public void RequestTask(ServerAgent agent)
    {
        idleAgents.Enqueue(agent);
        agent.GoIdle();
        DispatchPending();
    }

    // 새 작업이 큐에 들어오거나 알바가 유휴 상태가 될 때마다 대기 중인 알바에게 순서대로(FIFO) 배정한다.
    private void DispatchPending()
    {
        while (idleAgents.Count > 0)
        {
            ServingTask task = taskQueue.GetHighestPriority();
            if (task == null) { break; }

            ServerAgent agent = idleAgents.Dequeue();
            taskQueue.TryClaim(task, agent);
            agent.AssignTask(task);
        }
    }

    // 손님 착석 시
    public void RegisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived += HandleOrderReceived;
        customer.OnOrderTaken += HandleOrderTaken;
        customer.OnExited += UnregisterCustomer;
    }

    // 손님 퇴장 시
    private void UnregisterCustomer(CustomerAgent customer)
    {
        customer.OnOrderReceived -= HandleOrderReceived;
        customer.OnOrderTaken -= HandleOrderTaken;
        customer.OnExited -= UnregisterCustomer;
    }

    // 손님이 메뉴 결정 시
    private void HandleOrderReceived(CustomerAgent customer)
    {
        taskQueue.Enqueue(new ServingTask(ServingTaskType.TakeOrder, customer));
    }

    // 서버가 손님의 주문을 수주했을 시
    private void HandleOrderTaken(CustomerAgent customer)
    {
        KitchenSystem.Instance.HandleOrderReceived(customer);
    }

    // 쿠커로부터 Task를 전달 받았을 시
    public void HandleFoodReceived(CookingTask task)
    {
        taskQueue.Enqueue(new ServingTask(ServingTaskType.DeliverFood, task.GetCustomerAgent()));
    }

    // 만약 손님의 우선순위가 높아졌을 시
    public void BoostCustomer(CustomerAgent customer)
    {
        taskQueue.Boost(customer);
    }

    public void ResetDay()
    {
        idleAgents.Clear();
    }
}