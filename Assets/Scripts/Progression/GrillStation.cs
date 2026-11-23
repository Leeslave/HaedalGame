using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 굽기 단계 점수 규칙. 화면과 떼어 두어 테스트에서 바로 확인한다.
/// 불 세기에 따라 팬 온도가 움직이고, 팬이 120°C를 넘으면 지금 면이 익기 시작한다.
/// 면마다 익힘이 초록 구간(78~95%)일 때 뒤집거나 꺼내면 5점, 벗어난 만큼 깎인다.
/// 팬 온도가 170~190°C에 머문 시간 비율도 점수에 들어간다.
/// </summary>
public static class GrillRules
{
    public const float StartTemperature = 110f, TempLow = 170f, TempHigh = 190f;
    public const float FlipLow = .78f, FlipHigh = .95f, BurnAt = 1.05f;
    public const float TimeLimit = 45f;
    public static readonly string[] HeatNames = { "약불", "중불", "강불" };
    private static readonly float[] HeatTargets = { 140f, 180f, 230f };

    /// <summary>불 세기(0 약, 1 중, 2 강) 쪽으로 팬 온도가 천천히 따라간다.</summary>
    public static float StepTemperature(float temperature, int heat, float dt)
    {
        float target = HeatTargets[Mathf.Clamp(heat, 0, 2)];
        return temperature + (target - temperature) * (1f - Mathf.Exp(-.8f * dt));
    }
    /// <summary>1초에 익는 양. 중불(180°C)이면 6초쯤 걸려 100%가 된다.</summary>
    public static float CookRate(float temperature) => Mathf.Max(0f, temperature - 120f) / 60f * .16f;
    public static bool InTempZone(float temperature) => temperature >= TempLow && temperature <= TempHigh;
    public static bool InFlipZone(float doneness) => doneness >= FlipLow && doneness <= FlipHigh;
    /// <summary>뒤집거나 꺼낼 때 그 면의 점수(0~5).</summary>
    public static float SideScore(float doneness)
    {
        float miss = doneness < FlipLow ? FlipLow - doneness : doneness > FlipHigh ? doneness - FlipHigh : 0f;
        return Mathf.Clamp(5f - miss * 12.5f, 0f, 5f);
    }
    /// <summary>단계 점수: 두 면 평균 70% + 팬 온도를 지킨 비율 30%.</summary>
    public static float StepScore(float front, float back, float tempRatio) =>
        Mathf.Clamp((front + back) * .5f * .7f + Mathf.Clamp01(tempRatio) * 5f * .3f, 0f, 5f);
    public static string Verdict(float score) => score >= 4.5f ? "완벽!" : score >= 3.5f ? "좋아요!" : score >= 2f ? "아쉬워요" : "다시 해 봐요";
    /// <summary>요리하는 동안 셰프가 하는 말.</summary>
    public static string ChefHint(float temperature, float doneness, bool back)
    {
        if (doneness > FlipHigh) return "앗, 타기 시작했어!\n빨리 " + (back ? "꺼내!" : "뒤집어!");
        if (InFlipZone(doneness)) return back ? "지금이야!\n꺼내!" : "지금이야!\n뒤집어!";
        if (temperature < 150f) return "팬이 아직 덜 달궈졌어.\n조금 기다려!";
        if (temperature > 205f) return "불이 너무 세!\n불을 줄여 줘!";
        if (doneness > .5f) return "조금만 더…\n색이 나고 있어!";
        return back ? "반대쪽도\n노릇하게 굽자." : "좋아, 껍질이\n천천히 익고 있어.";
    }
    /// <summary>끝난 뒤 셰프가 하는 말. 가장 아쉬운 부분을 짚어 준다.</summary>
    public static string ChefReview(float score, float front, float back, float tempRatio)
    {
        if (score >= 4.5f) return "좋아! 겉은 노릇하고 속은 촉촉하게 딱 좋게 구워졌어!";
        if (tempRatio < .5f) return "팬 온도를 170~190°C에 맞춰 봐. 중불이면 딱이야.";
        if (Mathf.Min(front, back) < 3.5f) return "익힘 게이지가 초록 구간일 때 뒤집어 봐.";
        return "잘했어! 조금만 더 맞추면 완벽해.";
    }
}

