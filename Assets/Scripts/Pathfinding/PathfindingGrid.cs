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

    // 월드 좌표가 속한 그리드 좌표만 역산 (테이블 크기에 비례해 옮겨진 Seat이 실제로 어느 타일에 속하는지 계산할 때 사용)
    public Vector2Int WorldToGridPos(Vector3 worldPos)
    {
        Vector3Int cellPos = floorMap.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
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


    // 후보 칸들을 실제로 막지 않고(가상 적용) 임시로만 walkable=false로 가정한 뒤,
    // 그래도 나머지 이동 가능 영역이 하나로 계속 연결돼 있는지 BFS로 검사한다.
    // 배치 확정 전 Ghost 미리보기 단계에서 "통로를 완전히 막아버리는 배치"를 걸러내는 데 사용.
    public bool WouldStayConnected(List<Vector2Int> candidateBlockedCells)
    {
        Vector2Int? reference = FindReferenceWalkableCell(candidateBlockedCells);
        if (reference == null) { return true; } // 참조할 이동 가능 칸이 없으면 판단 불필요

        HashSet<Vector2Int> reachableBefore = FloodFillWalkable(reference.Value);

        foreach (Vector2Int cell in candidateBlockedCells)
        {
            if (nodes.TryGetValue(cell, out PathNode node)) { node.walkable = false; }
        }

        HashSet<Vector2Int> reachableAfter = FloodFillWalkable(reference.Value);

        // 위에서 candidateBlockedCells는 배치 유효성 검사(CheckTileValid)를 이미 통과한
        // 칸들이므로, 가상 적용 전에는 항상 walkable=true였다. 그대로 복원한다.
        foreach (Vector2Int cell in candidateBlockedCells)
        {
            if (nodes.TryGetValue(cell, out PathNode node)) { node.walkable = true; }
        }

        int removedFromReachable = 0;
        foreach (Vector2Int cell in candidateBlockedCells)
        {
            if (reachableBefore.Contains(cell)) { removedFromReachable++; }
        }

        // 후보 칸들만큼만 줄어들었다면(= 다른 영역을 갈라놓지 않았다면) 여전히 연결된 것
        return reachableAfter.Count == reachableBefore.Count - removedFromReachable;
    }

    private Vector2Int? FindReferenceWalkableCell(List<Vector2Int> excluding)
    {
        foreach (KeyValuePair<Vector2Int, PathNode> kv in nodes)
        {
            if (kv.Value.walkable && !excluding.Contains(kv.Key)) { return kv.Key; }
        }
        return null;
    }

    private HashSet<Vector2Int> FloodFillWalkable(Vector2Int start)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            PathNode current = nodes[queue.Dequeue()];
            foreach (PathNode neighbor in GetNeighbors(current))
            {
                if (!neighbor.walkable || visited.Contains(neighbor.gridPos)) { continue; }
                visited.Add(neighbor.gridPos);
                queue.Enqueue(neighbor.gridPos);
            }
        }
        return visited;
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