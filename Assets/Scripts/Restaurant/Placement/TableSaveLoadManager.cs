using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 테이블 배치를 세션(GameSaveData.placedTables)과 주고받는다. 파일 저장은 체크포인트에서만 한다.
/// 배치/이동/철거 직후 SavePlacement()로 세션에 바로 반영하므로, 씬 언로드 순서와 무관하게 최신 배치가 남는다.
/// </summary>
public class TableSaveLoadManager : MonoBehaviour
{
    public static TableSaveLoadManager Instance;

    [SerializeField] private TableData twoSeatData;
    [SerializeField] private TableData fourSeatData;
    [SerializeField] private Transform tableParent;
    private readonly Stack<List<PlacedTableEntry>> undo = new Stack<List<PlacedTableEntry>>();
    private List<PlacedTableEntry> original;
    private static List<PlacedTableEntry> Copy(List<PlacedTableEntry> source) => source.Select(e => new PlacedTableEntry
    { tableType = e.tableType, anchorX = e.anchorX, anchorY = e.anchorY, rotation = e.rotation, skin = e.skin }).ToList();
    public void RememberEdit() { if (GameSession.IsActive) undo.Push(Copy(GameSession.Current.placedTables)); }
    public void UndoEdit()
    {
        if (!RestaurantProgress.CanManage || undo.Count == 0) return;
        TablePlacementManager.Instance?.OnCancelClicked();
        GameSession.Current.placedTables = undo.Pop(); LoadPlacement();
    }
    public void ResetEdits()
    {
        if (!RestaurantProgress.CanManage || original == null) return;
        TablePlacementManager.Instance?.OnCancelClicked(); RememberEdit();
        GameSession.Current.placedTables = Copy(original); LoadPlacement();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameSession.IsActive) original = Copy(GameSession.Current.placedTables);
        LoadPlacement();
    }

    /// <summary>현재 씬의 배치를 세션에 반영한다.</summary>
    public void SavePlacement()
    {
        if (!GameSession.IsActive) { return; }

        List<PlacedTableEntry> entries = GameSession.Current.placedTables;
        entries.Clear();

        foreach (PlacedTable table in PlacedTable.Active)
        {
            entries.Add(new PlacedTableEntry
            {
                tableType = table.tableData.tableType.ToString(),
                anchorX   = table.anchorCell.x,
                anchorY   = table.anchorCell.y,
                rotation = table.tableData.rotation,
                skin = table.Skin,
            });
        }
    }

    /// <summary>세션의 배치로 씬 테이블을 다시 만든다. 이미 있던 배치 테이블은 먼저 제거해 중복 생성을 막는다.</summary>
    public void LoadPlacement()
    {
        if (!GameSession.IsActive) { return; }

        foreach (PlacedTable existing in new List<PlacedTable>(PlacedTable.Active))
        {
            existing.RemoveTable();
        }

        foreach (PlacedTableEntry entry in GameSession.Current.placedTables)
        {
            TableData basis = GetTableData(entry.tableType);
            TableData tableData = basis == null ? null : TableVariants.Copy(basis);
            if (entry.tableType == TableType.OneSeat.ToString() && basis != null) Destroy(basis);
            if (tableData == null)
            {
                Debug.LogWarning($"[TableSaveLoadManager] 알 수 없는 테이블 종류 '{entry.tableType}'를 건너뜁니다.");
                continue;
            }

            Vector2Int anchor   = new Vector2Int(entry.anchorX, entry.anchorY);
            for (int i = 0; i < entry.rotation % 4; i++) TableVariants.Rotate(tableData);
            Vector3    worldPos = PathfindingGrid.Instance.GetWorldPos(anchor);

            GameObject obj    = Instantiate(tableData.placedPrefab, worldPos, Quaternion.identity, tableParent);
            PlacedTable placed = obj.GetComponent<PlacedTable>();
            placed.Initialize(tableData, anchor);
            placed.SetSkin(entry.skin);
            Destroy(tableData);
        }
    }

    private TableData GetTableData(string typeStr)
    {
        if (typeStr == TableType.TwoSeat.ToString())  { return twoSeatData;  }
        if (typeStr == TableType.FourSeat.ToString()) { return fourSeatData; }
        if (typeStr == TableType.OneSeat.ToString()) { return TableVariants.OneSeat(twoSeatData); }
        return null;
    }
}
