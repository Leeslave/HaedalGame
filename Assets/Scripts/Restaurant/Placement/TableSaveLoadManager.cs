using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class TablePlacementEntry
{
    public string tableType; // TableType enum의 string 값
    public int    anchorX;
    public int    anchorY;
}

[Serializable]
public class TablePlacementSaveData
{
    public List<TablePlacementEntry> tables = new List<TablePlacementEntry>();
}

public class TableSaveLoadManager : MonoBehaviour
{
    public static TableSaveLoadManager Instance;

    [SerializeField] private TableData twoSeatData;
    [SerializeField] private TableData fourSeatData;
    [SerializeField] private Transform tableParent;

    private string savePath;

    private void Awake()
    {
        Instance = this;
        savePath = Path.Combine(Application.persistentDataPath, "table_placement.json");
    }

    private void Start()
    {
        LoadPlacement();
    }

    public void SavePlacement()
    {
        TablePlacementSaveData data = new TablePlacementSaveData();

        PlacedTable[] placed = FindObjectsByType<PlacedTable>(FindObjectsSortMode.None);
        foreach (PlacedTable table in placed)
        {
            TablePlacementEntry entry = new TablePlacementEntry();
            entry.tableType = table.tableData.tableType.ToString();
            entry.anchorX   = table.anchorCell.x;
            entry.anchorY   = table.anchorCell.y;
            data.tables.Add(entry);
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
    }

    public void LoadPlacement()
    {
        if (!File.Exists(savePath)) { return; }

        TablePlacementSaveData data = JsonUtility.FromJson<TablePlacementSaveData>(
            File.ReadAllText(savePath)
        );

        foreach (TablePlacementEntry entry in data.tables)
        {
            TableData tableData = GetTableData(entry.tableType);
            if (tableData == null) { continue; }

            Vector2Int anchor   = new Vector2Int(entry.anchorX, entry.anchorY);
            Vector3    worldPos = PathfindingGrid.Instance.GetWorldPos(anchor);

            GameObject obj    = Instantiate(tableData.placedPrefab, worldPos, Quaternion.identity, tableParent);
            PlacedTable placed = obj.GetComponent<PlacedTable>();
            placed.Initialize(tableData, anchor);
        }
    }

    private TableData GetTableData(string typeStr)
    {
        if (typeStr == TableType.TwoSeat.ToString())  { return twoSeatData;  }
        if (typeStr == TableType.FourSeat.ToString()) { return fourSeatData; }
        return null;
    }
}