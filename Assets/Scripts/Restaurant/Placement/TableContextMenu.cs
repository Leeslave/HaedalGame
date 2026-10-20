using UnityEngine;
using UnityEngine.UI;

// 배치된 테이블 클릭 시 표시되는 원형 컨텍스트 메뉴
public class TableContextMenu : MonoBehaviour
{
    public static TableContextMenu Instance;

    [SerializeField] private GameObject menuRoot;

    private PlacedTable selectedTable;
    private Canvas designCanvas;
    private RectTransform designMenu;

    private void Awake() { Instance = this; }

    public void Show(PlacedTable table)
    {
        if (!RestaurantProgress.CanManage) return;
        selectedTable         = table;
        transform.position    = table.transform.position;
        if (!GameSession.IsActive) { menuRoot.SetActive(true); return; }
        if (menuRoot != null) menuRoot.SetActive(false);
        if (designCanvas == null) BuildDesignMenu();
        designMenu.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        var canvasRect = (RectTransform)designCanvas.transform;
        Vector2 screen = Camera.main != null ? (Vector2)Camera.main.WorldToScreenPoint(table.transform.position) : new Vector2(Screen.width * .5f, Screen.height * .5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var point);
        point.x = Mathf.Clamp(point.x + 90, canvasRect.rect.xMin + DayLoopUI.RailWidth + 16, canvasRect.rect.xMax - 320);
        point.y = Mathf.Clamp(point.y, canvasRect.rect.yMin + 300, canvasRect.rect.yMax - 16);
        designMenu.anchoredPosition = point;
    }

    public void Hide()
    {
        selectedTable = null;
        menuRoot.SetActive(false);
        if (designMenu != null) designMenu.gameObject.SetActive(false);
    }

    public void OnMoveClicked()
    {
        if (selectedTable == null) { return; }
        PreOperationUIManager.Instance?.HideUI();
        TablePlacementManager.Instance.EnterPlacementMode();
        TablePlacementManager.Instance.StartMoving(selectedTable);
        Hide();
    }

    public void OnRemoveClicked()
    {
        if (selectedTable == null) { return; }
        TableSaveLoadManager.Instance.RememberEdit();
        selectedTable.RemoveTable();
        TableSaveLoadManager.Instance.SavePlacement();
        Hide();
    }

    public void OnCancelClicked() { Hide(); }

    private void BuildDesignMenu()
    {
        designCanvas = DayLoopUI.CreateCanvas("Table editing", 70, gameObject.scene);
        var panel = DayLoopUI.Card(designCanvas.transform, DayLoopUI.Kit.Sail, "TableMenu");
        designMenu = panel.rectTransform;
        DayLoopUI.Place(designMenu, new Vector2(.5f, .5f), new Vector2(0, 1), Vector2.zero, new Vector2(304, 284));
        DayLoopUI.Vertical(panel, 16, 12);
        Add("이동 · 회전", OnMoveClicked);
        Add("색상 바꾸기", () => {
            if (selectedTable == null) return;
            TableSaveLoadManager.Instance.RememberEdit();
            var skins = GameSession.Current.progression.unlockedTableSkins ?? new System.Collections.Generic.List<int> { 0, 1 };
            int index = skins.IndexOf(selectedTable.Skin);
            selectedTable.SetSkin(skins[(index + 1) % skins.Count]);
            TableSaveLoadManager.Instance.SavePlacement(); Hide();
        });
        Add("철거", OnRemoveClicked);
        Add("닫기", Hide);
    }

    private void Add(string label, UnityEngine.Events.UnityAction action)
    {
        var button = DayLoopUI.KitButton(designMenu, label, DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 24, action);
        DayLoopUI.Layout(button, preferredHeight: 50);
    }

    private void OnDestroy() { if (designCanvas != null) Destroy(designCanvas.gameObject); }
}
