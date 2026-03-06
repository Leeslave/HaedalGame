using UnityEngine;

public enum CookingDiff
{
    Easy,
    Normal,
    Hard
}


public class CookingTask
{
    public TaskState State { get; set; }
    public CookingDiff diff { get; private set; }
    public ChefAgent AssignedWorker { get; set; }
    public float EnqueuedTime { get; private set; }

    public CookingTask(CookingDiff diffcult)
    {
        State = TaskState.Waitng;
        diff = diffcult;
        EnqueuedTime = Time.time;
    }
}