using UnityEngine;

// 배치된 테이블 클릭 시 표시되는 원형 컨텍스트 메뉴
public class TableContextMenu : MonoBehaviour
{
    public static TableContextMenu Instance;

    [SerializeField] private GameObject menuRoot;

    private PlacedTable selectedTable;

    private void Awake() { Instance = this; }

    public void Show(PlacedTable table)
    {
        selectedTable         = table;
        transform.position    = table.transform.position;
        menuRoot.SetActive(true);
    }

    public void Hide()
    {
        selectedTable = null;
        menuRoot.SetActive(false);
    }

    public void OnMoveClicked()
    {
        if (selectedTable == null) { return; }
        TablePlacementManager.Instance.StartMoving(selectedTable);
        Hide();
    }

    public void OnRemoveClicked()
    {
        if (selectedTable == null) { return; }
        selectedTable.RemoveTable();
        TableSaveLoadManager.Instance.SavePlacement();
        Hide();
    }

    public void OnCancelClicked() { Hide(); }
}