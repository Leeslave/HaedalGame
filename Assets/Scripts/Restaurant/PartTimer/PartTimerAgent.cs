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
    [ReadOnly][SerializeField] protected bool isIdle;
    [SerializeField] protected float arrivalThreshold;

    protected Coroutine returnCoroutine;

    protected PartTimerMovement nm;

    protected void Awake()
    {
        nm = GetComponent<PartTimerMovement>();
    }

    protected IEnumerator ReturnToBase(float speed)
    {
        PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        PathNode endNode   = PathfindingGrid.Instance.GetNodeFromWorld(initPosition);

        if (startNode != null && endNode != null)
        {
            List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);
            if (path != null)
            {
                yield return StartCoroutine(MoveAlongPath(path, speed));
            }
        }

        returnCoroutine = null;
    }

    protected IEnumerator MoveAlongPath(List<Vector3> path, float speed)
    {
        nm?.SetMoving(true);
        foreach (Vector3 wayPoint in path)
        {
            while (Vector2.Distance(transform.position, wayPoint) > arrivalThreshold)
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