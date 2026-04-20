using System.Collections.Generic;
using UnityEngine;

// 배치 확정된 테이블에 부착. TableGroup과 함께 사용 (같은 GameObject에 두 컴포넌트 공존)
[RequireComponent(typeof(TableGroup))]
public class PlacedTable : MonoBehaviour
{
    public TableData tableData { get; private set; }
    public Vector2Int anchorCell { get; private set; }

    private List<Vector2Int> obstacleCells = new List<Vector2Int>(); // "테" 타일 위치
    private Seat[] seats;

    public void Initialize(TableData data, Vector2Int anchor)
    {
        tableData = data;
        anchorCell = anchor;

        seats = GetComponentsInChildren<Seat>();

        for (int i = 0; i < data.chairTiles.Length && i < seats.Length; i++)
        {
            Vector2Int chairGridPos = anchorCell + data.chairTiles[i];
            seats[i].SetGridPos(chairGridPos);

            // seatPoint를 그리드 셀 중심 위치로 강제 설정 → chairTile과 일치 보장
            Vector3 exactWorldPos = PathfindingGrid.Instance.GetWorldPos(chairGridPos);
            seats[i].GetSeatPoint().position = exactWorldPos;
        }

        obstacleCells.Clear();
        foreach (Vector2Int offset in data.tableTiles)
        {
            obstacleCells.Add(anchorCell + offset);
        }
        PathfindingGrid.Instance.RegisterObstacleTiles(obstacleCells);
    }

    // 테이블 제거 (철거)
    public void RemoveTable()
    {
        PathfindingGrid.Instance.UnregisterObstacleTiles(obstacleCells);
        Destroy(gameObject);
    }

    public List<Vector2Int> GetObstacleCells() { return obstacleCells; }

    private void OnMouseDown()
    {
        TableContextMenu.Instance.Show(this);
    }
}