using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 폐업 카드. 결산을 마친 뒤 식당 평점이 폐업 기준보다 낮으면 결산 대신 이어서 띄운다.
/// 밤 바다 배경 위 돛천 카드: 남색 제목 리본, 우는 해달, 버틴 날·최종 평점·남은 골드, 평점 게이지와 기준선, 다음 판 힌트.
/// </summary>
public class ClosurePanel : MonoBehaviour
{
    private const float MaxRating = 5f;
    private static readonly Color Track = new Color32(0xF3, 0xE6, 0xD3, 0xFF);
    private static readonly Color Halo = new Color32(0xEF, 0xE3, 0xCB, 0xFF);
    private static readonly Color NightDim = new Color32(0x16, 0x22, 0x3D, 0xB8);

    private Canvas _canvas;
    private TMP_Text _body, _days, _rating, _gold, _thresholdChip;
    private Image _fill;
    private RectTransform _marker;
    private Button _titleButton, _newGameButton;
    private Action _onTitle, _onNewGame;
    private bool _clicked;

    public bool IsVisible => _canvas != null && _canvas.gameObject.activeSelf;

    /// <summary>폐업 여부. 평점 이력이 없으면(0) 판정하지 않는다.</summary>
    public static bool IsClosure(float rating, float threshold) => rating > 0f && rating < threshold;

