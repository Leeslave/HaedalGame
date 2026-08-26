using System.Collections.Generic;
using UnityEngine;

// PlacedTable(확정 배치)과 TableGhostController(미리보기)가 "커진 테이블이 실제로 막는 영역"을
// 서로 다르게 계산하면 미리보기와 실제 설치 결과가 어긋나므로, 바운딩 박스 계산을 공유한다.
public static class TableFootprint
{
    // 좌석 칸들 사이의 바운딩 박스에서 좌석 칸 자체를 제외한 영역 (테이블 몸체 장애물)
    public static List<Vector2Int> GetBodyCells(List<Vector2Int> chairCells)
    {
        List<Vector2Int> bodyCells = new List<Vector2Int>();
        if (chairCells.Count == 0) { return bodyCells; }

        Vector2Int min = chairCells[0];
        Vector2Int max = chairCells[0];
        foreach (Vector2Int pos in chairCells)
        {
            min = Vector2Int.Min(min, pos);
            max = Vector2Int.Max(max, pos);
        }

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (chairCells.Contains(cell)) { continue; }
                bodyCells.Add(cell);
            }
        }
        return bodyCells;
    }
}
