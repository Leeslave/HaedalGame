using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 새로하기 확인 창. (첫 플레이 UI 시안 v2 · 01)
/// 타이틀과 같은 1280×720 기준 좌표를 쓴다. 지금 저장과 새 게임을 나란히 비교하고,
/// 저장이 바뀌는 시점(새 게임 첫날 밤 취침)을 한 줄로 알려 준다.
/// </summary>
public static class NewGameConfirmView
{
    private static Canvas _canvas;

    public static void Show(Scene scene, Action onStart)
    {
        Close();
        _canvas = DayLoopUI.CreateCanvas("Design / 01 New game confirm", 20, scene);
        _canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);

        var dim = DayLoopUI.Panel(_canvas.transform, new Color32(20, 30, 36, 158), "Dim", false);
        DayLoopUI.Stretch(dim.rectTransform);

        var card = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "Dialog", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 0));
        var layout = DayLoopUI.Vertical(card, 0, 18);
        layout.padding = new RectOffset(32, 32, 50, 28);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Ribbon(card.transform, "새 게임을 시작할까요?");
        Compare(card.transform);
        Notice(card.transform);
        Buttons(card.transform, onStart);
    }

    public static void Close()
    {
        if (_canvas == null) return;
        _canvas.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(_canvas.gameObject);
        _canvas = null;
    }

    private static void Ribbon(Transform card, string title)
    {
        var ribbon = DayLoopUI.Card(card, DayLoopUI.Kit.Awning, "Ribbon", 4f, 5f);
        ribbon.raycastTarget = false;
        ribbon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var text = Jua(ribbon.transform, title, 28, Color.white, TextAlignmentOptions.Center);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        float width = text.GetPreferredValues(title).x + 64;
        DayLoopUI.Place(ribbon.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, .5f), new Vector2(0, -2), new Vector2(width, 54));
        DayLoopUI.Stretch(text.rectTransform);
    }

    private static void Compare(Transform card)
    {
        var row = DayLoopUI.Rect("Compare", card);
        DayLoopUI.Layout(row, preferredHeight: 116);
        var h = DayLoopUI.Horizontal(row, 0, 12);
        h.childForceExpandHeight = true;

        string gold = DayLoopUI.Hex(DayLoopUI.Kit.GoldInk);
        if (SaveService.TryGetSummary(out var summary))
        {
            var time = TimeSpan.FromSeconds(summary.PlayTimeSeconds);
            SaveCard(row, "지금 저장", $"{summary.Day}일차 아침", $"Lv.{summary.RestaurantLevel} · <color={gold}>{summary.Gold:N0}G</color>",
                $"플레이 {(int)time.TotalHours}시간 {time.Minutes}분", false);
        }
        else SaveCard(row, "지금 저장", "확인 필요", "저장 파일을 읽지 못했어요", "", false);

        var arrow = ReferenceTitleView.Picture(row, "IconChevronRight");
        arrow.color = DayLoopUI.Kit.Line; arrow.preserveAspect = true;
        DayLoopUI.Layout(arrow, preferredWidth: 30, preferredHeight: 30);

        var config = NewGameConfig.Load();
        SaveCard(row, "새 게임", "1일차 아침", $"Lv.{config.StartLevel} · <color={gold}>{config.StartGold:N0}G</color>", "첫 조리도구부터 시작", true);
    }

    private static void SaveCard(Transform row, string label, string day, string stats, string note, bool fresh)
    {
        var box = DayLoopUI.Chunky(DayLoopUI.Panel(row, fresh ? DayLoopUI.Kit.SeaTint : Color.white, label), 3f, 0f);
        if (fresh) box.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        DayLoopUI.Layout(box, flexibleWidth: 1, preferredHeight: 116);
        var v = DayLoopUI.Vertical(box, 0, 4, TextAnchor.MiddleLeft);
        v.padding = new RectOffset(16, 12, 10, 10);
        Color soft = fresh ? DayLoopUI.Kit.SeaDeep : DayLoopUI.Kit.InkSoft;
        DayLoopUI.Layout(Body(box.transform, label, 13, soft), preferredHeight: 18);
        DayLoopUI.Layout(Jua(box.transform, day, 24, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft), preferredHeight: 30);
        DayLoopUI.Layout(Body(box.transform, stats, 14, DayLoopUI.Kit.Ink), preferredHeight: 20);
        if (!string.IsNullOrEmpty(note)) DayLoopUI.Layout(Body(box.transform, note, 13, soft), preferredHeight: 18);
    }

    private static void Notice(Transform card)
    {
        var box = DayLoopUI.Chunky(DayLoopUI.Panel(card, DayLoopUI.Kit.Sand, "Notice"), 2f, 0f);
        box.GetComponent<Outline>().effectColor = new Color32(0xD9, 0xBE, 0x8E, 0xFF);
        DayLoopUI.Layout(box, preferredHeight: 70);
        var h = DayLoopUI.Horizontal(box, 0, 12);
        h.padding = new RectOffset(14, 14, 10, 10);
        var house = ReferenceTitleView.Picture(box.transform, "IconHouse");
        house.color = DayLoopUI.Kit.Night; house.preserveAspect = true;
        DayLoopUI.Layout(house, preferredWidth: 26, preferredHeight: 26);
        string soft = DayLoopUI.Hex(DayLoopUI.Kit.InkSoft);
        var text = Body(box.transform, $"새 게임 <b>첫날 밤, 집에서 잠들 때</b> 저장이 새 게임으로 바뀌어요.\n<color={soft}>그 전에 그만두면 지금 저장이 그대로 남아요.</color>", 15, DayLoopUI.Kit.Ink);
        text.lineSpacing = 8;
        DayLoopUI.Layout(text, flexibleWidth: 1, preferredHeight: 50);
    }

    private static void Buttons(Transform card, Action onStart)
    {
        var row = DayLoopUI.Rect("Buttons", card);
        DayLoopUI.Layout(row, preferredHeight: 60);
        var h = DayLoopUI.Horizontal(row, 0, 14);
        h.childForceExpandHeight = true;
        h.padding = new RectOffset(0, 0, 4, 0);
        var cancel = DayLoopUI.KitButton(row, "취소", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, 22, Close, "Cancel");
        DayLoopUI.Layout(cancel, preferredWidth: 180, preferredHeight: 56);
        var start = DayLoopUI.KitButton(row, "새 게임 시작", DayLoopUI.Kit.Awning, Color.white, 22, () => { Close(); onStart?.Invoke(); }, "Start");
        DayLoopUI.Layout(start, flexibleWidth: 1, preferredHeight: 56);
        var play = ReferenceTitleView.Picture(start.transform, "IconPlay");
        play.preserveAspect = true;
        var label = start.GetComponentInChildren<TMP_Text>();
        float half = label.GetPreferredValues("새 게임 시작").x / 2f;
        DayLoopUI.Place(play.rectTransform, new Vector2(.5f, .5f), new Vector2(1, .5f), new Vector2(-half - 8, 0), new Vector2(22, 22));
    }

    private static TextMeshProUGUI Jua(Transform parent, string value, float size, Color color, TextAlignmentOptions align)
    {
        var text = DayLoopUI.Text(parent, value, size, color, align);
        text.font = DesignFonts.Heading; text.fontSharedMaterial = text.font.material;
        return text;
    }

    private static TextMeshProUGUI Body(Transform parent, string value, float size, Color color)
    {
        var text = DayLoopUI.Text(parent, value, size, color, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        text.font = DesignFonts.Body; text.fontSharedMaterial = text.font.material;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}
