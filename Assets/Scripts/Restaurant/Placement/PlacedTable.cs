using System.Collections.Generic;
using UnityEngine;

// 배치 확정된 테이블에 부착. TableGroup과 함께 사용 (같은 GameObject에 두 컴포넌트 공존)
[RequireComponent(typeof(TableGroup))]
public class PlacedTable : MonoBehaviour
{
    // 현재 씬에 배치되어 있는(철거·이동 전) 테이블 목록. Destroy는 프레임 끝에 처리되므로 씬 검색 대신 이 목록으로 배치를 저장한다.
    private static readonly List<PlacedTable> _active = new List<PlacedTable>();
    public static IReadOnlyList<PlacedTable> Active => _active;

    public TableData tableData { get; private set; }
    public Vector2Int anchorCell { get; private set; }
    public int Skin { get; private set; }
    public void SetSkin(int skin)
    {
        Skin = skin;
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.color = skin == 0 ? Color.white : new Color(.78f, .87f, .94f);
    }

    private List<Vector2Int> obstacleCells = new List<Vector2Int>(); // "테" 타일 위치
    private Seat[] seats;

    public void Initialize(TableData data, Vector2Int anchor)
    {
        tableData = TableVariants.Copy(data);
        anchorCell = anchor;
        if (!_active.Contains(this)) { _active.Add(this); }

        seats = GetComponentsInChildren<Seat>(true);
        for (int i = 0; i < seats.Length; i++) seats[i].gameObject.SetActive(i < data.chairTiles.Length);
        var visual = transform.Find("Visual");
        if (visual != null) visual.localRotation = Quaternion.Euler(0, 0, data.rotation * 90);

        float visualScale = GetComponent<TableGroup>().GetVisualScale();
        Vector3 anchorWorldPos = PathfindingGrid.Instance.GetWorldPos(anchorCell);

        // 좌석의 "정답" 위치는 항상 TableData.chairTiles (앵커 기준 그리드 오프셋) 이다.
        // 여기에 visualScale을 앵커(테이블 중심) 기준으로 곱해서, 테이블이 커진 만큼
        // 좌석도 비례해서 바깥으로 벌어지게 한다. (Seat의 프리팹 원본 Transform 값은 사용하지 않음)
        List<Vector2Int> chairGridPositions = new List<Vector2Int>();
        for (int i = 0; i < data.chairTiles.Length && i < seats.Length; i++)
        {
            Vector3 baseWorldPos = PathfindingGrid.Instance.GetWorldPos(anchorCell + data.chairTiles[i]);
            Vector3 scaledWorldPos = anchorWorldPos + (baseWorldPos - anchorWorldPos) * visualScale;

            seats[i].GetSeatPoint().position = scaledWorldPos;
            seats[i].SetFacingDirection(((Vector2)anchorWorldPos - (Vector2)scaledWorldPos).normalized);
            Vector2Int chairGridPos = PathfindingGrid.Instance.WorldToGridPos(scaledWorldPos);
            seats[i].SetGridPos(chairGridPos);
            chairGridPositions.Add(chairGridPos);
        }

        // 장애물(테이블 몸통) 범위를 고정된 TableData.tableTiles 대신, 실제로 벌어진 좌석들 사이의
        // 영역(좌석 좌표들의 바운딩 박스, 좌석 칸 자체는 제외)으로 계산한다. 그래야 visualScale로
        // 테이블을 키운 만큼 장애물 범위도 같이 넓어져서, 손님이 커진 테이블 그림을 가로질러
        // 걸어다니는 문제가 생기지 않는다.
        obstacleCells.Clear();
        obstacleCells.AddRange(TableFootprint.GetBodyCells(chairGridPositions)); // 좌석 칸 자체는 착석 여부에 따라 동적으로 막히므로 제외
        if (data.tableType == TableType.OneSeat) obstacleCells.Add(anchorCell);
        PathfindingGrid.Instance.RegisterObstacleTiles(obstacleCells);
    }

    // 테이블 제거 (철거)
    public void RemoveTable()
    {
        PathfindingGrid.Instance.UnregisterObstacleTiles(obstacleCells);
        MarkRemoved();
        Destroy(gameObject);
    }

    // 배치 목록에서 즉시 뺀다. (이동 확정처럼 장애물 처리를 따로 한 뒤 Destroy하는 경우에도 호출)
    public void MarkRemoved()
    {
        _active.Remove(this);
    }

    private void OnDestroy()
    {
        _active.Remove(this);
        if (tableData != null && tableData.hideFlags == HideFlags.DontSave) Destroy(tableData);
    }

    public List<Vector2Int> GetObstacleCells() { return obstacleCells; }

    private void OnMouseDown()
    {
        TableContextMenu.Instance.Show(this);
    }
}
