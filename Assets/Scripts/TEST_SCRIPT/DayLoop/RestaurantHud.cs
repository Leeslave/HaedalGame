using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 영업 HUD. (HUD-01, HUD-02, CLOSE-01) 캐주얼 시안: 글자를 줄이고 네 귀퉁이에 동글동글한 카드만 둔다.
///  - 왼쪽 위: 해 아이콘 · n일차 · 단계 칩 · 방문 게이지(마감 중에는 남은 손님)
///  - 오른쪽 위: 보유 골드 · 평점 · 배속(눌러서 x1 → x2 → x3) · 일시정지
///  - 왼쪽 아래: 오늘의 메뉴. 음식 그림에 남은 인분 스티커 (2인분 이하 코랄, 0이면 품절)
///  - 오른쪽 아래: 직원 얼굴. 둘레 링이 체력, 잠들면 Zz와 [깨우기]
/// 가운데는 월드(주방·식탁·웨이팅석)를 가리지 않게 비운다. 자리 대기 인원은 웨이팅석 아래 팻말(SeaRestaurantLayout)이 보여 준다.
/// 평점: 오늘 받은 평가의 평균(평가 전에는 식당 평점). 평가가 들어올 때마다 강조 이펙트와 점수 팝업.
/// </summary>
public class RestaurantHud : MonoBehaviour
{
    // 캐주얼 시안 색 (Kit 색보다 밝은 파스텔)
    private static readonly Color Sky = new Color32(0x7C, 0xC8, 0xEE, 0xFF);
    private static readonly Color Mint = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color Leaf = new Color32(0x5B, 0xBF, 0x8C, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    private static readonly Color Track = new Color32(0xF3, 0xE6, 0xD3, 0xFF);
    private static readonly Color Muted = new Color32(0xB8, 0xA8, 0x98, 0xFF);

    private class StaffSlot
    {
        public PartTimerAgent agent;
        public GameObject root;
        public Image ring;
        public Image face;
        public GameObject sleepBadge;
        public GameObject role;
        public GameObject wake;
    }

    private class MenuSlot
    {
        public RecipeData recipe;
        public Image bubble;
        public Image icon;
        public Image badge;
        public TMP_Text count;
    }

    private DayCycleController _day;
    private Canvas _canvas;
    private RectTransform _bar;
    private TMP_Text _dayText, _phaseText, _progressText, _goldText, _ratingText;
    private Image _phaseChip, _progressFill;
    private RectTransform _ratingBlock;
    private RestaurantRatingManager _rating;
    private float _lastRatingAverage;
    private int _lastRatingCount;
    private Coroutine _ratingPunch;
    private Button _speedButton, _pauseButton;
    private RectTransform _menuRow, _staffRow;
    private RectTransform _topRight;

    // 다른 UI가 영업 HUD 귀퉁이 카드를 피해 자리를 잡을 때 쓴다. (관리 버튼·첫날 안내 카드)
    public static RestaurantHud Current { get; private set; }
    public bool IsShowing => _bar != null && _bar.gameObject.activeInHierarchy;
    public float TopRightWidth => _topRight != null ? _topRight.rect.width : 0f;           // 오른쪽 위 알약 줄의 폭
    public float StaffTrayTop => _staffRow != null && _staffRow.parent.gameObject.activeInHierarchy
        ? ((RectTransform)_staffRow.parent).rect.height + 30f : 0f;                          // 오른쪽 아래 직원 카드 윗변 (화면 아래에서)
    private readonly List<MenuSlot> _menu = new List<MenuSlot>();
    private readonly List<StaffSlot> _staff = new List<StaffSlot>();
    private readonly List<int> _menuIds = new List<int>();
    private CanvasGroup _pauseOverlay;
    private CanvasGroup _toastGroup;
    private TMP_Text _toastText;
    private Coroutine _toastRoutine;
    private float _nextRefresh, _nextStaffScan;

    public void Init(DayCycleController day)
    {
        _day = day;
        Build();
        _day.OnPhaseChanged += HandlePhaseChanged;
        _day.OnToast += ShowToast;
        RestaurantSpeedController.OnFastForwardChanged += HandleSpeedChanged;
        _rating = RestaurantRatingManager.Instance;
        if (_rating != null)
        {
            _rating.OnTodayAverageChanged += HandleRatingChanged;
            _lastRatingAverage = _rating.TodayAverage;
            _lastRatingCount = _rating.TodayScoreCount;
        }
        HandlePhaseChanged(_day.Phase);
    }

    private void Awake() { Current = this; }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        if (_day != null)
        {
            _day.OnPhaseChanged -= HandlePhaseChanged;
            _day.OnToast -= ShowToast;
        }
        RestaurantSpeedController.OnFastForwardChanged -= HandleSpeedChanged;
        if (_rating != null) _rating.OnTodayAverageChanged -= HandleRatingChanged;
    }

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        bool operating = phase == DayLoopPhase.Open || phase == DayLoopPhase.Closing;
        _bar.gameObject.SetActive(operating);
        if (!operating) SetPauseOverlay(false);

