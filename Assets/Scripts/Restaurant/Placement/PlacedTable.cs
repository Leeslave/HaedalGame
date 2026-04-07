using System.Collections.Generic;
using UnityEngine;

// 배치 확정된 테이블에 부착. TableGroup과 함께 사용 (같은 GameObject에 두 컴포넌트 공존)
[RequireComponent(typeof(TableGroup))]
public class PlacedTable : MonoBehaviour
{
    public TableData  tableData  { get; private set; }
    public Vector2Int anchorCell { get; private set; }

    private List<Vector2Int> obstacleCells = new List<Vector2Int>(); // "테" 타일 위치
    private Seat[]            seats;

    public void Initialize(TableData data, Vector2Int anchor)
    {
        tableData  = data;
        anchorCell = anchor;

        seats = GetComponentsInChildren<Seat>();

        // 의자별 gridPos 설정
        for (int i = 0; i < data.chairTiles.Length && i < seats.Length; i++)
        {
            seats[i].SetGridPos(anchorCell + data.chairTiles[i]);
        }

        // "테" 타일 장애물 등록
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