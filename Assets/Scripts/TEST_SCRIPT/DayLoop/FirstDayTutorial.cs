using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 새 게임 1일차 튜토리얼. 해야 할 일을 한 단계씩 짚어 준다.
///  1 섬: [관리] 열기 → 2 첫 조리도구 무료로 받기 → 3 오늘의 메뉴 정하기 → 4 관리 창 닫고 [영업 준비]로 식당 가기
///  5 식당: [운영 시작] → (영업 중에는 FirstDayGuide가 첫 손님을 따라가며 안내) → 6 결산 확인 후 섬으로
///  7 섬: 집에서 취침·저장 → 2일차가 되면 끝
/// 단계는 저장 데이터(첫 도구·메뉴 확인·영업 완료·일차)로 정하므로 중간에 껐다 켜도 이어진다.
/// 짚는 곳만 눌리도록 나머지 화면은 어둡게 막는다. [튜토리얼 건너뛰기]를 누르면 다시 나오지 않는다.
/// </summary>
public class FirstDayTutorial : MonoBehaviour
{
    private const string DoneId = "Tutorial.FirstDay.Done";
    private const string MenuId = "Tutorial.FirstDay.Menu";
    private const int StepCount = 7;
    private const float HolePadding = 10f;
    // 관리 창 본문(왼쪽 450부터)을 짚을 때 왼쪽 띠 안에 말풍선이 들어가는 폭.
    private const float BubbleWidth = 400f;

    private enum Step { None, OpenManagement, ReceiveTool, ChooseMenu, GoRestaurant, StartOperation, Operating, Settlement, Sleep }

    /// <summary>한 번의 안내: 무엇을 짚고 무엇을 말할지.</summary>
    private struct Hint
    {
        public string key, title, body, action;
        public Action run;
        public Func<bool> actionEnabled;
        public RectTransform target;
        public bool soft; // 화면을 막지 않고 표시만 한다(결산처럼 읽을 거리가 있는 화면).
    }

    private Scene _scene;
    private Canvas _canvas;
    private RectTransform _root, _frame, _bubble;
    private readonly Image[] _dims = new Image[4];
    private TMP_Text _bubbleTitle, _bubbleBody, _bubbleChip;
    private Button _bubbleAction;
    private Func<bool> _actionEnabled;
    private string _shownKey;
    private Step _lastStep = Step.None;
    private Hint _hint;
    private float _nextLookup;
    private DayCycleController _day;
    private Button _watchedStart;
    private float _startCheckAt;
    private bool _startRefused;

