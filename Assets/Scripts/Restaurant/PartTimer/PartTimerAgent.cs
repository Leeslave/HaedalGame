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

    protected IEnumerator ReturnToBase(float speed)
    {
        PathNode startNode = PathfindingGrid.Instance.GetNodeFromWorld(transform.position);
        PathNode endNode   = PathfindingGrid.Instance.GetNodeFromWorld(initPosition);

        if (startNode != null && endNode != null)
        {
            List<Vector3> path = Pathfinder.Instance.FindPath(startNode.gridPos, endNode.gridPos);
            if (path != null)
            {
                foreach (Vector3 wayPoint in path)
                {
                    while (Vector2.Distance(transform.position, wayPoint) > arrivalThreshold)
                    {
                        transform.position = Vector2.MoveTowards(transform.position, wayPoint, speed * Time.deltaTime);
                        yield return null;
                    }
                }
            }
        }

        returnCoroutine = null;
    }

    protected IEnumerator MoveAlongPath(List<Vector3> path, float speed)
    {
        foreach (Vector3 wayPoint in path)
        {
            while (Vector2.Distance(transform.position, wayPoint) > arrivalThreshold)
            {
                transform.position = Vector2.MoveTowards(transform.position, wayPoint, speed * Time.deltaTime);
                yield return null;
            }
        }
    }

}