using UnityEngine;
using System.Collections;

public class ChefAgent : PartTimerAgent
{
    [SerializeField] private PartTimerStatus status;
    private CookingTask curTask;

    void Start()
    {
        KitchenSystem.Instance.OnTaskAvailable += OnTaskAvailable;
        initPosition = ChefManager.Instance.GetInitPosition(positionNumber);
    }
    private void OnTaskAvailable()
    {
        if (!isIdle) { return; }
        TryClaimTask();
    }

    private void TryClaimTask()
    {
        CookingTask task = KitchenSystem.Instance.ClaimTask(this);
        if (task == null)
        {
            if (Vector2.Distance(transform.position, initPosition) > arrivalThreshold && returnCoroutine == null)
            {
                returnCoroutine = StartCoroutine(ReturnToBase(status.serving)); // 서빙 속도에 따라서 스피드 변경
            }
            return;
        } // 이미 다른 알바가 다 점유하고 있거나 작업이 없음

        // 새 태스크가 있으면 복귀 중단
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        curTask = task;
        isIdle = false;
        StartCoroutine(ExecuteTask(curTask));
    }

    private IEnumerator ExecuteTask(CookingTask task)
    {
        /* */
        yield return new WaitForSeconds(1f);
    }



    private void OnDestroy()
    {
        KitchenSystem.Instance.OnTaskAvailable -= OnTaskAvailable;
    }

}