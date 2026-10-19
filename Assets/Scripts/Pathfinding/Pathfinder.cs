using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance;
    void Awake()
    {
        Instance = this;
    }

    public List<Vector3> FindPath(Vector2Int start, Vector2Int end)
    {
        PathNode startNode = PathfindingGrid.Instance.GetNode(start);
        PathNode endNode = PathfindingGrid.Instance.GetNode(end);

        if (startNode == null || endNode == null) { return null; }          // 만약 시작노드 혹은 엔드 노드가 없다면, 이동 X
        if (!endNode.walkable) { return null; }                             // 만약 엔드노드가 장애물이라면 이동 X

        Heap<PathNode> openSet = new Heap<PathNode>(PathfindingGrid.Instance.NodeCount);
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = GetManhattan(start, end);
        startNode.parent = null;

        openSet.Add(startNode);

        while(openSet.Count > 0)
        {
            PathNode current = openSet.RemoveFirst();

            if (current == endNode) { return RetracePath(endNode); }

            closedSet.Add(current);

            foreach (PathNode neighbor in PathfindingGrid.Instance.GetNeighbors(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) { continue; }

                int newGCost = current.gCost + 1;
                bool inOpenSet = openSet.Contains(neighbor);

                if (newGCost < neighbor.gCost || !inOpenSet)
                {
                    neighbor.gCost  = newGCost;
                    neighbor.hCost  = GetManhattan(neighbor.gridPos, end);
                    neighbor.parent = current;

                    if (!inOpenSet)
                    {
                        openSet.Add(neighbor);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbor);
                    }
                }
            }

        }
        return null;
    }

    private int GetManhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector3> RetracePath(PathNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode current = endNode;

        while (current != null)
        {
            path.Add(current.worldPos);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

}