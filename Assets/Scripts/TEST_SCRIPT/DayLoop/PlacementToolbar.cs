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

        // 배치 모드 안내: 바다색 띠 (제목 · 좌석 수 · 조작 안내)
        Image mode = DayLoopUI.Card(_root, DayLoopUI.Kit.Sea, "ModeBar", 3f, 5f);
        DayLoopUI.Place(mode.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 186), new Vector2(1500, 72));
        mode.raycastTarget = false;
        HorizontalLayoutGroup modeRow = DayLoopUI.Horizontal(mode, 0, 18);
        modeRow.padding = new RectOffset(28, 28, 8, 8);
        TMP_Text title = DayLoopUI.Text(mode.transform, "자리 배치", 32, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(title, preferredWidth: 150, preferredHeight: 50);
        _seats = DayLoopUI.Chip(mode.transform, "", DayLoopUI.Kit.SeaDeep, Color.white, 23, "Seats");
        TMP_Text hint = DayLoopUI.Text(mode.transform, "테이블을 고르고 칸으로 옮긴 뒤 [놓기]를 눌러요. 놓인 테이블을 누르면 이동·삭제할 수 있어요.",
            21, new Color32(0xD6, 0xF1, 0xF2, 0xFF), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(hint, flexibleWidth: 1, preferredHeight: 50);

        // 도구 막대: 자리 종류 | 회전·놓기·취소 | 배치 끝내기
        Image bar = DayLoopUI.Card(_root, DayLoopUI.Kit.Sail, "Toolbar", 3f, 7f);
        DayLoopUI.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(1380, 128));
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(bar, 0, 14, TextAnchor.MiddleCenter);
        row.padding = new RectOffset(22, 22, 16, 20);

        AddButton(bar.transform, "1인석", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, placement.OnOneSeatButtonClicked);
        AddButton(bar.transform, "2인석", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, placement.OnTwoSeatButtonClicked);
        AddButton(bar.transform, "4인석", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, placement.OnFourSeatButtonClicked);
        AddDivider(bar.transform);
        AddButton(bar.transform, "회전", DayLoopUI.Kit.WoodLight, Color.white, placement.OnRotateClicked);
        AddButton(bar.transform, "놓기", DayLoopUI.Kit.Kelp, Color.white, placement.OnConfirmClicked, 170);
        AddButton(bar.transform, "취소", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, placement.OnCancelClicked);
        AddDivider(bar.transform);
        AddButton(bar.transform, "배치 끝내기", DayLoopUI.Kit.Awning, Color.white, placement.ExitPlacementMode, 220);
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
