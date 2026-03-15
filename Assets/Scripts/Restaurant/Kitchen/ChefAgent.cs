using UnityEngine;
using System.Collections;

public class ChefAgent : PartTimerAgent
{
    [SerializeField] private PartTimerStatus status;
    private CookingTask curTask;
    [ReadOnly][SerializeField] private Transform target;

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
        Transform target = null;
        CookingType curType = task.GetCookingType();
        if (curType != CookingType.none)
        {
            target = ChefManager.Instance.GetCookingToolTransform(curType);
        }
        if (target == null) { yield break; }
        yield return new WaitForSeconds(1f);

        while (Vector2.Distance(transform.position, target.position) > arrivalThreshold)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, status.serving * Time.deltaTime);
            yield return null;
        }

        if (task.Customer == null)
        {
            isIdle = true;
            TryClaimTask();
            yield break;
        }

        yield return StartCoroutine(ExecuteCooking(task));

        target.position = ChefManager.Instance.GetKitchenPosition();

        while (Vector2.Distance(transform.position, target.position) > arrivalThreshold)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, status.serving * Time.deltaTime);
            yield return null;
        }

        if (task.Customer == null)
        {
            Debug.Log("요리가 완성되었지만 손님이 가셔서 폐기처분 했습니다.");

        }
        isIdle = true;
        TryClaimTask();
        yield break;

    }

    private IEnumerator ExecuteCooking(CookingTask task)
    {
        // 뭐 UI 변경 하겠지
        yield return new WaitForSeconds(task.CookingTime);

    }





    private void OnDestroy()
    {
        KitchenSystem.Instance.OnTaskAvailable -= OnTaskAvailable;
    }

}