using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 실습 단계 화면(굽기, 염지 등)이 함께 쓰는 틀.
/// 조리대 배경, 위쪽 레시피 카드·단계 점·남은 시간, 셰프 말풍선, 아래 안내 줄,
/// 시작 전 안내 카드와 끝난 뒤 결과 화면을 만든다.
/// </summary>
public abstract class LabStation : MonoBehaviour
{
    protected const float FrameW = 1920f, FrameH = 1080f;
    protected RectTransform root, frame;
    protected Image timeFill;
    protected TMP_Text timeText, chefLine;
    protected Action<float> done;
    protected bool running, finished;

    protected static T Create<T>(string name, Transform screen) where T : LabStation
    {
        var layer = DayLoopUI.Rect(name, screen);
        DayLoopUI.Stretch(layer);
        layer.SetAsLastSibling();
        return layer.gameObject.AddComponent<T>();
    }

    /// <summary>바닥, 1920x1080 틀, 조리대 그림, 선반 위 재료 병을 놓는다.</summary>
    protected void BuildStage(RecipeData recipe, Action<float> onDone, params string[] shelfExtras)
    {
        done = onDone;
        root = (RectTransform)transform;
        var floor = DayLoopUI.Panel(root, new Color32(0xE2, 0xB5, 0x7E, 0xFF), "Floor", false);
        DayLoopUI.Stretch(floor.rectTransform);
        floor.raycastTarget = true; // 아래 연구 화면을 누르지 못하게 막는다.
        frame = DayLoopUI.Rect("Frame", root);
        DayLoopUI.Place(frame, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(FrameW, FrameH));
        Art("Lab/LabKitchen", 0, 0, FrameW, FrameH, true);

        var shelf = new List<Sprite>();
        if (recipe != null)
            foreach (var need in recipe.Requirements) { var s = DesignArt.Ingredient(need.IngredientId); if (s != null && !shelf.Contains(s)) shelf.Add(s); }
        foreach (var extra in shelfExtras)
        { var s = DesignArt.Load(extra); if (s != null && !shelf.Contains(s) && shelf.Count < 5) shelf.Add(s); }
        for (int i = 0; i < shelf.Count && i < 5; i++) { var jar = Art(null, 640 + i * 132, 102, 128, 128, true); jar.sprite = shelf[i]; jar.preserveAspect = true; }
    }

