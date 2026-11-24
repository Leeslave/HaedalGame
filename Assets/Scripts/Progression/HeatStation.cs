using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum HeatMode { Boil, Simmer, Fry }

/// <summary>끓이기·졸이기·튀기기 한 단계의 설정. 온도 구간, 진행 이름, 끝내는 버튼 이름.</summary>
public class HeatProfile
{
    public HeatMode Mode;
    public float Start, ZoneLow, ZoneHigh, GaugeMin, GaugeMax, Limit = 35f;
    public float[] Targets;
    public string TempTitle, ProgressTitle, FinishLabel, SpillName;
}

/// <summary>
/// 끓이기·졸이기·튀기기 점수 규칙. 불 세기에 따라 온도가 따라가고, 온도가 맞으면 진행(익힘·농도·튀김 색)이 오른다.
/// 진행이 초록 구간(85~100%)일 때 끝내면 5점, 벗어난 만큼 깎인다.
/// 끓이기는 강불에서 넘치고 졸이기는 강불에서 타며, 튀기기는 기름 온도를 지킨 비율이 점수에 들어간다.
/// </summary>
public static class HeatRules
{
    public const float DoneLow = .85f, DoneHigh = 1f;

    public static HeatProfile Profile(string station, string text)
    {
        text ??= "";
        switch (station)
        {
            case "Fry":
                return new HeatProfile { Mode = HeatMode.Fry, Start = 140f, Targets = new[] { 155f, 175f, 200f }, ZoneLow = 170f, ZoneHigh = 180f,
                    GaugeMin = 130f, GaugeMax = 210f, TempTitle = "기름 온도", ProgressTitle = "튀김 색", FinishLabel = "건지기", SpillName = "" };
            case "Simmer":
                return new HeatProfile { Mode = HeatMode.Simmer, Start = 70f, Targets = new[] { 92f, 102f, 114f }, ZoneLow = 98f, ZoneHigh = 106f,
                    GaugeMin = 60f, GaugeMax = 120f, TempTitle = "냄비 온도", ProgressTitle = text.Contains("국물") ? "국물 졸인 정도" : "소스 농도",
                    FinishLabel = "불 끄기", SpillName = "탄 정도" };
            default:
                string progress = text.Contains("찌") ? "찜 익힘" : text.Contains("데치") ? "데친 정도" : text.Contains("육수") ? "육수 우러남"
                    : text.Contains("걸쭉") ? "걸쭉한 정도" : "익힘";
                string finish = text.Contains("찌") || text.Contains("데치") || text.Contains("확인") ? "꺼내기" : "불 끄기";
                return new HeatProfile { Mode = HeatMode.Boil, Start = 25f, Targets = new[] { 88f, 100f, 100f }, ZoneLow = 96f, ZoneHigh = 100f,
                    GaugeMin = 20f, GaugeMax = 110f, TempTitle = "물 온도", ProgressTitle = progress, FinishLabel = finish, SpillName = "넘친 정도" };
        }
    }

    /// <summary>불 세기(0 약, 1 중, 2 강) 쪽으로 온도가 천천히 따라간다.</summary>
    public static float StepTemperature(HeatProfile p, float temperature, int heat, float dt)
    {
        float target = p.Targets[Mathf.Clamp(heat, 0, 2)];
        return temperature + (target - temperature) * (1f - Mathf.Exp(-.8f * dt));
    }
    /// <summary>1초에 오르는 진행. 중불이면 9~11초쯤 걸려 100%가 된다.</summary>
    public static float ProgressRate(HeatProfile p, float temperature, int heat)
    {
        switch (p.Mode)
        {
            case HeatMode.Fry: return Mathf.Max(0f, temperature - 140f) / 35f * .11f;
            case HeatMode.Simmer: return temperature < 96f ? 0f : (temperature - 90f) / 12f * .1f;
            default: return temperature < p.ZoneLow ? 0f : heat >= 2 ? .14f : .1f;
        }
    }
    /// <summary>1초에 넘치거나 타는 양(0~1 게이지). 강불이 아니면 조금씩 가라앉는다.</summary>
    public static float SpillRate(HeatProfile p, float temperature, int heat, float progress)
    {
        switch (p.Mode)
        {
            case HeatMode.Boil: return heat >= 2 && temperature >= 98f ? .18f : -.05f;
            case HeatMode.Simmer: return heat >= 2 && progress > .4f ? .16f : -.03f;
            default: return 0f;
        }
    }
    public static bool InTempZone(HeatProfile p, float temperature) => temperature >= p.ZoneLow - .01f && temperature <= p.ZoneHigh + .01f;

