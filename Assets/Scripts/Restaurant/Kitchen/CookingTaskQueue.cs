using System;
using System.Collections;
using System.Collections.Generic;

public class CookingTaskQueue
{
    private readonly List<CookingTask> tasks = new List<CookingTask>();
    public event Action OnTaskEnqueue;

    public int Count => tasks.Count;
    
    public void Enqueue(CookingTask task)
    {
        tasks.Add(task);
        OnTaskEnqueue?.Invoke();
    }

    public CookingTask GetNext()
    {
        if (tasks.Count == 0) { return null;}
        return tasks[0];
    }

    public bool TryClaim(CookingTask task, ChefAgent worker)
    {
        if (!tasks.Contains(task)) { return false; }
        task.AssignedWorker = worker;
        task.State = TaskState.Claimed;
        tasks.Remove(task);
        return true;
    }
}