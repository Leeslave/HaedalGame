using System.Collections.Generic;
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

    private bool frozen = false;
    public void Initialize(TableData data, Tilemap tilemap, OverlayPool pool)
    {
        TableData       = data;
        floorMap        = tilemap;
        overlayPool     = pool;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        lastCellPos     = new Vector2Int(int.MinValue, int.MinValue);

        // PlacedTable(TableGroup.Awake)과 동일하게 visualScale을 적용해야, 미리보기 크기가
        // 실제 배치되는 테이블 크기와 일치한다. Ghost 프리팹에는 TableGroup을 붙이지 않으므로
        // (붙이면 OnEnable에서 TableManager에 실제 테이블처럼 등록돼버림) 여기서 직접 적용.
        Transform visual = transform.Find("Visual");
        if (visual != null) { visual.localScale *= data.visualScale; }
    }

    private void Update()
    {
        if (frozen) { return; }
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

        // 좌석 위치를 PlacedTable.Initialize()와 동일하게 visualScale만큼 앵커 기준으로 벌려서 계산해야,
        // 테이블이 커진 만큼 미리보기의 유효 영역도 같이 넓어져서 실제 설치 결과와 어긋나지 않는다.
        Vector3 anchorWorldPos = PathfindingGrid.Instance.GetWorldPos(anchor);
        List<Vector2Int> chairCells = new List<Vector2Int>();
        foreach (Vector2Int offset in TableData.chairTiles)
        {
            Vector3 baseWorldPos   = PathfindingGrid.Instance.GetWorldPos(anchor + offset);
            Vector3 scaledWorldPos = anchorWorldPos + (baseWorldPos - anchorWorldPos) * TableData.visualScale;
            chairCells.Add(PathfindingGrid.Instance.WorldToGridPos(scaledWorldPos));
        }
        List<Vector2Int> bodyCells = TableFootprint.GetBodyCells(chairCells);

        foreach (Vector2Int cell in bodyCells)
        {
            bool valid = CheckTileValid(cell, false);
            if (!valid) { IsPlaceable = false; }
            overlayPool.ShowOverlay(PathfindingGrid.Instance.GetWorldPos(cell), valid);
        }

        foreach (Vector2Int cell in chairCells)
        {
            bool valid = CheckTileValid(cell, true);
            if (!valid) { IsPlaceable = false; }
            overlayPool.ShowOverlay(PathfindingGrid.Instance.GetWorldPos(cell), valid);
        }

        // 칸 하나하나는 다 비어있어도, 이 테이블이 통로를 완전히 갈라놓아서 반대편으로
        // 이동할 수 없게 만드는 배치라면 거부한다. (실제로 막지 않고 가상으로만 검사)
        if (IsPlaceable && !PathfindingGrid.Instance.WouldStayConnected(bodyCells))
        {
            IsPlaceable = false;
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

    public void Freeze() { frozen = true; }
    public void Unfreeze() { frozen = false; }
}