/// <summary>
/// 연구 실습의 굽기 단계 화면. 위에서 내려다본 조리대에 팬을 올리고
/// 불 세기를 맞춰 굽다가 초록 구간에서 뒤집고 꺼낸다. 시작 전 안내 카드와 끝난 뒤 결과 화면을 함께 보여 준다.
/// </summary>
public class GrillStation : MonoBehaviour
{
    private const float FrameW = 1920f, FrameH = 1080f;
    private RectTransform root, frame, fishPivot;
    private Image raw, cooked, glow, bubbles, steam, timeFill, tempFill, cookFill;
    private RectTransform knob, tempNeedle;
    private TMP_Text tempValue, cookValue, cookTitle, cookHint, timeText, chefLine, sizzle;
    private Button flipButton;
    private readonly Button[] heatButtons = new Button[3];
    private Action<float> done;
    private int heat = 1;
    private float temperature = GrillRules.StartTemperature, doneness, elapsed, inZone, front, flipAnim = -1f;
    private float tempSum; private int tempSamples;
    private bool running, back, finished;

    public static GrillStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var layer = DayLoopUI.Rect("GrillStage", screen);
        DayLoopUI.Stretch(layer);
        layer.SetAsLastSibling();
        var station = layer.gameObject.AddComponent<GrillStation>();
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    // ───────────── 화면 만들기 ─────────────
    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        done = onDone;
        root = (RectTransform)transform;
        var floor = DayLoopUI.Panel(root, new Color32(0xE2, 0xB5, 0x7E, 0xFF), "Floor", false);
        DayLoopUI.Stretch(floor.rectTransform);
        floor.raycastTarget = true; // 아래 연구 화면을 누르지 못하게 막는다.
        frame = DayLoopUI.Rect("Frame", root);
        DayLoopUI.Place(frame, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(FrameW, FrameH));
        Art("Lab/LabKitchen", 0, 0, FrameW, FrameH);

        // 선반 위 재료 병
        var shelf = new List<Sprite>();
        if (recipe != null)
            foreach (var need in recipe.Requirements) { var s = DesignArt.Ingredient(need.IngredientId); if (s != null && !shelf.Contains(s)) shelf.Add(s); }
        foreach (var extra in new[] { "Ingredient/Salt", "Ingredient/Oil", "Ingredient/Pepper", "Ingredient/Lemon" })
        { var s = DesignArt.Load(extra); if (s != null && !shelf.Contains(s) && shelf.Count < 5) shelf.Add(s); }
        for (int i = 0; i < shelf.Count; i++) { var jar = Art(null, 640 + i * 132, 102, 128, 128); jar.sprite = shelf[i]; jar.preserveAspect = true; }

        // 팬과 고등어
        glow = Art("Lab/LabPanGlow", 405, 308, 900, 800);
        Art("Lab/LabPan", 450, 342, 1140, 690);
        fishPivot = DayLoopUI.Rect("Fish", frame);
        DayLoopUI.Place(fishPivot, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(855, -700), new Vector2(500, 500));
        fishPivot.localEulerAngles = new Vector3(0, 0, -6);
        Sprite food = DesignArt.Food(recipe), rawFood = RawFood(recipe) ?? food;
        if (food == null && recipe != null && recipe.Requirements.Count > 0) food = rawFood = DesignArt.Ingredient(recipe.Requirements[0].IngredientId);
        cooked = Fill(fishPivot, food); raw = Fill(fishPivot, rawFood);
        bubbles = Art("Lab/LabBubbles", 555, 475, 600, 450);
        steam = Art("Lab/LabSteam", 630, 300, 450, 180);
        sizzle = Label(frame, "지글지글!", 66, Color.white, 1110, 360, 360, 90, true);
        sizzle.rectTransform.localEulerAngles = new Vector3(0, 0, -8);
        Outline(sizzle, .28f);

