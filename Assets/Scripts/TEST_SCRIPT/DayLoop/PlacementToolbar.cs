using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 자리 배치 도구 모음.
/// 씬의 원래 배치 UI(왼쪽 아래 노란 상자)는 고정 픽셀 캔버스라 화면이 크면 버튼이 작고 멀게 보인다.
/// 같은 기능을 화면 아래쪽 가운데의 큰 버튼으로 보여 주고, 원래 배치 UI는 투명하게 숨긴다.
/// 표시 여부는 원래 배치 UI의 활성 상태(배치 모드)를 그대로 따른다.
/// </summary>
public class PlacementToolbar : MonoBehaviour
{
    private GameObject _legacy;
    private Canvas _canvas;
    private RectTransform _root;
    private TMP_Text _seats;
    private float _nextSeatRefresh;

    private void Start()
    {
        TablePlacementManager placement = TablePlacementManager.Instance;
        if (placement == null || placement.PlacementUI == null)
        {
            enabled = false;
            return;
        }

        _legacy = placement.PlacementUI;
        CanvasGroup group = _legacy.GetComponent<CanvasGroup>();
        if (group == null) group = _legacy.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Build(placement);
        _root.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_root == null) return;
        bool show = _legacy != null && _legacy.activeInHierarchy;
        if (_root.gameObject.activeSelf != show)
            _root.gameObject.SetActive(show);
        if (show && Time.unscaledTime >= _nextSeatRefresh)
        {
            _nextSeatRefresh = Time.unscaledTime + 0.25f;
            RefreshSeats();
        }
    }

    private void RefreshSeats()
    {
        SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
        int count = seats != null ? seats.IndoorSeatCount : 0;
        _seats.text = GameSession.IsActive
            ? $"좌석 {count} / 최대 {RestaurantRules.SeatLimit(GameSession.Current.restaurantLevel)}석"
            : $"좌석 {count}석";
    }

    private void Build(TablePlacementManager placement)
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] Placement", 50, gameObject.scene);
        _canvas.transform.SetParent(transform, false);

        _root = DayLoopUI.Rect("Root", _canvas.transform);
        DayLoopUI.Stretch(_root);

        var rail = DayLoopUI.ScreenRail(_root, "PlacementRail");
        var heading = DayLoopUI.Card(rail, DayLoopUI.Kit.Sail, "PlacementStatus");
        DayLoopUI.Layout(heading, preferredHeight: 224);
        DayLoopUI.Vertical(heading, 20, 12);
        DayLoopUI.Layout(DayLoopUI.Text(heading.transform, "자리 배치", 38, DayLoopUI.Kit.Ink, style: FontStyles.Bold), preferredHeight: 50);
        _seats = DayLoopUI.Text(heading.transform, "", 26, DayLoopUI.Kit.SeaDeep, style: FontStyles.Bold);
        DayLoopUI.Layout(_seats, preferredHeight: 38);
        DayLoopUI.Layout(DayLoopUI.Text(heading.transform, "테이블을 고르고 칸으로 옮긴 뒤 [놓기]. 놓인 테이블을 누르면 편집해요.", 22, DayLoopUI.Kit.InkSoft), preferredHeight: 84);
        DayLoopUI.Layout(DayLoopUI.Text(rail, "테이블", 22, Color.white, style: FontStyles.Bold), preferredHeight: 28);
        DayLoopUI.RailButton(rail, "1인석", placement.OnOneSeatButtonClicked, height: 58);
        DayLoopUI.RailButton(rail, "2인석", placement.OnTwoSeatButtonClicked, height: 58);
        DayLoopUI.RailButton(rail, "4인석", placement.OnFourSeatButtonClicked, height: 58);
        DayLoopUI.RailButton(rail, "놓기", placement.OnConfirmClicked, true, 72);
        DayLoopUI.RailButton(rail, "회전", placement.OnRotateClicked, height: 56);
        DayLoopUI.RailButton(rail, "되돌리기", () => { placement.OnCancelClicked(); TableSaveLoadManager.Instance?.UndoEdit(); }, height: 56);
        DayLoopUI.RailButton(rail, "취소", placement.OnCancelClicked, height: 56);
        DayLoopUI.RailButton(rail, "전체 복원", () => { placement.OnCancelClicked(); TableSaveLoadManager.Instance?.ResetEdits(); }, height: 56);
        DayLoopUI.Spacer(rail);
        DayLoopUI.RailButton(rail, "배치 끝내기", placement.ExitPlacementMode, height: 66);
    }

    private static void AddButton(Transform parent, string label, Color background, Color foreground, UnityEngine.Events.UnityAction onClick, float width = 150)
    {
        Button button = DayLoopUI.KitButton(parent, label, background, foreground, 28, onClick, label);
        DayLoopUI.Layout(button, preferredWidth: width, preferredHeight: 86);
    }

    private static void AddDivider(Transform parent)
    {
        Image divider = DayLoopUI.Panel(parent, DayLoopUI.Kit.Divider, "Divider", false);
        divider.raycastTarget = false;
        DayLoopUI.Layout(divider, preferredWidth: 4, preferredHeight: 74);
    }
}
