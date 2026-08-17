#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대사 화면 계층을 한 번에 만들어주는 에디터 도구.
/// [GameObject > UI > Dialogue System] 메뉴로 매니저 + 화면이 연결된 상태로 생성된다.
/// 생성 후 스프라이트/색만 취향대로 바꾸고 프리팹으로 저장해서 쓰면 된다.
/// </summary>
public static class DialogueSystemBuilder
{
    private const float ReferenceWidth = 800f;
    private const float ReferenceHeight = 600f;

    private const string KoreanFontPath = "Assets/Font/Pretendard-Regular 1 SDF.asset";

    [MenuItem("GameObject/UI/Dialogue System", false, 2101)]
    public static void CreateDialogueSystem(MenuCommand menuCommand)
    {
        GameObject systemObject = new GameObject("DialogueSystem");
        GameObjectUtility.SetParentAndAlign(systemObject, menuCommand.context as GameObject);

        DialogueManager manager = systemObject.AddComponent<DialogueManager>();
        DialogueView view = CreateView(systemObject.transform);

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("_view").objectReferenceValue = view;
        serializedManager.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(systemObject, "Create Dialogue System");
        Selection.activeGameObject = systemObject;

        Debug.Log("[Dialogue] DialogueSystem을 만들었다. DialogueManager의 Scripts에 JSON을 등록하거나 " +
                  "DialogueTrigger로 재생하면 된다.");
    }

