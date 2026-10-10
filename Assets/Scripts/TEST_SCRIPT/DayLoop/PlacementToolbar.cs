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
    }

    private void Build(TablePlacementManager placement)
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] Placement", 50, gameObject.scene);
        _canvas.transform.SetParent(transform, false);

        _root = DayLoopUI.Rect("Root", _canvas.transform);
        DayLoopUI.Stretch(_root);

        Image hint = DayLoopUI.Panel(_root, new Color(0.18f, 0.12f, 0.08f, 0.85f), "Hint");
        DayLoopUI.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(1000, 54));
        hint.raycastTarget = false;
        TMP_Text hintText = DayLoopUI.Text(hint.transform, "자리를 고르고 원하는 칸으로 옮긴 뒤 [놓기]를 눌러요. 놓인 테이블을 누르면 이동·삭제할 수 있어요.",
            22, Color.white, TextAlignmentOptions.Center);
        DayLoopUI.Stretch(hintText.rectTransform, 16, 0, 16, 0);

        Image bar = DayLoopUI.Panel(_root, DayLoopUI.Cream, "Toolbar");
        DayLoopUI.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(1320, 122));
        DayLoopUI.AddOutline(bar, DayLoopUI.Wood, 3f);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(bar, 0, 14, TextAnchor.MiddleCenter);
        row.padding = new RectOffset(22, 22, 14, 14);

        AddButton(bar.transform, "1인석", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, placement.OnOneSeatButtonClicked);
        AddButton(bar.transform, "2인석", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, placement.OnTwoSeatButtonClicked);
        AddButton(bar.transform, "4인석", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, placement.OnFourSeatButtonClicked);
        AddButton(bar.transform, "회전", DayLoopUI.Wood, Color.white, placement.OnRotateClicked);

        RectTransform gap = DayLoopUI.Rect("Gap", bar.transform);
        DayLoopUI.Layout(gap, preferredWidth: 30, preferredHeight: 10);

        AddButton(bar.transform, "놓기", DayLoopUI.Good, Color.white, placement.OnConfirmClicked);
        AddButton(bar.transform, "취소", DayLoopUI.Coral, Color.white, placement.OnCancelClicked);
        AddButton(bar.transform, "배치 끝내기", DayLoopUI.Sea, Color.white, placement.ExitPlacementMode, 200);
    }

    private static void AddButton(Transform parent, string label, Color background, Color foreground, UnityEngine.Events.UnityAction onClick, float width = 150)
    {
        Button button = DayLoopUI.Button(parent, label, background, foreground, 28, onClick, label);
        DayLoopUI.Layout(button, preferredWidth: width, preferredHeight: 90);
    }
}
