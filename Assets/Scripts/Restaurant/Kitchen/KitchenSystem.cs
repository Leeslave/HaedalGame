using System.Collections.Generic;
using UnityEngine;

// 

public class KitchenSystem : MonoBehaviour
{
    public static KitchenSystem Instance;
    private CookingTaskQueue taskQueue = new CookingTaskQueue();
    private readonly Queue<ChefAgent> idleAgents = new Queue<ChefAgent>();

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
    public void RequestTask(ChefAgent agent)
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
            CookingTask task = taskQueue.GetNext();
            if (task == null) { break; }

            ChefAgent agent = idleAgents.Dequeue();
            taskQueue.TryClaim(task, agent);
            agent.AssignTask(task);
        }
    }

    // 요리 수주. 서버로부터 Task를 전달 받음
    public void HandleOrderReceived(CustomerAgent customer)
    {
        RecipeData data = customer.coc.GetOrderData();
        if (data != null) 
        { 
            taskQueue.Enqueue(new CookingTask(customer, (CookingType)data.ClassId, data.CookTime)); 
        }
        else { Debug.Log("주문한 요리가 없습니다!"); }
    }

    // 요리 완료. 서버에게 Task를 전달
    public void CompleteFood(CookingTask task)
    {
        HallSystem.Instance.HandleFoodReceived(task);
    }

    public void ResetDay()
    {
        idleAgents.Clear();
    }
}
