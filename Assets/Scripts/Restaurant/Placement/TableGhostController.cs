using UnityEngine;
using UnityEngine.Tilemaps;

// 테이블 배치 미리보기 Ghost - 마우스를 따라 그리드에 스냅하며 유효성을 색상으로 표시
public class TableGhostController : MonoBehaviour
{
    public TableData TableData       { get; private set; }
    public bool      IsPlaceable     { get; private set; }
    public Vector2Int CurrentCellPos { get; private set; }
    public Vector3    SnapPosition   { get; private set; }

    private Tilemap         floorMap;
    private OverlayPool     overlayPool;
    private SpriteRenderer[] spriteRenderers;
    private Vector2Int       lastCellPos;

    private static readonly Color GhostValid   = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color GhostInvalid = new Color(1f, 0.3f, 0.3f, 0.5f);

    public void Initialize(TableData data, Tilemap tilemap, OverlayPool pool)
    {
        TableData       = data;
        floorMap        = tilemap;
        overlayPool     = pool;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        lastCellPos     = new Vector2Int(int.MinValue, int.MinValue);
    }

    private void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3Int cell3    = floorMap.WorldToCell(mouseWorld);
        Vector2Int cellPos  = new Vector2Int(cell3.x, cell3.y);

        if (cellPos == lastCellPos) { return; }
        lastCellPos   = cellPos;
        CurrentCellPos = cellPos;
        SnapPosition   = floorMap.GetCellCenterWorld(cell3);
        transform.position = SnapPosition;

        RefreshOverlays(cellPos);
    }

    private void RefreshOverlays(Vector2Int anchor)
    {
        overlayPool.ReturnAll();
        IsPlaceable = true;

        for (int y = 0; y < TableData.gridHeight; y++)
        {
            for (int x = 0; x < TableData.gridWidth; x++)
            {
                Vector2Int local     = new Vector2Int(x, y);
                Vector2Int worldCell = anchor + local;
                bool       isTable   = IsTile(local, TableData.tableTiles);
                bool       isChair   = IsTile(local, TableData.chairTiles);

                if (!isTable && !isChair) { continue; } // "빈" 타일은 검사 생략

                bool valid = CheckTileValid(worldCell, isChair);
                if (!valid) { IsPlaceable = false; }

                Vector3 overlayPos = PathfindingGrid.Instance.GetWorldPos(worldCell);
                overlayPool.ShowOverlay(overlayPos, valid);
            }
        }

        // Ghost 전체 색상 갱신
        Color ghostColor = IsPlaceable ? GhostValid : GhostInvalid;
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.color = ghostColor;
        }
    }

    private bool CheckTileValid(Vector2Int worldCell, bool isChair)
    {
        // 바닥 타일 존재 여부
        Vector3Int tile3 = new Vector3Int(worldCell.x, worldCell.y, 0);
        if (!floorMap.HasTile(tile3)) { return false; }

        // 기존 장애물 충돌 여부
        PathNode node = PathfindingGrid.Instance.GetNode(worldCell);
        if (node == null || !node.walkable) { return false; }

        // 의자 타일: 아래칸이 walkable인지 추가 검사 (서버 접근 경로 보장)
        if (isChair)
        {
            Vector2Int    below     = worldCell + Vector2Int.down;
            PathNode      belowNode = PathfindingGrid.Instance.GetNode(below);
            if (belowNode == null || !belowNode.walkable) { return false; }
        }

        return true;
    }

    private bool IsTile(Vector2Int local, Vector2Int[] tiles)
    {
        foreach (Vector2Int t in tiles)
        {
            if (t == local) { return true; }
        }
        return false;
    }
}