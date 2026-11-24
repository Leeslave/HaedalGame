using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 젓기·볶기·빚기 점수 규칙. 누른 채 원을 그리며 돌린 바퀴 수가 진행이고,
/// 1초에 0.6~1.4바퀴(알맞은 속도)로 돌린 시간 비율이 솜씨다.
/// 점수 = 진행 60% + 알맞은 속도 비율 40%.
/// </summary>
public static class StirRules
{
    public const float SpeedLow = .6f, SpeedHigh = 1.4f, SpeedMax = 2.5f;
    public const int StirTurns = 8, TurnsPerBall = 3, Balls = 5;
    public static float Need(bool shape) => shape ? TurnsPerBall * Balls : StirTurns;
    public static float TimeLimit(bool shape) => shape ? 32f : 25f;
    public static bool GoodSpeed(float speed) => speed >= SpeedLow && speed <= SpeedHigh;
    public static float Score(float turns, float need, float goodRatio) =>
        Mathf.Clamp(Mathf.Clamp01(turns / need) * 5f * .6f + Mathf.Clamp01(goodRatio) * 5f * .4f, 0f, 5f);
    public static string SpeedLabel(float speed, bool shape) =>
        speed < .05f ? "멈춤" : speed < SpeedLow ? "느려요" : speed <= SpeedHigh ? "딱 좋아요" : shape ? "너무 빨라요!" : "튀어요!";
    public static string ChefHint(float turns, float need, float speed, bool held, bool shape)
    {
        if (turns >= need) return shape ? "완자 완성!" : "다 섞였어!";
        if (!held) return shape ? "반죽을 누른 채\n동글동글 돌려!" : "누른 채로\n빙글빙글 저어 봐!";
        if (speed > SpeedHigh) return shape ? "너무 빨라!\n모양이 망가져." : "너무 빨라!\n다 튀겠어.";
        if (speed < SpeedLow) return shape ? "조금 더\n빠르게 돌려!" : "너무 느려!\n눌어붙겠어.";
        return turns / need > .5f ? "좋아, 반 넘었어!\n그대로 계속!" : "좋아, 그 속도야!";
    }
    public static string ChefReview(float score, float turns, float need, float goodRatio, bool shape)
    {
        if (score >= 4.5f) return shape ? "좋아! 완자가 동글동글 예쁘게 빚어졌어!" : "좋아! 덩어리 하나 없이 부드럽게 섞였어!";
        if (turns < need) return shape ? "시간 안에 완자를 다 못 빚었어. 반죽 위에서 계속 원을 그려 봐." : "덜 저었어. 시간 안에 게이지를 끝까지 채워 봐.";
        if (goodRatio < .6f) return "속도가 들쭉날쭉했어. 1초에 한 바퀴쯤 일정하게 돌려 봐.";
        return "잘했어! 조금만 더 일정하게 돌리면 완벽해.";
    }
}

/// <summary>
/// 연구 실습의 젓기·볶기(냄비·팬을 숟가락으로 젓기)와 빚기(반죽을 둥글게 굴려 완자 만들기) 화면.
/// </summary>
public class StirStation : LabStation
{
    private bool shape, pan;
    private Vector2 center;
    private float minRadius, maxRadius, need, limit, elapsed, heldTime, goodTime;
    private readonly CircleTracker tracker = new CircleTracker();
    private LabPointer pad;
    private RectTransform tool, dough;
    private Image liquid;
    private readonly List<Image> made = new List<Image>();
    private readonly List<RectTransform> floaters = new List<RectTransform>();
    private Gauge progressGauge, speedGauge;
    private TMP_Text pop;

