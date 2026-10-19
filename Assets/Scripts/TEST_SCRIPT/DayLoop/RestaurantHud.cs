using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 영업 HUD. (HUD-01, HUD-02, CLOSE-01)
/// 불투명 크림 패널 위에 날짜 / 단계 / 진행도 / 대기열(또는 마감 잔여) / 평점 / 골드 / 배속·일시정지를 표시한다.
///  - 방문: 생성(입장) 기준. 분모는 오늘 입장 예정 손님 수
///  - 자리 대기: 실제 대기 줄(SeatManager)의 인원
///  - 남은 손님: 아직 퇴장하지 않은 손님 (마감 중 표시)
///  - 평점: 오늘 받은 평가의 평균(평가 전에는 식당 평점). 평가가 들어올 때마다 강조 이펙트와 점수 팝업
///  - 골드: 누적 보유액. 오늘 매출은 따로 표시
///  - 배속: x1 → x2 → x3
/// </summary>
public class RestaurantHud : MonoBehaviour
{
    private DayCycleController _day;
    private Canvas _canvas;
    private RectTransform _bar;
    private TMP_Text _dayText, _phaseText, _progressText, _queueText, _goldText, _todayText, _ratingText, _ratingSub;
    private RectTransform _ratingBlock;
    private RestaurantRatingManager _rating;
    private float _lastRatingAverage;
    private int _lastRatingCount;
    private Coroutine _ratingPunch;
    private Image _phaseChip, _progressFill;
    private Button _speedButton, _pauseButton;
    private CanvasGroup _pauseOverlay;
    private RectTransform _toast;
    private CanvasGroup _toastGroup;
    private TMP_Text _toastText;
    private Coroutine _toastRoutine;
    private float _nextRefresh;

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

    private void OnDestroy()
    {
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
                _phaseChip.color = DayLoopUI.Good;
                break;
            case DayLoopPhase.Closing:
                _phaseText.text = "입장 마감";
                _phaseChip.color = DayLoopUI.Coral;
                break;
            case DayLoopPhase.Settlement:
                _phaseText.text = "영업 종료";
                _phaseChip.color = DayLoopUI.WoodDark;
                break;
            default:
                _phaseText.text = "준비 중";
                _phaseChip.color = DayLoopUI.Wood;
                break;
        }

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
        _progressText.text = planned > 0 ? $"방문 {visited} / {planned}명" : $"방문 {visited}명";
        _progressFill.rectTransform.anchorMax = new Vector2(planned > 0 ? Mathf.Clamp01((float)visited / planned) : 0f, 1f);

        if (_day.Phase == DayLoopPhase.Closing)
        {
            int remaining = spawner != null ? spawner.ActiveCustomerCount : 0;
            _queueText.text = $"마감 중 • 남은 손님 {remaining}명";
        }
        else
        {
            SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
            int waiting = seats != null ? seats.WaitingCount : 0;
            _queueText.text = $"자리 대기 {waiting}명";
        }

        int gold = _day.CurrentGold;
        _goldText.text = gold >= 0 ? "보유 " + DayLoopUI.Gold(gold) : "보유 -";
        _todayText.text = "오늘 매출 " + DayLoopUI.SignedGold(_day.Ledger.Revenue);

