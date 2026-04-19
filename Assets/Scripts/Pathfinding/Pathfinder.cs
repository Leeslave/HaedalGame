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

        List<PathNode> openList = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = GetManhattan(start, end);
        startNode.parent = null;

        openList.Add(startNode);

        while(openList.Count > 0)
        {
            PathNode current = GetLowestFCost(openList);

            if (current == endNode) { return RetracePath(endNode); }

            openList.Remove(current);
            closedSet.Add(current);

            foreach (PathNode neighbor in PathfindingGrid.Instance.GetNeighbors(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) { continue; }

                int newGCost = current.gCost + 1;

                if (newGCost < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost  = newGCost;
                    neighbor.hCost  = GetManhattan(neighbor.gridPos, end);
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }

        }
        return null;
    }

    private PathNode GetLowestFCost(List<PathNode> list)
    {
        PathNode lowest = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            PathNode node = list[i];
            if (node.FCost() < lowest.FCost())
            {
                lowest = node;
            }
            else if (node.FCost() == lowest.FCost() && node.hCost < lowest.hCost)
            {
                lowest = node;
            }
        }

        return lowest;
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