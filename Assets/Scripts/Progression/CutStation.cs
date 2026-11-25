using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손질·썰기 점수 규칙. 재료 위 점선을 따라 칼을 한 번에 쓱 긋는다.
/// 한 줄 점수 = 5 - 기운 각도/5° - 줄에서 벗어난 거리/14px, 단계 점수 = 줄 평균 - 헛칼질 × 0.2.
/// </summary>
public static class CutRules
{
    public const float Span = 380f, LineLength = 400f, MinSwipe = 170f, MatchAngle = 35f, MatchDistance = 90f;
    /// <summary>점선 하나: 재료 가운데에서 수직 방향으로 offset만큼, 세로에서 angle도 기운 줄.</summary>
    public struct Guide { public float Offset, Angle; public Guide(float o, float a) { Offset = o; Angle = a; } }

    public static List<Guide> Guides(string text)
    {
        text ??= "";
        var list = new List<Guide>();
        if (text.Contains("다지"))
        {
            foreach (float o in new[] { -110f, 0f, 110f }) list.Add(new Guide(o, 0f));
            foreach (float o in new[] { -90f, 0f, 90f }) list.Add(new Guide(o, 90f));
            return list;
        }
        if (text.Contains("칼집")) { foreach (float o in new[] { -110f, 0f, 110f }) list.Add(new Guide(o, 30f)); return list; }
        int n = text.Contains("얇게") ? 6 : text.Contains("회로") || text.EndsWith("회 손질하기") ? 5 : text.Contains("먹기 좋은") || text.Contains("썰") ? 4 : text.Contains("토막") ? 3 : 2;
        for (int i = 0; i < n; i++) list.Add(new Guide(-Span * .5f + Span * (i + .5f) / n, 0f));
        return list;
    }
    /// <summary>여러 재료를 손질하면 재료마다 줄을 줄인다(하나에 2줄).</summary>
    public static List<Guide> GuidesFor(string text, int items) => items > 1 && !text.Contains("다지") && !text.Contains("칼집")
        ? new List<Guide> { new Guide(-70f, 0f), new Guide(70f, 0f) } : Guides(text);
    public static float TimeLimit(int lines) => 6f + 3f * lines;

    public static Vector2 Direction(float angle) { float a = angle * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)); }
    public static Vector2 Normal(float angle) { float a = angle * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), -Mathf.Sin(a)); }
    public static Vector2 Center(Vector2 item, Guide g) => item + Normal(g.Angle) * g.Offset;

    /// <summary>긋기(왼쪽 위 기준 좌표)가 점선과 얼마나 기울었고 얼마나 떨어졌는지.</summary>
    public static void Measure(Vector2 from, Vector2 to, Vector2 item, Guide g, out float angle, out float distance, out float along)
    {
        var v = to - from;
        var d = Direction(g.Angle);
        along = Mathf.Abs(Vector2.Dot(v, d));
        angle = v.sqrMagnitude < 1f ? 90f : Mathf.Acos(Mathf.Clamp01(along / v.magnitude)) * Mathf.Rad2Deg;
        distance = Mathf.Abs(Vector2.Dot((from + to) * .5f - Center(item, g), Normal(g.Angle)));
    }
    public static float CutScore(float angle, float distance) => Mathf.Clamp(5f - angle / 5f - distance / 14f, 0f, 5f);
    public static float Score(IList<float> cuts, int lines, int misses) =>
        lines <= 0 ? 0f : Mathf.Clamp(cuts.Sum() / lines - misses * .2f, 0f, 5f);

    public static string ChefHint(int done, int lines, float last, bool held)
    {
        if (done >= lines) return "손질 끝!";
        if (held) return "끝까지\n쓱 그어!";
        if (done == 0) return "점선을 따라\n한 번에 쓱!";
        if (last >= 4.5f) return "깔끔해!\n다음 줄!";
        if (last >= 3f) return "좋아,\n조금 더 반듯하게!";
        return "점선에 맞춰\n똑바로 그어 봐!";
    }
    public static string ChefReview(float score, int done, int lines, int misses)
    {
        if (score >= 4.5f) return "좋아! 칼질이 반듯해서 모양이 깔끔하게 나왔어!";
        if (done < lines) return "시간 안에 다 못 썰었어. 점선마다 한 번에 쓱 그어 봐.";
        if (misses >= 3) return "헛칼질이 많았어. 점선 위에서 시작해서 끝까지 그어 봐.";
        return "잘했어! 점선에 조금 더 똑바로 맞추면 완벽해.";
    }
}

/// <summary>연구 실습의 손질·썰기 화면. 도마 위 재료의 점선을 따라 칼을 긋는다.</summary>
public class CutStation : LabStation
{
    private static readonly Vector2 ItemCenter = new Vector2(960, 555);
    private const float ItemSize = 440f;

