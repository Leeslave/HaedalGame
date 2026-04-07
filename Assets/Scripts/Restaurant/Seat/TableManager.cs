using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;


public class TableManager : MonoBehaviour
{
    public static TableManager Instance;

    [SerializeField] private Transform entranceTf;
    [ReadOnly] private List<TableGroup> tables;
    private bool renumberRequest = false;

    void Awake()
    {
        Instance = this;
        tables = new();
    }

    void Start()
    {
        foreach (var table in FindObjectsByType<TableGroup>(FindObjectsSortMode.None))
        {
            RegisterTable(table);
        }
        RequestRenumber();
    }

    public void RegisterTable(TableGroup table)
    {
        if (!tables.Contains(table)) { tables.Add(table); } // 만약 이미 등록되어 있지 않다면 테이블을 등록
    }

    public void UnregisterTable(TableGroup table)
    {
        if (tables.Contains(table)) { tables.Remove(table); } // 등록되어있는 테이블을 해제하는 코드
    }

    public void RequestRenumber()
    {
        renumberRequest = true;
    }

    private void LateUpdate()
    {
        if (!renumberRequest) { return; }
        renumberRequest = false;
        RenumberTables();
    }

    private void RenumberTables()
    {
        if (entranceTf == null) { return; }

        var sorted = tables
        .Where(t => t != null && t.isActiveAndEnabled)
        .OrderBy(t => (t.position - entranceTf.position).sqrMagnitude)
        .ToList();

        for (int i = 0; i < sorted.Count; i++) { sorted[i].SetTableId(i + 1); } // 테이블 넘버링 부여
        if (RestaurantGameManager.instance?.seatManager == null) return;
        RestaurantGameManager.instance.seatManager.RegisterSeats(sorted);
    }

    public int GetPlacedTableCount()
    {
        return tables.Count;
    }


}
