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
    /// <summary>앞 단계에서 붓으로 바른 양념 색. 없으면 null.</summary>
    public static Color32? Glaze(ResearchStep[] steps, int index) => SeasonRules.LastCoat(steps, index, true);
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
public class GrillStation : LabStation
{
    private RectTransform fishPivot;
    private Image raw, cooked, glow, bubbles, steam, tempFill, cookFill;
    private RectTransform tempNeedle;
    private TMP_Text tempValue, cookValue, cookTitle, cookHint, sizzle;
    private Button flipButton;
    private float temperature = GrillRules.StartTemperature, doneness, elapsed, inZone, front, flipAnim = -1f;
    private float tempSum; private int tempSamples;
    private bool back;
    private Color rawTint = Color.white, cookedTint = Color.white;   // 앞에서 바른 양념 색

    public static GrillStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var station = Create<GrillStation>("GrillStage", screen);
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    // ───────────── 화면 만들기 ─────────────
    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        BuildStage(recipe, onDone, "Ingredient/Salt", "Ingredient/Oil", "Ingredient/Pepper", "Ingredient/Lemon");

        // 팬과 고등어
        glow = Art("Lab/LabPanGlow", 405, 308, 900, 800);
        Art("Lab/LabPan", 450, 342, 1140, 690, true);
        fishPivot = DayLoopUI.Rect("Fish", frame);
        DayLoopUI.Place(fishPivot, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(855, -700), new Vector2(500, 500));
        fishPivot.localEulerAngles = new Vector3(0, 0, -6);
        // 날것·익은 것 짝이 있는 요리 그림만 팬에 올린다. 접시에 담긴 임시 그림(짝 없음)은 재료 그림으로 굽는다.
        Sprite rawFood = RawFood(recipe), food = rawFood != null ? DesignArt.Food(recipe) : null;
        if (food == null && recipe != null && recipe.Requirements.Count > 0) food = rawFood = DesignArt.Ingredient(recipe.Requirements[0].IngredientId);
        cooked = Fill(fishPivot, food); raw = Fill(fishPivot, rawFood);
        var glaze = GrillRules.Glaze(steps, index);
        if (glaze.HasValue) { rawTint = Color.Lerp(Color.white, glaze.Value, .65f); cookedTint = Color.Lerp(Color.white, glaze.Value, .3f); }
        bubbles = Art("Lab/LabBubbles", 555, 475, 600, 450);
        steam = Art("Lab/LabSteam", 630, 300, 450, 180);
        sizzle = Label(frame, "지글지글!", 66, Color.white, 1110, 360, 360, 90, true);
        sizzle.rectTransform.localEulerAngles = new Vector3(0, 0, -8);
        Outline(sizzle, .28f);

        BuildHud(recipe, steps, index, onExit);
        BuildHeatKnob(105, 600);
        BuildGauges();
        BuildChef(1545, 440, 345, 1270, 525);

        flipButton = DayLoopUI.KitButton(frame, "뒤집기", DayLoopUI.Kit.Sea, Color.white, 48, Flip, "Flip");
        TopLeft((RectTransform)flipButton.transform, 1570, 905, 315, 126);
        BuildInstruction("중불 유지 <color=#C8443A>→</color> 껍질이 노릇해지면 뒤집기 <color=#C8443A>→</color> 반대쪽도 굽고 꺼내기");

        Refresh();
        ShowIntro(steps, index,
            "팬을 불에 올리고 불 세기를 맞춰요. 한쪽이 노릇해지면 한 번 뒤집고,\n반대쪽도 노릇해지면 꺼내요. 너무 오래 두면 타 버려요!",
            preview => {
                PreviewArt(preview, DesignArt.Load("Lab/LabPan"), 30, 40, 418, 253, false);
                PreviewArt(preview, cooked.sprite, 70, 70, 190, 190);
            },
            "불 세기를 <color=#C8443A>중불</color>에 맞추기\n<color=#C8443A>→</color> 노릇해지면 <color=#1F7F8A>뒤집기</color>\n<color=#C8443A>→</color> 반대쪽도 굽고 <color=#1F7F8A>꺼내기</color>",
            $"제한 시간 {GrillRules.TimeLimit:0}초 · 팬 온도 {GrillRules.TempLow:0}~{GrillRules.TempHigh:0}°C",
            "팬이 충분히 달궈져야\n껍질이 안 달라붙어!");
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
        float average = tempSamples > 0 ? tempSum / tempSamples : temperature;
        ShowResult(score, new[] { $"평균 {average:0}°C", $"앞면 {front:0.0}점", $"뒷면 {backScore:0.0}점" },
            GrillRules.ChefReview(score, front, backScore, tempRatio));
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
        raw.color = new Color(rawTint.r, rawTint.g, rawTint.b, 1f - cook);
        float burn = Mathf.Clamp01((doneness - GrillRules.FlipHigh) / (GrillRules.BurnAt + .3f - GrillRules.FlipHigh));
        cooked.color = Color.Lerp(cookedTint, new Color(.42f, .3f, .24f), burn);

        tempValue.text = $"{temperature:0}°C";
        DayLoopUI.SetBar(tempFill, Mathf.InverseLerp(100f, 250f, temperature));
        tempFill.color = temperature > GrillRules.TempHigh ? DayLoopUI.Kit.Alert : DayLoopUI.Kit.Coin;
        SetNeedle(tempNeedle, Mathf.InverseLerp(100f, 250f, temperature));
        cookTitle.text = back ? "뒷면 익힘" : "껍질 익힘";
        cookHint.text = back ? "78~95%에서 꺼내기" : "78~95%에서 뒤집기";
        cookValue.text = doneness > 1.3f ? "탔어요!" : $"{doneness * 100f:0}%";
        cookValue.color = GrillRules.InFlipZone(doneness) ? DayLoopUI.Kit.GoodInk : doneness > GrillRules.FlipHigh ? DayLoopUI.Kit.WarnInk : DayLoopUI.Kit.Ink;
        DayLoopUI.SetBar(cookFill, doneness);
        cookFill.color = doneness > GrillRules.FlipHigh ? DayLoopUI.Kit.Alert : new Color32(0xD9, 0x96, 0x3F, 0xFF);

        SetTimer(GrillRules.TimeLimit - elapsed, GrillRules.TimeLimit);
        RefreshHeatKnob();
        chefLine.text = finished ? "다 구웠다!" : running ? GrillRules.ChefHint(temperature, doneness, back) : "준비되면\n[시작]을 눌러!";
        flipButton.interactable = running && !finished;
    }
}