    /// <summary>단계 점수: 진행 70% + (넘침·탐이 적을수록 / 기름 온도를 지킨 비율) 30%.</summary>
    public static float StepScore(HeatProfile p, float progress, float spillPeak, float tempRatio)
    {
        float done = LabStation.ZoneScore(progress, DoneLow, DoneHigh);
        float care = p.Mode == HeatMode.Fry ? Mathf.Clamp01(tempRatio) : 1f - Mathf.Clamp01(spillPeak);
        return Mathf.Clamp(done * .7f + care * 5f * .3f, 0f, 5f);
    }

    public static string ChefHint(HeatProfile p, float temperature, float progress, float spill, int heat)
    {
        if (progress > DoneHigh) return p.Mode == HeatMode.Fry ? "너무 튀겨졌어!\n빨리 건져!" : (p.FinishLabel == "불 끄기" ? "너무 익었어!\n빨리 불을 꺼!" : "너무 익었어!\n빨리 꺼내!");
        if (progress >= DoneLow) return $"지금이야!\n[{p.FinishLabel}]!";
        if (spill > .3f) return p.Mode == HeatMode.Boil ? "넘친다!\n불을 줄여!" : "타기 시작했어!\n불을 줄여!";
        if (temperature < p.ZoneLow - 6f) return p.Mode == HeatMode.Fry ? "기름이 덜 달궈졌어.\n조금 기다려!" : "아직 안 끓어.\n조금 기다려!";
        if (p.Mode == HeatMode.Fry && temperature > p.ZoneHigh + 8f) return "기름이 너무 뜨거워!\n불을 줄여 줘!";
        if (heat >= 2 && p.Mode != HeatMode.Fry) return "강불은 위험해!\n중불이면 충분해.";
        if (progress > .5f) return "조금만 더…\n거의 다 됐어!";
        return p.Mode == HeatMode.Fry ? "좋아, 노릇노릇\n튀겨지고 있어." : "좋아, 보글보글\n잘 되고 있어.";
    }
    public static string ChefReview(HeatProfile p, float score, float progress, float spillPeak, float tempRatio)
    {
        if (score >= 4.5f) return p.Mode == HeatMode.Fry ? "좋아! 겉은 바삭하고 속은 촉촉해!" : p.Mode == HeatMode.Simmer ? "좋아! 농도가 딱 알맞게 졸여졌어!" : "좋아! 딱 알맞게 익었어!";
        if (progress < DoneLow) return $"조금 일찍 끝냈어. {p.ProgressTitle} 게이지가 초록 구간일 때 [{p.FinishLabel}]를 눌러 봐.";
        if (progress > DoneHigh) return $"너무 오래 뒀어. 초록 구간을 넘기기 전에 [{p.FinishLabel}]를 눌러 봐.";
        if (p.Mode == HeatMode.Fry && tempRatio < .5f) return "기름 온도를 170~180°C에 맞춰 봐. 중불이면 딱이야.";
        if (spillPeak > .3f) return p.Mode == HeatMode.Boil ? "강불로 두면 넘쳐. 끓기 시작하면 중불로!" : "강불로 오래 두면 타. 중불로 천천히 졸여 봐.";
        return "잘했어! 조금만 더 맞추면 완벽해.";
    }
}