    private List<LabItem> items;
    private List<CutRules.Guide> guides;
    private readonly List<float> cuts = new List<float>();
    private readonly List<Image> guideArt = new List<Image>(), marks = new List<Image>();
    private readonly List<RectTransform> doneItems = new List<RectTransform>();
    private int itemIndex, lineIndex, lines, misses;
    private float elapsed, limit, last = -1f;
    private LabPointer pad;
    private Image itemArt, trail;
    private RectTransform knife;
    private Vector2 swipeFrom;
    private bool swiping;
    private Gauge countGauge, aimGauge;
    private TMP_Text pop, itemTag;
    private float popTimer;

    public static CutStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var station = Create<CutStation>("CutStage", screen);
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        string text = steps[index].text;
        items = LabItems.FromText(recipe, text).Where(i => !i.Liquid && i.Art != null).Take(3).ToList();
        if (items.Count == 0) items = LabItems.MainIngredients(recipe).Take(1).ToList();
        if (items.Count == 0) items.Add(new LabItem { Name = "재료" });
        guides = CutRules.GuidesFor(text, items.Count);
        lines = guides.Count * items.Count;
        limit = CutRules.TimeLimit(lines);
        BuildStage(recipe, onDone);

        Art("Lab/LabBoard", 480, 262, 960, 585, true);
        itemArt = Art(null, ItemCenter.x - ItemSize / 2, ItemCenter.y - ItemSize / 2, ItemSize, ItemSize);
        itemArt.preserveAspect = true;
        foreach (var g in guides)
        {
            var c = CutRules.Center(ItemCenter, g);
            var line = Art("Lab/LabLine", c.x - 20, c.y - CutRules.LineLength / 2, 40, CutRules.LineLength);
            line.rectTransform.localEulerAngles = new Vector3(0, 0, g.Angle);
            guideArt.Add(line);
            var mark = Art(null, c.x - 5, c.y - 180, 10, 360);
            mark.color = new Color(DayLoopUI.Kit.Line.r, DayLoopUI.Kit.Line.g, DayLoopUI.Kit.Line.b, .75f);
            mark.rectTransform.localEulerAngles = new Vector3(0, 0, g.Angle);
            marks.Add(mark);
        }
        // 손질을 마친 재료는 도마 오른쪽 접시 자리로 옮긴다.
        for (int i = 0; i < items.Count; i++)
        {
            var art = Art(null, 1265, 400 + i * 160, 150, 150);
            art.preserveAspect = true; art.sprite = items[i].Art; art.color = new Color(1, 1, 1, 0f);
            doneItems.Add(art.rectTransform);
        }
        var tag = DayLoopUI.Card(frame, DayLoopUI.Kit.Sea, "ItemTag", 3f, 0f);
        TopLeft(tag.rectTransform, 820, 795, 280, 52);
        itemTag = DayLoopUI.Text(tag.transform, "", 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(itemTag.rectTransform);
        tag.gameObject.SetActive(items.Count > 1);

        trail = Art(null, 0, 0, 10, 10);
        trail.color = new Color(1f, 1f, 1f, .7f);
        trail.gameObject.SetActive(false);
        pad = AddPad(480, 225, 960, 660);
        pad.Down += () => { if (running && !finished && PointerOnFrame(pad, out var p)) { swipeFrom = p; swiping = true; } };
        pad.Up += EndSwipe;
        knife = Art("Lab/LabKnife", 1180, 760, 420, 120).rectTransform;
        knife.localEulerAngles = new Vector3(0, 0, 35);
        pop = Pop("싹둑!", 60, 1150, 330);
        pop.gameObject.SetActive(false);

        BuildHud(recipe, steps, index, onExit);
        var gauges = BuildGaugeCard(1460, 150,
            ("썬 줄", 1f, 1f, "점선마다 한 번씩"),
            ("칼질 솜씨", .7f, 1f, "똑바로, 점선 위에"));
        countGauge = gauges[0]; aimGauge = gauges[1];
        BuildChef(1560, 470, 330, 1275, 530);
        BuildInstruction("점선 위를 <color=#C8443A>누른 채</color> 한 번에 쓱 긋기 <color=#C8443A>→</color> 줄마다 반듯하게");
        ShowItem();
        Refresh();

        string names = string.Join(", ", items.Select(i => i.Name));
        ShowIntro(steps, index,
            $"도마 위 {names}{SeasonRules.Josa(items.Last().Name, "을", "를")} 점선을 따라 썰어요. 점선마다 칼을 한 번에 쓱! 비뚤거나 줄에서 벗어나면 감점이에요.",
            preview => {
                PreviewArt(preview, DesignArt.Load("Lab/LabBoard"), 20, 40, 400, 244, false);
                PreviewArt(preview, items[0].Art, 110, 30, 230, 230);
                PreviewArt(preview, DesignArt.Load("Lab/LabLine"), 215, 40, 20, 200, false);
                var k = PreviewArt(preview, DesignArt.Load("Lab/LabKnife"), 250, 150, 240, 69);
                k.rectTransform.localEulerAngles = new Vector3(0, 0, 35);
            },
            "점선 위를 <color=#C8443A>누른 채</color>\n위에서 아래로 쓱!\n<color=#C8443A>→</color> <color=#1F7F8A>반듯하게</color> 썰기",
            $"제한 시간 {limit:0}초 · 줄 {lines}개",
            "손 조심!\n한 번에 쓱~");
    }

