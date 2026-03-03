using System;
using System.Collections.Generic;

public class ServingTaskQueue
{
    private readonly List<ServingTask> tasks = new List<ServingTask>(); // 현재 수주받은 작업 목록
    public event Action OnTaskEnqueue;
    public int Count => tasks.Count; // 나중에 사용할 수 있으니 둔 변수
    private int curMax = 2; // 우선 순위 (0 = 일반, 1 = 특수, 2~ 부스트)

    public void Enqueue(ServingTask task) // 새로운 태스크 등록
    {
        tasks.Add(task);
        OnTaskEnqueue?.Invoke();
    }    

    public ServingTask GetHighestPriority() // 서빙 알바가 일을 가져갈 때 사용할 함수
    {
        ServingTask best = null;
        foreach (var task in tasks)
        {
            if (best == null) { best = task; }

            if (task.Priority > best.Priority) { best = task; }
        }

        return best; // 현재 해야할 일을 리턴 -> 만약 null이라면 서빙 알바는 원래 자리로 이동하기
    }

    public ServingTask FindByCustomer(CustomerAgent customer) // 만약 부스트 한 태스크가 큐에 등록이 되어있다면 리턴하도록
    {
        return tasks.Find(t => t.Customer == customer);
    }

    // 자동이던 수동이던 모두 이 메서드를 통과해서 태스크를 선점한다. false시 누군가 해당 주문을 선점한 것으로 판단해서 이중 선점을 막는다.
    public bool TryClaim(ServingTask task, ServerAgent worker)
    {
        if (!tasks.Contains(task)) { return false; } // 이미 누군가 해당 주문을 처리하고 있는 중임
        
        task.State = TaskState.Claimed;
        task.AssginedWorkder = worker;
        tasks.Remove(task);
        return true;
    }

    public void Boost(CustomerAgent customer)
    {
        ServingTask task = FindByCustomer(customer);
        if (task != null) 
        { 
            task.Priority = curMax; 
            curMax += 1;
        }
    }
}