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

    // 알바의 유일한 이동 수단. A*(PathfindingGrid + Pathfinder)로 경로를 구해 그대로 따라간다.
    // 경로를 못 찾으면 이동 없이 즉시 반환 - 호출부는 이 경우에도 다음 단계로 진행하면 된다.
    protected IEnumerator MoveTo(Vector2 destination, float speed)
    {
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