/// <summary>
/// 연구 실습의 끓이기·졸이기·튀기기 단계 화면. 냄비(튀김기)를 불에 올리고 불 세기를 맞추다가
/// 진행 게이지가 초록 구간일 때 [불 끄기]/[꺼내기]/[건지기]를 누른다.
/// </summary>
public class HeatStation : LabStation
{
    private HeatProfile profile;
    private Image water, foam, bubbles, steam, glow, burnt;
    private readonly List<RectTransform> floaters = new List<RectTransform>();
    private readonly List<Image> floaterArt = new List<Image>();
    private Gauge tempGauge, progressGauge, spillGauge;
    private TMP_Text pop;
    private Button finishButton;
    private Color liquid, liquidDone = new Color32(0x6E, 0x3A, 0x1C, 0xFF);
    private Color coatTint = Color.white;   // 튀길 때 앞에서 입힌 튀김옷 색
    private float temperature, progress, spill, spillPeak, elapsed, inZone;

    public static HeatStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var station = Create<HeatStation>("HeatStage", screen);
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        string text = steps[index].text;
        profile = HeatRules.Profile(LabStations.StationFor(steps[index]), text);
        temperature = profile.Start;
        var items = LabItems.FromText(recipe, text);
        var solids = PotSolids(recipe, steps, index);
        BuildStage(recipe, onDone, items.Select(i => i.Art).Where(a => a != null).Select(a => a.name.Replace("Design ", "")).Where(a => !a.StartsWith("Lab/")).ToArray());

