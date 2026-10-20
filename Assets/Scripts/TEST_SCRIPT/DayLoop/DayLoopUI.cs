using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 코드로 UI를 조립할 때 쓰는 공용 도구와 색상 규격.
/// 크림·나무 계열 불투명 패널, 9-slice 둥근 모서리, 한글 TMP 폰트 탐색을 담당한다.
/// </summary>
public static class DayLoopUI
{
    // ───── 색상 (크림·나무 계열) ─────
    public static readonly Color Cream = new Color32(0xFB, 0xF4, 0xE6, 0xFF);
    public static readonly Color CreamDeep = new Color32(0xF0, 0xE1, 0xC4, 0xFF);
    public static readonly Color Wood = new Color32(0x9A, 0x66, 0x42, 0xFF);
    public static readonly Color WoodDark = new Color32(0x5E, 0x3C, 0x27, 0xFF);
    public static readonly Color Ink = new Color32(0x3A, 0x29, 0x1D, 0xFF);
    public static readonly Color InkSoft = new Color32(0x7D, 0x66, 0x54, 0xFF);
    public static readonly Color Coral = new Color32(0xD9, 0x5F, 0x45, 0xFF);
    public static readonly Color Sea = new Color32(0x2F, 0x86, 0x8C, 0xFF);
    public static readonly Color Good = new Color32(0x3B, 0x8A, 0x5A, 0xFF);
    public static readonly Color Warn = new Color32(0xC2, 0x45, 0x36, 0xFF);
    public static readonly Color GoldText = new Color32(0xB9, 0x7C, 0x10, 0xFF);
    public static readonly Color Dim = new Color(0.05f, 0.08f, 0.10f, 0.72f);
    public static readonly Color Curtain = new Color32(0x16, 0x22, 0x29, 0xFF);

    /// <summary>
    /// UI 시안(해달식당 UI 키트) 색상. 진갈색 외곽선 + 아래쪽 단단한 그림자로 게임 아트의 굵은 선과 맞춘다.
    /// 영업 HUD는 위의 기존 색을 그대로 쓰고, 다시 그린 화면만 이 색을 쓴다.
    /// 차양색(Awning)은 화면마다 가장 중요한 행동 하나에만 쓴다.
    /// </summary>
    public static class Kit
    {
        public static readonly Color Sail = new Color32(0xFB, 0xF3, 0xE2, 0xFF);      // 패널 바탕
        public static readonly Color Sand = new Color32(0xF2, 0xE2, 0xC2, 0xFF);      // 보조 버튼·칸
        public static readonly Color Wood = new Color32(0x8E, 0x5A, 0x34, 0xFF);      // 상단 바·탭
        public static readonly Color WoodLight = new Color32(0x9A, 0x62, 0x38, 0xFF);
        public static readonly Color Line = new Color32(0x4A, 0x2E, 0x1B, 0xFF);      // 외곽선·그림자
        public static readonly Color Awning = new Color32(0xC8, 0x44, 0x3A, 0xFF);    // 핵심 행동
        public static readonly Color Sea = new Color32(0x1F, 0x7F, 0x8A, 0xFF);       // 확인·선택됨
        public static readonly Color SeaDeep = new Color32(0x15, 0x5A, 0x63, 0xFF);
        public static readonly Color SeaTint = new Color32(0xEA, 0xF6, 0xF6, 0xFF);
        public static readonly Color Coin = new Color32(0xF2, 0xB6, 0x32, 0xFF);      // 골드·별
        public static readonly Color Kelp = new Color32(0x3B, 0x8A, 0x5A, 0xFF);      // 성공·호평
        public static readonly Color Alert = new Color32(0xE0, 0x57, 0x4A, 0xFF);     // 위험 게이지
        public static readonly Color Night = new Color32(0x2D, 0x3E, 0x66, 0xFF);     // 잠듦·취침
        public static readonly Color Ink = new Color32(0x3A, 0x27, 0x18, 0xFF);       // 본문 글자
        public static readonly Color InkSoft = new Color32(0x7D, 0x66, 0x54, 0xFF);   // 보조 글자
        public static readonly Color GoldInk = new Color32(0x8A, 0x5E, 0x00, 0xFF);
        public static readonly Color GoodInk = new Color32(0x2F, 0x70, 0x48, 0xFF);
        public static readonly Color WarnInk = new Color32(0xA3, 0x39, 0x2F, 0xFF);
        public static readonly Color WarnTint = new Color32(0xFF, 0xF1, 0xE8, 0xFF);
        public static readonly Color GoodTint = new Color32(0xE4, 0xF2, 0xE8, 0xFF);
        public static readonly Color Divider = new Color32(0xE6, 0xD2, 0xAE, 0xFF);
    }

    public const float RefWidth = 1920f;
    public const float RefHeight = 1080f;

    // ───── 폰트 ─────

