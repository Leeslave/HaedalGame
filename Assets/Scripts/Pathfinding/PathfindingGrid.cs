using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance;
    [SerializeField] private Tilemap obstacleMap; // 장애물 레이어 타일맵
    [SerializeField] private Tilemap floorMap;    // 바닥 레이어 타일맵

    private Dictionary<Vector2Int, PathNode> nodes = new Dictionary<Vector2Int, PathNode>();

    void Awake()
    {
        Instance = this;
        BulidGrid();
    }

    private void BulidGrid()
    {
        BoundsInt bounds = floorMap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!floorMap.HasTile(pos)) { continue; }

            Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
            Vector3 worldPos = floorMap.GetCellCenterWorld(pos);

            // 장애물 타일이 있으면 walkable = false;
            bool walkable = !obstacleMap.HasTile(pos);

            nodes[gridPos] = new PathNode(gridPos, worldPos, walkable);
        }
    }

    public PathNode GetNode(Vector2Int gridPos)
    {
        nodes.TryGetValue(gridPos, out PathNode node);
        return node;
    }

    public PathNode GetNodeFromWorld(Vector3 worldPos)
    {
        Vector3Int cellPos = floorMap.WorldToCell(worldPos);
        return GetNode(new Vector2Int(cellPos.x, cellPos.y));
    }

    public List<PathNode> GetNeighbors(PathNode node)
    {
        List<PathNode> neighbors = new List<PathNode>();

        // 4방향
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.right,
            Vector2Int.left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = node.gridPos + dir;
            PathNode neighbor = GetNode(neighborPos);
            if (neighbor != null) { neighbors.Add(neighbor); }
        }
        return neighbors;
    }
}