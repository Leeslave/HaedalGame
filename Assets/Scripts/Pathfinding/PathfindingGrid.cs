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

    // 런 타임에 특정 노드의 walkable 상태 변경 (손님 착석 / 퇴장 시)
    public void SetWalkable(Vector2Int gridPos, bool walkable)
    {
        if (nodes.TryGetValue(gridPos, out PathNode node))
        {
            node.walkable = walkable;
        }
    }

    // 테이블 타일을 영구 장애물로 등록
    public void RegisterObstacleTiles(List<Vector2Int> positions)
    {
        foreach (Vector2Int pos in positions)
        {
            if (nodes.TryGetValue(pos, out PathNode node))
            {
                node.walkable = false;
            }
        }
    }

    // 테이블 제거/ 이동 시 장애물 해제 (obstacleMap 기준으로 복원)
    public void UnregisterObstacleTiles(List<Vector2Int> positions)
    {
        foreach (Vector2Int pos in positions)
        {
            if (nodes.TryGetValue(pos, out PathNode node))
            {
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                node.walkable = !obstacleMap.HasTile(tilePos);
            }
        }
    }


    // 런타임에서 그리드 리빌드
    public void RebuildGrid()
    {
        nodes.Clear();
        BulidGrid();
    }

    // 그리드 좌표 -> 월드 좌표 변환
    public Vector3 GetWorldPos(Vector2Int gridPos)
    {
        return floorMap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
    }

    // 배치 시스템에서 스냅 계산 용
    public Tilemap FloorMap => floorMap;
}