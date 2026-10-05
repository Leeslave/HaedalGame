using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 결산 카드. (RESULT-01/02/03)
/// 어두운 배경 위 불투명 카드. 순이익을 가장 크게, 그다음 대접 손님·평점 변화.
/// 판매 내역은 이름/판매 수/단가/매출로 나누고 길면 스크롤한다. 합계와 다음 행동 버튼은 고정.
/// 표시값은 확정된 DaySettlementRecord 스냅샷만 사용한다. (다시 계산하지 않음)
/// </summary>
public class SettlementPanel : MonoBehaviour
{
    public enum SaveState { Saving, Saved, Failed }

    private Canvas _canvas;
    private CanvasGroup _group;
    private TMP_Text _title, _subtitle, _headline, _profit;
    private TMP_Text _servedValue, _servedSub, _ratingValue, _ratingSub, _goldValue, _goldSub;
    private RectTransform _salesContent;
    private TMP_Text _totals, _ratingNote, _metrics, _suggestion, _status;
    private Button _nextButton, _retryButton;

    private Action _onNext, _onRetry;
    private bool _nextClicked;

    public bool IsVisible => _group != null && _group.alpha > 0.5f;

    /// <param name="onNext">다음 행동 버튼. 한 번만 호출된다.</param>
    /// <param name="onRetry">저장 실패 시 다시 시도 버튼.</param>
    public void Show(DaySettlementRecord record, string nextLabel, Action onNext, Action onRetry)
    {
        Build();
        _onNext = onNext;
        _onRetry = onRetry;
        _nextClicked = false;

        Fill(record);
        DayLoopUI.SetButtonLabel(_nextButton, nextLabel);
        _group.alpha = 1f;
        _group.blocksRaycasts = true;
        _group.interactable = true;
    }

