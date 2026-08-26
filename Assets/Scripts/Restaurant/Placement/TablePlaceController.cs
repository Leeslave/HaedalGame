using UnityEngine;

public enum PlacementState 
{ 
    Idle, 
    PlacingGhost, 
    Moving,
    Awating 
}
public class TablePlaceController : MonoBehaviour
{
    // [SerializeField] private TableData twoSeat; // 2개짜리 좌석
    // [SerializeField] private TableData fourSeat;// 4개짜리 좌석
    // [SerializeField] private OverlayPool overlayPool;

    // [SerializeField] private GameObject mainUI; // 좌석배치와 관련된 UI 모음

    // private PlacementState state;

    // private void Awake()
    // {
    //     mainUI.SetActive(false);
    // }

    // public void EnterPlacementMode()
    // {
    //     state = PlacementState.Idle;
    //     if (mainUI != null)
    //     {
    //         mainUI.SetActive(true);
    //     }
    // }

    // public void ExitPlacementMode()
    // {
    //     CancelGhost();                // 배치 모드 종료 이후 고스트 오브젝트가 떠있는 현상 방지
    //     state = PlacementState.Idle;
    //     if (mainUI != null)
    //     {
    //         mainUI.SetActive(false);
    //     }
    //     // 다시 PreOperation UI 활성화
    // }

    // public void OnTwoSeatButtonClicked()  { StartPlacing(twoSeat);  }
    // public void OnFourSeatButtonClicked() { StartPlacing(fourSeat); }

    // private void StartPlacing(TableData data)
    // {
    //     CancelGhost();              // 중복 방지
    //     SpawnGhost(data);           
    //     state = PlacementState.PlacingGhost;
    // }

    //     public void StartMoving(PlacedTable table)
    // {
    //     CancelGhost();
    //     PathfindingGrid.Instance.UnregisterObstacleTiles(table.GetObstacleCells()); // 이미 설치된 곳은 장애물 판정이 되어있을 테니까 일시적으로 해제
    //     SpawnGhost(table.tableData);
    //     state = PlacementState.Moving;
    // }


}