        BuildHud(recipe, steps, index, onExit);
        BuildHeatKnob();
        BuildGauges();

        // 셰프와 말풍선
        var chef = Art("OtterChef", 1545, 440, 345, 345);
        chef.preserveAspect = true;
        var bubble = DayLoopUI.Card(frame, Color.white, "ChefBubble", 3f, 0f);
        TopLeft(bubble.rectTransform, 1270, 525, 270, 120);
        chefLine = DayLoopUI.Text(bubble.transform, "", 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(chefLine.rectTransform, 12, 8, 12, 8);
        chefLine.lineSpacing = 30f;

        flipButton = DayLoopUI.KitButton(frame, "뒤집기", DayLoopUI.Kit.Sea, Color.white, 48, Flip, "Flip");
        TopLeft((RectTransform)flipButton.transform, 1570, 905, 315, 126);

        var guide = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Instruction", 3f, 5f);
        DayLoopUI.Place(guide.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-120, 24), new Vector2(1020, 84));
        var guideText = DayLoopUI.Text(guide.transform, "중불 유지 <color=#C8443A>→</color> 껍질이 노릇해지면 뒤집기 <color=#C8443A>→</color> 반대쪽도 굽고 꺼내기", 29, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(guideText.rectTransform, 16, 4, 16, 4);

        Refresh();
        ShowIntro(steps, index);
    }

