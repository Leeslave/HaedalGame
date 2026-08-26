using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;


public class TablePlacementManager : MonoBehaviour
{
    public static TablePlacementManager Instance;

    [SerializeField] private TableData  twoSeatData;
    [SerializeField] private TableData  fourSeatData;
    [SerializeField] private Transform  tableParent;       // 배치된 테이블들의 부모
    //[SerializeField] private GameObject preOperationUI;
    [SerializeField] private GameObject placementUI;       // V/X 버튼 포함 UI
    [SerializeField] private OverlayPool overlayPool;      // Ghost가 공유할 풀

    private TableGhostController activeGhost;
    private PlacementState       state       = PlacementState.Idle;
    private PlacedTable          movingTable;              // 이동 중인 기존 테이블

    private void Awake() { Instance = this; placementUI.SetActive(false); }

    // ─── 외부 진입 / 퇴출 ────────────────────────────

    public void EnterPlacementMode()
    {
        state = PlacementState.Idle;
        if (placementUI != null) { placementUI.SetActive(true); }
    }

    public void ExitPlacementMode()
    {
        CancelGhost();
        state = PlacementState.Idle;
        if (placementUI != null) { placementUI.SetActive(false); }
        PreOperationUIManager.Instance.ShowUI();
    }

    // ─── 배치 버튼 ───────────────────────────────────

    public void OnTwoSeatButtonClicked()  { StartPlacing(twoSeatData);  }
    public void OnFourSeatButtonClicked() { StartPlacing(fourSeatData); }

    private void StartPlacing(TableData data)
    {
        CancelGhost();
        SpawnGhost(data);
        state = PlacementState.PlacingGhost;
    }

    // 기존 테이블 이동 시작 (TableContextMenu에서 호출)
    public void StartMoving(PlacedTable table)
    {
        CancelGhost();
        movingTable = table;
        // 이동 동안만 장애물 임시 해제
        PathfindingGrid.Instance.UnregisterObstacleTiles(table.GetObstacleCells());
        SpawnGhost(table.tableData);
        state = PlacementState.Moving;
    }

    // ─── V 버튼 (확정) ───────────────────────────────

    public void OnConfirmClicked()
    {
        if (activeGhost == null || !activeGhost.IsPlaceable) { return; }

        Vector2Int anchor  = activeGhost.CurrentCellPos;
        TableData  data    = activeGhost.TableData;
        Vector3    worldPos = activeGhost.SnapPosition;

        if (state == PlacementState.Moving && movingTable != null)
        {
            Destroy(movingTable.gameObject);
            movingTable = null;
        }

        // 실제 테이블 생성
        GameObject obj    = Instantiate(data.placedPrefab, worldPos, Quaternion.identity, tableParent);
        PlacedTable placed = obj.GetComponent<PlacedTable>();
        placed.Initialize(data, anchor);

        TableSaveLoadManager.Instance.SavePlacement();
        CancelGhost();
        state = PlacementState.Idle;
    }

    // ─── X 버튼 (취소) ───────────────────────────────

    public void OnCancelClicked()
    {
        if (state == PlacementState.Moving && movingTable != null)
        {
            // 원래 위치 장애물 복원
            PathfindingGrid.Instance.RegisterObstacleTiles(movingTable.GetObstacleCells());
            movingTable = null;
        }
        CancelGhost();
        state = PlacementState.Idle;
    }

    // ─── 내부 유틸 ───────────────────────────────────

    private void SpawnGhost(TableData data)
    {
        GameObject ghostObj = Instantiate(data.ghostPrefab);
        activeGhost = ghostObj.GetComponent<TableGhostController>();
        activeGhost.Initialize(data, PathfindingGrid.Instance.FloorMap, overlayPool);
    }

    private void CancelGhost()
    {
        if (activeGhost == null) { return; }
        Destroy(activeGhost.gameObject);
        activeGhost = null;
    }

    private void Update()
    {
        if (state == PlacementState.Idle) { return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { OnCancelClicked(); }
        if (Input.GetMouseButton(0)) { OnClickWhilePlacing(); }
    }

    private void OnClickWhilePlacing()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }
        if (activeGhost == null || !activeGhost.IsPlaceable ) { return; }

        activeGhost.Freeze();

        PopupManager.Instance.ShowConfirmPopup
        (
            "이 위치에 테이블을 설치하겠습니까?",
            "설치",
            "취소",
            OnPlaceConfirmed,
            OnPlaceDenied
        );
    }

    private void OnPlaceConfirmed()
    {
        OnConfirmClicked();
    }

    private void OnPlaceDenied()
    {
        Debug.Log("설치 취소");
        if (activeGhost != null) {activeGhost.Unfreeze(); }
    }
}