        bool fry = profile.Mode == HeatMode.Fry;
        var coat = fry ? SeasonRules.LastCoat(steps, index, false) : null;
        if (coat.HasValue) coatTint = Color.Lerp(Color.white, coat.Value, .8f);
        glow = Art("Lab/LabPanGlow", 510, 200, 900, 800);
        Vector2 center = new Vector2(960, 597);
        if (fry)
        {
            Art("Lab/LabFryer", 501, 262, 918, 663, true);
            center = new Vector2(960, 600);
        }
        else
        {
            Art("Lab/LabPot", 510, 260, 900, 668, true);
            liquid = profile.Mode == HeatMode.Simmer && items.Any(i => i.Name == "레몬" || i.Name == "꿀") ? new Color32(0xF2, 0xC4, 0x4E, 0xFF)
                : profile.Mode == HeatMode.Simmer ? new Color32(0xC9, 0x8A, 0x4E, 0xFF)
                : items.Any(i => i.Name == "간장" || i.Name == "된장" || i.Name == "육수" || i.Name == "양념") ? new Color32(0xD9, 0xB7, 0x7A, 0xFF)
                : new Color32(0xCF, 0xE9, 0xF4, 0xFF);
            if (items.Any(i => i.Name == "레몬" || i.Name == "꿀")) liquidDone = new Color32(0xC9, 0x84, 0x2A, 0xFF); // 레몬·꿀 소스는 호박색으로
            water = Art("Lab/LabWater", 659, 337, 602, 520, true);
            water.color = liquid;
        }
        // 재료: 냄비 가운데 둘레에 띄운다.
        for (int i = 0; i < solids.Count; i++)
        {
            float angle = solids.Count == 1 ? 0f : i * Mathf.PI * 2f / solids.Count + .4f;
            float radius = solids.Count == 1 ? 0f : 130f;
            float size = solids.Count == 1 ? 330f : 250f;
            var at = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .75f) * radius;
            var art = Art(null, at.x - size / 2, at.y - size / 2, size, size);
            art.sprite = solids[i].Art; art.preserveAspect = true;
            floaters.Add(art.rectTransform); floaterArt.Add(art);
        }
        burnt = Art("Lab/LabWater", 659, 337, 602, 520, true);
        burnt.color = new Color(.2f, .1f, .05f, 0f);
        burnt.gameObject.SetActive(profile.Mode == HeatMode.Simmer);
        bubbles = Art("Lab/LabBubbles", 660, 380, 600, 450);
        steam = Art("Lab/LabSteam", 735, 250, 450, 180);
        foam = Art("Lab/LabWater", 620, 300, 680, 590, true);
        foam.color = new Color(1, 1, 1, 0);
        foam.gameObject.SetActive(profile.Mode == HeatMode.Boil);
        pop = Pop(fry ? "지글지글!" : profile.Mode == HeatMode.Simmer ? "뽀글뽀글!" : "보글보글!", 66, 1130, 330);

        BuildHud(recipe, steps, index, onExit);
        BuildHeatKnob(105, 600);
        var rows = new List<(string, float, float, string)>
        {
            (profile.TempTitle, Mathf.InverseLerp(profile.GaugeMin, profile.GaugeMax, profile.ZoneLow), Mathf.InverseLerp(profile.GaugeMin, profile.GaugeMax, profile.ZoneHigh),
                $"{profile.ZoneLow:0}~{profile.ZoneHigh:0}°C 유지"),
            (profile.ProgressTitle, HeatRules.DoneLow / 1.3f, HeatRules.DoneHigh / 1.3f, $"85~100%에서 [{profile.FinishLabel}]"),
        };
        if (!fry) rows.Add((profile.SpillName, 0f, .3f, profile.Mode == HeatMode.Boil ? "강불로 두면 넘쳐요" : "강불로 두면 타요"));
        var gauges = BuildGaugeCard(1460, 150, rows.ToArray());
        tempGauge = gauges[0]; progressGauge = gauges[1]; spillGauge = fry ? null : gauges[2];
        float chefY = fry ? 440 : 560;
        BuildChef(1560, chefY, 330, 1275, chefY + 60);

        finishButton = DayLoopUI.KitButton(frame, profile.FinishLabel, DayLoopUI.Kit.Sea, Color.white, 46, Finish, "Finish");
        TopLeft((RectTransform)finishButton.transform, 1570, 905, 315, 126);
        string arrow = "<color=#C8443A>→</color>";
        BuildInstruction(fry ? $"기름 {profile.ZoneLow:0}~{profile.ZoneHigh:0}°C 유지 {arrow} 노릇해지면 [건지기]"
            : profile.Mode == HeatMode.Simmer ? $"중불로 천천히 졸이기 {arrow} 농도가 초록 구간이면 [불 끄기]"
            : $"중불로 보글보글 끓이기 {arrow} {profile.ProgressTitle}{SeasonRules.Josa(profile.ProgressTitle, "이", "가")} 초록 구간이면 [{profile.FinishLabel}]");

        Refresh();
        string what = string.Join(", ", solids.Select(s => s.Name));
        ShowIntro(steps, index,
            fry ? $"튀김기 기름 온도를 맞춰 {what}{SeasonRules.Josa(what, "을", "를")} 튀겨요. 노릇해지면 건지고, 너무 오래 두면 타요!"
                : profile.Mode == HeatMode.Simmer ? "냄비를 불에 올려 천천히 졸여요. 알맞은 농도가 되면 불을 끄고, 강불로 오래 두면 타요!"
                : $"냄비 물이 끓으면 불을 알맞게 맞춰요. {profile.ProgressTitle}{SeasonRules.Josa(profile.ProgressTitle, "이", "가")} 알맞을 때 [{profile.FinishLabel}]! 강불로 두면 넘쳐요.",
            preview => {
                PreviewArt(preview, DesignArt.Load(fry ? "Lab/LabFryer" : "Lab/LabPot"), 40, 30, 400, fry ? 289 : 297, false);
                if (solids.Count > 0) PreviewArt(preview, solids[0].Art, 150, 70, 180, 180);
            },
            fry ? "불 세기를 <color=#C8443A>중불</color>에 맞추기\n<color=#C8443A>→</color> 노릇해지면 <color=#1F7F8A>건지기</color>"
                : $"불 세기를 <color=#C8443A>중불</color>에 맞추기\n<color=#C8443A>→</color> 초록 구간에서\n<color=#1F7F8A>{profile.FinishLabel}</color>",
            $"제한 시간 {profile.Limit:0}초 · {profile.TempTitle} {profile.ZoneLow:0}~{profile.ZoneHigh:0}°C",
            fry ? "기름이 충분히\n달궈져야 바삭해!" : profile.Mode == HeatMode.Simmer ? "자주 들여다보면서\n천천히 졸이자!" : "끓어 넘치지\n않게 조심해!");
    }

    /// <summary>냄비·튀김기에 띄울 재료(최대 3개).</summary>
    public static List<LabItem> PotSolids(RecipeData recipe, ResearchStep[] steps, int index)
    {
        var items = LabItems.FromText(recipe, steps[index].text);
        var solids = items.Where(i => !i.Liquid && i.Art != null && !LabItems.IsSeasoning(i.Name)).Take(3).ToList();
        // "양념을 넣고 졸이기"처럼 레시피 재료 이름이 하나도 없으면 앞 단계에서 굽거나 넣은 재료를 이어서 끓인다.
        // "레몬과 꿀을 넣고 소스 졸이기"처럼 재료를 따로 끓이는 단계는 이어받지 않는다.
        var names = recipe != null ? recipe.Requirements.Select(r => r.IngredientName).ToList() : new List<string>();
        if (solids.Count == 0 && !items.Any(i => names.Contains(i.Name))) solids.AddRange(CarriedSolids(recipe, steps, index));
        // "물과 간장을 넣어 육수 끓이기"처럼 국물만 끓이는 단계는 재료를 띄우지 않는다.
        // "레몬과 꿀을 넣고 소스 졸이기"처럼 양념만 끓이는 단계도 비워 둔다.
        if (solids.Count == 0 && !items.Any(i => i.Liquid || names.Contains(i.Name))) solids.AddRange(LabItems.MainIngredients(recipe).Take(1));
        return solids;
    }

    private static readonly string[] CookingStations = { "Grill", "Fry", "Add", "Boil", "Simmer", "Stir" };
    /// <summary>앞 단계 중 불에 올렸거나 냄비에 넣은 재료(가까운 단계부터, 최대 3개).</summary>
    public static List<LabItem> CarriedSolids(RecipeData recipe, ResearchStep[] steps, int index)
    {
        var found = new List<LabItem>();
        for (int k = index - 1; k >= 0 && found.Count < 3; k--)
        {
            if (!CookingStations.Contains(LabStations.StationFor(steps[k]))) continue;
            foreach (var item in LabItems.FromText(recipe, steps[k].text))
                if (!item.Liquid && item.Art != null && !LabItems.IsSeasoning(item.Name) && found.Count < 3 && found.All(f => f.Name != item.Name))
                    found.Add(item);
        }
        return found;
    }

    private void Finish()
    {
        if (!running || finished) return;
        finished = true; running = false;
        float tempRatio = elapsed > 0f ? inZone / elapsed : 0f;
        float score = HeatRules.StepScore(profile, progress, spillPeak, tempRatio);
        finishButton.gameObject.SetActive(false);
        var chips = new List<string> { $"{profile.ProgressTitle} {progress * 100f:0}%" };
        if (profile.Mode == HeatMode.Fry) chips.Add($"온도 지킴 {tempRatio * 100f:0}%");
        else chips.Add(spillPeak > .3f ? (profile.Mode == HeatMode.Boil ? "넘쳤어요" : "조금 탔어요") : (profile.Mode == HeatMode.Boil ? "안 넘쳤어요" : "안 탔어요"));
        ShowResult(score, chips.ToArray(), HeatRules.ChefReview(profile, score, progress, spillPeak, tempRatio));
        Refresh();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (running && !finished)
        {
            temperature = HeatRules.StepTemperature(profile, temperature, heat, dt);
            progress += HeatRules.ProgressRate(profile, temperature, heat) * dt;
            spill = Mathf.Clamp01(spill + HeatRules.SpillRate(profile, temperature, heat, progress) * dt);
            spillPeak = Mathf.Max(spillPeak, spill);
            elapsed += dt;
            if (HeatRules.InTempZone(profile, temperature)) inZone += dt;
            if (elapsed >= profile.Limit) Finish();
        }
        Refresh();
    }

    private void Refresh()
    {
        if (tempGauge == null) return;
        float t = Time.unscaledTime, wobble = Mathf.Sin(t * 9f) * .5f + .5f;
        bool hot = temperature >= profile.ZoneLow - 4f;
        glow.color = new Color(1, 1, 1, .2f + heat * .35f);
        bubbles.color = new Color(1, 1, 1, hot ? Mathf.Lerp(.45f, 1f, wobble) : 0f);
        steam.color = new Color(1, 1, 1, profile.Mode == HeatMode.Fry ? 0f : Mathf.Clamp01((temperature - profile.ZoneLow + 20f) / 20f) * Mathf.Lerp(.4f, .85f, wobble));
        pop.gameObject.SetActive(running && hot);
        for (int i = 0; i < floaters.Count; i++)
        {
            var p = floaters[i].anchoredPosition;
            floaters[i].localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 1.3f + i) * (hot ? 6f : 2f));
            floaters[i].localScale = Vector3.one * (1f + (hot ? Mathf.Sin(t * 5f + i * 2f) * .02f : 0f));
            // 익을수록 색이 진해지고, 너무 오래 두면 어두워진다.
            float cook = Mathf.Clamp01(progress / HeatRules.DoneHigh), over = Mathf.Clamp01((progress - HeatRules.DoneHigh) / .3f);
            Color done = profile.Mode == HeatMode.Fry ? new Color(1f, .78f, .45f) : new Color(1f, .9f, .85f);
            floaterArt[i].color = Color.Lerp(Color.Lerp(coatTint, done, cook), new Color(.45f, .3f, .2f), over);
        }
        if (water != null && profile.Mode == HeatMode.Simmer)
            water.color = Color.Lerp(liquid, liquidDone, Mathf.Clamp01(progress));
        if (burnt.gameObject.activeSelf) burnt.color = new Color(.15f, .08f, .04f, spill * .55f);
        if (foam.gameObject.activeSelf) foam.color = new Color(1, 1, 1, spill * .8f);

        bool tempGood = HeatRules.InTempZone(profile, temperature);
        SetGauge(tempGauge, Mathf.InverseLerp(profile.GaugeMin, profile.GaugeMax, temperature), $"{temperature:0}°C", tempGood,
            profile.Mode == HeatMode.Fry ? temperature > profile.ZoneHigh + 8f : temperature > profile.ZoneHigh + .5f);
        SetGauge(progressGauge, progress / 1.3f, progress > 1.3f ? "너무 익었어요!" : $"{progress * 100f:0}%",
            progress >= HeatRules.DoneLow && progress <= HeatRules.DoneHigh, progress > HeatRules.DoneHigh);
        if (spillGauge != null) SetGauge(spillGauge, spill, spill > .3f ? (profile.Mode == HeatMode.Boil ? "넘쳐요!" : "타요!") : spill > .05f ? "조심!" : "괜찮아요", false, spill > .3f);
        SetTimer(profile.Limit - elapsed, profile.Limit);
        RefreshHeatKnob();
        chefLine.text = finished ? "다 됐다!" : running ? HeatRules.ChefHint(profile, temperature, progress, spill, heat) : "준비되면\n[시작]을 눌러!";
        finishButton.interactable = running && !finished;
    }
}
