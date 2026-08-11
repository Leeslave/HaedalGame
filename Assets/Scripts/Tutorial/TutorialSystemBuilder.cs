#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 오버레이 계층을 한 번에 만들어주는 에디터 도구.
/// [GameObject > UI > Tutorial System] 메뉴로 매니저 + 오버레이가 연결된 상태로 생성된다.
/// 생성 후 스프라이트/폰트/색만 취향대로 바꾸고 프리팹으로 저장해서 쓰면 된다.
/// </summary>
public static class TutorialSystemBuilder
{
    private const float TextBlockWidth = 700f;

    [MenuItem("GameObject/UI/Tutorial System", false, 2100)]
    public static void CreateTutorialSystem(MenuCommand menuCommand)
    {
        GameObject systemObject = new GameObject("TutorialSystem");
        GameObjectUtility.SetParentAndAlign(systemObject, menuCommand.context as GameObject);

        TutorialManager manager = systemObject.AddComponent<TutorialManager>();
        TutorialHighlightView view = CreateOverlay(systemObject.transform);

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("_view").objectReferenceValue = view;
        serializedManager.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(systemObject, "Create Tutorial System");
        Selection.activeGameObject = systemObject;

        Debug.Log("[Tutorial] TutorialSystem을 만들었다. TutorialManager의 Sequences에 시퀀스를 등록하고 사용하면 된다.");
    }

    private static TutorialHighlightView CreateOverlay(Transform parent)
    {
        // --- Canvas ---
        GameObject canvasObject = new GameObject("TutorialOverlay",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 다른 UI보다 항상 위

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0f;

        TutorialHighlightView view = canvasObject.AddComponent<TutorialHighlightView>();

        // --- 강조 테두리 ---
        GameObject highlightObject = new GameObject("HighlightImage", typeof(RectTransform), typeof(Image));
        highlightObject.transform.SetParent(canvasObject.transform, false);

        RectTransform highlightRect = (RectTransform)highlightObject.transform;
        highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
        highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
        highlightRect.pivot = new Vector2(0.5f, 0.5f);
        highlightRect.sizeDelta = new Vector2(160f, 160f);

        Image highlightImage = highlightObject.GetComponent<Image>();
        highlightImage.raycastTarget = false;
        highlightImage.color = new Color(1f, 1f, 1f, 0.35f);
        highlightImage.type = Image.Type.Sliced;

        // 임시 스프라이트. 실제 링 이미지가 준비되면 인스펙터에서 교체한다.
        Sprite circleSprite = GetBuiltinSprite("UI/Skin/Knob.psd");
        Sprite rectangleSprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
        highlightImage.sprite = circleSprite;

        // --- 문구 묶음 ---
        GameObject textBlockObject = new GameObject("TextBlock", typeof(RectTransform));
        textBlockObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textBlockRect = (RectTransform)textBlockObject.transform;
        textBlockRect.anchorMin = new Vector2(0.5f, 0.5f);
        textBlockRect.anchorMax = new Vector2(0.5f, 0.5f);
        textBlockRect.pivot = new Vector2(0.5f, 0.5f);
        textBlockRect.sizeDelta = new Vector2(TextBlockWidth, 0f);

        VerticalLayoutGroup blockLayout = textBlockObject.AddComponent<VerticalLayoutGroup>();
        blockLayout.childAlignment = TextAnchor.MiddleCenter;
        blockLayout.spacing = 12f;
        blockLayout.childControlWidth = true;
        blockLayout.childControlHeight = true;
        blockLayout.childForceExpandWidth = true;
        blockLayout.childForceExpandHeight = false;

        ContentSizeFitter blockFitter = textBlockObject.AddComponent<ContentSizeFitter>();
        blockFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        blockFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text titleText = CreateText(textBlockObject.transform, "TitleText", 36f, FontStyles.Bold, Color.white);
        TMP_Text descriptionText = CreateText(textBlockObject.transform, "DescriptionText", 24f, FontStyles.Normal, Color.white);
        TMP_Text instructionText = CreateText(textBlockObject.transform, "InstructionText", 28f, FontStyles.Bold, Color.white);

        GameObject extraRootObject = new GameObject("ExtraLineRoot", typeof(RectTransform));
        extraRootObject.transform.SetParent(textBlockObject.transform, false);

        VerticalLayoutGroup extraLayout = extraRootObject.AddComponent<VerticalLayoutGroup>();
        extraLayout.childAlignment = TextAnchor.MiddleCenter;
        extraLayout.spacing = 6f;
        extraLayout.childControlWidth = true;
        extraLayout.childControlHeight = true;
        extraLayout.childForceExpandWidth = true;
        extraLayout.childForceExpandHeight = false;

        ContentSizeFitter extraFitter = extraRootObject.AddComponent<ContentSizeFitter>();
        extraFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        extraFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 부연 설명 줄의 원본. 꺼둔 채로 두고 복제해서 쓴다.
        TMP_Text extraLineTemplate = CreateText(canvasObject.transform, "ExtraLineTemplate", 20f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.8f));
        extraLineTemplate.gameObject.SetActive(false);

        // --- 건너뛰기 버튼 ---
        GameObject skipObject = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipObject.transform.SetParent(canvasObject.transform, false);

        RectTransform skipRect = (RectTransform)skipObject.transform;
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.anchoredPosition = new Vector2(-24f, -24f);
        skipRect.sizeDelta = new Vector2(140f, 56f);

        Image skipImage = skipObject.GetComponent<Image>();
        skipImage.sprite = rectangleSprite;
        skipImage.type = Image.Type.Sliced;
        skipImage.color = new Color(0f, 0f, 0f, 0.6f);

        TMP_Text skipText = CreateText(skipObject.transform, "Text", 22f, FontStyles.Normal, Color.white);
        skipText.text = "건너뛰기";
        RectTransform skipTextRect = skipText.rectTransform;
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.offsetMin = Vector2.zero;
        skipTextRect.offsetMax = Vector2.zero;

        // --- 참조 연결 ---
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("_root").objectReferenceValue = canvasObject;
        serializedView.FindProperty("_canvas").objectReferenceValue = canvas;
        serializedView.FindProperty("_maskRoot").objectReferenceValue = canvasObject.GetComponent<RectTransform>();
        serializedView.FindProperty("_highlightImage").objectReferenceValue = highlightImage;
        serializedView.FindProperty("_circleSprite").objectReferenceValue = circleSprite;
        serializedView.FindProperty("_rectangleSprite").objectReferenceValue = rectangleSprite;
        serializedView.FindProperty("_textBlock").objectReferenceValue = textBlockRect;
        serializedView.FindProperty("_titleText").objectReferenceValue = titleText;
        serializedView.FindProperty("_descriptionText").objectReferenceValue = descriptionText;
        serializedView.FindProperty("_instructionText").objectReferenceValue = instructionText;
        serializedView.FindProperty("_extraLineRoot").objectReferenceValue = extraRootObject.GetComponent<RectTransform>();
        serializedView.FindProperty("_extraLinePrefab").objectReferenceValue = extraLineTemplate;
        serializedView.FindProperty("_skipButton").objectReferenceValue = skipObject.GetComponent<Button>();
        serializedView.ApplyModifiedProperties();

        return view;
    }

    private static TMP_Text CreateText(Transform parent, string objectName, float fontSize, FontStyles style, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = objectName;

        return text;
    }

    private static Sprite GetBuiltinSprite(string path)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }
}
#endif