    public void Hide()
    {
        if (_group == null) return;
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    public void SetSaveState(SaveState state, string error = null)
    {
        Build();
        switch (state)
        {
            case SaveState.Saving:
                _status.text = "결산을 저장하는 중…";
                _status.color = DayLoopUI.InkSoft;
                break;
            case SaveState.Saved:
                _status.text = GameSession.IsDevSession ? "결산 확정 (에디터 직접 실행 — 파일 저장 생략)" : "결산이 확정되어 저장됐어요.";
                _status.color = DayLoopUI.Good;
                break;
            default:
                _status.text = "저장하지 못했어요. 날짜는 아직 넘어가지 않았어요." + (string.IsNullOrEmpty(error) ? "" : "\n<size=80%>" + error + "</size>");
                _status.color = DayLoopUI.Warn;
                break;
        }

        _nextButton.gameObject.SetActive(state != SaveState.Failed);
        _nextButton.interactable = state == SaveState.Saved;
        _retryButton.gameObject.SetActive(state == SaveState.Failed);
    }

    private void HandleNext()
    {
        if (_nextClicked) return;   // 연타 방지: 다음 날 이동은 한 번만
        _nextClicked = true;
        _nextButton.interactable = false;
        _onNext?.Invoke();
    }

    private void HandleRetry()
    {
        _onRetry?.Invoke();
    }

    // ───── 내용 ─────

    private void Fill(DaySettlementRecord r)
    {
        _title.text = $"{r.day}일차 결산";
        _subtitle.text = "영업 종료";
        _headline.text = DayLoopSave.Headline(r);

        _profit.text = DayLoopUI.SignedGold(r.netProfit);
        _profit.color = r.netProfit >= 0 ? DayLoopUI.Good : DayLoopUI.Warn;

        _servedValue.text = $"{r.servedCount}명";
        _servedSub.text = $"방문 {r.visitedCount}명 • 미대접 {r.notServedCount}명";

        _ratingValue.text = $"{DayLoopUI.Rating(r.previousRating)} → {DayLoopUI.Rating(r.totalRating)}";
        _ratingSub.text = r.ratingCount > 0 ? $"오늘 {DayLoopUI.Rating(r.todayRating)} • 평가 {r.ratingCount}건" : "오늘 평가 없음";

        _goldValue.text = DayLoopUI.Gold(r.goldAtClose);
        _goldSub.text = $"영업 시작 {DayLoopUI.Gold(r.goldAtOpen)}";

        for (int i = _salesContent.childCount - 1; i >= 0; i--)
            Destroy(_salesContent.GetChild(i).gameObject);

        if (r.sales.Count == 0)
        {
            TMP_Text empty = DayLoopUI.Text(_salesContent, "판매한 메뉴가 없어요.", 24, DayLoopUI.InkSoft, TextAlignmentOptions.Center);
            DayLoopUI.Layout(empty, preferredHeight: 44);
        }

        int totalCount = 0;
        foreach (DaySaleEntry sale in r.sales)
        {
            totalCount += sale.count;
            AddSaleRow(sale.recipeName, $"{sale.count}개", DayLoopUI.Gold(sale.unitPrice), DayLoopUI.Gold(sale.revenue));
        }

        string totals = $"매출 <b>{DayLoopUI.Gold(r.revenue)}</b>  ({totalCount}개)";
        if (r.otherIncome > 0) totals += $"   기타 수입 <b>{DayLoopUI.Gold(r.otherIncome)}</b>";
        totals += $"   지출 <b>{DayLoopUI.Gold(r.expense)}</b>   순이익 <b>{DayLoopUI.SignedGold(r.netProfit)}</b>";
        int goldDelta = r.goldAtClose - r.goldAtOpen;
        if (goldDelta != r.netProfit)
            totals += $"\n<size=80%><color=#{ColorUtility.ToHtmlStringRGB(DayLoopUI.Warn)}>골드 변화 {DayLoopUI.SignedGold(goldDelta)}가 순이익과 달라요. (집계 밖 거래 확인 필요)</color></size>";
        _totals.text = totals;

        _ratingNote.text = DayLoopSave.RatingNote(r);

        string metrics = $"평균 자리 대기 {DayLoopUI.Seconds(r.seatWaitSum, r.seatWaitSamples)}  •  주문 접수 {DayLoopUI.Seconds(r.orderTakeSum, r.orderTakeSamples)}  •  음식 도착 {DayLoopUI.Seconds(r.foodWaitSum, r.foodWaitSamples)}";
        if (r.firstRevenueSeconds >= 0f) metrics += $"  •  첫 수익까지 {r.firstRevenueSeconds:0}초";
        if (r.forcedLeaveCount > 0) metrics += $"\n진행이 멈춰 돌려보낸 손님 {r.forcedLeaveCount}명 (미대접에 포함)";
        _metrics.text = metrics;
        _suggestion.text = DayLoopSave.Suggestion(r);

        Canvas.ForceUpdateCanvases();
    }

    private void AddSaleRow(string name, string count, string unit, string revenue)
    {
        RectTransform row = DayLoopUI.Rect("Row", _salesContent);
        DayLoopUI.Horizontal(row, 0, 8);
        DayLoopUI.Layout(row, preferredHeight: 42);

        DayLoopUI.Layout(DayLoopUI.Text(row, name, 25, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(row, count, 25, DayLoopUI.Ink, TextAlignmentOptions.MidlineRight), preferredWidth: 120);
        DayLoopUI.Layout(DayLoopUI.Text(row, unit, 25, DayLoopUI.Ink, TextAlignmentOptions.MidlineRight), preferredWidth: 140);
        DayLoopUI.Layout(DayLoopUI.Text(row, revenue, 25, DayLoopUI.GoldText, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 160);
    }

    // ───── 조립 ─────

    private void Build()
    {
        if (_canvas != null) return;

        _canvas = DayLoopUI.CreateCanvas("[DayLoop] Settlement", 300, gameObject.scene);
        _canvas.transform.SetParent(transform, false);
        _group = _canvas.gameObject.AddComponent<CanvasGroup>();

        Image dim = DayLoopUI.Panel(_canvas.transform, DayLoopUI.Dim, "Dim", false);
        DayLoopUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image card = DayLoopUI.Panel(_canvas.transform, DayLoopUI.Cream, "Card");
        DayLoopUI.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 1010));
        DayLoopUI.AddOutline(card, DayLoopUI.Wood, 3f);

        RectTransform body = DayLoopUI.Rect("Body", card.transform);
        DayLoopUI.Stretch(body, 44, 36, 44, 36);
        DayLoopUI.Vertical(body, 0, 14);

        // 제목
        RectTransform header = DayLoopUI.Rect("Header", body);
        DayLoopUI.Horizontal(header, 0, 14, TextAnchor.LowerLeft);
        DayLoopUI.Layout(header, preferredHeight: 56);
        _title = DayLoopUI.Text(header, "", 42, DayLoopUI.WoodDark, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        DayLoopUI.Layout(_title, preferredWidth: 300);
        _subtitle = DayLoopUI.Text(header, "", 24, DayLoopUI.InkSoft, TextAlignmentOptions.BottomLeft);
        DayLoopUI.Layout(_subtitle, flexibleWidth: 1);

        // 순이익 (가장 크게)
        Image hero = DayLoopUI.Panel(body, DayLoopUI.CreamDeep, "Hero");
        DayLoopUI.Layout(hero, preferredHeight: 150);
        RectTransform heroBody = DayLoopUI.Rect("HeroBody", hero.transform);
        DayLoopUI.Stretch(heroBody, 28, 12, 28, 12);
        DayLoopUI.Vertical(heroBody, 0, 0, TextAnchor.MiddleCenter);
        _headline = DayLoopUI.Text(heroBody, "", 26, DayLoopUI.InkSoft, TextAlignmentOptions.Center);
        DayLoopUI.Layout(_headline, preferredHeight: 36);
        TMP_Text profitLabel = DayLoopUI.Text(heroBody, "오늘 순이익", 22, DayLoopUI.InkSoft, TextAlignmentOptions.Center);
        DayLoopUI.Layout(profitLabel, preferredHeight: 28);
        _profit = DayLoopUI.Text(heroBody, "", 64, DayLoopUI.Good, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(_profit, preferredHeight: 72);

        // 대접 손님 · 평점 · 골드
        RectTransform stats = DayLoopUI.Rect("Stats", body);
        HorizontalLayoutGroup statsLayout = DayLoopUI.Horizontal(stats, 0, 14);
        statsLayout.childForceExpandWidth = true;
        DayLoopUI.Layout(stats, preferredHeight: 112);
        BuildStat(stats, "대접한 손님", out _servedValue, out _servedSub);
        BuildStat(stats, "식당 평점", out _ratingValue, out _ratingSub);
        BuildStat(stats, "보유 골드", out _goldValue, out _goldSub);

        // 판매 내역 (스크롤)
        TMP_Text salesTitle = DayLoopUI.Text(body, "판매 내역", 24, DayLoopUI.WoodDark, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(salesTitle, preferredHeight: 30);

        // 열 제목 (목록 스크롤과 무관하게 고정)
        RectTransform columns = DayLoopUI.Rect("Columns", body);
        DayLoopUI.Layout(columns, preferredHeight: 28);
        HorizontalLayoutGroup columnLayout = DayLoopUI.Horizontal(columns, 0, 8);
        columnLayout.padding = new RectOffset(14, 14, 0, 0);
        Color headColor = DayLoopUI.InkSoft;
        DayLoopUI.Layout(DayLoopUI.Text(columns, "메뉴", 20, headColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "판매", 20, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 120);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "단가", 20, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 140);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "매출", 20, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 160);

        Image listBg = DayLoopUI.Panel(body, Color.white, "SalesList");
        DayLoopUI.Layout(listBg, preferredHeight: 140, flexibleHeight: 1);
        ScrollRect scroll = listBg.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = DayLoopUI.Rect("Viewport", listBg.transform);
        DayLoopUI.Stretch(viewport, 14, 6, 14, 6);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportHit = viewport.gameObject.AddComponent<Image>();
        viewportHit.color = new Color(1, 1, 1, 0);

        _salesContent = DayLoopUI.Rect("Content", viewport);
        _salesContent.anchorMin = new Vector2(0, 1);
        _salesContent.anchorMax = new Vector2(1, 1);
        _salesContent.pivot = new Vector2(0.5f, 1);
        _salesContent.offsetMin = Vector2.zero;
        _salesContent.offsetMax = Vector2.zero;
        DayLoopUI.Vertical(_salesContent, 0, 2);
        ContentSizeFitter fitter = _salesContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = _salesContent;

        // 합계 (고정)
        _totals = DayLoopUI.Text(body, "", 24, DayLoopUI.Ink, TextAlignmentOptions.MidlineRight);
        DayLoopUI.Layout(_totals, preferredHeight: 40);

        _ratingNote = DayLoopUI.Text(body, "", 20, DayLoopUI.InkSoft, TextAlignmentOptions.TopLeft);
        DayLoopUI.Layout(_ratingNote, preferredHeight: 52);

        // 운영 측정값과 다음 준비 한 가지
        Image insight = DayLoopUI.Panel(body, new Color(DayLoopUI.Sea.r, DayLoopUI.Sea.g, DayLoopUI.Sea.b, 0.12f), "Insight");
        DayLoopUI.Layout(insight, preferredHeight: 104);
        RectTransform insightBody = DayLoopUI.Rect("InsightBody", insight.transform);
        DayLoopUI.Stretch(insightBody, 20, 10, 20, 10);
        DayLoopUI.Vertical(insightBody, 0, 4);
        _metrics = DayLoopUI.Text(insightBody, "", 19, DayLoopUI.InkSoft, TextAlignmentOptions.TopLeft);
        DayLoopUI.Layout(_metrics, preferredHeight: 40);
        _suggestion = DayLoopUI.Text(insightBody, "", 23, DayLoopUI.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        DayLoopUI.Layout(_suggestion, preferredHeight: 40);

        // 하단 고정: 저장 상태 + 다음 행동
        RectTransform footer = DayLoopUI.Rect("Footer", body);
        DayLoopUI.Horizontal(footer, 0, 16);
        DayLoopUI.Layout(footer, preferredHeight: 72);
        _status = DayLoopUI.Text(footer, "", 20, DayLoopUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        DayLoopUI.Layout(_status, flexibleWidth: 1);
        _retryButton = DayLoopUI.Button(footer, "저장 다시 시도", DayLoopUI.Warn, Color.white, 26, HandleRetry, "Retry");
        DayLoopUI.Layout(_retryButton, preferredWidth: 240, preferredHeight: 68);
        _nextButton = DayLoopUI.Button(footer, "다음 날 준비하기", DayLoopUI.Coral, Color.white, 28, HandleNext, "Next");
        DayLoopUI.Layout(_nextButton, preferredWidth: 300, preferredHeight: 68);
        _retryButton.gameObject.SetActive(false);

        Hide();
    }

    private static void BuildStat(Transform parent, string label, out TMP_Text value, out TMP_Text sub)
    {
        Image tile = DayLoopUI.Panel(parent, DayLoopUI.CreamDeep, "Stat");
        DayLoopUI.Layout(tile, flexibleWidth: 1, preferredHeight: 112);
        RectTransform tileBody = DayLoopUI.Rect("Body", tile.transform);
        DayLoopUI.Stretch(tileBody, 18, 10, 18, 10);
        DayLoopUI.Vertical(tileBody, 0, 2);
        DayLoopUI.Layout(DayLoopUI.Text(tileBody, label, 19, DayLoopUI.InkSoft), preferredHeight: 24);
        value = DayLoopUI.Text(tileBody, "", 32, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(value, preferredHeight: 40);
        sub = DayLoopUI.Text(tileBody, "", 18, DayLoopUI.InkSoft);
        DayLoopUI.Layout(sub, preferredHeight: 24);
    }
}
