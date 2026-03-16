using System;
using System.Collections;
using UnityEngine;

// ServerData로부터 받아온 서빙 알바의 정보를 나타내는 클래스
public class ServerAgent : PartTimerAgent
{
    [SerializeField] private PartTimerStatus status;
    private ServingTask curTask;

    void Start()
    {
        isIdle = true;
        HallSystem.Instance.OnTaskAvailable += OnTaskAvailable;
        initPosition = ServerManager.Instance.GetInitPosition(positionNumber);
    }

    private void OnTaskAvailable()
    {
        if (!isIdle) { return; }
        TryClaimTask();
    }

    private void TryClaimTask()
    {
        ServingTask task = HallSystem.Instance.ClaimTask(this);
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

        if (task.Customer.GetCheckBoost())
        {
            Debug.Log("**※※※** Boost한 업무를 수주했습니다.");
            task.Customer.SetCheckBoost(false);
        }
        curTask = task;
        isIdle = false;
        StartCoroutine(ExecuteTask(curTask));
    }

    private IEnumerator ExecuteTask(ServingTask task)
    {
        Vector2 target = Vector2.zero;
        // moving logic(non A*)
        if (task.TypeTask == ServingTaskType.TakeOrder)
        {
            target = task.Customer.transform.position;


            while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
                yield return null;
            }
            // 실제로 올바른 위치에 도달하면
            if (task.Customer == null)
            {
                isIdle = true;
                TryClaimTask();
                yield break;
            }
            TaskLogger.Instance.LogServing($"현재 {positionNumber}번째 직원이 {task.Customer.GetOrderFoodData().foodName}주문을 받았습니다.");
            TaskLogger.Instance.LogCooking($"현재 {task.Customer.GetOrderFoodData().foodName}주문이 들어왔습니다.");
            task.Customer.ReceiveOrder();
        }


        else if (task.TypeTask == ServingTaskType.DeliverFood)
        {
            target = ServerManager.Instance.GetKitchenPosition();

            while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
                yield return null;
            }
            Debug.Log("음식을 수령 중입니다.");
            TaskLogger.Instance.LogServing($"현재 {positionNumber}번째 직원이 {task.Customer.GetOrderFoodData().foodName}음식을 받았습니다.");
            yield return new WaitForSeconds(2f);

            if (task.Customer == null)
            {
                isIdle = true;
                TryClaimTask();
                yield break;
            }

            target = task.Customer.transform.position;

            while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
                yield return null;
            }

            task.Customer.ReceiveFood();
        }

        yield return new WaitForSeconds(1f);
        isIdle = true;
        TryClaimTask();
    }

    public void InitialServerSetting(PartTimerData data, int number)
    {
        partTimerName = data.serverName;
        level = data.level;
        status = data.status;
        positionNumber = number;
        initPosition = ServerManager.Instance.GetInitPosition(number);
    }


    private void OnDestroy()
    {
        HallSystem.Instance.OnTaskAvailable -= OnTaskAvailable;
    }


}