        switch (phase)
        {
            case DayLoopPhase.Open:
                _phaseText.text = "영업 중";
                _phaseChip.color = Leaf;
                break;
            case DayLoopPhase.Closing:
                _phaseText.text = "입장 마감";
                _phaseChip.color = Peach;
                break;
            case DayLoopPhase.Settlement:
                _phaseText.text = "영업 종료";
                _phaseChip.color = Muted;
                break;
            default:
                _phaseText.text = "준비 중";
                _phaseChip.color = Muted;
                break;
        }

        _nextStaffScan = 0f;
        Refresh();
    }

    private void HandleSpeedChanged(bool fast)
    {
        RefreshControls();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.2f;
        Refresh();
    }

    private void Refresh()
    {
        if (_day == null || !_bar.gameObject.activeSelf) return;

        _dayText.text = $"{_day.Day}일차";

        CustomerSpawner spawner = _day.Spawner;
        int planned = spawner != null ? spawner.PlannedCount : 0;
        int visited = _day.Ledger.Visited;
        if (_day.Phase == DayLoopPhase.Closing)
            _progressText.text = $"남은 손님 {(spawner != null ? spawner.ActiveCustomerCount : 0)}";
        else
            _progressText.text = planned > 0 ? $"손님 {visited}/{planned}" : $"손님 {visited}";
        DayLoopUI.SetBar(_progressFill, planned > 0 ? (float)visited / planned : 0f);

        int gold = _day.CurrentGold;
        _goldText.text = gold >= 0 ? gold.ToString("N0") : "-";

        RefreshMenu();
        if (Time.unscaledTime >= _nextStaffScan) ScanStaff();
        RefreshStaff();
        RefreshRating();
        RefreshControls();
    }

    // ───── 오늘의 메뉴 ─────

    private void RefreshMenu()
    {
        if (!GameSession.IsActive) return;

        // 메뉴 구성이 바뀌었을 때만 다시 만든다.
        List<int> slots = GameSession.Current.menuSlots;
        bool changed = slots.Count != _menuIds.Count;
        for (int i = 0; !changed && i < slots.Count; i++) changed = slots[i] != _menuIds[i];
        if (changed)
        {
            _menuIds.Clear();
            _menuIds.AddRange(slots);
            foreach (MenuSlot slot in _menu) Destroy(slot.bubble.gameObject);
            _menu.Clear();
            _menuRow.parent.gameObject.SetActive(true);
            foreach (int id in slots)
                if (GameDatabase.TryGetRecipe(id, out RecipeData recipe)) _menu.Add(CreateMenuSlot(recipe));
            _menuRow.parent.gameObject.SetActive(_menu.Count > 0);
        }

        foreach (MenuSlot slot in _menu)
        {
            int count = IngredientInventoryService.Instance != null ? IngredientInventoryService.Instance.CraftableCount(slot.recipe) : 0;
            bool soldOut = count <= 0, low = !soldOut && count <= 2;
            slot.count.text = soldOut ? "품절" : count.ToString();
            slot.count.color = soldOut || low ? Color.white : DayLoopUI.Kit.Ink;
            slot.badge.color = soldOut ? Muted : low ? Peach : Color.white;
            slot.badge.rectTransform.sizeDelta = new Vector2(soldOut ? 58 : count >= 10 ? 44 : 36, 36);
            slot.bubble.color = soldOut ? new Color32(0xED, 0xE3, 0xD3, 0xFF) : Color.white;
            if (slot.icon != null) slot.icon.color = soldOut ? new Color(0.6f, 0.6f, 0.6f, 0.55f) : Color.white;
        }
    }

    private MenuSlot CreateMenuSlot(RecipeData recipe)
    {
        MenuSlot slot = new MenuSlot { recipe = recipe };
        slot.bubble = Disc(_menuRow, Color.white, 84, recipe.RecipeName);
        DayLoopUI.Chunky(slot.bubble, 3f, 0f);
        slot.icon = DayLoopUI.FoodIcon(slot.bubble.transform, recipe, 60);
        DayLoopUI.Place(slot.icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));

        slot.badge = Pill(slot.bubble.transform, Color.white, "Count", 3f, 0f);
        DayLoopUI.Place(slot.badge.rectTransform, new Vector2(1f, 1f), new Vector2(.5f, .5f), new Vector2(-8, -8), new Vector2(36, 36));
        slot.count = Jua(DayLoopUI.Text(slot.badge.transform, "", 18, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(slot.count.rectTransform);
        return slot;
    }

    // ───── 직원 ─────

    private void ScanStaff()
    {
        _nextStaffScan = Time.unscaledTime + 1f;
        var agents = new List<PartTimerAgent>();
        foreach (PartTimerAgent agent in FindObjectsByType<PartTimerAgent>(FindObjectsSortMode.None))
            if (agent.isActiveAndEnabled) agents.Add(agent);
        // 주방 먼저, 같은 역할은 이름순
        agents.Sort((a, b) =>
        {
            int ra = a is ChefAgent ? 0 : 1, rb = b is ChefAgent ? 0 : 1;
            return ra != rb ? ra.CompareTo(rb) : string.CompareOrdinal(a.name, b.name);
        });
        if (agents.Count > 4) agents.RemoveRange(4, agents.Count - 4);

        bool same = agents.Count == _staff.Count;
        for (int i = 0; same && i < agents.Count; i++) same = agents[i] == _staff[i].agent;
        if (same) return;

        foreach (StaffSlot slot in _staff) Destroy(slot.root);
        _staff.Clear();
        // 카드를 먼저 켜 두고 만든다. (꺼진 채로 만들면 버튼 등의 Awake가 미뤄진다)
        _staffRow.parent.gameObject.SetActive(agents.Count > 0);
        foreach (PartTimerAgent agent in agents) _staff.Add(CreateStaffSlot(agent));
    }

    private StaffSlot CreateStaffSlot(PartTimerAgent agent)
    {
        bool chef = agent is ChefAgent;
        StaffSlot slot = new StaffSlot { agent = agent };

        RectTransform column = DayLoopUI.Rect(agent.name, _staffRow);
        slot.root = column.gameObject;
        DayLoopUI.Vertical(column, 0, 6, TextAnchor.UpperCenter).childForceExpandWidth = false;
        DayLoopUI.Layout(column, preferredWidth: 92);

        RectTransform holder = DayLoopUI.Rect("Face", column);
        DayLoopUI.Layout(holder, preferredWidth: 84, preferredHeight: 84);
        Image track = Disc(holder, Track, 84, "StaminaTrack");
        DayLoopUI.Chunky(track, 3f, 0f);
        DayLoopUI.Stretch(track.rectTransform);

        slot.ring = DayLoopUI.Panel(track.transform, Mint, "Stamina", false);
        slot.ring.sprite = DayLoopUI.Circle;
        slot.ring.type = Image.Type.Filled;
        slot.ring.fillMethod = Image.FillMethod.Radial360;
        slot.ring.fillOrigin = (int)Image.Origin360.Top;
        slot.ring.fillClockwise = true;
        slot.ring.raycastTarget = false;
        DayLoopUI.Stretch(slot.ring.rectTransform);

        Image inner = Disc(track.transform, DayLoopUI.Kit.Sail, 64, "Inner");
        DayLoopUI.Place(inner.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(64, 64));
        inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        slot.face = DayLoopUI.Panel(inner.transform, Color.white, "Portrait", false);
        slot.face.sprite = DesignArt.Load(chef ? "OtterChef" : "OtterServer");
        slot.face.preserveAspect = true;
        slot.face.raycastTarget = false;
        DayLoopUI.Place(slot.face.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -6), new Vector2(80, 80));

        Image zz = Pill(holder, Color.white, "Sleeping", 3f, 0f);
        DayLoopUI.Place(zz.rectTransform, new Vector2(1f, 1f), new Vector2(.5f, .5f), new Vector2(-6, -6), new Vector2(44, 30));
        TMP_Text zzText = Jua(DayLoopUI.Text(zz.transform, "Zz", 18, Sky, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(zzText.rectTransform);
        slot.sleepBadge = zz.gameObject;

        TMP_Text role = Jua(DayLoopUI.Text(column, chef ? "주방" : "홀", 17, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center));
        DayLoopUI.Layout(role, preferredWidth: 84, preferredHeight: 30);
        slot.role = role.gameObject;

        Button wake = DayLoopUI.KitButton(column, "깨우기", Peach, Color.white, 17, agent.WakeUp, "Wake");
        Round(wake.image);
        Jua(wake.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(wake, preferredWidth: 84, preferredHeight: 30);
        slot.wake = wake.gameObject;
        return slot;
    }

    private void RefreshStaff()
    {
        foreach (StaffSlot slot in _staff)
        {
            if (slot.agent == null) { _nextStaffScan = 0f; continue; }
            float stamina = slot.agent.StaminaRatio;
            bool sleeping = slot.agent.IsSleeping;
            slot.ring.fillAmount = stamina;
            slot.ring.color = stamina > .6f ? Mint : stamina > .3f ? Butter : Peach;
            slot.face.color = sleeping ? new Color(1f, 1f, 1f, .6f) : Color.white;
            slot.sleepBadge.SetActive(sleeping);
            slot.role.SetActive(!sleeping);
            slot.wake.SetActive(sleeping);
        }
    }

    // ───── 평점 ─────

    private void RefreshRating()
    {
        if (_rating == null)
        {
            _ratingText.text = "-";
            return;
        }

        int count = _rating.TodayScoreCount;
        if (count == 0)
        {
            float restaurant = _rating.RestaurantRating;
            _ratingText.text = restaurant > 0f ? DayLoopUI.Rating(restaurant) : "-";
            _ratingText.color = DayLoopUI.Kit.Ink;
            return;
        }

        _ratingText.text = DayLoopUI.Rating(_rating.TodayAverage);
        _ratingText.color = _rating.CurrentTier == RatingBuffTier.Buff ? DayLoopUI.Kit.GoodInk
            : _rating.CurrentTier == RatingBuffTier.Debuff ? DayLoopUI.Kit.WarnInk : DayLoopUI.Kit.Ink;
    }

    private void HandleRatingChanged(float average)
    {
        int count = _rating != null ? _rating.TodayScoreCount : 0;
        // 방금 들어온 평가 한 건의 점수 = 새 합계 - 이전 합계
        float score = count > _lastRatingCount ? average * count - _lastRatingAverage * _lastRatingCount : average;
        _lastRatingAverage = average;
        _lastRatingCount = count;

        if (!_bar.gameObject.activeSelf) return;
        RefreshRating();
        if (_ratingPunch != null) StopCoroutine(_ratingPunch);
        _ratingPunch = StartCoroutine(RatingPunch());
        StartCoroutine(RatingPopup(score));
    }

    private IEnumerator RatingPunch()
    {
        const float duration = 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            _ratingBlock.localScale = Vector3.one * (1f + 0.25f * k);
            yield return null;
        }
        _ratingBlock.localScale = Vector3.one;
        _ratingPunch = null;
    }

    private IEnumerator RatingPopup(float score)
    {
        Color color = score >= 4f ? Leaf : score <= 2f ? Peach : DayLoopUI.Kit.Coin;
        Image popup = Pill(_ratingBlock, Color.white, "RatingPopup", 3f, 0f);
        popup.raycastTarget = false;
        popup.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        RectTransform rt = popup.rectTransform;
        DayLoopUI.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(110, 42));
        TMP_Text text = Jua(DayLoopUI.Text(popup.transform, "+" + DayLoopUI.Rating(Mathf.Clamp(score, 0f, 5f)), 24, color, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(text.rectTransform);

        const float duration = 1.4f;
        float t = 0f;
        while (t < duration && popup != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            rt.anchoredPosition = new Vector2(0f, -12f - 36f * Mathf.Sqrt(k));
            group.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }
        if (popup != null) Destroy(popup.gameObject);
    }

    private void RefreshControls()
    {
        DayLoopUI.SetButtonLabel(_speedButton, "x" + (RestaurantSpeedController.SpeedIndex + 1));
        DayLoopUI.SetButtonColors(_speedButton, RestaurantSpeedController.SpeedIndex > 0 ? DayLoopUI.Kit.Sea : Sky, Color.white);

        bool paused = _day != null && _day.IsPaused;
        DayLoopUI.SetButtonColors(_pauseButton, paused ? Butter : DayLoopUI.Kit.Sail, DayLoopUI.Kit.Line);
        SetPauseOverlay(paused);
    }

    private void SetPauseOverlay(bool visible)
    {
        if (_pauseOverlay == null) return;
        _pauseOverlay.alpha = visible ? 1f : 0f;
        _pauseOverlay.blocksRaycasts = visible;
        _pauseOverlay.interactable = visible;
    }

    // ───── 알림 ─────

    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        _toastText.text = message;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        _toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.8f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            _toastGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        _toastGroup.alpha = 0f;
        _toastRoutine = null;
    }

    // ───── 조립 ─────

    private void Build()
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] HUD", 40, gameObject.scene);
        _canvas.transform.SetParent(transform, false);
        _bar = DayLoopUI.Rect("OperationHud", _canvas.transform);
        DayLoopUI.Stretch(_bar);

        BuildToday();
        BuildTopRight();
        BuildMenuTray();
        BuildStaffTray();
        BuildPauseOverlay();
        BuildToast();
    }

    // 왼쪽 위: 해 · n일차 · 단계 칩 / 방문 게이지
    private void BuildToday()
    {
        Image today = Pill(_bar, DayLoopUI.Kit.Sail, "Today");
        DayLoopUI.Place(today.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), Vector2.zero);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(today, 0, 14);
        row.padding = new RectOffset(12, 28, 10, 12);
        Fit(today);

        Image sun = Disc(today.transform, Butter, 52, "Sun");
        DayLoopUI.Chunky(sun, 3f, 0f);

        RectTransform info = DayLoopUI.Rect("Info", today.transform);
        DayLoopUI.Vertical(info, 0, 6).childForceExpandWidth = false;

        RectTransform heading = DayLoopUI.Rect("DayAndPhase", info);
        DayLoopUI.Horizontal(heading, 0, 10);
        _dayText = Jua(DayLoopUI.Text(heading, "", 30, DayLoopUI.Kit.Ink));
        _dayText.textWrappingMode = TextWrappingModes.NoWrap;
        _phaseChip = Pill(heading, Leaf, "Phase", 0f, 0f);
        DayLoopUI.Horizontal(_phaseChip, 0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(14, 14, 3, 3);
        _phaseText = Jua(DayLoopUI.Text(_phaseChip.transform, "", 18, Color.white, TextAlignmentOptions.Center));
        _phaseText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform progress = DayLoopUI.Rect("Visits", info);
        DayLoopUI.Horizontal(progress, 0, 10);
        _progressFill = DayLoopUI.Bar(progress, 0, Mint);
        DayLoopUI.Layout(_progressFill.transform.parent, preferredWidth: 150, preferredHeight: 14);
        _progressText = Jua(DayLoopUI.Text(progress, "", 19, DayLoopUI.Kit.InkSoft));
        _progressText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // 오른쪽 위: 골드 · 평점 · 배속 · 일시정지
    private void BuildTopRight()
    {
        RectTransform group = DayLoopUI.Rect("TopRight", _bar);
        _topRight = group;
        DayLoopUI.Place(group, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), Vector2.zero);
        DayLoopUI.Horizontal(group, 0, 12, TextAnchor.MiddleRight);
        Fit(group);

        Image gold = Pill(group, DayLoopUI.Kit.Sail, "Gold");
        DayLoopUI.Horizontal(gold, 0, 8).padding = new RectOffset(10, 24, 0, 0);
        DayLoopUI.Layout(gold, preferredHeight: 64);
        Image coin = Disc(gold.transform, DayLoopUI.Kit.Coin, 40, "Coin");
        DayLoopUI.Chunky(coin, 3f, 0f);
        _goldText = Jua(DayLoopUI.Text(gold.transform, "", 30, DayLoopUI.Kit.Ink));
        _goldText.textWrappingMode = TextWrappingModes.NoWrap;

        Image rating = Pill(group, DayLoopUI.Kit.Sail, "Rating");
        _ratingBlock = rating.rectTransform;
        DayLoopUI.Horizontal(rating, 0, 6).padding = new RectOffset(14, 24, 0, 0);
        DayLoopUI.Layout(rating, preferredHeight: 64);
        string star = DayLoopUI.Glyph("★", "평점");
        TMP_Text starText = DayLoopUI.Text(rating.transform, star, star == "★" ? 36 : 20, DayLoopUI.Kit.Coin, TextAlignmentOptions.Center);
        starText.font = DayLoopUI.Font;
        starText.fontSharedMaterial = starText.font.material;
        starText.outlineWidth = 0.2f;
        starText.outlineColor = DayLoopUI.Kit.Line;
        starText.textWrappingMode = TextWrappingModes.NoWrap;
        _ratingText = Jua(DayLoopUI.Text(rating.transform, "", 30, DayLoopUI.Kit.Ink));
        _ratingText.textWrappingMode = TextWrappingModes.NoWrap;

        _speedButton = DayLoopUI.KitButton(group, "x1", Sky, Color.white, 26, RestaurantSpeedController.CycleSpeed, "Speed");
        Round(_speedButton.image);
        Jua(_speedButton.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(_speedButton, preferredWidth: 64, preferredHeight: 64);

        _pauseButton = DayLoopUI.KitButton(group, "II", DayLoopUI.Kit.Sail, DayLoopUI.Kit.Line, 26, () => _day.SetPaused(!_day.IsPaused), "Pause");
        Round(_pauseButton.image);
        Jua(_pauseButton.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(_pauseButton, preferredWidth: 64, preferredHeight: 64);
    }

    // 왼쪽 아래: 오늘의 메뉴
    private void BuildMenuTray()
    {
        Image tray = DayLoopUI.Card(_bar, DayLoopUI.Kit.Sail, "TodayMenu", 3f, 5f);
        tray.GetComponent<RoundedCorner>().radius = 32f;
        DayLoopUI.Place(tray.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24, 30), Vector2.zero);
        DayLoopUI.Vertical(tray, 0, 8).padding = new RectOffset(20, 22, 10, 18);
        Fit(tray);
        DayLoopUI.Layout(Jua(DayLoopUI.Text(tray.transform, "오늘의 메뉴", 18, DayLoopUI.Kit.InkSoft)), preferredHeight: 24);
        _menuRow = DayLoopUI.Rect("Dishes", tray.transform);
        DayLoopUI.Horizontal(_menuRow, 0, 18);
        tray.gameObject.SetActive(false);
    }

    // 오른쪽 아래: 직원
    private void BuildStaffTray()
    {
        Image tray = DayLoopUI.Card(_bar, DayLoopUI.Kit.Sail, "Staff", 3f, 5f);
        tray.GetComponent<RoundedCorner>().radius = 32f;
        DayLoopUI.Place(tray.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 30), Vector2.zero);
        DayLoopUI.Vertical(tray, 0, 8).padding = new RectOffset(20, 20, 10, 14);
        Fit(tray);
        DayLoopUI.Layout(Jua(DayLoopUI.Text(tray.transform, "직원", 18, DayLoopUI.Kit.InkSoft)), preferredHeight: 24);
        _staffRow = DayLoopUI.Rect("Faces", tray.transform);
        DayLoopUI.Horizontal(_staffRow, 0, 14, TextAnchor.UpperLeft);
        tray.gameObject.SetActive(false);
    }

    // 일시정지: 화면을 살짝 어둡게 하고 가운데에 [다시 시작]
    private void BuildPauseOverlay()
    {
        Image dim = DayLoopUI.Panel(_bar, new Color(0.16f, 0.24f, 0.31f, 0.45f), "PauseOverlay", false);
        DayLoopUI.Stretch(dim.rectTransform);
        _pauseOverlay = dim.gameObject.AddComponent<CanvasGroup>();

        Image card = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "PauseCard", 4f, 8f);
        card.GetComponent<RoundedCorner>().radius = 40f;
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(480, 0));
        DayLoopUI.Vertical(card, 0, 14, TextAnchor.UpperCenter).padding = new RectOffset(36, 36, 28, 32);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image chef = DayLoopUI.Panel(card.transform, Color.white, "Chef", false);
        chef.sprite = DesignArt.Load("OtterChef");
        chef.preserveAspect = true;
        chef.raycastTarget = false;
        DayLoopUI.Layout(chef, preferredHeight: 120);
        DayLoopUI.Layout(Jua(DayLoopUI.Text(card.transform, "잠깐 쉬어요", 42, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center)), preferredHeight: 54);
        Button resume = DayLoopUI.KitButton(card.transform, "다시 시작", Mint, Color.white, 30, () => _day.SetPaused(false), "Resume");
        Round(resume.image);
        Jua(resume.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(resume, preferredHeight: 72);
        SetPauseOverlay(false);
    }

    private void BuildToast()
    {
        Image toast = Pill(_canvas.transform, DayLoopUI.Kit.Sail, "Toast", 3f, 4f);
        DayLoopUI.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -116), Vector2.zero);
        toast.raycastTarget = false;
        DayLoopUI.Horizontal(toast, 0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(26, 26, 10, 12);
        Fit(toast);
        _toastText = Jua(DayLoopUI.Text(toast.transform, "", 24, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center));
        _toastText.textWrappingMode = TextWrappingModes.NoWrap;
        _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.blocksRaycasts = false;
        _toastGroup.alpha = 0f;
    }

    // ───── 조립 도구 ─────

    /// <summary>끝이 반원인 알약 카드. drop이 0이면 외곽선만.</summary>
    private static Image Pill(Transform parent, Color color, string name, float line = 3f, float drop = 5f)
    {
        Image pill = line > 0f ? DayLoopUI.Card(parent, color, name, line, drop) : DayLoopUI.Panel(parent, color, name);
        Round(pill);
        return pill;
    }

    private static void Round(Image image)
    {
        RoundedCorner corner = image.GetComponent<RoundedCorner>();
        if (corner != null) corner.radius = 999f;
    }

    /// <summary>원. 레이아웃 안에서는 size 크기로 잡힌다.</summary>
    private static Image Disc(Transform parent, Color color, float size, string name)
    {
        Image disc = DayLoopUI.Panel(parent, color, name, false);
        disc.sprite = DayLoopUI.Circle;
        disc.type = Image.Type.Simple;
        disc.raycastTarget = false;
        DayLoopUI.Layout(disc, preferredWidth: size, preferredHeight: size);
        disc.rectTransform.sizeDelta = new Vector2(size, size);
        return disc;
    }

    private static void Fit(Component target)
    {
        ContentSizeFitter fit = target.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>캐주얼 HUD는 제목·숫자 모두 주아체를 쓴다.</summary>
    private static T Jua<T>(T text) where T : TMP_Text
    {
        text.font = DesignFonts.Heading;
        text.fontSharedMaterial = text.font.material;
        return text;
    }
}
