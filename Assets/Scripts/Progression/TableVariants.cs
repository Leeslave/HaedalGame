using System.Linq;
using UnityEngine;

public static class TableVariants
{
    public static TableData Copy(TableData source)
    {
        var result = Object.Instantiate(source); result.hideFlags = HideFlags.DontSave; return result;
    }
    public static TableData OneSeat(TableData source)
    {
        var result = Copy(source); result.tableType = TableType.OneSeat; result.tableName = "1인 테이블";
        result.chairTiles = new[] { source.chairTiles[0] }; result.tableTiles = new[] { Vector2Int.zero }; return result;
    }
    public static void Rotate(TableData data)
    {
        data.chairTiles = data.chairTiles.Select(p => new Vector2Int(-p.y, p.x)).ToArray();
        data.tableTiles = data.tableTiles.Select(p => new Vector2Int(-p.y, p.x)).ToArray();
        data.rotation = (data.rotation + 1) % 4;
    }
}