    private void ShowItem()
    {
        var item = items[Mathf.Min(itemIndex, items.Count - 1)];
        itemArt.sprite = item.Art; itemArt.enabled = item.Art != null;
        itemTag.text = $"{item.Name} ({Mathf.Min(itemIndex + 1, items.Count)}/{items.Count})";
        for (int i = 0; i < guides.Count; i++) { guideArt[i].gameObject.SetActive(true); marks[i].gameObject.SetActive(false); }
        lineIndex = 0;
        HighlightNext();
    }

    private void HighlightNext()
    {
        for (int i = 0; i < guides.Count; i++)
            guideArt[i].color = !guideArt[i].gameObject.activeSelf ? Color.white : i == NextGuide() ? Color.white : new Color(1, 1, 1, .45f);
    }
    private int NextGuide()
    {
        for (int i = 0; i < guides.Count; i++) if (guideArt[i].gameObject.activeSelf) return i;
        return -1;
    }

    private void EndSwipe()
    {
        trail.gameObject.SetActive(false);
        if (!swiping || !running || finished) { swiping = false; return; }
        swiping = false;
        if (!PointerOnFrame(pad, out var to)) return;
        if ((to - swipeFrom).magnitude < CutRules.MinSwipe * .5f) return; // 톡 누른 것은 세지 않는다.
        int best = -1; float bestAngle = 0, bestDistance = float.MaxValue;
        for (int i = 0; i < guides.Count; i++)
        {
            if (!guideArt[i].gameObject.activeSelf) continue;
            CutRules.Measure(swipeFrom, to, ItemCenter, guides[i], out float angle, out float distance, out float along);
            if (angle > CutRules.MatchAngle || distance > CutRules.MatchDistance || along < CutRules.MinSwipe) continue;
            if (distance < bestDistance) { best = i; bestAngle = angle; bestDistance = distance; }
        }
        if (best < 0)
        {
            misses++;
            ShowPop("헛칼질!", DayLoopUI.Kit.Alert);
            Refresh();
            return;
        }
        last = CutRules.CutScore(bestAngle, bestDistance);
        cuts.Add(last);
        guideArt[best].gameObject.SetActive(false);
        marks[best].gameObject.SetActive(true);
        lineIndex++;
        ShowPop(last >= 4.5f ? "싹둑!" : last >= 3f ? "쓱!" : "삐뚤…", last >= 3f ? Color.white : DayLoopUI.Kit.Coin);
        if (lineIndex >= guides.Count)
        {
            doneItems[itemIndex].GetComponent<Image>().color = Color.white;
            itemIndex++;
            if (itemIndex >= items.Count) { Finish(); return; }
            ShowItem();
        }
        HighlightNext();
        Refresh();
    }

    private void ShowPop(string text, Color color)
    {
        pop.text = text; pop.color = color;
        pop.gameObject.SetActive(true); popTimer = .7f;
    }

    private void Finish()
    {
        if (finished) return;
        finished = true; running = false;
        float score = CutRules.Score(cuts, lines, misses);
        float aim = cuts.Count > 0 ? cuts.Average() : 0f;
        ShowResult(score, new[] { $"썬 줄 {cuts.Count}/{lines}", $"칼질 솜씨 {aim / 5f * 100f:0}%", $"헛칼질 {misses}번" },
            CutRules.ChefReview(score, cuts.Count, lines, misses));
        Refresh();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (pad != null && pad.Held && PointerOnFrame(pad, out var p))
        {
            MoveTo(knife, p + new Vector2(150f, -30f));
            knife.localEulerAngles = new Vector3(0, 0, 20);
            if (swiping)
            {
                var v = p - swipeFrom;
                trail.gameObject.SetActive(v.magnitude > 10f);
                MoveTo(trail.rectTransform, (swipeFrom + p) * .5f);
                trail.rectTransform.sizeDelta = new Vector2(10f, v.magnitude);
                trail.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg);
            }
        }
        if (popTimer > 0f) { popTimer -= dt; if (popTimer <= 0f) pop.gameObject.SetActive(false); }
        if (running && !finished)
        {
            elapsed += dt;
            if (elapsed >= limit) Finish();
        }
        Refresh();
    }

    private void Refresh()
    {
        if (countGauge == null) return;
        SetGauge(countGauge, lines > 0 ? cuts.Count / (float)lines : 0f, $"{cuts.Count} / {lines}", cuts.Count >= lines, false);
        float aim = cuts.Count > 0 ? cuts.Average() / 5f : 0f;
        SetGauge(aimGauge, aim, cuts.Count > 0 ? $"{aim * 100f:0}%" : "-", aim >= .7f, cuts.Count > 0 && aim < .5f);
        SetTimer(limit - elapsed, limit);
        chefLine.text = finished ? "손질 끝!" : running ? CutRules.ChefHint(cuts.Count, lines, last, swiping) : "준비되면\n[시작]을 눌러!";
    }
}
