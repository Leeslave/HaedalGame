using UnityEngine;

public enum TableType { TwoSeat, FourSeat }

[CreateAssetMenu(fileName = "TableData", menuName = "Game Data/Restaurant/Table Data")]
public class TableData : ScriptableObject
{
    [Header("기본 설정")]
    public string tableName;
    public TableType tableType;
    [Tooltip("2인 = 3, 4인 = 4")] public int gridWidth;
    [Tooltip("2인 = 3, 4인 = 3")] public int gridHeight;
    [Tooltip("placedPrefab의 TableGroup.visualScale과 반드시 같은 값으로 맞출 것. 배치 미리보기(Ghost)의 유효 영역 계산에 사용됨")]
    public float visualScale = 1f;

    [Header("타일 구성 (앵커 기준 상대 좌표)")]
    public Vector2Int[] tableTiles;  // "테" 타일 - 영구 장애물
    public Vector2Int[] chairTiles;  // "의" 타일 - 착석 시 장애물, Seat 위치

    [Header("프리팹")]
    public GameObject placedPrefab;  // 실제 배치용 프리팹
    public GameObject ghostPrefab;   // 미리보기용 Ghost 프리
}

// ──────────────────────────────────────────────
// Inspector 설정 가이드
// ──────────────────────────────────────────────
// 2인 테이블 (3×3):
//   gridWidth=3, gridHeight=3
//   tableTiles : (1,0) (1,1) (1,2)
//   chairTiles : (0,1) (2,1)
//
// 4인 테이블 (4×3):
//   gridWidth=4, gridHeight=3
//   tableTiles : (1,0)(2,0) (1,1)(2,1) (1,2)(2,2)
//   chairTiles : (0,0)(3,0) (0,2)(3,2)
// ──────────────────────────────────────────────