    private static DialogueView CreateView(Transform parent)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);

        if (font == null)
            Debug.LogWarning($"[Dialogue] 한글 폰트({KoreanFontPath})를 찾지 못해 TMP 기본 폰트로 만든다. " +
                             "한글이 깨지면 인스펙터에서 폰트를 바꿔준다.");

        Sprite panelSprite = GetBuiltinSprite("UI/Skin/Background.psd");
        Sprite buttonSprite = GetBuiltinSprite("UI/Skin/UISprite.psd");

        // --- Canvas ---
        GameObject canvasObject = new GameObject("DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400; // 게임 UI보다는 위, 튜토리얼(500)보다는 아래

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0f;

        DialogueView view = canvasObject.AddComponent<DialogueView>();

        // --- 배경 ---
        // Cover로 맞출 때 넘치는 부분을 잘라내야 해서 마스크를 씌운다.
        GameObject backgroundRoot = CreateStretched(canvasObject.transform, "BackgroundRoot");
        backgroundRoot.AddComponent<RectMask2D>();

        Image backgroundImage = CreateBackgroundLayer(backgroundRoot.transform, "BackgroundImage");
        Image backgroundNextImage = CreateBackgroundLayer(backgroundRoot.transform, "BackgroundNextImage");
        backgroundNextImage.enabled = false;

        // --- 클릭 판 ---
        // 배경 위, 대사 상자 아래에 깔아 화면 아무 곳이나 눌러도 다음으로 넘어가게 한다.
        GameObject clickObject = CreateStretched(canvasObject.transform, "ClickCatcher");
        Image clickImage = clickObject.AddComponent<Image>();
        clickImage.color = new Color(0f, 0f, 0f, 0f); // 투명해도 클릭은 받는다
        clickImage.raycastTarget = true;
        DialogueClickCatcher clickCatcher = clickObject.AddComponent<DialogueClickCatcher>();

        // --- 초상화 ---
        GameObject portraitRoot = CreateStretched(canvasObject.transform, "PortraitRoot");

        Image leftPortrait = CreatePortrait(portraitRoot.transform, "LeftPortrait", new Vector2(0f, 0f), new Vector2(170f, 0f));
        Image centerPortrait = CreatePortrait(portraitRoot.transform, "CenterPortrait", new Vector2(0.5f, 0f), Vector2.zero);
        Image rightPortrait = CreatePortrait(portraitRoot.transform, "RightPortrait", new Vector2(1f, 0f), new Vector2(-170f, 0f));

        // --- 대사 상자 ---
        GameObject textBox = new GameObject("TextBox", typeof(RectTransform), typeof(Image));
        textBox.transform.SetParent(canvasObject.transform, false);

        RectTransform textBoxRect = (RectTransform)textBox.transform;
        textBoxRect.anchorMin = new Vector2(0f, 0f);
        textBoxRect.anchorMax = new Vector2(1f, 0f);
        textBoxRect.pivot = new Vector2(0.5f, 0f);
        textBoxRect.sizeDelta = new Vector2(-72f, 168f);
        textBoxRect.anchoredPosition = new Vector2(0f, 28f);

        Image textBoxImage = textBox.GetComponent<Image>();
        textBoxImage.sprite = panelSprite;
        textBoxImage.type = Image.Type.Sliced;
        textBoxImage.color = new Color(0.05f, 0.08f, 0.14f, 0.88f);
        // 여기서 클릭을 먹으면 ClickCatcher까지 닿지 않으므로 꺼둔다.
        textBoxImage.raycastTarget = false;

        // 이름표
        GameObject nameBox = new GameObject("NameBox", typeof(RectTransform), typeof(Image));
        nameBox.transform.SetParent(textBox.transform, false);

        RectTransform nameBoxRect = (RectTransform)nameBox.transform;
        nameBoxRect.anchorMin = new Vector2(0f, 1f);
        nameBoxRect.anchorMax = new Vector2(0f, 1f);
        nameBoxRect.pivot = new Vector2(0f, 0f);
        nameBoxRect.sizeDelta = new Vector2(180f, 38f);
        nameBoxRect.anchoredPosition = new Vector2(26f, -4f);

        Image nameBoxImage = nameBox.GetComponent<Image>();
        nameBoxImage.sprite = panelSprite;
        nameBoxImage.type = Image.Type.Sliced;
        nameBoxImage.color = new Color(0.12f, 0.20f, 0.32f, 0.95f);
        nameBoxImage.raycastTarget = false;

        TMP_Text nameText = CreateText(nameBox.transform, "NameText", 20f, FontStyles.Bold,
            Color.white, TextAlignmentOptions.Center, font);
        StretchInside(nameText.rectTransform, 10f, 4f);

        // 본문
        TMP_Text bodyText = CreateText(textBox.transform, "BodyText", 22f, FontStyles.Normal,
            Color.white, TextAlignmentOptions.TopLeft, font);
        StretchInside(bodyText.rectTransform, 30f, 24f);
        bodyText.text = "";

        // 다음 표시
        TMP_Text nextIndicator = CreateText(textBox.transform, "NextIndicator", 20f, FontStyles.Normal,
            new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Center, font);
        nextIndicator.text = "▼";

        RectTransform indicatorRect = nextIndicator.rectTransform;
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.sizeDelta = new Vector2(30f, 30f);
        indicatorRect.anchoredPosition = new Vector2(-18f, 10f);
        nextIndicator.gameObject.SetActive(false);

        // --- 선택지 ---
        GameObject choiceRoot = new GameObject("ChoiceRoot", typeof(RectTransform));
        choiceRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform choiceRect = (RectTransform)choiceRoot.transform;
        choiceRect.anchorMin = new Vector2(0.5f, 0.5f);
        choiceRect.anchorMax = new Vector2(0.5f, 0.5f);
        choiceRect.pivot = new Vector2(0.5f, 0.5f);
        choiceRect.sizeDelta = new Vector2(420f, 0f);
        choiceRect.anchoredPosition = new Vector2(0f, 40f);

        VerticalLayoutGroup choiceLayout = choiceRoot.AddComponent<VerticalLayoutGroup>();
        choiceLayout.childAlignment = TextAnchor.MiddleCenter;
        choiceLayout.spacing = 10f;
        choiceLayout.childControlWidth = true;
        choiceLayout.childControlHeight = true;
        choiceLayout.childForceExpandWidth = true;
        choiceLayout.childForceExpandHeight = false;

        ContentSizeFitter choiceFitter = choiceRoot.AddComponent<ContentSizeFitter>();
        choiceFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        choiceFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button choiceTemplate = CreateButton(choiceRoot.transform, "ChoiceTemplate", "선택지",
            new Vector2(420f, 46f), buttonSprite, new Color(0.15f, 0.24f, 0.38f, 0.95f), 20f, font);

        LayoutElement choiceElement = choiceTemplate.gameObject.AddComponent<LayoutElement>();
        choiceElement.minHeight = 46f;
        choiceElement.preferredHeight = 46f;

        choiceTemplate.gameObject.SetActive(false);
        choiceRoot.SetActive(false);

        // --- 건너뛰기 버튼 ---
        // canSkip을 켠 대사에서만 켜지므로 기본은 꺼둔다.
        Button skipButton = CreateButton(canvasObject.transform, "SkipButton", "건너뛰기 ▶▶",
            new Vector2(118f, 40f), buttonSprite, new Color(0f, 0f, 0f, 0.55f), 17f, font);

        RectTransform skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.anchoredPosition = new Vector2(-18f, -18f);
        skipButton.gameObject.SetActive(false);

        // --- 참조 연결 ---
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("_root").objectReferenceValue = canvasObject;
        serializedView.FindProperty("_canvas").objectReferenceValue = canvas;
        serializedView.FindProperty("_backgroundRoot").objectReferenceValue = backgroundRoot.GetComponent<RectTransform>();
        serializedView.FindProperty("_backgroundImage").objectReferenceValue = backgroundImage;
        serializedView.FindProperty("_backgroundNextImage").objectReferenceValue = backgroundNextImage;
        serializedView.FindProperty("_leftPortrait").objectReferenceValue = leftPortrait;
        serializedView.FindProperty("_centerPortrait").objectReferenceValue = centerPortrait;
        serializedView.FindProperty("_rightPortrait").objectReferenceValue = rightPortrait;
        serializedView.FindProperty("_textBox").objectReferenceValue = textBox;
        serializedView.FindProperty("_nameBox").objectReferenceValue = nameBox;
        serializedView.FindProperty("_nameText").objectReferenceValue = nameText;
        serializedView.FindProperty("_bodyText").objectReferenceValue = bodyText;
        serializedView.FindProperty("_nextIndicator").objectReferenceValue = nextIndicator.gameObject;
        serializedView.FindProperty("_choiceRoot").objectReferenceValue = choiceRect;
        serializedView.FindProperty("_choiceTemplate").objectReferenceValue = choiceTemplate;
        serializedView.FindProperty("_skipButton").objectReferenceValue = skipButton;
        serializedView.FindProperty("_clickCatcher").objectReferenceValue = clickCatcher;
        serializedView.ApplyModifiedProperties();

        // 에디터에서 모양을 보고 고칠 수 있게 켜둔 채로 둔다. 실행하면 DialogueManager가 바로 감춘다.
        return view;
    }

    #region 조각 만들기

    private static GameObject CreateStretched(Transform parent, string objectName)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);

        StretchInside((RectTransform)created.transform, 0f, 0f);

        return created;
    }

    private static void StretchInside(RectTransform rect, float horizontalPadding, float verticalPadding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private static Image CreateBackgroundLayer(Transform parent, string objectName)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        created.transform.SetParent(parent, false);

        StretchInside((RectTransform)created.transform, 0f, 0f);

        Image image = created.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;

        return image;
    }

    private static Image CreatePortrait(Transform parent, string objectName, Vector2 anchor, Vector2 offset)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        created.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)created.transform;
        rect.anchorMin = new Vector2(anchor.x, 0f);
        rect.anchorMax = new Vector2(anchor.x, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(300f, 360f);
        rect.anchoredPosition = new Vector2(offset.x, 150f);

        Image image = created.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.enabled = false;

        return image;
    }

    private static TMP_Text CreateText(Transform parent, string objectName, float fontSize, FontStyles style,
        Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.text = objectName;

        return text;
    }

    private static Button CreateButton(Transform parent, string objectName, string label, Vector2 size,
        Sprite sprite, Color color, float fontSize, TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        TMP_Text text = CreateText(buttonObject.transform, "Label", fontSize, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Center, font);
        StretchInside(text.rectTransform, 8f, 4f);
        text.text = label;

        return buttonObject.GetComponent<Button>();
    }

    private static Sprite GetBuiltinSprite(string path)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }

    #endregion
}
#endif