    public void Show(int days, float rating, float threshold, int gold, Action onTitle, Action onNewGame)
    {
        Build();
        _onTitle = onTitle;
        _onNewGame = onNewGame;
        _clicked = false;
        _titleButton.interactable = _newGameButton.interactable = true;

        _body.text = $"{days}일 동안 해달식당을 지켰어요.\n평점이 폐업 기준보다 낮아져 문을 닫게 됐어요.";
        _days.text = $"{days}일";
        _rating.text = $"{DayLoopUI.Rating(rating)}<size=50%><color={DayLoopUI.Hex(DayLoopUI.Kit.Ink)}>  / 기준 {DayLoopUI.Rating(threshold)}</color></size>";
        _gold.text = DayLoopUI.Gold(gold);
        _thresholdChip.text = $"기준 {DayLoopUI.Rating(threshold)}";

        DayLoopUI.SetBar(_fill, rating / MaxRating);
        float mark = Mathf.Clamp01(threshold / MaxRating);
        _marker.anchorMin = _marker.anchorMax = new Vector2(mark, 0.5f);

        _canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    private void Choose(Action action)
    {
        if (_clicked) return;   // 연타 방지: 씬 이동은 한 번만
        _clicked = true;
        _titleButton.interactable = _newGameButton.interactable = false;
        action?.Invoke();
    }

    // ───── 조립 ─────

    private void Build()
    {
        if (_canvas != null) return;

        _canvas = DayLoopUI.CreateCanvas("[DayLoop] Closure", 320, gameObject.scene);
        _canvas.transform.SetParent(transform, false);

        // 식당 앞 그림을 밤빛으로 덮어 문 닫은 저녁처럼 보이게 한다.
        Image backdrop = ReferenceTitleView.Picture(_canvas.transform, "RestaurantPrepBackground");
        backdrop.raycastTarget = true;
        DayLoopUI.Place(backdrop.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        AspectRatioFitter cover = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        cover.aspectRatio = backdrop.sprite != null ? backdrop.sprite.rect.width / backdrop.sprite.rect.height : 16f / 9f;
        Image dim = DayLoopUI.Panel(_canvas.transform, NightDim, "Night", false);
        DayLoopUI.Stretch(dim.rectTransform);

        Image card = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "Card", 4, 10);
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(1140, 720));
        card.GetComponent<RoundedCorner>().radius = 34;
        Transform c = card.transform;
        DayLoopUI.Ribbon(c, "식당 폐업", DayLoopUI.Kit.Night, 440, 42);

        // 왼쪽: 우는 해달
        Image halo = DayLoopUI.Panel(c, Halo, "Halo");
        halo.sprite = DayLoopUI.Circle; halo.type = Image.Type.Simple; halo.raycastTarget = false;
        ReferenceTitleView.Place(halo.rectTransform, 65, 95, 300, 300);
        Image otter = ReferenceTitleView.Picture(c, "ClosedOtter");
        otter.preserveAspect = true;
        ReferenceTitleView.Place(otter.rectTransform, 65, 95, 300, 300);

        // 오른쪽: 제목·설명
        TMP_Text heading = DayLoopUI.Text(c, "손님들의 발길이 끊겼어요", 46, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft);
        ReferenceTitleView.Place(heading.rectTransform, 430, 76, 660, 64);
        _body = DayLoopUI.Text(c, "", 25, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        _body.lineSpacing = 12;
        ReferenceTitleView.Place(_body.rectTransform, 430, 152, 660, 80);

        // 버틴 날 · 최종 평점 · 남은 골드
        RectTransform stats = DayLoopUI.Rect("Stats", c);
        ReferenceTitleView.Place(stats, 430, 236, 660, 112);
        HorizontalLayoutGroup statsLayout = DayLoopUI.Horizontal(stats, 0, 18);
        statsLayout.childForceExpandWidth = true; statsLayout.childForceExpandHeight = true;
        _days = Stat(stats, "버틴 날", DayLoopUI.Kit.Ink);
        _rating = Stat(stats, "최종 평점", DayLoopUI.Kit.WarnInk);
        _gold = Stat(stats, "남은 골드", DayLoopUI.Kit.GoldInk);

        // 평점 게이지: 빨간 채움 + 기준선 + 기준 칩
        TMP_Text barLabel = DayLoopUI.Text(c, "평점", 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        ReferenceTitleView.Place(barLabel.rectTransform, 430, 384, 48, 38);
        Image track = DayLoopUI.Chunky(DayLoopUI.Panel(c, Track, "RatingBar"), 3f, 0f);
        track.raycastTarget = false;
        ReferenceTitleView.Place(track.rectTransform, 494, 390, 470, 26);
        _fill = DayLoopUI.Panel(track.transform, DayLoopUI.Kit.Alert, "Fill");
        _fill.raycastTarget = false;
        _fill.rectTransform.anchorMin = Vector2.zero;
        _fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        _fill.rectTransform.offsetMin = new Vector2(3, 3);
        _fill.rectTransform.offsetMax = new Vector2(-3, -3);
        Image marker = DayLoopUI.Panel(track.transform, DayLoopUI.Kit.Ink, "Threshold", false);
        marker.raycastTarget = false;
        _marker = marker.rectTransform;
        _marker.pivot = new Vector2(.5f, .5f);
        _marker.sizeDelta = new Vector2(4, 38);
        _thresholdChip = DayLoopUI.Text(c, "", 17, DayLoopUI.Kit.WarnInk, TextAlignmentOptions.Center, FontStyles.Bold);
        Image chip = DayLoopUI.Panel(c, DayLoopUI.Kit.WarnTint, "ThresholdChip");
        chip.raycastTarget = false;
        Outline chipLine = chip.gameObject.AddComponent<RoundedOutline>();
        chipLine.effectColor = DayLoopUI.Kit.WarnInk; chipLine.effectDistance = new Vector2(2, -2);
        ReferenceTitleView.Place(chip.rectTransform, 980, 386, 110, 34);
        _thresholdChip.transform.SetParent(chip.transform, false);
        DayLoopUI.Stretch(_thresholdChip.rectTransform);

        // 다음 판 힌트
        Image hint = DayLoopUI.Panel(c, DayLoopUI.Kit.SeaTint, "Hint");
        hint.raycastTarget = false;
        Outline hintLine = hint.gameObject.AddComponent<RoundedOutline>();
        hintLine.effectColor = DayLoopUI.Kit.Sea; hintLine.effectDistance = new Vector2(3, -3);
        ReferenceTitleView.Place(hint.rectTransform, 50, 490, 1040, 68);
        Image idea = ReferenceTitleView.Picture(hint.transform, "IconIdea");
        idea.preserveAspect = true; idea.color = DayLoopUI.Kit.SeaDeep;
        DayLoopUI.Place(idea.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(26, 0), new Vector2(30, 30));
        TMP_Text hintText = DayLoopUI.Text(hint.transform, "다음엔: 맛 점수가 높은 메뉴를 고르면 평점이 빨리 회복돼요.", 23, DayLoopUI.Kit.SeaDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Stretch(hintText.rectTransform, 70, 0, 24, 0);

        // 버튼
        _titleButton = DayLoopUI.KitButton(c, "타이틀로", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 32, () => Choose(_onTitle), "TitleButton");
        ReferenceTitleView.Place((RectTransform)_titleButton.transform, 488, 590, 260, 84);
        Icon(_titleButton, "IconExit", DayLoopUI.Kit.Ink);
        _newGameButton = DayLoopUI.KitButton(c, "새 게임 시작", DayLoopUI.Kit.Awning, Color.white, 32, () => Choose(_onNewGame), "NewGameButton");
        ReferenceTitleView.Place((RectTransform)_newGameButton.transform, 770, 590, 320, 84);
        Icon(_newGameButton, "IconNew", Color.white);

        Hide();
    }

    private static TMP_Text Stat(Transform parent, string label, Color valueColor)
    {
        Image tile = DayLoopUI.Card(parent, Color.white, "Stat", 3f, 5f);
        tile.raycastTarget = false;
        DayLoopUI.Layout(tile, flexibleWidth: 1);
        TMP_Text title = DayLoopUI.Text(tile.transform, label, 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -14), new Vector2(200, 26));
        TMP_Text value = DayLoopUI.Text(tile.transform, "", 44, valueColor, TextAlignmentOptions.Center);
        value.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Place(value.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(200, 56));
        return value;
    }

    // 버튼 글자 앞에 아이콘을 붙이고 둘을 함께 가운데로 모은다.
    private static void Icon(Button button, string asset, Color color)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        float textWidth = label.GetPreferredValues(label.text).x;
        float width = 30 + 14 + textWidth;
        Image icon = ReferenceTitleView.Picture(button.transform, asset);
        icon.preserveAspect = true; icon.color = color;
        DayLoopUI.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(0, .5f), new Vector2(-width / 2, 0), new Vector2(30, 30));
        DayLoopUI.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(1, .5f), new Vector2(width / 2, 0), new Vector2(textWidth + 4, 60));
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
