using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

// ServerData로부터 받아온 서빙 알바의 정보를 나타내는 클래스
public class ServerAgent : MonoBehaviour
{
    //[ReadOnly]
    [SerializeField] private string serverName;
    //[ReadOnly]
    [SerializeField] private ServerStatus status;
    //[ReadOnly]
    [SerializeField] private Vector2 iniPosition;
    //[ReadOnly]
    [SerializeField] private int level;
    [SerializeField] private int serverNumber; // 서빙 알바의 대기 위치를 나타내는 번호

    [ReadOnly][SerializeField] private bool isIdle = true;
    private ServingTask curTask;
    float arrivalThreshold = 0.1f; // 약간의 오차범위
    private Coroutine returnCoroutine;




    void Start()
    {
        TaskManager.Instance.OnTaskAvailable += OnTaskAvailable;
        iniPosition = ServerManager.Instance.GetInitPosition(serverNumber);
    }

    private void OnTaskAvailable()
    {
        if (!isIdle) { return; }
        TryClaimTask();
    }

    private void TryClaimTask()
    {
        ServingTask task = TaskManager.Instance.ClaimTask(this);
        if (task == null)
        {
            if (Vector2.Distance(transform.position, iniPosition) > arrivalThreshold && returnCoroutine == null)
            {
                returnCoroutine = StartCoroutine(ReturnToBase());
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

    private IEnumerator ReturnToBase()
    {
        while (Vector2.Distance(transform.position, iniPosition) > arrivalThreshold)
        {
            transform.position = Vector2.MoveTowards(transform.position, iniPosition, status.speed * Time.deltaTime);
            yield return null;
        }
        returnCoroutine = null;
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
                transform.position = Vector2.MoveTowards(transform.position, target, status.speed * Time.deltaTime);
                yield return null;
            }
            // 실제로 올바른 위치에 도달하면
            if (task.Customer == null)
            {
                isIdle = true;
                TryClaimTask();
                yield break;
            }
            task.Customer.ReceiveOrder();
        }


        else if (task.TypeTask == ServingTaskType.DeliverFood)
        {
            target = ServerManager.Instance.GetKitchenPosition();

            while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, status.speed * Time.deltaTime);
                yield return null;
            }
            Debug.Log("음식을 수령 중입니다.");
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
                transform.position = Vector2.MoveTowards(transform.position, target, status.speed * Time.deltaTime);
                yield return null;
            }

            task.Customer.ReceiveFood();
        }

        yield return new WaitForSeconds(1f);
        isIdle = true;
        TryClaimTask();
    }

    public void InitialServerSetting(ServerData data, int number)
    {
        serverName = data.serverName;
        level = data.level;
        status = data.status;
        serverNumber = number;
        iniPosition = ServerManager.Instance.GetInitPosition(number);
    }


    private void OnDestroy()
    {
        TaskManager.Instance.OnTaskAvailable -= OnTaskAvailable;
    }


}