    private static TMP_FontAsset _font;

    /// <summary>
    /// 씬에 이미 쓰이고 있는 한글 TMP 폰트를 찾아 공유한다. (Pretendard 우선)
    /// 찾지 못하면 TMP 기본 폰트를 쓰며, 이때 한글이 깨질 수 있으므로 경고를 남긴다.
    /// </summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (_font != null)
                return _font;

            TMP_FontAsset fallback = null;
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                TMP_FontAsset candidate = text.font;
                if (candidate == null || !candidate.HasCharacter('가'))
                    continue;

                if (candidate.name.Contains("Pretendard"))
                {
                    _font = candidate;
                    return _font;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            if (fallback != null)
            {
                _font = fallback;
                return _font;
            }

            Debug.LogWarning("[DayLoop] 씬에서 한글 TMP 폰트를 찾지 못해 기본 폰트를 사용합니다.");
            return TMP_Settings.defaultFontAsset;
        }
    }

    // ───── 스프라이트 ─────

    private static Sprite _rounded;
    private static Sprite _white;

    /// <summary>9-slice 둥근 사각형. Image.type = Sliced, SpriteRenderer.drawMode = Sliced로 크기를 바꿔 쓴다.</summary>
    public static Sprite Rounded
    {
        get
        {
            if (_rounded == null)
                _rounded = CreateRoundedSprite(48, 18);
            return _rounded;
        }
    }

    public static Sprite White
    {
        get
        {
            if (_white == null)
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "DayLoopWhite", hideFlags = HideFlags.DontSave };
                Color32[] pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                _white.hideFlags = HideFlags.DontSave;
            }
            return _white;
        }
    }

    private static Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "DayLoopRounded",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        // 월드 오브젝트에서도 쓰므로 PPU는 size(1칸 = 1유닛)로 둔다.
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    // ───── 월드 정렬 ─────

    /// <summary>가장 위에 그려지는 Sorting Layer. 월드 상태 표시가 캐릭터·가구에 가려지지 않게 쓴다.</summary>
    public static int TopSortingLayerId
    {
        get
        {
            SortingLayer[] layers = SortingLayer.layers;
            return layers.Length > 0 ? layers[layers.Length - 1].id : 0;
        }
    }

    // ───── UI 조립 ─────

    public static Canvas CreateCanvas(string name, int sortingOrder, Scene scene)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (scene.IsValid())
            SceneManager.MoveGameObjectToScene(go, scene);

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            go.layer = uiLayer;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : go.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static Image Panel(Transform parent, Color color, string name = "Panel", bool rounded = true)
    {
        RectTransform rt = Rect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        if (rounded)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }
        return image;
    }

    public static TextMeshProUGUI Text(Transform parent, string value, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Normal, string name = "Text")
    {
        RectTransform rt = Rect(name, parent);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    public static Button Button(Transform parent, string label, Color background, Color foreground, float fontSize, UnityAction onClick, string name = "Button")
    {
        Image image = Panel(parent, background, name);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        button.colors = colors;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI text = Text(image.transform, label, fontSize, foreground, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        Stretch(text.rectTransform);
        return button;
    }

    public static void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }

    public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static LayoutElement Layout(Component target, float preferredWidth = -1, float preferredHeight = -1,
        float flexibleWidth = -1, float flexibleHeight = -1, float minHeight = -1)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        element.minHeight = minHeight;
        return element;
    }

    public static VerticalLayoutGroup Vertical(Component target, int padding, float spacing, TextAnchor align = TextAnchor.UpperLeft)
    {
        VerticalLayoutGroup group = target.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(padding, padding, padding, padding);
        group.spacing = spacing;
        group.childAlignment = align;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        return group;
    }

    public static HorizontalLayoutGroup Horizontal(Component target, int padding, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
    {
        HorizontalLayoutGroup group = target.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(padding, padding, padding, padding);
        group.spacing = spacing;
        group.childAlignment = align;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return group;
    }

    public static Outline AddOutline(Component target, Color color, float distance = 2f)
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        return outline;
    }

    // ───── UI 키트 조립 ─────

    /// <summary>진갈색 외곽선과 아래쪽 그림자를 붙인다. drop이 0이면 외곽선만 붙인다.</summary>
    public static T Chunky<T>(T target, float line = 3f, float drop = 5f) where T : Component
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = Kit.Line;
        outline.effectDistance = new Vector2(line, -line);
        if (drop > 0f)
        {
            Shadow shadow = target.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Kit.Line;
            shadow.effectDistance = new Vector2(0f, -drop);
        }
        return target;
    }

    /// <summary>키트 패널: 둥근 바탕 + 외곽선 + 아래 그림자.</summary>
    public static Image Card(Transform parent, Color color, string name = "Card", float line = 3f, float drop = 6f)
    {
        return Chunky(Panel(parent, color, name), line, drop);
    }

    /// <summary>키트 버튼: 외곽선과 아래 그림자가 있는 눌리는 버튼.</summary>
    public static Button KitButton(Transform parent, string label, Color background, Color foreground, float fontSize, UnityAction onClick, string name = "Button")
    {
        Button button = Button(parent, label, background, foreground, fontSize, onClick, name);
        Chunky(button.image, 3f, 5f);
        return button;
    }

    /// <summary>둥근 상태 칩. 레이아웃 그룹 안에서는 글자 길이만큼 넓어진다.</summary>
    public static TextMeshProUGUI Chip(Transform parent, string label, Color background, Color foreground, float fontSize = 20f, string name = "Chip")
    {
        Image chip = Chunky(Panel(parent, background, name), 2f, 0f);
        chip.raycastTarget = false;
        HorizontalLayoutGroup layout = Horizontal(chip, 0, 0, TextAnchor.MiddleCenter);
        layout.padding = new RectOffset(16, 16, 4, 4);
        TextMeshProUGUI text = Text(chip.transform, label, fontSize, foreground, TextAlignmentOptions.Center, FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>게이지. 반환한 Image의 rectTransform.anchorMax.x로 채움 비율을 바꾼다. (SetBar 사용)</summary>
    public static Image Bar(Transform parent, float value, Color fill, string name = "Bar")
    {
        Image track = Chunky(Panel(parent, Kit.Divider, name), 2f, 0f);
        track.raycastTarget = false;
        Image bar = Panel(track.transform, fill, "Fill");
        bar.raycastTarget = false;
        bar.rectTransform.anchorMin = Vector2.zero;
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.rectTransform.offsetMin = Vector2.zero;
        bar.rectTransform.offsetMax = Vector2.zero;
        SetBar(bar, value);
        return bar;
    }

    public static void SetBar(Image bar, float value)
    {
        if (bar == null) return;
        bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        bar.enabled = value > 0.001f;
    }

    /// <summary>카드 위쪽 가운데에 걸치는 제목 리본.</summary>
    public static TextMeshProUGUI Ribbon(Transform card, string label, Color background, float width = 420f, float fontSize = 40f)
    {
        Image ribbon = Card(card, background, "Ribbon", 4f, 6f);
        ribbon.raycastTarget = false;
        Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 84));
        TextMeshProUGUI text = Text(ribbon.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Title");
        Stretch(text.rectTransform, 12, 0, 12, 0);
        return text;
    }

    /// <summary>음식 아이콘. 아이콘이 없으면 모래색 접시에 이름 첫 글자를 보여 준다.</summary>
    public static Image FoodIcon(Transform parent, RecipeData recipe, float size, string name = "FoodIcon")
    {
        Sprite icon = recipe != null ? recipe.Icon : null;
        Image image;
        if (icon != null)
        {
            RectTransform rt = Rect(name, parent);
            image = rt.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
        }
        else
        {
            image = Chunky(Panel(parent, Kit.Sand, name), 2f, 0f);
            string initial = recipe != null && !string.IsNullOrEmpty(recipe.RecipeName) ? recipe.RecipeName.Substring(0, 1) : "?";
            TextMeshProUGUI text = Text(image.transform, initial, size * 0.42f, Kit.Wood, TextAlignmentOptions.Center, FontStyles.Bold, "Initial");
            Stretch(text.rectTransform);
        }
        image.raycastTarget = false;
        Layout(image, preferredWidth: size, preferredHeight: size);
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    /// <summary>레이아웃 안에서 남는 공간을 채우는 빈 칸.</summary>
    public static RectTransform Spacer(Transform parent, float width = -1f, float height = -1f)
    {
        RectTransform spacer = Rect("Spacer", parent);
        if (width < 0f && height < 0f) Layout(spacer, flexibleWidth: 1, flexibleHeight: 1);
        else Layout(spacer, preferredWidth: width, preferredHeight: height);
        return spacer;
    }

    public static void SetButtonColors(Button button, Color background, Color foreground)
    {
        if (button == null) return;
        button.image.color = background;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.color = foreground;
    }

    public static string Hex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

    /// <summary>폰트에 글리프가 없으면 대체 문자를 쓴다.</summary>
    public static string Glyph(string glyph, string fallback)
    {
        TMP_FontAsset font = Font;
        return font != null && font.HasCharacters(glyph) ? glyph : fallback;
    }

    // ───── 문자열 ─────

    public static string Gold(int amount) => amount.ToString("N0") + "G";

    public static string SignedGold(int amount) => (amount > 0 ? "+" : amount < 0 ? "-" : "") + Mathf.Abs(amount).ToString("N0") + "G";

    public static string Rating(float value) => value.ToString("0.0");

    public static string Seconds(float sum, int samples) => samples > 0 ? (sum / samples).ToString("0.0") + "초" : "측정 없음";
}