    // ───── 설치 ─────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameScenes.Island && scene.name != GameScenes.Restaurant) return;
        if (FindObjectsByType<FirstDayTutorial>(FindObjectsSortMode.None).Any(t => t.gameObject.scene == scene)) return;
        var go = new GameObject("[Tutorial] FirstDay");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<FirstDayTutorial>();
    }

    private void Start()
    {
        _scene = gameObject.scene;
        _canvas = DayLoopUI.CreateCanvas("[Tutorial] Overlay", 400, _scene);
        _canvas.transform.SetParent(transform, false);
        _root = (RectTransform)_canvas.transform;
        Build();
        _root.gameObject.SetActive(false);
    }

    // ───── 단계 ─────

    private static bool Running => GameSession.IsActive && GameSession.Current.day == 1 && !GameSession.Current.HasTutorialCompleted(DoneId);

    private Step Current()
    {
        if (!Running) return Step.None;
        GameSaveData s = GameSession.Current;
        if (_scene.name == GameScenes.Restaurant)
        {
            if (_day == null) _day = FindFirstObjectByType<DayCycleController>();
            if (!s.progression.firstToolChosen) return Step.ReceiveTool;
            if (_day == null) return Step.None;
            switch (_day.Phase)
            {
                case DayLoopPhase.Preparation: return GameSession.OperationCompletedToday ? Step.None : Step.StartOperation;
                case DayLoopPhase.Settlement: return Step.Settlement;
                default: return Step.Operating;
            }
        }
        if (GameSession.OperationCompletedToday) return Step.Sleep;
        if (!s.progression.firstToolChosen) return ManagementOpen || FirstToolPicker.IsOpen ? Step.ReceiveTool : Step.OpenManagement;
        if (!s.HasTutorialCompleted(MenuId)) return Step.ChooseMenu;
        return Step.GoRestaurant;
    }

    private static int Number(Step step) => step switch
    {
        Step.OpenManagement => 1,
        Step.ReceiveTool => 2,
        Step.ChooseMenu => 3,
        Step.GoRestaurant => 4,
        Step.StartOperation => 5,
        Step.Settlement => 6,
        _ => 7,
    };

    /// <summary>지금 단계에서 짚을 곳과 말. 짚을 곳이 아직 없으면 null을 돌려 안내를 숨긴다.</summary>
    private Hint? Describe(Step step)
    {
        switch (step)
        {
            case Step.OpenManagement:
                return Target("관리 열기", "식당 운영에 필요한 일은 관리 창에서 해요.\n왼쪽 아래 [관리]를 눌러 볼까요?",
                    Find("[DayLoop] IslandHUD", "관리"));

            case Step.ReceiveTool:
                if (FirstToolPicker.IsOpen)
                    return Target("첫 조리도구 받기", "처음 도구 하나는 무료예요. 마음에 드는 도구를 고르고 아래 [무료로 받기]를 눌러요.\n그 도구로 만드는 기본 메뉴가 함께 생겨요.",
                        Find("[FirstTool] Picker", "Card"), "picker");
                return Ask("첫 조리도구 받기", "아직 첫 조리도구를 받지 않았어요. 도구가 있어야 요리를 팔 수 있어요.", "도구 고르기",
                    () => FirstToolPicker.Show(_scene, OpenMenu));

            case Step.ChooseMenu:
                if (Find("Restaurant management", "Slots") != null)
                {
                    string menu = FirstMenuName();
                    string body = menu != null
                        ? $"'{menu}'{Particle(menu)} 오늘의 메뉴에 올라갔어요.\n메뉴 카드를 누르고 오른쪽 아래 버튼으로 등록하거나 뺄 수 있어요."
                        : "메뉴 카드를 누르고 오른쪽 아래 [오늘 메뉴에 등록]을 눌러요.";
                    var hint = Target("오늘의 메뉴 정하기", body, Find("Restaurant management", "ContentFrame"), menu ?? "empty");
                    if (hint == null) return null;
                    var value = hint.Value;
                    value.action = "메뉴 정했어요";
                    value.run = () => GameSession.TryMarkTutorialCompleted(MenuId);
                    value.actionEnabled = () => GameSession.Current.menuSlots.Any(id => id >= 0);
                    return value;
                }
                return Ask("오늘의 메뉴 정하기", "오늘 팔 메뉴를 정해요.", "메뉴 열기", OpenMenu);

            case Step.GoRestaurant:
                if (ManagementOpen)
                    return Target("식당으로 가기", "준비가 끝났어요. 관리 창을 닫아요.", Find("Restaurant management", "Close"), "close");
                return Target("식당으로 가기", "[영업 준비]를 눌러 식당으로 가요.", Find("[DayLoop] IslandHUD", "영업 준비"), "go");

            case Step.StartOperation:
                if (ManagementOpen)
                    return Target("영업 시작", "관리 창을 닫고 영업을 시작해요.", Find("Restaurant management", "Close"), "close");
                RectTransform start = Find("[DayLoop] Guide", "운영 시작");
                WatchStart(start);
                if (_startRefused)
                {
                    // 영업을 시작하지 못했으면 화면을 풀어 준비 카드의 안내대로 고칠 수 있게 한다.
                    var refused = Target("영업 시작", "아직 영업을 시작할 수 없어요. 왼쪽 준비 카드의 안내를 확인하고 다시 [운영 시작]을 눌러요.", start, "refused");
                    if (refused == null) return null;
                    var open = refused.Value; open.soft = true;
                    return open;
                }
                return Target("영업 시작", "식당에 왔어요! [운영 시작]을 누르면 손님이 들어와요.\n주문부터 계산까지는 직원이 알아서 해요.", start);

            case Step.Settlement:
                var settle = Target("오늘의 결산", "첫 영업 끝! 번 돈과 손님 평가를 확인하고 섬으로 돌아가요.",
                    Find("[DayLoop] Settlement", "섬으로 돌아가기"));
                if (settle == null) return null;
                var soft = settle.Value; soft.soft = true;
                return soft;

            case Step.Sleep:
                if (FindCanvas("[DayLoop] Settlement") != null) return null; // 다시 보여 주는 결산은 그대로 둔다.
                RectTransform sleep = FindButton("Design / 09 House", "취침");
                if (sleep != null)
                    return Target("하루 마무리", "[취침 · 저장]을 누르면 첫날이 끝나고 2일차 아침으로 저장돼요.", sleep, "house");
                return Target("하루 마무리", "영업을 마치면 자유시간이에요.\n집에서 잠들면 하루가 저장돼요. [집]을 눌러요.", Find("[DayLoop] IslandHUD", "Map 집"), "map");
        }
        return null;
    }

    private static Hint? Target(string title, string body, RectTransform target, string variant = "")
    {
        if (target == null) return null;
        return new Hint { key = title + variant, title = title, body = body, target = target };
    }

    private static Hint Ask(string title, string body, string action, Action run)
        => new Hint { key = title + "?", title = title, body = body, action = action, run = run };

    private static void OpenMenu() => RestaurantManagementUI.OpenSection("메뉴");

    /// <summary>[운영 시작]을 누른 뒤 잠시 지나도 준비 단계면 시작이 막힌 것으로 본다.</summary>
    private void WatchStart(RectTransform start)
    {
        Button button = start != null ? start.GetComponent<Button>() : null;
        if (button == null || button == _watchedStart) return;
        _watchedStart = button;
        button.onClick.AddListener(() => _startCheckAt = Time.unscaledTime + 1f);
    }

    private void Skip()
    {
        GameSession.TryMarkTutorialCompleted(DoneId);
        _root.gameObject.SetActive(false);
    }

    // ───── 매 프레임 ─────

    private void Update()
    {
        if (_root == null) return;
        if (Time.unscaledTime >= _nextLookup)
        {
            _nextLookup = Time.unscaledTime + .2f;
            Step step = Current();
            // 관리 창을 처음 열면 바로 도구 선택 창을 띄운다.
            if (step == Step.ReceiveTool && _lastStep == Step.OpenManagement && !FirstToolPicker.IsOpen)
                FirstToolPicker.Show(_scene, OpenMenu);
            if (step != Step.StartOperation) { _startRefused = false; _startCheckAt = 0f; }
            else if (_startCheckAt > 0f && Time.unscaledTime >= _startCheckAt) { _startRefused = true; _startCheckAt = 0f; }
            _lastStep = step;
            Hint? hint = step == Step.None || step == Step.Operating ? null : Describe(step);
            _root.gameObject.SetActive(hint != null);
            if (hint == null) { _shownKey = null; return; }
            _hint = hint.Value;
            if (_shownKey != _hint.key) Show(step, _hint);
        }
        if (_root.gameObject.activeSelf) Layout();
    }

    // ───── 화면 ─────

    private void Build()
    {
        for (int i = 0; i < _dims.Length; i++)
        {
            _dims[i] = DayLoopUI.Panel(_root, new Color(0.05f, 0.08f, 0.10f, 0.62f), "Dim", false);
            Corner(_dims[i].rectTransform);
        }

        var ring = DayLoopUI.Panel(_root, DayLoopUI.Kit.Coin, "Focus");
        ring.sprite = RingSprite; ring.raycastTarget = false;
        _frame = ring.rectTransform;
        Corner(_frame);

        var bubble = DayLoopUI.Card(_root, DayLoopUI.Kit.Sail, "Bubble", 4f, 6f);
        bubble.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Line;
        _bubble = bubble.rectTransform;
        Corner(_bubble);
        _bubble.sizeDelta = new Vector2(BubbleWidth, 0);
        var layout = DayLoopUI.Vertical(bubble, 0, 12);
        layout.padding = new RectOffset(24, 24, 20, 14);
        bubble.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var header = DayLoopUI.Rect("Header", _bubble);
        DayLoopUI.Layout(header, preferredHeight: 40);
        DayLoopUI.Horizontal(header, 0, 10);
        _bubbleChip = DayLoopUI.Chip(header, "", DayLoopUI.Kit.Sea, Color.white, 19);
        _bubbleTitle = DayLoopUI.Text(header, "", 30, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft);
        _bubbleTitle.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(_bubbleTitle, flexibleWidth: 1, preferredHeight: 40);

        _bubbleBody = DayLoopUI.Text(_bubble, "", 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.TopLeft);
        _bubbleBody.lineSpacing = 8f;

        _bubbleAction = DayLoopUI.KitButton(_bubble, "", DayLoopUI.Kit.Sea, Color.white, 26, () => { _hint.run?.Invoke(); _nextLookup = 0f; }, "Action");
        DayLoopUI.Layout(_bubbleAction, preferredHeight: 64);

        var skip = DayLoopUI.Button(_bubble, "튜토리얼 건너뛰기", Color.clear, DayLoopUI.Kit.InkSoft, 18, Skip, "Skip");
        skip.GetComponentInChildren<TMP_Text>().font = DesignFonts.Body;
        DayLoopUI.Layout(skip, preferredHeight: 34);
    }

    private void Show(Step step, Hint hint)
    {
        _shownKey = hint.key;
        _bubbleChip.text = $"{Number(step)} / {StepCount}";
        var chipText = _bubbleChip;
        DayLoopUI.Layout(chipText, preferredWidth: chipText.GetPreferredValues(chipText.text).x, preferredHeight: 19 * 1.25f);
        _bubbleTitle.text = hint.title;
        _bubbleBody.text = hint.body;
        DayLoopUI.Layout(_bubbleBody, preferredHeight: Mathf.Ceil(_bubbleBody.GetPreferredValues(hint.body, BubbleWidth - 48, 0).y) + 6);
        _bubbleAction.gameObject.SetActive(hint.action != null);
        if (hint.action != null) DayLoopUI.SetButtonLabel(_bubbleAction, hint.action);
        _actionEnabled = hint.actionEnabled;
        foreach (var dim in _dims) dim.raycastTarget = !hint.soft;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
    }

    private void Layout()
    {
        Vector2 size = _root.rect.size;
        Rect hole = Rect.zero;
        bool hasHole = _hint.target != null && _hint.target.gameObject.activeInHierarchy;
        if (hasHole)
        {
            hole = ToOverlay(_hint.target);
            hole.xMin -= HolePadding; hole.yMin -= HolePadding; hole.xMax += HolePadding; hole.yMax += HolePadding;
        }
        else if (_hint.target != null)
        {
            // 짚던 버튼이 사라졌으면(창이 닫히는 중 등) 다음 확인 때 단계를 다시 고른다.
            _nextLookup = 0f;
        }

        float alpha = _hint.soft ? 0f : 1f;
        foreach (var dim in _dims) dim.color = new Color(0.05f, 0.08f, 0.10f, 0.62f * alpha);
        SetRect(_dims[0], 0, 0, size.x, hole.yMin);
        SetRect(_dims[1], 0, hole.yMax, size.x, size.y);
        SetRect(_dims[2], 0, hole.yMin, hole.xMin, hole.yMax);
        SetRect(_dims[3], hole.xMax, hole.yMin, size.x, hole.yMax);

        _frame.gameObject.SetActive(hasHole);
        if (hasHole)
        {
            float pulse = 6f + Mathf.Sin(Time.unscaledTime * 5f) * 4f;
            _frame.anchoredPosition = new Vector2(hole.xMin - pulse, hole.yMin - pulse);
            _frame.sizeDelta = new Vector2(hole.width + pulse * 2, hole.height + pulse * 2);
        }

        if (_bubbleAction.gameObject.activeSelf) _bubbleAction.interactable = _actionEnabled == null || _actionEnabled();
        PlaceBubble(size, hasHole ? hole : (Rect?)null);
    }

    /// <summary>짚은 곳 오른쪽 → 왼쪽 → 아래 → 위 순서로 들어갈 자리를 찾는다. 짚은 곳이 없으면 가운데.</summary>
    private void PlaceBubble(Vector2 size, Rect? hole)
    {
        Vector2 b = _bubble.rect.size;
        const float gap = 14f, margin = 16f;
        Vector2 position;
        if (hole == null) position = (size - b) / 2f;
        else
        {
            Rect h = hole.Value;
            float y = Mathf.Clamp(h.center.y - b.y / 2f, margin, size.y - b.y - margin);
            float x = Mathf.Clamp(h.center.x - b.x / 2f, margin, size.x - b.x - margin);
            if (h.xMax + gap + b.x <= size.x - margin) position = new Vector2(h.xMax + gap, y);
            else if (h.xMin - gap - b.x >= margin) position = new Vector2(h.xMin - gap - b.x, y);
            else if (h.yMin - gap - b.y >= margin) position = new Vector2(x, h.yMin - gap - b.y);
            else if (h.yMax + gap + b.y <= size.y - margin) position = new Vector2(x, h.yMax + gap);
            else position = new Vector2(size.x - b.x - margin, margin);
        }
        _bubble.anchoredPosition = position;
    }

    private Rect ToOverlay(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Canvas source = target.GetComponentInParent<Canvas>();
        Camera camera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            local += _root.rect.size / 2f;
            min = Vector2.Min(min, local); max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void Corner(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
    }

    private static void SetRect(Image image, float x0, float y0, float x1, float y1)
    {
        image.rectTransform.anchoredPosition = new Vector2(x0, y0);
        image.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, x1 - x0), Mathf.Max(0, y1 - y0));
    }

    // ───── 찾기 ─────

    private bool ManagementOpen
    {
        get
        {
            Transform window = FindCanvas("Restaurant management")?.transform.Find("ManagementWindow");
            return window != null && window.gameObject.activeSelf;
        }
    }

    private Canvas FindCanvas(string name)
    {
        foreach (GameObject root in _scene.GetRootGameObjects())
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>())
                if (canvas.name == name && canvas.isActiveAndEnabled) return canvas;
        return null;
    }

    private RectTransform Find(string canvasName, string objectName)
    {
        Canvas canvas = FindCanvas(canvasName);
        if (canvas == null) return null;
        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>())
            if (rect.name == objectName) return rect;
        return null;
    }

    private RectTransform FindButton(string canvasName, string labelPrefix)
    {
        Canvas canvas = FindCanvas(canvasName);
        if (canvas == null) return null;
        foreach (Button button in canvas.GetComponentsInChildren<Button>())
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null && label.text.StartsWith(labelPrefix)) return (RectTransform)button.transform;
        }
        return null;
    }

    private static string FirstMenuName()
    {
        foreach (int id in GameSession.Current.menuSlots)
            if (id >= 0 && GameDatabase.TryGetRecipe(id, out RecipeData recipe)) return recipe.RecipeName;
        return null;
    }

    /// <summary>받침이 있으면 "이", 없으면 "가".</summary>
    private static string Particle(string word)
    {
        char last = word[word.Length - 1];
        return last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0 ? "이" : "가";
    }

    // ───── 강조 테두리 ─────

    private static Sprite _ring;

    /// <summary>둥근 사각형 테두리만 있는 9-slice. 가운데가 비어 짚은 버튼이 그대로 보인다.</summary>
    private static Sprite RingSprite
    {
        get
        {
            if (_ring != null) return _ring;
            const int size = 128, radius = 48;
            const float stroke = 14f, soft = 2.6f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "TutorialRing", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + .5f, py = y + .5f;
                    float cx = Mathf.Clamp(px, radius, size - radius), cy = Mathf.Clamp(py, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - radius; // 바깥선까지 거리(안쪽은 음수)
                    float alpha = Mathf.Clamp01(.5f - d / soft) * Mathf.Clamp01(.5f + (d + stroke) / soft);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            texture.SetPixels32(pixels);
            texture.Apply(true);
            _ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _ring.hideFlags = HideFlags.DontSave;
            return _ring;
        }
    }
}
