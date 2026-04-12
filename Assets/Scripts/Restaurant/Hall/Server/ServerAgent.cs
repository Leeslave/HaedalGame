using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ServerData로부터 받아온 서빙 알바의 정보를 나타내는 클래스
public class ServerAgent : PartTimerAgent
{
    [SerializeField] private PartTimerStatus status;
    private ServingTask curTask;

    void Start() { }

    public void Initialize()
    {
        isIdle = true;
        HallSystem.Instance.OnTaskAvailable += OnTaskAvailable;
        initPosition = ServerManager.Instance.GetInitPosition(positionNumber);
    }

    private void OnTaskAvailable()
    {
        Debug.LogWarning("알바가 움직입니다.");
        if (!isIdle) { return; }
        TryClaimTask();
    }

    private void TryClaimTask()
    {
        ServingTask task = HallSystem.Instance.ClaimTask(this);
        if (task == null)
        {
            Debug.LogWarning("Task가 없습니다.");
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

        Debug.LogWarning("Task를 수주했습니다..");

        if (task.Customer.cbc.GetCheckBoost())
        {
            Debug.Log("**※※※** Boost한 업무를 수주했습니다.");
            task.Customer.cbc.SetCheckBoost(false);
        }
        curTask = task;
        isIdle = false;
        StartCoroutine(ExecuteTask(curTask));
    }

    private IEnumerator ExecuteTask(ServingTask task)
    {
        // Vector2 target = Vector2.zero;
        // moving logic(non A*)
        // if (task.TypeTask == ServingTaskType.TakeOrder)
        // {
        //     target = task.Customer.transform.position;
        //     PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        //     PathNode endNode = PathfindingGrid.Instance.GetNodeFromWorld(target);
        //     List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);

        //     // while (Vector2.Distance(transform.position, target) > arrivalThreshold)
        //     // {
        //     //     transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
        //     //     yield return null;
        //     // }

        //     if (path != null)
        //     {
        //         yield return StartCoroutine(MoveAlongPath(path, status.serving));    
        //     }


        //     // 실제로 올바른 위치에 도달하면
        //     if (task.Customer == null)
        //     {
        //         isIdle = true;
        //         TryClaimTask();
        //         yield break;
        //     }
        //     task.Customer.ReceiveOrder();
        // }
        if (task.TypeTask == ServingTaskType.TakeOrder)
        {
            PathNode customerNode = PathfindingGrid.Instance.GetNodeFromWorld(task.Customer.transform.position);
            if (customerNode == null) { isIdle = true; TryClaimTask(); yield break; }

            PathNode approachNode = FindApproachNode(customerNode);

            if (approachNode != null)
            {
                PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
                if (startNode == null) { isIdle = true; TryClaimTask(); yield break; }

                List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, approachNode.gridPos);
                if (path != null)
                {
                    yield return StartCoroutine(MoveAlongPath(path, status.serving));
                }
            }
            // approachNode == null이면 이동 생략하고 바로 주문 처리

            if (task.Customer == null) { isIdle = true; TryClaimTask(); yield break; }
            task.Customer.ReceiveOrder();
        }


        else if (task.TypeTask == ServingTaskType.DeliverFood)
        {
            Vector2 target = ServerManager.Instance.GetKitchenPosition();

            PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
            PathNode endNode = PathfindingGrid.Instance.GetNodeFromWorld(target);
            List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);

            // while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            // {
            //     transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
            //     yield return null;
            // }

            if (path != null)
            {
                yield return StartCoroutine(MoveAlongPath(path, status.serving));
            }

            yield return new WaitForSeconds(2f);

            if (task.Customer == null)
            {
                isIdle = true;
                TryClaimTask();
                yield break;
            }

            target = task.Customer.transform.position;

            startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
            endNode = PathfindingGrid.Instance.GetNodeFromWorld(target);
            path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);

            // while (Vector2.Distance(transform.position, target) > arrivalThreshold)
            // {
            //     transform.position = Vector2.MoveTowards(transform.position, target, status.serving * Time.deltaTime);
            //     yield return null;
            // }

            if (path != null)
            {
                yield return StartCoroutine(MoveAlongPath(path, status.serving));
            }

            if (task.Customer == null)
            {
                isIdle = true;
                TryClaimTask();
                yield break;
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

    // 손님 주변에서 접근 가능한 인접 타일 탐색 (아래 → 좌 → 우 → 위 순)
    private PathNode FindApproachNode(PathNode customerNode)
    {
        Vector2Int[] directions = { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };
        foreach (Vector2Int dir in directions)
        {
            PathNode neighbor = PathfindingGrid.Instance.GetNode(customerNode.gridPos + dir);
            if (neighbor != null && neighbor.walkable) { return neighbor; }
        }
        return null;
    }


    private void OnDestroy()
    {
        HallSystem.Instance.OnTaskAvailable -= OnTaskAvailable;
    }


}