    private void BuildHud(RecipeData recipe, ResearchStep[] steps, int index, Action onExit)
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "RecipeCard", 3f, 6f);
        TopLeft(card.rectTransform, 24, 18, 600, 108);
        var icon = DayLoopUI.Card(card.transform, Color.white, "Icon", 3f, 0f);
        DayLoopUI.Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(78, 78));
        var iconArt = DayLoopUI.Panel(icon.transform, Color.white, "Art", false);
        iconArt.sprite = DesignArt.Food(recipe); iconArt.preserveAspect = true; iconArt.raycastTarget = false;
        iconArt.enabled = iconArt.sprite != null;
        DayLoopUI.Stretch(iconArt.rectTransform, 4, 4, 4, 4);
        Label(card.transform, recipe != null ? recipe.RecipeName : "굽기", 40, DayLoopUI.Kit.Ink, 112, 14, 470, 48);
        Label(card.transform, $"STEP {index + 1} · {steps[index].text}", 21, DayLoopUI.Kit.InkSoft, 114, 62, 470, 32);

        float x = 660;
        for (int i = 0; i < steps.Length && i < 7; i++, x += 88)
        {
            bool past = i < index, now = i == index;
            var dot = DayLoopUI.Chunky(DayLoopUI.Panel(frame, past ? DayLoopUI.Kit.Kelp : now ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Divider, "Dot" + i), 4f, 0f);
            TopLeft(dot.rectTransform, x, 18, 66, 66);
            dot.raycastTarget = false;
            var number = DayLoopUI.Text(dot.transform, past ? DayLoopUI.Glyph("✓", "V") : (i + 1).ToString(), 30,
                past ? Color.white : now ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            DayLoopUI.Stretch(number.rectTransform);
            var name = Label(frame, ResearchPractice.StepLabel(steps[i].kind), 21, Color.white, x - 17, 86, 100, 28, true);
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

    private void BuildHeatKnob()
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "HeatKnob", 3f, 6f);
        TopLeft(card.rectTransform, 105, 600, 300, 345);
        Label(card.transform, "불 세기", 30, DayLoopUI.Kit.Ink, 0, 12, 300, 40, false, TextAlignmentOptions.Center);
        var dial = DayLoopUI.Panel(card.transform, Color.white, "Dial", false);
        dial.sprite = DesignArt.Load("Lab/LabDial"); dial.raycastTarget = false;
        TopLeft(dial.rectTransform, 22, 62, 255, 150);
        knob = DayLoopUI.Panel(card.transform, Color.white, "Knob", false).rectTransform;
        knob.GetComponent<Image>().sprite = DesignArt.Load("Lab/LabKnob");
        knob.GetComponent<Image>().raycastTarget = false;
        DayLoopUI.Place(knob, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(150, -200), new Vector2(120, 120));
        for (int i = 0; i < 3; i++)
        {
            int level = i;
            heatButtons[i] = DayLoopUI.KitButton(card.transform, new[] { "약", "중", "강" }[i], DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 28, () => { heat = level; Refresh(); }, "Heat" + i);
            TopLeft((RectTransform)heatButtons[i].transform, 22 + i * 88, 268, 80, 58);
        }
    }

    private void BuildGauges()
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Gauges", 3f, 6f);
        TopLeft(card.rectTransform, 1460, 150, 435, 285);
        Label(card.transform, "팬 온도", 24, DayLoopUI.Kit.Ink, 24, 16, 200, 34);
        tempValue = Label(card.transform, "", 34, DayLoopUI.Kit.Ink, 200, 12, 212, 40, false, TextAlignmentOptions.MidlineRight);
        tempFill = GaugeBar(card.transform, 60, (GrillRules.TempLow - 100f) / 150f, (GrillRules.TempHigh - 100f) / 150f, out tempNeedle);
        Label(card.transform, $"{GrillRules.TempLow:0}~{GrillRules.TempHigh:0}°C 유지", 18, DayLoopUI.Kit.InkSoft, 24, 94, 387, 26, false, TextAlignmentOptions.MidlineRight);
        cookTitle = Label(card.transform, "껍질 익힘", 24, DayLoopUI.Kit.Ink, 24, 140, 200, 34);
        cookValue = Label(card.transform, "", 34, DayLoopUI.Kit.Ink, 200, 136, 212, 40, false, TextAlignmentOptions.MidlineRight);
        cookFill = GaugeBar(card.transform, 184, GrillRules.FlipLow, GrillRules.FlipHigh, out _);
        cookHint = Label(card.transform, "78~95%에서 뒤집기", 18, DayLoopUI.Kit.InkSoft, 24, 218, 387, 26, false, TextAlignmentOptions.MidlineRight);
    }

    private Image GaugeBar(Transform parent, float y, float zoneFrom, float zoneTo, out RectTransform needle)
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

    // ───────────── 안내 카드 · 결과 ─────────────
    private void ShowIntro(ResearchStep[] steps, int index)
    {
        var dim = Overlay("Intro", .5f);
        var card = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "Card", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-150, -40), new Vector2(1230, 780));
        var tag = DayLoopUI.Card(card.transform, DayLoopUI.Kit.Awning, "Tag", 3f, 0f);
        TopLeft(tag.rectTransform, 54, 46, 190, 50);
        var tagText = DayLoopUI.Text(tag.transform, $"STEP {index + 1} / {steps.Length}", 28, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(tagText.rectTransform);
        Label(card.transform, steps[index].text, 62, DayLoopUI.Kit.Ink, 54, 112, 1120, 84);
        Label(card.transform, "팬을 불에 올리고 불 세기를 맞춰요. 한쪽이 노릇해지면 한 번 뒤집고,\n반대쪽도 노릇해지면 꺼내요. 너무 오래 두면 타 버려요!", 27, DayLoopUI.Kit.Ink, 54, 206, 1120, 90);

        var preview = DayLoopUI.Card(card.transform, new Color32(0x2F, 0x4B, 0x52, 0xFF), "Preview", 4f, 0f);
        TopLeft(preview.rectTransform, 54, 330, 500, 290);
        var pan = DayLoopUI.Panel(preview.transform, Color.white, "Pan", false);
        pan.sprite = DesignArt.Load("Lab/LabPan"); pan.raycastTarget = false;
        TopLeft(pan.rectTransform, 30, 40, 418, 253);
        var fish = DayLoopUI.Panel(preview.transform, Color.white, "Fish", false);
        fish.sprite = cooked.sprite; fish.preserveAspect = true; fish.raycastTarget = false;
        TopLeft(fish.rectTransform, 70, 70, 190, 190);
        Label(preview.transform, "▶ 미리보기", 22, new Color(1, 1, 1, .85f), 16, 246, 200, 34);

        Label(card.transform, "조작", 22, DayLoopUI.Kit.InkSoft, 600, 330, 400, 30);
        Label(card.transform, "불 세기를 <color=#C8443A>중불</color>에 맞추기\n<color=#C8443A>→</color> 노릇해지면 <color=#1F7F8A>뒤집기</color>\n<color=#C8443A>→</color> 반대쪽도 굽고 <color=#1F7F8A>꺼내기</color>", 38, DayLoopUI.Kit.Ink, 600, 364, 580, 180);
        Label(card.transform, $"제한 시간 {GrillRules.TimeLimit:0}초 · 팬 온도 {GrillRules.TempLow:0}~{GrillRules.TempHigh:0}°C", 22, DayLoopUI.Kit.InkSoft, 600, 556, 580, 32);
        var start = DayLoopUI.KitButton(card.transform, "▶  시작!", DayLoopUI.Kit.Awning, Color.white, 46, () => {
            running = true; dim.gameObject.SetActive(false); Destroy(dim.gameObject);
        }, "Start");
        DayLoopUI.Place((RectTransform)start.transform, Vector2.one, Vector2.one, new Vector2(-48, -670), new Vector2(300, 96));

        var chef = DayLoopUI.Panel(dim.transform, Color.white, "Chef", false);
        chef.sprite = DesignArt.Load("OtterChef"); chef.preserveAspect = true; chef.raycastTarget = false;
        DayLoopUI.Place(chef.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(690, -330), new Vector2(450, 450));
        var bubble = DayLoopUI.Card(dim.transform, Color.white, "Bubble", 3f, 0f);
        DayLoopUI.Place(bubble.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(660, 60), new Vector2(380, 110));
        var line = DayLoopUI.Text(bubble.transform, "팬이 충분히 달궈져야\n껍질이 안 달라붙어!", 26, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(line.rectTransform, 12, 6, 12, 6);
        line.lineSpacing = 30f;
    }

    private void ShowResult(float score, float backScore, float tempRatio)
    {
        var dim = Overlay("Result", .45f);
        var verdict = DayLoopUI.Text(dim.transform, GrillRules.Verdict(score), 210, DayLoopUI.Kit.Coin, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Place(verdict.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 300), new Vector2(1200, 260));
        verdict.rectTransform.localEulerAngles = new Vector3(0, 0, 4);
        verdict.textWrappingMode = TextWrappingModes.NoWrap;
        Outline(verdict, .2f);
        if (score < 3.5f) verdict.color = new Color32(0xF6, 0xE6, 0xC8, 0xFF);
        Chip(dim.transform, $"{score:0.0} / 5점", 50, new Vector2(0, 120), new Vector2(300, 84));
        float average = tempSamples > 0 ? tempSum / tempSamples : temperature;
        Chip(dim.transform, $"평균 {average:0}°C", 26, new Vector2(-300, 30), new Vector2(250, 54));
        Chip(dim.transform, $"앞면 {front:0.0}점", 26, new Vector2(-20, 30), new Vector2(250, 54));
        Chip(dim.transform, $"뒷면 {backScore:0.0}점", 26, new Vector2(260, 30), new Vector2(250, 54));

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
        Label(box.transform, GrillRules.ChefReview(score, front, backScore, tempRatio), 36, DayLoopUI.Kit.Ink, 216, 76, 920, 96);

        var next = DayLoopUI.KitButton(dim.transform, "다음 단계  ▶", DayLoopUI.Kit.Awning, Color.white, 40, () => {
            var callback = done; done = null; callback?.Invoke(score);
            gameObject.SetActive(false); Destroy(gameObject);
        }, "Next");
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(560, 76), new Vector2(330, 114));
    }

    // ───────────── 굽기 ─────────────
    private void Flip()
    {
        if (!running || finished) return;
        if (!back)
        {
            front = GrillRules.SideScore(doneness);
            back = true; doneness = 0f; flipAnim = 0f;
            DayLoopUI.SetButtonLabel(flipButton, "꺼내기");
            Refresh();
            return;
        }
        Finish(GrillRules.SideScore(doneness));
    }

    private void Finish(float backScore)
    {
        if (finished) return;
        finished = true; running = false;
        float tempRatio = elapsed > 0f ? inZone / elapsed : 0f;
        float score = GrillRules.StepScore(front, backScore, tempRatio);
        flipButton.gameObject.SetActive(false);
        ShowResult(score, backScore, tempRatio);
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (running && !finished)
        {
            temperature = GrillRules.StepTemperature(temperature, heat, dt);
            doneness += GrillRules.CookRate(temperature) * dt;
            elapsed += dt; tempSum += temperature; tempSamples++;
            if (GrillRules.InTempZone(temperature)) inZone += dt;
            if (elapsed >= GrillRules.TimeLimit)
            {
                // 시간이 다 되면 지금 상태로 끝낸다. 아직 안 뒤집었다면 뒷면은 0점이다.
                if (!back) { front = GrillRules.SideScore(doneness); Finish(0f); }
                else Finish(GrillRules.SideScore(doneness));
            }
        }
        if (flipAnim >= 0f)
        {
            flipAnim += dt / .35f;
            float squash = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(flipAnim) * Mathf.PI));
            fishPivot.localScale = new Vector3(1f, Mathf.Max(.05f, squash), 1f);
            fishPivot.anchoredPosition = new Vector2(855, -700) + Vector2.up * Mathf.Sin(Mathf.Clamp01(flipAnim) * Mathf.PI) * 120f;
            if (flipAnim >= 1f) { flipAnim = -1f; fishPivot.localScale = Vector3.one; fishPivot.anchoredPosition = new Vector2(855, -700); }
        }
        Refresh();
    }

    private void Refresh()
    {
        if (tempValue == null) return;
        float heatAmount = Mathf.InverseLerp(120f, 220f, temperature);
        glow.color = new Color(1, 1, 1, Mathf.Lerp(.15f, 1f, heatAmount));
        float wobble = Mathf.Sin(Time.unscaledTime * 9f) * .5f + .5f;
        bubbles.color = new Color(1, 1, 1, temperature > 150f ? Mathf.Lerp(.4f, 1f, wobble) : 0f);
        steam.color = new Color(1, 1, 1, Mathf.Clamp01((temperature - 140f) / 50f) * Mathf.Lerp(.45f, .85f, wobble));
        steam.rectTransform.anchoredPosition = new Vector2(855, -390 + wobble * 10f);
        sizzle.gameObject.SetActive(running && temperature > 160f);

        // 덜 익은 그림 위에 익은 그림이 서서히 나타나고, 너무 익으면 어두워진다.
        float cook = Mathf.Clamp01(doneness / GrillRules.FlipLow);
        raw.color = new Color(1, 1, 1, 1f - cook);
        float burn = Mathf.Clamp01((doneness - GrillRules.FlipHigh) / (GrillRules.BurnAt + .3f - GrillRules.FlipHigh));
        cooked.color = Color.Lerp(Color.white, new Color(.42f, .3f, .24f), burn);

        tempValue.text = $"{temperature:0}°C";
        DayLoopUI.SetBar(tempFill, Mathf.InverseLerp(100f, 250f, temperature));
        tempFill.color = temperature > GrillRules.TempHigh ? DayLoopUI.Kit.Alert : DayLoopUI.Kit.Coin;
        tempNeedle.anchorMin = tempNeedle.anchorMax = new Vector2(Mathf.InverseLerp(100f, 250f, temperature), .5f);
        tempNeedle.anchoredPosition = Vector2.zero;
        cookTitle.text = back ? "뒷면 익힘" : "껍질 익힘";
        cookHint.text = back ? "78~95%에서 꺼내기" : "78~95%에서 뒤집기";
        cookValue.text = doneness > 1.3f ? "탔어요!" : $"{doneness * 100f:0}%";
        cookValue.color = GrillRules.InFlipZone(doneness) ? DayLoopUI.Kit.GoodInk : doneness > GrillRules.FlipHigh ? DayLoopUI.Kit.WarnInk : DayLoopUI.Kit.Ink;
        DayLoopUI.SetBar(cookFill, doneness);
        cookFill.color = doneness > GrillRules.FlipHigh ? DayLoopUI.Kit.Alert : new Color32(0xD9, 0x96, 0x3F, 0xFF);

        float left = Mathf.Max(0f, GrillRules.TimeLimit - elapsed);
        timeText.text = $"{Mathf.CeilToInt(left)}초";
        DayLoopUI.SetBar(timeFill, left / GrillRules.TimeLimit);
        knob.localEulerAngles = new Vector3(0, 0, (1 - heat) * 60f);
        for (int i = 0; i < 3; i++)
        {
            heatButtons[i].image.color = i == heat ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand;
            heatButtons[i].GetComponentInChildren<TMP_Text>().color = i == heat ? Color.white : DayLoopUI.Kit.Ink;
        }
        chefLine.text = finished ? "다 구웠다!" : running ? GrillRules.ChefHint(temperature, doneness, back) : "준비되면\n[시작]을 눌러!";
        flipButton.interactable = running && !finished;
    }

    // ───────────── 도우미 ─────────────
    private static Sprite RawFood(RecipeData recipe)
    {
        var cooked = DesignArt.Food(recipe);
        return cooked == null ? null : DesignArt.Load("Food/Raw/" + cooked.name.Replace("Design Food/", ""));
    }
    private void TopLeft(RectTransform rect, float x, float y, float w, float h) =>
        DayLoopUI.Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));
    private Image Art(string asset, float x, float y, float w, float h)
    {
        var image = DayLoopUI.Panel(frame, Color.white, asset ?? "Art", false);
        if (asset != null) image.sprite = DesignArt.Load(asset);
        image.raycastTarget = false;
        if (asset == "Lab/LabPan" || asset == "Lab/LabKitchen" || asset == null) TopLeft(image.rectTransform, x, y, w, h);
        else DayLoopUI.Place(image.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(x + w / 2, -(y + h / 2)), new Vector2(w, h));
        return image;
    }
    private static Image Fill(RectTransform parent, Sprite sprite)
    {
        var image = DayLoopUI.Panel(parent, Color.white, "Layer", false);
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        image.enabled = sprite != null;
        DayLoopUI.Stretch(image.rectTransform);
        return image;
    }
    private TMP_Text Label(Transform parent, string text, float size, Color color, float x, float y, float w, float h,
        bool center = false, TextAlignmentOptions? align = null)
    {
        var label = DayLoopUI.Text(parent, text, size, color, align ?? (center ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft), FontStyles.Bold, size >= 28 ? "Label" : "Text");
        TopLeft(label.rectTransform, x, y, w, h);
        label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = 30f;
        return label;
    }
    // 글자 테두리와 아래 그림자. 공유 머티리얼을 건드리지 않도록 이 글자 전용 머티리얼에 켠다.
    private static void Outline(TMP_Text text, float width)
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
    private Image Overlay(string name, float alpha)
    {
        var dim = DayLoopUI.Panel(root, new Color(.16f, .09f, .05f, alpha), name, false);
        DayLoopUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        return dim;
    }
    private static void Chip(Transform parent, string text, float size, Vector2 position, Vector2 box)
    {
        var chip = DayLoopUI.Card(parent, DayLoopUI.Kit.Sail, "Chip", 3f, 0f);
        DayLoopUI.Place(chip.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, box);
        var label = DayLoopUI.Text(chip.transform, text, size, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(label.rectTransform);
    }
}