    /// <summary>왼쪽 위 레시피 카드, 단계 점, 남은 시간, 나가기 버튼.</summary>
    protected void BuildHud(RecipeData recipe, ResearchStep[] steps, int index, Action onExit)
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "RecipeCard", 3f, 6f);
        TopLeft(card.rectTransform, 24, 18, 600, 108);
        var icon = DayLoopUI.Card(card.transform, Color.white, "Icon", 3f, 0f);
        DayLoopUI.Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(78, 78));
        var iconArt = DayLoopUI.Panel(icon.transform, Color.white, "Art", false);
        iconArt.sprite = DishArt(recipe); iconArt.preserveAspect = true; iconArt.raycastTarget = false;
        iconArt.enabled = iconArt.sprite != null;
        DayLoopUI.Stretch(iconArt.rectTransform, 4, 4, 4, 4);
        Label(card.transform, recipe != null ? recipe.RecipeName : "연구", 40, DayLoopUI.Kit.Ink, 112, 14, 470, 48);
        var stepLine = Label(card.transform, $"STEP {index + 1} · {steps[index].text}", 21, DayLoopUI.Kit.InkSoft, 114, 62, 470, 32);
        stepLine.textWrappingMode = TextWrappingModes.NoWrap; stepLine.overflowMode = TextOverflowModes.Ellipsis; // 긴 단계 글이 두 줄로 넘쳐 이름과 겹쳤다.

        // 단계가 6개 이상이면 점을 줄여 남은 시간 칸(x 1140)과 겹치지 않게 한다.
        int count = Mathf.Min(steps.Length, 7);
        float dotSize = count > 5 ? 54f : 66f, gap = count > 5 ? 70f : 88f, x = 660;
        for (int i = 0; i < count; i++, x += gap)
        {
            bool past = i < index, now = i == index;
            var dot = DayLoopUI.Chunky(DayLoopUI.Panel(frame, past ? DayLoopUI.Kit.Kelp : now ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Divider, "Dot" + i), 4f, 0f);
            TopLeft(dot.rectTransform, x, 18 + (66 - dotSize) / 2, dotSize, dotSize);
            dot.raycastTarget = false;
            var number = DayLoopUI.Text(dot.transform, past ? DayLoopUI.Glyph("✓", "V") : (i + 1).ToString(), dotSize * .45f,
                past ? Color.white : now ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            DayLoopUI.Stretch(number.rectTransform);
            var name = Label(frame, ResearchPractice.StepLabel(steps[i]), count > 5 ? 19 : 21, Color.white, x + dotSize / 2 - 50, 86, 100, 28, true);
            Outline(name, .3f);
        }

        var timer = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Timer", 3f, 5f);
        TopLeft(timer.rectTransform, 1140, 30, 375, 72);
        Label(timer.transform, "남은 시간", 21, DayLoopUI.Kit.InkSoft, 18, 0, 110, 72);
        timeFill = DayLoopUI.Bar(timer.transform, 1f, DayLoopUI.Kit.Coin, "TimeBar");
        DayLoopUI.Place((RectTransform)timeFill.transform.parent, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(124, 0), new Vector2(160, 22));
        timeText = Label(timer.transform, "", 30, DayLoopUI.Kit.Ink, 292, 0, 76, 72);

        var exit = DayLoopUI.KitButton(frame, "나가기", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 27, () => onExit?.Invoke(), "Exit");
        TopLeft((RectTransform)exit.transform, 1735, 33, 165, 66);
        exit.interactable = onExit != null;
    }

    protected void SetTimer(float left, float limit)
    {
        left = Mathf.Max(0f, left);
        timeText.text = $"{Mathf.CeilToInt(left)}초";
        DayLoopUI.SetBar(timeFill, limit > 0f ? left / limit : 0f);
    }

    /// <summary>셰프 그림과 말풍선. 말풍선 글자는 chefLine에 넣는다.</summary>
    protected void BuildChef(float x, float y, float size, float bubbleX, float bubbleY)
    {
        var chef = Art("OtterChef", x, y, size, size);
        chef.preserveAspect = true;
        var bubble = DayLoopUI.Card(frame, Color.white, "ChefBubble", 3f, 0f);
        TopLeft(bubble.rectTransform, bubbleX, bubbleY, 270, 120);
        chefLine = DayLoopUI.Text(bubble.transform, "", 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(chefLine.rectTransform, 12, 8, 12, 8);
        chefLine.lineSpacing = 30f;
    }

    /// <summary>화면 아래 가운데 안내 줄.</summary>
    protected TMP_Text BuildInstruction(string text, float offsetX = -120f, float width = 1020f)
    {
        var guide = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Instruction", 3f, 5f);
        DayLoopUI.Place(guide.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(offsetX, 24), new Vector2(width, 84));
        var guideText = DayLoopUI.Text(guide.transform, text, 29, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(guideText.rectTransform, 16, 4, 16, 4);
        return guideText;
    }

    /// <summary>
    /// 시작 전 안내 카드. 미리보기 칸은 buildPreview가 채운다(500x290).
    /// [시작!]을 누르면 running이 켜지고 OnStart가 불린다.
    /// </summary>
    protected void ShowIntro(ResearchStep[] steps, int index, string description, Action<RectTransform> buildPreview,
        string controls, string limits, string chefTip)
    {
        var dim = Overlay("Intro", .5f);
        var card = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "Card", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-150, -40), new Vector2(1230, 780));
        var tag = DayLoopUI.Card(card.transform, DayLoopUI.Kit.Awning, "Tag", 3f, 0f);
        TopLeft(tag.rectTransform, 54, 46, 190, 50);
        var tagText = DayLoopUI.Text(tag.transform, $"STEP {index + 1} / {steps.Length}", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(tagText.rectTransform);
        Label(card.transform, steps[index].text, 62, DayLoopUI.Kit.Ink, 54, 112, 1120, 84);
        Label(card.transform, description, 27, DayLoopUI.Kit.Ink, 54, 206, 1120, 90);

        var preview = DayLoopUI.Card(card.transform, new Color32(0x2F, 0x4B, 0x52, 0xFF), "Preview", 4f, 0f);
        TopLeft(preview.rectTransform, 54, 330, 500, 290);
        buildPreview?.Invoke(preview.rectTransform);
        Label(preview.transform, "▶ 미리보기", 22, new Color(1, 1, 1, .85f), 16, 246, 200, 34);

        Label(card.transform, "조작", 22, DayLoopUI.Kit.InkSoft, 600, 330, 400, 30);
        Label(card.transform, controls, 38, DayLoopUI.Kit.Ink, 600, 364, 580, 180);
        Label(card.transform, limits, 22, DayLoopUI.Kit.InkSoft, 600, 556, 580, 32);
        var start = DayLoopUI.KitButton(card.transform, "▶  시작!", DayLoopUI.Kit.Awning, Color.white, 46, () => {
            running = true; dim.gameObject.SetActive(false); Destroy(dim.gameObject); OnStart();
        }, "Start");
        DayLoopUI.Place((RectTransform)start.transform, Vector2.one, Vector2.one, new Vector2(-48, -670), new Vector2(300, 96));

        var chef = DayLoopUI.Panel(dim.transform, Color.white, "Chef", false);
        chef.sprite = DesignArt.Load("OtterChef"); chef.preserveAspect = true; chef.raycastTarget = false;
        DayLoopUI.Place(chef.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(690, -330), new Vector2(450, 450));
        var bubble = DayLoopUI.Card(dim.transform, Color.white, "Bubble", 3f, 0f);
        DayLoopUI.Place(bubble.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(660, 60), new Vector2(380, 110));
        var line = DayLoopUI.Text(bubble.transform, chefTip, 26, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(line.rectTransform, 12, 6, 12, 6);
        line.lineSpacing = 30f;
    }

    protected virtual void OnStart() { }

    /// <summary>미리보기 칸에 그림 한 장을 놓는다.</summary>
    protected static Image PreviewArt(RectTransform preview, Sprite sprite, float x, float y, float w, float h, bool keepAspect = true)
    {
        var image = DayLoopUI.Panel(preview, Color.white, "PreviewArt", false);
        image.sprite = sprite; image.preserveAspect = keepAspect; image.raycastTarget = false;
        image.enabled = sprite != null;
        DayLoopUI.Place(image.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));
        return image;
    }

    /// <summary>끝난 뒤 큰 판정 글자, 점수, 세부 칩, 셰프 한마디, [다음 단계] 버튼.</summary>
    protected void ShowResult(float score, string[] chips, string review)
    {
        var dim = Overlay("Result", .45f);
        var verdict = DayLoopUI.Text(dim.transform, GrillRules.Verdict(score), 210, DayLoopUI.Kit.Coin, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Place(verdict.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 300), new Vector2(1200, 260));
        verdict.rectTransform.localEulerAngles = new Vector3(0, 0, 4);
        verdict.textWrappingMode = TextWrappingModes.NoWrap;
        Outline(verdict, .2f);
        if (score < 3.5f) verdict.color = new Color32(0xF6, 0xE6, 0xC8, 0xFF);
        Chip(dim.transform, $"{score:0.0} / 5점", 50, new Vector2(0, 120), new Vector2(300, 84));
        float step = 280f, start = -(chips.Length - 1) * step * .5f;
        for (int i = 0; i < chips.Length; i++)
            Chip(dim.transform, chips[i], 26, new Vector2(start + i * step, 30), new Vector2(260, 54));

        var box = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "Talk", 4f, 8f);
        DayLoopUI.Place(box.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-230, 40), new Vector2(1170, 198));
        var face = DayLoopUI.Card(box.transform, new Color32(0xF5, 0xD6, 0xB8, 0xFF), "Face", 4f, 0f);
        DayLoopUI.Place(face.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(30, 0), new Vector2(156, 156));
        face.gameObject.AddComponent<RectMask2D>();
        var portrait = DayLoopUI.Panel(face.transform, Color.white, "Chef", false);
        portrait.sprite = DesignArt.Load("OtterChef"); portrait.raycastTarget = false;
        DayLoopUI.Place(portrait.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(20, -20), new Vector2(225, 225));
        var name = DayLoopUI.Card(box.transform, DayLoopUI.Kit.Sea, "Name", 0f, 0f);
        TopLeft(name.rectTransform, 216, 26, 150, 40);
        var nameText = DayLoopUI.Text(name.transform, "해달 셰프", 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(nameText.rectTransform);
        Label(box.transform, review, 36, DayLoopUI.Kit.Ink, 216, 76, 920, 96);

        var next = DayLoopUI.KitButton(dim.transform, "다음 단계  ▶", DayLoopUI.Kit.Awning, Color.white, 40, () => {
            var callback = done; done = null; callback?.Invoke(score);
            gameObject.SetActive(false); Destroy(gameObject);
        }, "Next");
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(560, 76), new Vector2(330, 114));
    }

    // ───────────── 누르기 ─────────────
    /// <summary>틀 위 왼쪽 위 기준 (x, y, w, h)를 누를 수 있는 투명한 자리로 만든다.</summary>
    protected LabPointer AddPad(float x, float y, float w, float h)
    {
        var image = DayLoopUI.Panel(frame, new Color(0, 0, 0, 0), "Pad", false);
        TopLeft(image.rectTransform, x, y, w, h);
        image.raycastTarget = true;
        return image.gameObject.AddComponent<LabPointer>();
    }
    /// <summary>손가락 자리를 틀의 왼쪽 위 기준 좌표(오른쪽 x+, 아래 y+)로.</summary>
    protected bool PointerOnFrame(LabPointer pad, out Vector2 topLeft)
    {
        topLeft = default;
        if (pad == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, pad.Screen, pad.Cam, out var local)) return false;
        topLeft = new Vector2(local.x + FrameW * .5f, FrameH * .5f - local.y);
        return true;
    }
    /// <summary>왼쪽 위 기준 좌표에 가운데 기준 그림을 옮긴다(Art로 만든 그림).</summary>
    protected static void MoveTo(RectTransform art, Vector2 topLeft) => art.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);

    // ───────────── 불 세기 ─────────────
    protected int heat = 1;
    private readonly Button[] heatButtons = new Button[3];
    private RectTransform heatKnob;
    /// <summary>약·중·강 불 세기 손잡이.</summary>
    protected void BuildHeatKnob(float x, float y)
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "HeatKnob", 3f, 6f);
        TopLeft(card.rectTransform, x, y, 300, 345);
        Label(card.transform, "불 세기", 30, DayLoopUI.Kit.Ink, 0, 12, 300, 40, false, TextAlignmentOptions.Center);
        var dial = DayLoopUI.Panel(card.transform, Color.white, "Dial", false);
        dial.sprite = DesignArt.Load("Lab/LabDial"); dial.raycastTarget = false;
        TopLeft(dial.rectTransform, 22, 62, 255, 150);
        heatKnob = DayLoopUI.Panel(card.transform, Color.white, "Knob", false).rectTransform;
        heatKnob.GetComponent<Image>().sprite = DesignArt.Load("Lab/LabKnob");
        heatKnob.GetComponent<Image>().raycastTarget = false;
        DayLoopUI.Place(heatKnob, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(150, -200), new Vector2(120, 120));
        for (int i = 0; i < 3; i++)
        {
            int level = i;
            heatButtons[i] = DayLoopUI.KitButton(card.transform, new[] { "약", "중", "강" }[i], DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 28, () => { heat = level; RefreshHeatKnob(); }, "Heat" + i);
            TopLeft((RectTransform)heatButtons[i].transform, 22 + i * 88, 268, 80, 58);
        }
        RefreshHeatKnob();
    }
    protected void RefreshHeatKnob()
    {
        if (heatKnob == null) return;
        heatKnob.localEulerAngles = new Vector3(0, 0, (1 - heat) * 60f);
        for (int i = 0; i < 3; i++)
        {
            heatButtons[i].image.color = i == heat ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand;
            heatButtons[i].GetComponentInChildren<TMP_Text>().color = i == heat ? Color.white : DayLoopUI.Kit.Ink;
        }
    }

    // ───────────── 게이지 카드 ─────────────
    protected class Gauge { public TMP_Text Title, Value, Hint; public Image Fill; public RectTransform Needle; }
    /// <summary>오른쪽 위 게이지 카드. 줄마다 (이름, 초록 구간 시작·끝, 아래 설명).</summary>
    protected Gauge[] BuildGaugeCard(float x, float y, params (string title, float zoneFrom, float zoneTo, string hint)[] rows)
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Gauges", 3f, 6f);
        TopLeft(card.rectTransform, x, y, 435, 30 + rows.Length * 125);
        var gauges = new Gauge[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            float top = i * 124;
            var g = gauges[i] = new Gauge();
            g.Title = Label(card.transform, rows[i].title, 24, DayLoopUI.Kit.Ink, 24, top + 16, 240, 34);
            g.Value = Label(card.transform, "", 32, DayLoopUI.Kit.Ink, 200, top + 12, 212, 40, false, TextAlignmentOptions.MidlineRight);
            g.Fill = GaugeBar(card.transform, top + 60, rows[i].zoneFrom, rows[i].zoneTo, out g.Needle);
            g.Hint = Label(card.transform, rows[i].hint, 18, DayLoopUI.Kit.InkSoft, 24, top + 94, 387, 26, false, TextAlignmentOptions.MidlineRight);
        }
        return gauges;
    }
    protected static void SetGauge(Gauge g, float bar, string value, bool good, bool bad)
    {
        g.Value.text = value;
        g.Value.color = bad ? DayLoopUI.Kit.WarnInk : good ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink;
        DayLoopUI.SetBar(g.Fill, Mathf.Clamp01(bar));
        g.Fill.color = bad ? DayLoopUI.Kit.Alert : good ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Coin;
        SetNeedle(g.Needle, bar);
    }

    /// <summary>큰 효과 글자(지글지글, 퐁당 등).</summary>
    protected TMP_Text Pop(string text, float size, float x, float y, float w = 360, float h = 90)
    {
        var label = Label(frame, text, size, Color.white, x, y, w, h, true);
        label.rectTransform.localEulerAngles = new Vector3(0, 0, -8);
        Outline(label, .28f);
        return label;
    }

    /// <summary>점수 구간(0~5) 하나: 초록 구간 안이면 5점, 벗어난 만큼 깎인다.</summary>
    public static float ZoneScore(float value, float low, float high, float slope = 12.5f)
    {
        float miss = value < low ? low - value : value > high ? value - high : 0f;
        return Mathf.Clamp(5f - miss * slope, 0f, 5f);
    }

    // ───────────── 게이지 ─────────────
    /// <summary>초록 구간과 바늘이 있는 막대. 막대 길이는 387, 높이 26.</summary>
    protected Image GaugeBar(Transform parent, float y, float zoneFrom, float zoneTo, out RectTransform needle)
    {
        var fill = DayLoopUI.Bar(parent, 0f, DayLoopUI.Kit.Coin, "Gauge");
        var track = (RectTransform)fill.transform.parent;
        TopLeft(track, 24, y, 387, 26);
        var zone = DayLoopUI.Chunky(DayLoopUI.Panel(track, new Color(.23f, .54f, .35f, .3f), "Zone", false), 3f, 0f);
        zone.color = new Color(.23f, .54f, .35f, .3f); zone.raycastTarget = false;
        zone.rectTransform.anchorMin = new Vector2(zoneFrom, -.15f); zone.rectTransform.anchorMax = new Vector2(zoneTo, 1.15f);
        zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
        needle = DayLoopUI.Panel(track, DayLoopUI.Kit.Line, "Needle").rectTransform;
        needle.anchorMin = needle.anchorMax = new Vector2(0, .5f); needle.sizeDelta = new Vector2(9, 40);
        needle.GetComponent<Image>().raycastTarget = false;
        return fill;
    }
    protected static void SetNeedle(RectTransform needle, float value)
    {
        needle.anchorMin = needle.anchorMax = new Vector2(Mathf.Clamp01(value), .5f);
        needle.anchoredPosition = Vector2.zero;
    }

    // ───────────── 도우미 ─────────────
    // 디자이너 그림이 아직 없는 요리는 기존 요리·재료 그림을 조합한 임시 그림(Food/)을 쓴다.
    private static readonly Dictionary<string, string> MixedDishArt = new Dictionary<string, string>
    {
        { "가리비간장구이", "ScallopSoyGrill" }, { "광어간장찜", "FlounderSoySteam" }, { "문어초무침", "OctopusChomuchim" },
        { "바다의진미플래터", "SeaPlatter" }, { "바지락술찜", "ClamSakeSteam" }, { "연어스테이크레몬소스", "SalmonLemon" },
        { "해물된장구이모둠", "SeafoodDoenjangGrill" }, { "해물크림스튜", "SeafoodCreamStew" }, { "해물튀김모둠", "SeafoodTempura" },
        { "해산물전골", "SeafoodHotpot" },
    };
    /// <summary>요리 그림. 디자이너 그림 → 조합한 임시 그림 → 주재료 그림 순으로 찾는다.</summary>
    public static Sprite DishArt(RecipeData recipe) => DesignArt.Food(recipe)
        ?? (recipe != null && MixedDishArt.TryGetValue(recipe.RecipeName.Replace(" ", ""), out var file) ? DesignArt.Load("Food/" + file) : null)
        ?? (recipe != null && recipe.Requirements.Count > 0 ? DesignArt.Ingredient(recipe.Requirements[0].IngredientId) : null);
    protected static Sprite RawFood(RecipeData recipe)
    {
        var cooked = DesignArt.Food(recipe);
        return cooked == null ? null : DesignArt.Load("Food/Raw/" + cooked.name.Replace("Design Food/", ""));
    }
    protected static void TopLeft(RectTransform rect, float x, float y, float w, float h) =>
        DayLoopUI.Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));
    /// <summary>틀 위에 그림을 놓는다. topLeft가 아니면 가운데 기준이라 돌리거나 흔들 수 있다.</summary>
    protected Image Art(string asset, float x, float y, float w, float h, bool topLeft = false)
    {
        var image = DayLoopUI.Panel(frame, Color.white, asset ?? "Art", false);
        if (asset != null) image.sprite = DesignArt.Load(asset);
        image.raycastTarget = false;
        if (topLeft) TopLeft(image.rectTransform, x, y, w, h);
        else DayLoopUI.Place(image.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(x + w / 2, -(y + h / 2)), new Vector2(w, h));
        return image;
    }
    protected static Image Fill(RectTransform parent, Sprite sprite)
    {
        var image = DayLoopUI.Panel(parent, Color.white, "Layer", false);
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        image.enabled = sprite != null;
        DayLoopUI.Stretch(image.rectTransform);
        return image;
    }
    protected static TMP_Text Label(Transform parent, string text, float size, Color color, float x, float y, float w, float h,
        bool center = false, TextAlignmentOptions? align = null)
    {
        var label = DayLoopUI.Text(parent, text, size, color, align ?? (center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft), FontStyles.Bold, size >= 28 ? "Label" : "Text");
        TopLeft(label.rectTransform, x, y, w, h);
        label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = 30f;
        return label;
    }
    // 글자 테두리와 아래 그림자. 공유 머티리얼을 건드리지 않도록 이 글자 전용 머티리얼에 켠다.
    protected static void Outline(TMP_Text text, float width)
    {
        var material = text.fontMaterial;
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
        material.SetColor(ShaderUtilities.ID_OutlineColor, DayLoopUI.Kit.Line);
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, DayLoopUI.Kit.Line);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.8f);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, width * .6f);
        text.fontMaterial = material;
    }
    protected Image Overlay(string name, float alpha)
    {
        var dim = DayLoopUI.Panel(root, new Color(.16f, .09f, .05f, alpha), name, false);
        DayLoopUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        return dim;
    }
    protected static void Chip(Transform parent, string text, float size, Vector2 position, Vector2 box)
    {
        var chip = DayLoopUI.Card(parent, DayLoopUI.Kit.Sail, "Chip", 3f, 0f);
        DayLoopUI.Place(chip.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, box);
        var label = DayLoopUI.Text(chip.transform, text, size, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(label.rectTransform);
    }
}
