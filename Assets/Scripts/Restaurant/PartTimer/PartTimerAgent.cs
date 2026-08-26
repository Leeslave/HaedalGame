using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartTimerAgent : MonoBehaviour
{
    [Header("공통 사항")]
    [ReadOnly][SerializeField] protected string partTimerName;
    [SerializeField] protected Vector2 initPosition;
    [ReadOnly][SerializeField] protected string level;
    [SerializeField] protected int positionNumber;
    [SerializeField] protected float arrivalThreshold;

    // arrivalThreshold가 0(또는 음수)이면 "좌표가 정확히 일치해야 도착"으로 취급되는데,
    // transform.position은 부동소수점 오차 때문에 목표 지점과 정확히 같아지지 않을 수 있어
    // 이동 루프가 끝나지 않을 위험이 있다. 최소 허용 오차로 이를 방지한다.
    private const float MinArrivalThreshold = 0.05f;

    protected PartTimerMovement nm;

    protected void Awake()
    {
        nm = GetComponent<PartTimerMovement>();
    }

    // MoveTo의 성공 여부를 호출자에게 돌려주기 위한 out 파라미터용 결과 상자.
    // IEnumerator는 return으로 값을 못 주므로 이 방식을 사용한다.
    protected class MoveResult
    {
        public bool Success;
    }

    // 알바의 유일한 이동 수단. A*(PathfindingGrid + Pathfinder)로 경로를 구해 그대로 따라간다.
    // 목적지에 도달하지 못하면(경로 탐색 실패 등) result.Success가 false로 남는다.
    // 호출부는 도착을 전제로 하는 다음 상태(조리 시작, 주문 접수 등)로 넘어가기 전에 반드시 이 값을 확인해야 한다.
    protected IEnumerator MoveTo(Vector2 destination, float speed, MoveResult result)
    {
        result.Success = false;

        if (speed <= 0f)
        {
            Debug.LogError($"[{name}] MoveTo: 이동 속도가 {speed}입니다. PartTimerStatus 설정을 확인하세요. 이동을 건너뜁니다.");
            yield break;
        }

        if (RestaurantRatingManager.Instance != null) { speed *= RestaurantRatingManager.Instance.StaffSpeedMultiplier; }
        speed *= RestaurantSpeedController.SpeedMultiplier;

        PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        PathNode endNode = PathfindingGrid.Instance.GetNodeFromWorld(destination);
        if (startNode == null || endNode == null) { yield break; }

        List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);
        if (path == null) { yield break; }

        yield return FollowPath(path, speed);
        result.Success = true;
    }

    // 도착 여부를 신경 쓰지 않는 호출부(대기 위치 복귀 등)를 위한 편의 오버로드.
    protected IEnumerator MoveTo(Vector2 destination, float speed)
    {
        yield return MoveTo(destination, speed, new MoveResult());
    }

    // 일시적으로 경로가 막힌 상황(다른 알바/손님이 타일을 점유 등)을 감안해 제한된 횟수만큼 재시도한다.
    // 그래도 실패하면 result.Success는 false로 남고, 호출부가 작업 포기 등으로 처리해야 한다.
    protected IEnumerator MoveToWithRetry(Vector2 destination, float speed, MoveResult result, int maxAttempts = 3, float retryDelay = 0.5f)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            yield return MoveTo(destination, speed, result);
            if (result.Success) { yield break; }
            yield return new WaitForSeconds(retryDelay);
        }
    }

    // 대상 타일 자체가 walkable하지 않을 때(손님 좌석 등) 그 옆의 walkable 타일을 찾는다.
    protected PathNode FindApproachNode(PathNode targetNode)
    {
        if (targetNode == null) { return null; }

        Vector2Int[] directions = { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };
        foreach (Vector2Int dir in directions)
        {
            PathNode neighbor = PathfindingGrid.Instance.GetNode(targetNode.gridPos + dir);
            if (neighbor != null && neighbor.walkable) { return neighbor; }
        }
        return null;
    }

    private IEnumerator FollowPath(List<Vector3> path, float speed)
    {
        float threshold = arrivalThreshold > 0f ? arrivalThreshold : MinArrivalThreshold;

        nm?.SetMoving(true);
        foreach (Vector3 wayPoint in path)
        {
            while (Vector2.Distance(transform.position, wayPoint) > threshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, wayPoint, speed * Time.deltaTime);
                Vector2 dir = (wayPoint - transform.position).normalized;
                nm?.SetDirection(dir);
                yield return null;
            }
        }
        nm?.SetMoving(false);
    }
}
