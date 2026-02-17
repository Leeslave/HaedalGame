using System;
using System.Collections.Generic;

public class ServingTaskQueue
{
    private readonly List<ServingTask> tasks = new List<ServingTask>();

    public event Action OnTaskEnqueued;
    public event Action OnTaskCompleted;

    public int Count => tasks.Count;

    public void Enqueue(ServingTask task)
    {
        tasks.Add(task);
        OnTaskEnqueued?.Invoke();
    }    

    public ServingTask GetHighestPriority() // 가장 먼저 처리해야하는 일을 서치
    {
        ServingTask best = null;
        float minPatience = float.MaxValue;

        foreach (var task in tasks)
        {
            float patience = task.Customer.Patience;
            if (patience < minPatience)
            {
                minPatience = patience;
                best = task;
            }
        }
        return best;
    }

    public ServingTask FindByCustomer(CustomerAgent customer)
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

    public void Complete(ServingTask task)
    {
        task.State = TaskState.Done;
        tasks.Remove(task);
        OnTaskCompleted?.Invoke();
    }


}