        RefreshRating();
        RefreshControls();
    }

    // ───── 평점 ─────

    private void RefreshRating()
    {
        string star = StarGlyph();
        if (_rating == null)
        {
            _ratingText.text = star + " -";
            _ratingSub.text = "평점";
            return;
        }

        int count = _rating.TodayScoreCount;
        if (count == 0)
        {
            float restaurant = _rating.RestaurantRating;
            _ratingText.text = star + " " + (restaurant > 0f ? DayLoopUI.Rating(restaurant) : "-");
            _ratingText.color = DayLoopUI.GoldText;
            _ratingSub.text = "오늘 평가 전";
            return;
        }

        _ratingText.text = star + " " + DayLoopUI.Rating(_rating.TodayAverage);
        switch (_rating.CurrentTier)
        {
            case RatingBuffTier.Buff:
                _ratingText.color = DayLoopUI.Good;
                _ratingSub.text = "호평: 팁·직원 속도 증가";
                break;
            case RatingBuffTier.Debuff:
                _ratingText.color = DayLoopUI.Warn;
                _ratingSub.text = "혹평: 손님이 빨리 지쳐요";
                break;
            default:
                _ratingText.color = DayLoopUI.GoldText;
                _ratingSub.text = $"오늘 평가 {count}건";
                break;
        }
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
            _ratingBlock.localScale = Vector3.one * (1f + 0.3f * k);
            yield return null;
        }
        _ratingBlock.localScale = Vector3.one;
        _ratingPunch = null;
    }

    private IEnumerator RatingPopup(float score)
    {
        Color color = score >= 4f ? new Color32(0x7E, 0xE0, 0x9A, 0xFF) : score <= 2f ? new Color32(0xFF, 0x8A, 0x7A, 0xFF) : new Color32(0xF6, 0xC3, 0x3B, 0xFF);
        Image popup = DayLoopUI.Panel(_ratingBlock, new Color(0.18f, 0.12f, 0.08f, 0.9f), "RatingPopup");
        popup.raycastTarget = false;
        popup.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        RectTransform rt = popup.rectTransform;
        DayLoopUI.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(150, 48));
        TMP_Text text = DayLoopUI.Text(popup.transform, StarGlyph() + " " + DayLoopUI.Rating(Mathf.Clamp(score, 0f, 5f)), 30, color, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(text.rectTransform);

        const float duration = 1.4f;
        float t = 0f;
        while (t < duration && popup != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            rt.anchoredPosition = new Vector2(0f, -14f - 40f * Mathf.Sqrt(k));
            group.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }
        if (popup != null) Destroy(popup.gameObject);
    }

    private static string StarGlyph()
    {
        TMP_FontAsset font = DayLoopUI.Font;
        return font != null && font.HasCharacters("★") ? "★" : "평점";
    }

    private void RefreshControls()
    {
        bool fast = RestaurantSpeedController.IsFastForward;
        DayLoopUI.SetButtonLabel(_speedButton, "x" + RestaurantSpeedController.CurrentSpeed + (fast ? " 빠르게" : " 보통"));
        _speedButton.image.color = fast ? DayLoopUI.GoldText : DayLoopUI.Wood;

        bool paused = _day != null && _day.IsPaused;
        DayLoopUI.SetButtonLabel(_pauseButton, paused ? "▶ 계속" : "|| 정지");
        _pauseButton.image.color = paused ? DayLoopUI.Coral : DayLoopUI.Wood;
        SetPauseOverlay(paused);
    }

    private void SetPauseOverlay(bool visible)
    {
        if (_pauseOverlay == null) return;
        _pauseOverlay.alpha = visible ? 1f : 0f;
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

        // 상단 바: 화면 위쪽 0~0.11 높이 안에 들어간다. (카메라는 이 영역을 비워 둔다)
        Image bar = DayLoopUI.Panel(_canvas.transform, DayLoopUI.Cream, "TopBar");
        _bar = bar.rectTransform;
        DayLoopUI.Place(_bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(1640, 96));
        DayLoopUI.AddOutline(bar, DayLoopUI.Wood, 2.5f);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(bar, 0, 18, TextAnchor.MiddleLeft);
        row.padding = new RectOffset(28, 18, 10, 10);

        _dayText = DayLoopUI.Text(bar.transform, "1일차", 38, DayLoopUI.WoodDark, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_dayText, preferredWidth: 140, preferredHeight: 70);

        _phaseChip = DayLoopUI.Panel(bar.transform, DayLoopUI.Good, "PhaseChip");
        DayLoopUI.Layout(_phaseChip, preferredWidth: 150, preferredHeight: 52);
        _phaseText = DayLoopUI.Text(_phaseChip.transform, "영업 중", 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(_phaseText.rectTransform);

        RectTransform progress = DayLoopUI.Rect("Progress", bar.transform);
        DayLoopUI.Vertical(progress, 0, 6, TextAnchor.MiddleLeft);
        DayLoopUI.Layout(progress, preferredWidth: 240, preferredHeight: 70);
        _progressText = DayLoopUI.Text(progress, "", 24, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_progressText, preferredHeight: 32);
        Image track = DayLoopUI.Panel(progress, DayLoopUI.CreamDeep, "Track");
        DayLoopUI.Layout(track, preferredHeight: 12);
        _progressFill = DayLoopUI.Panel(track.transform, DayLoopUI.Sea, "Fill");
        _progressFill.rectTransform.anchorMin = Vector2.zero;
        _progressFill.rectTransform.anchorMax = new Vector2(0, 1);
        _progressFill.rectTransform.offsetMin = Vector2.zero;
        _progressFill.rectTransform.offsetMax = Vector2.zero;

        _queueText = DayLoopUI.Text(bar.transform, "", 24, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft);
        DayLoopUI.Layout(_queueText, preferredWidth: 230, preferredHeight: 70);

        Image ratingChip = DayLoopUI.Panel(bar.transform, DayLoopUI.CreamDeep, "Rating");
        _ratingBlock = ratingChip.rectTransform;
        DayLoopUI.Layout(ratingChip, preferredWidth: 230, preferredHeight: 76);
        DayLoopUI.Vertical(ratingChip, 4, 0, TextAnchor.MiddleCenter);
        _ratingText = DayLoopUI.Text(ratingChip.transform, "", 32, DayLoopUI.GoldText, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(_ratingText, preferredHeight: 40);
        _ratingSub = DayLoopUI.Text(ratingChip.transform, "", 17, DayLoopUI.InkSoft, TextAlignmentOptions.Center);
        DayLoopUI.Layout(_ratingSub, preferredHeight: 24);

        RectTransform gold = DayLoopUI.Rect("Gold", bar.transform);
        DayLoopUI.Vertical(gold, 0, 0, TextAnchor.MiddleLeft);
        DayLoopUI.Layout(gold, preferredWidth: 200, preferredHeight: 70, flexibleWidth: 1);
        _goldText = DayLoopUI.Text(gold, "", 26, DayLoopUI.GoldText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_goldText, preferredHeight: 34);
        _todayText = DayLoopUI.Text(gold, "", 19, DayLoopUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        DayLoopUI.Layout(_todayText, preferredHeight: 26);

        _speedButton = DayLoopUI.Button(bar.transform, "x1 보통", DayLoopUI.Wood, Color.white, 22, RestaurantSpeedController.CycleSpeed, "Speed");
        DayLoopUI.Layout(_speedButton, preferredWidth: 130, preferredHeight: 60);
        _pauseButton = DayLoopUI.Button(bar.transform, "|| 정지", DayLoopUI.Wood, Color.white, 22, () => _day.SetPaused(!_day.IsPaused), "Pause");
        DayLoopUI.Layout(_pauseButton, preferredWidth: 120, preferredHeight: 60);

        // 일시정지 안내 (클릭은 막지 않는다)
        Image pausePanel = DayLoopUI.Panel(_canvas.transform, new Color(0.08f, 0.12f, 0.14f, 0.78f), "PauseOverlay");
        DayLoopUI.Place(pausePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 120));
        pausePanel.raycastTarget = false;
        TMP_Text pauseText = DayLoopUI.Text(pausePanel.transform, "일시정지 중\n<size=70%>위쪽 [▶ 계속]을 누르면 이어서 영업해요</size>", 38, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(pauseText.rectTransform);
        _pauseOverlay = pausePanel.gameObject.AddComponent<CanvasGroup>();
        _pauseOverlay.blocksRaycasts = false;
        _pauseOverlay.alpha = 0f;

        // 알림
        Image toast = DayLoopUI.Panel(_canvas.transform, new Color(0.18f, 0.12f, 0.08f, 0.92f), "Toast");
        _toast = toast.rectTransform;
        DayLoopUI.Place(_toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(760, 70));
        toast.raycastTarget = false;
        _toastText = DayLoopUI.Text(toast.transform, "", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(_toastText.rectTransform, 20, 0, 20, 0);
        _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.blocksRaycasts = false;
        _toastGroup.alpha = 0f;
    }
}