    public static StirStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit, bool shape)
    {
        var station = Create<StirStation>("StirStage", screen);
        station.shape = shape;
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        string text = steps[index].text;
        need = StirRules.Need(shape);
        limit = StirRules.TimeLimit(shape);
        var items = LabItems.FromText(recipe, text);
        BuildStage(recipe, onDone, items.Select(i => i.Art).Where(a => a != null).Select(a => a.name.Replace("Design ", "")).Where(a => !a.StartsWith("Lab/")).ToArray());

        if (shape)
        {
            Art("Lab/LabBoard", 480, 262, 960, 585, true);
            center = new Vector2(1000, 555); minRadius = 25f; maxRadius = 280f;
            dough = Art("Lab/LabDough", center.x - 170, center.y - 170, 340, 340).rectTransform;
            // 빚은 완자가 놓일 자리(도마 왼쪽).
            for (int i = 0; i < StirRules.Balls; i++)
            {
                var ball = Art("Lab/LabBall", 560, 300 + i * 105, 96, 96);
                ball.color = new Color(1, 1, 1, .18f);
                made.Add(ball);
            }
        }
        else
        {
            pan = text.Contains("프라이팬") || (text.Contains("볶") && !text.Contains("냄비"));
            Art("Lab/LabPanGlow", 510, 200, 900, 800).color = new Color(1, 1, 1, .45f);
            if (pan) { Art("Lab/LabPan", 450, 342, 1140, 690, true); center = new Vector2(855, 700); maxRadius = 300f; }
            else { Art("Lab/LabPot", 510, 260, 900, 668, true); center = new Vector2(960, 597); maxRadius = 290f; }
            minRadius = 40f;
            var solids = items.Where(i => !i.Liquid && i.Art != null).ToList();
            if (!pan)
            {
                liquid = Art("Lab/LabWater", center.x - 301, center.y - 260, 602, 520, true);
                bool roux = text.Contains("루") || items.Any(i => i.Name == "밀가루");
                liquid.color = roux ? new Color32(0xE8, 0xC9, 0x8E, 0xFF) : text.Contains("레몬") ? new Color32(0xF4, 0xD9, 0x6A, 0xFF) : new Color32(0xE9, 0xD7, 0xB0, 0xFF);
                solids = solids.Where(s => s.Name != "밀가루" && s.Name != "식용유").ToList();
            }
            if (solids.Count == 0 && pan) solids.AddRange(LabItems.MainIngredients(recipe).Take(3));
            for (int i = 0; i < solids.Count && i < 4; i++)
            {
                float a = i * Mathf.PI * 2f / Mathf.Max(1, solids.Count) + .3f;
                var at = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .7f) * (solids.Count == 1 ? 0f : 140f);
                var art = Art(null, at.x - 90, at.y - 90, 180, 180);
                art.sprite = solids[i].Art; art.preserveAspect = true;
                floaters.Add(art.rectTransform);
            }
        }
        pad = AddPad(480, 225, 960, 760);
        pad.Up += tracker.Release;
        tool = Art(shape ? null : "Lab/LabSpoon", center.x - 210, center.y - 60, 420, 120).rectTransform;
        tool.gameObject.SetActive(!shape);
        pop = shape ? Pop("동글동글~", 56, 820, 280) : Pop("휘휘~", 56, 1130, 330);

        BuildHud(recipe, steps, index, onExit);
        var gauges = BuildGaugeCard(1460, 150,
            (shape ? "빚은 완자" : text.Contains("볶") ? "고루 볶은 정도" : "고르게 섞인 정도", 1f, 1f, shape ? $"완자 {StirRules.Balls}개 빚기" : "끝까지 채우면 완성"),
            ("돌리는 속도", StirRules.SpeedLow / StirRules.SpeedMax, StirRules.SpeedHigh / StirRules.SpeedMax, "1초에 한 바퀴쯤 일정하게"));
        progressGauge = gauges[0]; speedGauge = gauges[1];
        BuildChef(1560, 470, 330, 1275, 530);
        string arrow = "<color=#C8443A>→</color>";
        BuildInstruction(shape ? $"반죽을 누른 채 동글동글 원 그리기 {arrow} {StirRules.TurnsPerBall}바퀴마다 완자 하나"
            : $"{(pan ? "팬" : "냄비")} 안을 누른 채 빙글빙글 원 그리기 {arrow} 게이지 끝까지 채우기");

        Refresh();
        ShowIntro(steps, index,
            shape ? $"반죽을 손바닥으로 굴리듯 원을 그려 완자를 빚어요. {StirRules.TurnsPerBall}바퀴마다 완자가 하나씩, {StirRules.Balls}개를 빚어요."
                : "숟가락으로 원을 그리며 저어요. 너무 빠르면 튀고, 너무 느리면 눌어붙어요. 일정한 속도가 중요해요!",
            preview => {
                if (shape) { PreviewArt(preview, DesignArt.Load("Lab/LabBoard"), 20, 40, 400, 244, false); PreviewArt(preview, DesignArt.Load("Lab/LabDough"), 140, 70, 170, 170); }
                else { PreviewArt(preview, DesignArt.Load(pan ? "Lab/LabPan" : "Lab/LabPot"), 40, 30, 400, pan ? 242 : 297, false); PreviewArt(preview, DesignArt.Load("Lab/LabSpoon"), 160, 120, 260, 74); }
            },
            (shape ? "반죽을 <color=#C8443A>누른 채</color>\n" : "<color=#C8443A>누른 채</color> ") + "빙글빙글 원 그리기\n<color=#C8443A>→</color> 속도는 <color=#1F7F8A>1초에 한 바퀴</color>",
            $"제한 시간 {limit:0}초 · " + (shape ? $"완자 {StirRules.Balls}개" : $"{StirRules.StirTurns}바퀴 젓기"),
            shape ? "힘을 빼고\n살살 굴려 봐!" : "바닥까지\n싹싹 저어 줘!");
    }

    private void Finish()
    {
        if (finished) return;
        finished = true; running = false;
        float good = heldTime > 0f ? goodTime / heldTime : 0f;
        float score = StirRules.Score(tracker.Turns, need, good);
        var chips = shape
            ? new[] { $"완자 {Mathf.Min(StirRules.Balls, Mathf.FloorToInt(tracker.Turns / StirRules.TurnsPerBall))}/{StirRules.Balls}개", $"알맞은 속도 {good * 100f:0}%" }
            : new[] { $"섞인 정도 {Mathf.Clamp01(tracker.Turns / need) * 100f:0}%", $"알맞은 속도 {good * 100f:0}%" };
        ShowResult(score, chips, StirRules.ChefReview(score, tracker.Turns, need, good, shape));
        Refresh();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        bool moving = false;
        if (pad != null && pad.Held && PointerOnFrame(pad, out var p))
        {
            if (!shape)
            {
                // 숟가락은 손가락 자리에서 바깥쪽으로 누워 있다.
                var dir = p - center;
                float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
                MoveTo(tool, p + dir.normalized * 150f);
                tool.localEulerAngles = new Vector3(0, 0, angle + 180f);
            }
            if (running && !finished)
            {
                float turned = tracker.Feed(p - center, dt, minRadius, maxRadius);
                moving = turned > 0f || tracker.Speed > .05f;
                if (moving)
                {
                    heldTime += dt;
                    if (StirRules.GoodSpeed(tracker.Speed)) goodTime += dt;
                }
            }
        }
        else tracker.Decay(dt);
        pop.gameObject.SetActive(moving);
        if (running && !finished)
        {
            elapsed += dt;
            if (tracker.Turns >= need || elapsed >= limit) Finish();
        }
        Refresh();
    }

    private void Refresh()
    {
        if (progressGauge == null) return;
        float ratio = Mathf.Clamp01(tracker.Turns / need);
        if (shape)
        {
            int balls = Mathf.Min(StirRules.Balls, Mathf.FloorToInt(tracker.Turns / StirRules.TurnsPerBall));
            for (int i = 0; i < made.Count; i++) made[i].color = new Color(1, 1, 1, i < balls ? 1f : .18f);
            float left = 1f - (balls >= StirRules.Balls ? 1f : balls / (float)StirRules.Balls) * .6f;
            dough.localScale = Vector3.one * left;
            dough.localEulerAngles = new Vector3(0, 0, -tracker.Turns * 360f * .2f);
            SetGauge(progressGauge, ratio, $"{balls} / {StirRules.Balls}개", balls >= StirRules.Balls, false);
        }
        else
        {
            SetGauge(progressGauge, ratio, $"{ratio * 100f:0}%", ratio >= 1f, false);
            for (int i = 0; i < floaters.Count; i++)
            {
                // 저은 만큼 재료가 냄비 안을 돈다.
                float a = i * Mathf.PI * 2f / floaters.Count + .3f + tracker.Turns * Mathf.PI * 2f * .5f;
                float r = floaters.Count == 1 ? 0f : 140f;
                MoveTo(floaters[i], center + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .7f) * r);
                floaters[i].localEulerAngles = new Vector3(0, 0, tracker.Turns * 90f + i * 40f);
            }
        }
        float speed = tracker.Speed;
        SetGauge(speedGauge, speed / StirRules.SpeedMax, StirRules.SpeedLabel(speed, shape), StirRules.GoodSpeed(speed), speed > StirRules.SpeedHigh);
        SetTimer(limit - elapsed, limit);
        chefLine.text = finished ? (shape ? "완자 완성!" : "다 저었다!") : running ? StirRules.ChefHint(tracker.Turns, need, speed, pad != null && pad.Held, shape) : "준비되면\n[시작]을 눌러!";
    }
}
