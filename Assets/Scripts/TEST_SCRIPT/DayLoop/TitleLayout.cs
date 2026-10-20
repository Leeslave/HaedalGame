using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 타이틀 화면 배치 (UI 시안 01).
/// 씬 파일은 그대로 두고, 타이틀이 열리면 씬의 UI 오브젝트를 화면 기준으로 다시 배치한다.
///  - 왼쪽: 반투명 진갈색 세로 띠 위에 로고(Banner)와 버튼 4개를 세로로 정렬
///    저장이 있으면 [이어하기]를 맨 위에 두고 바로 아래에 저장 요약 띠(TitleMenuController)를 보여 준다.
///  - 오른쪽 아래: 서빙하는 해달(Character)
/// 씬 구성이 예상과 다르면(버튼을 못 찾으면) 아무것도 바꾸지 않는다.
/// </summary>
public class TitleLayout : MonoBehaviour
{
    private const float ColumnWidth = 700f;
    private const float ButtonWidth = 440f;
    private const float ButtonHeight = 116f;    // 버튼 그림 비율(1901:499)
    private const float StackHeight = 1060f;    // 이 높이보다 화면이 낮으면 왼쪽 칸 전체를 줄인다

    private RectTransform _canvasRect;
    private RectTransform _stack;

    private void Start()
    {
        Button newGame = FindButton("New Start");
        Button resume = FindButton("Continue");
        Button settings = FindButton("Setting");
        Button quit = FindButton("Exit");
        if (newGame == null || resume == null || settings == null || quit == null)
            return;

        Canvas canvas = resume.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvas = canvas.rootCanvas;
        _canvasRect = (RectTransform)canvas.transform;

        // 서빙 해달: 오른쪽 아래 (띠보다 먼저 두어 띠·버튼 뒤에 그려지게 한다)
        Transform character = FindChild("Character");
        if (character is RectTransform characterRect)
        {
            characterRect.SetParent(_canvasRect, false);
            characterRect.SetAsLastSibling();
            characterRect.anchorMin = characterRect.anchorMax = new Vector2(1f, 0f);
            characterRect.pivot = new Vector2(1f, 0f);
            characterRect.localScale = Vector3.one * 0.46f;
            characterRect.anchoredPosition = new Vector2(-90f, -10f);
        }

        // 왼쪽 세로 띠
        RectTransform column = DayLoopUI.Rect("[Title] LeftColumn", _canvasRect);
        column.anchorMin = new Vector2(0f, 0f);
        column.anchorMax = new Vector2(0f, 1f);
        column.pivot = new Vector2(0f, 0.5f);
        column.offsetMin = Vector2.zero;
        column.offsetMax = new Vector2(ColumnWidth, 0f);
        column.SetAsLastSibling();
        Image band = column.gameObject.AddComponent<Image>();
        band.color = new Color(DayLoopUI.Kit.Line.r, DayLoopUI.Kit.Line.g, DayLoopUI.Kit.Line.b, 0.55f);
        band.raycastTarget = false;

        _stack = DayLoopUI.Rect("Stack", column);
        DayLoopUI.Place(_stack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(ColumnWidth, StackHeight));

        // 로고: 위에서 매달린 간판
        Transform banner = FindChild("Banner");
        if (banner is RectTransform bannerRect)
        {
            bannerRect.SetParent(_stack, false);
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.localScale = Vector3.one * 0.46f;
            bannerRect.anchoredPosition = Vector2.zero;
        }

        // 버튼: 저장이 있으면 이어하기가 먼저. 이어하기 아래에는 저장 요약 띠(58) 자리를 둔다.
        bool hasSave = SaveService.HasSaveFile();
        Button first = hasSave ? resume : newGame;
        Button second = hasSave ? newGame : resume;
        float top = 470f;
        top = PlaceButton(first, top) + (hasSave ? 76f : 18f);
        top = PlaceButton(second, top) + 18f;
        top = PlaceButton(settings, top) + 18f;
        PlaceButton(quit, top);

        FitToScreen();
    }

    private void LateUpdate()
    {
        FitToScreen();
    }

    // 가로로 긴 화면에서 캔버스 높이가 낮아지면 왼쪽 칸이 아래로 넘치지 않게 줄인다.
    private void FitToScreen()
    {
        if (_stack == null || _canvasRect == null) return;
        float height = _canvasRect.rect.height;
        float scale = height > 0f ? Mathf.Min(1f, height / StackHeight) : 1f;
        if (!Mathf.Approximately(_stack.localScale.x, scale))
            _stack.localScale = new Vector3(scale, scale, 1f);
    }

    /// <returns>버튼 아래쪽 끝(위에서부터의 거리)</returns>
    private float PlaceButton(Button button, float top)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.SetParent(_stack, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        rect.anchoredPosition = new Vector2(0f, -(top + ButtonHeight * 0.5f));
        return top + ButtonHeight;
    }

    private Button FindButton(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == objectName)
                    return button;
            }
        }
        return null;
    }

    private Transform FindChild(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName && child.GetComponentInParent<Canvas>(true) != null && child.GetComponentInParent<DialogueView>(true) == null)
                    return child;
            }
        }
        return null;
    }
}
