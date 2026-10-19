using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 결산 카드. (RESULT-01/02/03)
/// 어두운 배경 위 돛천 카드와 제목 리본. 순이익을 가장 크게, 그다음 대접 손님·평점 변화 카드 세 장.
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
                _status.text = "결산을 확정하는 중…";
                _status.color = DayLoopUI.Kit.InkSoft;
                break;
            case SaveState.Saved:
                _status.text = "오늘 결산을 확정했어요. 집에서 잠들면 저장돼요.";
                _status.color = DayLoopUI.Kit.GoodInk;
                break;
            default:
                _status.text = "저장하지 못했어요. 날짜는 아직 넘어가지 않았어요." + (string.IsNullOrEmpty(error) ? "" : "\n<size=80%>" + error + "</size>");
                _status.color = DayLoopUI.Kit.WarnInk;
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
        _profit.color = r.netProfit >= 0 ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.WarnInk;

        _servedValue.text = $"{r.servedCount}명";
        _servedSub.text = $"방문 {r.visitedCount}명 • 미대접 {r.notServedCount}명";

        _ratingValue.text = $"{DayLoopUI.Rating(r.previousRating)} → {DayLoopUI.Rating(r.totalRating)}";
        _ratingSub.text = r.ratingCount > 0 ? $"오늘 {DayLoopUI.Rating(r.todayRating)} • 평가 {r.ratingCount}건" : "오늘 평가 없음";

        _goldValue.text = DayLoopUI.Gold(r.goldAtClose);
        _goldValue.color = DayLoopUI.Kit.GoldInk;
        _goldSub.text = $"영업 시작 {DayLoopUI.Gold(r.goldAtOpen)}";

        for (int i = _salesContent.childCount - 1; i >= 0; i--)
            Destroy(_salesContent.GetChild(i).gameObject);

        if (r.sales.Count == 0)
        {
            TMP_Text empty = DayLoopUI.Text(_salesContent, "판매한 메뉴가 없어요.", 24, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center);
            DayLoopUI.Layout(empty, preferredHeight: 44);
        }

        int totalCount = 0;
        foreach (DaySaleEntry sale in r.sales)
        {
            totalCount += sale.count;
            AddSaleRow(sale.recipeId, sale.recipeName, $"{sale.count}개", DayLoopUI.Gold(sale.unitPrice), DayLoopUI.Gold(sale.revenue));
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

    private void AddSaleRow(int recipeId, string name, string count, string unit, string revenue)
    {
        RectTransform row = DayLoopUI.Rect("Row", _salesContent);
        DayLoopUI.Horizontal(row, 0, 10);
        DayLoopUI.Layout(row, preferredHeight: 52);

        GameDatabase.TryGetRecipe(recipeId, out RecipeData recipe);
        DayLoopUI.FoodIcon(row, recipe, 44);
        DayLoopUI.Layout(DayLoopUI.Text(row, name, 25, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(row, count, 25, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight), preferredWidth: 120);
        DayLoopUI.Layout(DayLoopUI.Text(row, unit, 25, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight), preferredWidth: 140);
        DayLoopUI.Layout(DayLoopUI.Text(row, revenue, 25, DayLoopUI.Kit.GoldInk, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 160);
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

        Image card = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "Card", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(1240, 980));

        // 제목 리본 (N일차 결산)
        _title = DayLoopUI.Ribbon(card.transform, "", DayLoopUI.Kit.Awning, 440f, 44f);

        RectTransform body = DayLoopUI.Rect("Body", card.transform);
        DayLoopUI.Stretch(body, 48, 64, 48, 30);
        DayLoopUI.Vertical(body, 0, 14);

        // 순이익 (가장 크게)
        RectTransform hero = DayLoopUI.Rect("Hero", body);
        DayLoopUI.Layout(hero, preferredHeight: 128);
        DayLoopUI.Vertical(hero, 0, 0, TextAnchor.MiddleCenter);
        _subtitle = DayLoopUI.Text(hero, "", 22, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(_subtitle, preferredHeight: 26);
        _headline = DayLoopUI.Text(hero, "", 24, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(_headline, preferredHeight: 32);
        RectTransform profitRow = DayLoopUI.Rect("ProfitRow", hero);
        DayLoopUI.Horizontal(profitRow, 0, 22, TextAnchor.MiddleCenter);
        DayLoopUI.Layout(profitRow, preferredHeight: 70);
        TMP_Text profitLabel = DayLoopUI.Text(profitRow, "오늘 순이익", 28, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        DayLoopUI.Layout(profitLabel, preferredWidth: 200, preferredHeight: 70);
        _profit = DayLoopUI.Text(profitRow, "", 76, DayLoopUI.Kit.GoodInk, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        _profit.textWrappingMode = TextWrappingModes.NoWrap;
        _profit.overflowMode = TextOverflowModes.Overflow;
        DayLoopUI.Layout(_profit, preferredWidth: 420, preferredHeight: 70);

        // 대접 손님 · 평점 · 골드
        RectTransform stats = DayLoopUI.Rect("Stats", body);
        HorizontalLayoutGroup statsLayout = DayLoopUI.Horizontal(stats, 0, 18);
        statsLayout.childForceExpandWidth = true;
        statsLayout.padding = new RectOffset(4, 4, 4, 8);
        DayLoopUI.Layout(stats, preferredHeight: 128);
        BuildStat(stats, "대접한 손님", out _servedValue, out _servedSub);
        BuildStat(stats, "식당 평점", out _ratingValue, out _ratingSub);
        BuildStat(stats, "보유 골드", out _goldValue, out _goldSub);

        // 판매 내역: 열 제목 고정 + 목록 스크롤
        Image sales = DayLoopUI.Chunky(DayLoopUI.Panel(body, Color.white, "Sales"), 3f, 0f);
        DayLoopUI.Layout(sales, preferredHeight: 250, flexibleHeight: 1);
        VerticalLayoutGroup salesLayout = DayLoopUI.Vertical(sales, 0, 4);
        salesLayout.padding = new RectOffset(22, 22, 12, 12);

        RectTransform columns = DayLoopUI.Rect("Columns", sales.transform);
        DayLoopUI.Layout(columns, preferredHeight: 32);
        DayLoopUI.Horizontal(columns, 0, 10);
        Color headColor = DayLoopUI.Kit.InkSoft;
        DayLoopUI.Layout(DayLoopUI.Text(columns, "판매 내역", 22, headColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "판매", 22, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 120);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "단가", 22, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 140);
        DayLoopUI.Layout(DayLoopUI.Text(columns, "매출", 22, headColor, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 160);
        Image rule = DayLoopUI.Panel(sales.transform, DayLoopUI.Kit.Divider, "Rule", false);
        DayLoopUI.Layout(rule, preferredHeight: 2);

        RectTransform list = DayLoopUI.Rect("SalesList", sales.transform);
        DayLoopUI.Layout(list, preferredHeight: 120, flexibleHeight: 1);
        ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = DayLoopUI.Rect("Viewport", list);
        DayLoopUI.Stretch(viewport);
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
        _totals = DayLoopUI.Text(sales.transform, "", 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight);
        DayLoopUI.Layout(_totals, preferredHeight: 40);

        _ratingNote = DayLoopUI.Text(body, "", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.TopLeft);
        DayLoopUI.Layout(_ratingNote, preferredHeight: 48);

        // 운영 측정값과 다음 준비 한 가지
        Image insight = DayLoopUI.Chunky(DayLoopUI.Panel(body, DayLoopUI.Kit.Sand, "Insight"), 2f, 0f);
        DayLoopUI.Layout(insight, preferredHeight: 100);
        RectTransform insightBody = DayLoopUI.Rect("InsightBody", insight.transform);
        DayLoopUI.Stretch(insightBody, 22, 10, 22, 10);
        DayLoopUI.Vertical(insightBody, 0, 4);
        _metrics = DayLoopUI.Text(insightBody, "", 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        DayLoopUI.Layout(_metrics, preferredHeight: 40);
        _suggestion = DayLoopUI.Text(insightBody, "", 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        DayLoopUI.Layout(_suggestion, preferredHeight: 40);

        // 하단 고정: 저장 상태 + 다음 행동
        RectTransform footer = DayLoopUI.Rect("Footer", body);
        DayLoopUI.Horizontal(footer, 0, 18);
        DayLoopUI.Layout(footer, preferredHeight: 84);
        _status = DayLoopUI.Text(footer, "", 22, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_status, flexibleWidth: 1);
        _retryButton = DayLoopUI.KitButton(footer, "저장 다시 시도", DayLoopUI.Kit.Awning, Color.white, 28, HandleRetry, "Retry");
        DayLoopUI.Layout(_retryButton, preferredWidth: 260, preferredHeight: 74);
        _nextButton = DayLoopUI.KitButton(footer, "다음 날 준비하기", DayLoopUI.Kit.Awning, Color.white, 30, HandleNext, "Next");
        DayLoopUI.Layout(_nextButton, preferredWidth: 330, preferredHeight: 74);
        _retryButton.gameObject.SetActive(false);

        Hide();
    }

    private static void BuildStat(Transform parent, string label, out TMP_Text value, out TMP_Text sub)
    {
        Image tile = DayLoopUI.Chunky(DayLoopUI.Panel(parent, Color.white, "Stat"), 3f, 0f);
        DayLoopUI.Layout(tile, flexibleWidth: 1, preferredHeight: 116);
        RectTransform tileBody = DayLoopUI.Rect("Body", tile.transform);
        DayLoopUI.Stretch(tileBody, 22, 12, 22, 12);
        DayLoopUI.Vertical(tileBody, 0, 2);
        DayLoopUI.Layout(DayLoopUI.Text(tileBody, label, 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 26);
        value = DayLoopUI.Text(tileBody, "", 36, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(value, preferredHeight: 44);
        sub = DayLoopUI.Text(tileBody, "", 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(sub, preferredHeight: 24);
    }
}
