using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DropMode { Add, Sauce, Plate }

/// <summary>
/// 넣기·양념 섞기·담기 점수 규칙. 재료를 끌어 냄비·그릇·접시에 놓는다.
/// 넣기·섞기는 글에 나온 순서가 맞아야 하고, 담기는 정해진 자리에 가깝게 놓아야 한다.
/// </summary>
public static class DropRules
{
    public const float SlotRadius = 120f, StirTurns = 4f;
    private static readonly string[] SauceResults = { "양념", "소스", "된장소스", "레몬소스", "육수" };

    public static DropMode Mode(string station) => station == "Sauce" ? DropMode.Sauce : station == "Plate" ? DropMode.Plate : DropMode.Add;
    /// <summary>섞어서 만드는 결과물(양념·소스)은 넣을 재료가 아니다.</summary>
    public static bool IsResult(DropMode mode, LabItem item) => mode == DropMode.Sauce && SauceResults.Contains(item.Name);
    public static int Copies(LabItem item) => item.Name == "완자" ? 5 : 1;
    /// <summary>다음에 넣을 차례인지. 같은 묶음(해산물)끼리는 아무 순서나 된다.</summary>
    public static bool InOrder(IList<string> order, int next, string name, IDictionary<string, int> groups)
    {
        if (next >= order.Count) return false;
        if (order[next] == name) return true;
        int g = groups != null && groups.TryGetValue(name, out var a) ? a : -1;
        int want = groups != null && groups.TryGetValue(order[next], out var b) ? b : -1;
        return g >= 0 && g == want && order.Skip(next).Contains(name);
    }
    public static float TimeLimit(DropMode mode, int count) => 8f + count * 4f + (mode == DropMode.Sauce ? 10f : 0f);

    /// <summary>담기 한 개 점수: 자리 가운데에서 25px 안은 5점, 바깥으로 갈수록 깎인다.</summary>
    public static float PlaceScore(float distance) => Mathf.Clamp(5f - Mathf.Max(0f, distance - 25f) / 20f, 0f, 5f);
    /// <summary>
    /// 넣기·섞기 점수 = 넣은 비율 × 5 - 순서 틀림 × 0.8. 섞기는 여기에 젓기 점수를 40% 섞는다.
    /// </summary>
    public static float OrderScore(int placed, int total, int mistakes) =>
        total <= 0 ? 0f : Mathf.Clamp(5f * placed / total - mistakes * .8f, 0f, 5f);
    public static float SauceScore(float order, float turns, float goodRatio) =>
        Mathf.Clamp(order * .6f + StirRules.Score(turns, StirTurns, goodRatio) * .4f, 0f, 5f);
    public static float PlateScore(IList<float> places, int total, int mistakes) =>
        total <= 0 ? 0f : Mathf.Clamp(places.Sum() / total - mistakes * .5f, 0f, 5f);

    public static string ChefReview(DropMode mode, float score, int placed, int total, int mistakes, bool underMixed = false)
    {
        if (score >= 4.5f) return mode == DropMode.Plate ? "좋아! 자리에 딱 맞게 예쁘게 담았어!" : mode == DropMode.Sauce ? "좋아! 순서대로 넣고 골고루 잘 섞었어!" : "좋아! 순서대로 딱딱 넣었어!";
        if (placed < total) return "시간 안에 다 못 넣었어. 재료를 끌어서 놓아 봐.";
        if (underMixed) return "덜 섞였어. 다 넣은 뒤 그릇 안을 빙글빙글 끝까지 저어 봐.";
        if (mistakes > 0) return mode == DropMode.Plate ? "자리를 헷갈렸어. 접시에 적힌 이름을 잘 봐." : "순서가 틀렸어. 아래 안내 줄의 번호 순서대로 넣어 봐.";
        return mode == DropMode.Plate ? "잘했어! 자리 가운데에 조금 더 맞추면 완벽해." : "잘했어! 조금 더 골고루 섞으면 완벽해.";
    }
}

/// <summary>
/// 연구 실습의 넣기(냄비·팬에 순서대로), 양념 섞기(그릇에 넣고 젓기), 담기(접시 자리에 놓기) 화면.
/// </summary>
public class DropStation : LabStation
{
    private class Piece { public LabItem Item; public RectTransform Art; public Vector2 Home; public bool Placed; public int Slot = -1; }

    private DropMode mode;
    private bool pan, plateTarget;
    private Vector2 center;
    private float radius, elapsed, limit, heldTime, goodTime, last = -1f;
    private readonly List<Piece> pieces = new List<Piece>();
    private readonly List<string> order = new List<string>();
    private readonly Dictionary<string, int> groups = new Dictionary<string, int>();
    private readonly List<(Vector2 at, string name, Image ring)> slots = new List<(Vector2, string, Image)>();
    private readonly List<float> places = new List<float>();
    private int next, mistakes;
    private Piece dragging;
    private LabPointer pad;
    private Image liquid;
    private RectTransform spoon;
    private readonly CircleTracker tracker = new CircleTracker();
    private bool stirring;
    private Gauge countGauge, secondGauge;
    private TMP_Text pop, guide;
    private float popTimer;
    private Color liquidFrom = new Color(1, 1, 1, 0), liquidTo;

    public static DropStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var station = Create<DropStation>("DropStage", screen);
        station.mode = DropRules.Mode(LabStations.StationFor(steps[index]));
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        string text = steps[index].text;
        var items = LabItems.FromText(recipe, text, mode == DropMode.Plate).Where(i => !DropRules.IsResult(mode, i)).Take(5).ToList();
        if (mode == DropMode.Plate && items.Count <= 1 && (text.Contains("한 접시") || text.Contains("종류")))
            items = LabItems.MainIngredients(recipe).Take(3).ToList();
        if (items.Count == 0) items.Add(new LabItem { Name = "재료" });
        BuildStage(recipe, onDone);

        // 받는 그릇
        bool garnish = mode == DropMode.Plate && (text.Contains("곁들") || text.Contains("마무리"));
        plateTarget = mode == DropMode.Plate || text.Contains("구운");
        if (plateTarget)
        {
            Art("Lab/LabPlate", 510, 300, 900, 600, true);
            center = new Vector2(960, 590); radius = 380f;
            Sprite main = null;
            if (garnish) main = DishArt(recipe);
            else if (mode == DropMode.Add) { main = items[0].Art; items.RemoveAt(0); if (items.Count == 0) items.Add(new LabItem { Name = "소스", Art = DesignArt.Load("Ingredient/SoySauce"), Liquid = true }); }
            if (main != null) { var dish = Art(null, center.x - 210, center.y - 200, 420, 380); dish.sprite = main; dish.preserveAspect = true; }
        }
        else if (mode == DropMode.Sauce)
        {
            Art("Lab/LabBowl", 570, 225, 780, 660, true);
            center = new Vector2(960, 561); radius = 300f;
        }
        else
        {
            pan = text.Contains("프라이팬") || (index > 0 && steps[index - 1].kind == "Grill" && !text.Contains("냄비"));
            if (pan) { Art("Lab/LabPanGlow", 405, 308, 900, 800).color = new Color(1, 1, 1, .45f); Art("Lab/LabPan", 450, 342, 1140, 690, true); center = new Vector2(855, 700); radius = 300f; }
            else { Art("Lab/LabPot", 510, 260, 900, 668, true); center = new Vector2(960, 597); radius = 300f; }
        }
        if (!pan && !plateTarget)
        {
            liquid = Art("Lab/LabWater", center.x - 301, center.y - 235, 602, 470, true);
            // 앞 단계에서 끓이거나 저어 둔 냄비에 넣을 때만 국물이 이미 있다.
            bool cooked = steps.Take(index).Any(st => st.kind == "Boil" || st.kind == "Simmer" || st.kind == "Stir");
            if (mode == DropMode.Add && cooked && !items.Any(i => i.Liquid)) liquidFrom = liquidTo = new Color32(0xD9, 0xB7, 0x7A, 0xD8);
            liquid.color = liquidFrom;
        }

        // 담기 자리
        if (mode == DropMode.Plate)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Vector2 at = garnish ? center + new Vector2(230, 90) :
                    items.Count == 1 ? center : center + new Vector2(Mathf.Cos(i * Mathf.PI * 2f / items.Count - Mathf.PI / 2f) * 210f, Mathf.Sin(i * Mathf.PI * 2f / items.Count - Mathf.PI / 2f) * 125f + 25f);
                // 흰 접시 위에서 보이도록 청록 점선 자리와 진한 이름.
                var ring = Art("Lab/LabSlot", at.x - 90, at.y - 90, 180, 180);
                ring.color = new Color(DayLoopUI.Kit.Sea.r, DayLoopUI.Kit.Sea.g, DayLoopUI.Kit.Sea.b, .85f);
                var label = Label(frame, items[i].Name, 30, DayLoopUI.Kit.Sea, at.x - 90, at.y - 20, 180, 40, true);
                slots.Add((at, items[i].Name, ring));
            }
        }

        // 쟁반(왼쪽): 순서가 드러나지 않게 섞어 놓는다.
        foreach (var item in items) { groups[item.Name] = item.Group; for (int c = 0; c < DropRules.Copies(item); c++) order.Add(item.Name); }
        var shuffled = order.Select((name, i) => (name, key: (i * 7 + 3) % Mathf.Max(1, order.Count + 1))).OrderBy(x => x.key).Select(x => x.name).ToList();
        if (mode == DropMode.Plate || order.Count == 1) shuffled = order.ToList();
        var tray = DayLoopUI.Card(frame, DayLoopUI.Kit.Sand, "Tray", 3f, 6f);
        int rows = Mathf.CeilToInt(shuffled.Count / 2f);
        TopLeft(tray.rectTransform, 50, 250, 380, Mathf.Max(220, rows * 190 + 50));
        Label(tray.transform, "재료", 24, DayLoopUI.Kit.InkSoft, 20, 8, 200, 34);
        for (int i = 0; i < shuffled.Count; i++)
        {
            var item = items.First(x => x.Name == shuffled[i]);
            var home = new Vector2(150 + (i % 2) * 180, 350 + (i / 2) * 190);
            var art = Art(null, home.x - 75, home.y - 75, 150, 150);
            art.sprite = item.Art; art.preserveAspect = true; art.enabled = item.Art != null;
            if (item.Art != null && item.Art.name.EndsWith("LabWater")) art.color = item.Tint;
            var name = Label(art.transform, item.Name, 24, Color.white, 0, 148, 150, 34, true);
            Outline(name, .3f);
            pieces.Add(new Piece { Item = item, Art = art.rectTransform, Home = home });
        }

        pad = AddPad(40, 230, 1420, 760);
        pad.Down += PickUp;
        pad.Up += Drop;
        spoon = Art("Lab/LabSpoon", center.x - 210, center.y - 60, 420, 120).rectTransform;
        spoon.gameObject.SetActive(false);
        pop = Pop("퐁당!", 60, 1130, 330);
        pop.gameObject.SetActive(false);

        BuildHud(recipe, steps, index, onExit);
        string what = mode == DropMode.Plate ? "담은 것" : "넣은 재료";
        var gauges = mode == DropMode.Sauce
            ? BuildGaugeCard(1460, 150, (what, 1f, 1f, "번호 순서대로"), ("섞은 정도", 1f, 1f, $"{DropRules.StirTurns:0}바퀴 젓기"))
            : BuildGaugeCard(1460, 150, (what, 1f, 1f, mode == DropMode.Plate ? "이름이 적힌 자리에" : "번호 순서대로"),
                (mode == DropMode.Plate ? "놓은 자리" : "순서 실수", mode == DropMode.Plate ? .8f : 0f, mode == DropMode.Plate ? 1f : .01f, mode == DropMode.Plate ? "가운데에 딱 맞게" : "틀리면 감점"));
        countGauge = gauges[0]; secondGauge = gauges[1];
        BuildChef(1560, 470, 330, 1275, 530);
        string arrow = "<color=#C8443A>→</color>";
        string target = plateTarget ? "접시" : mode == DropMode.Sauce ? "그릇" : pan ? "팬" : "냄비";
        string only = items[0].Name;
        var orderText = items.Count == 1 ? (order.Count > 1 ? $"{only} {order.Count}개를 {target}에 넣기"
                : $"{only}{SeasonRules.Josa(only, "을", "를")} 끌어서 {target}에 {(plateTarget ? "뿌리기" : "넣기")}")
            : string.Join($" {arrow} ", items.GroupBy(it => it.Group < 0 ? "#" + it.Name : "g" + it.Group)
                .Select((g, i) => $"<color=#1F7F8A>{i + 1}</color> {string.Join("·", g.Select(it => it.Name))}"));
        guide = BuildInstruction(mode == DropMode.Plate ? $"재료를 끌어 접시 위 이름 자리에 놓기" : orderText, -120f, 1060f);
        Refresh();

        string names = string.Join(", ", items.Select(i => i.Name));
        ShowIntro(steps, index,
            mode == DropMode.Plate ? $"{names}{SeasonRules.Josa(items.Last().Name, "을", "를")} 끌어서 접시 위 정해진 자리에 담아요. 자리 가운데에 가깝게 놓을수록 좋아요."
            : mode == DropMode.Sauce ? $"{names}{SeasonRules.Josa(items.Last().Name, "을", "를")} 순서대로 그릇에 넣고, 숟가락으로 빙글빙글 {DropRules.StirTurns:0}바퀴 섞어요."
            : $"{names}{SeasonRules.Josa(items.Last().Name, "을", "를")} 순서대로 {target}에 넣어요. 순서가 틀리면 다시 튕겨 나와요!",
            preview => {
                PreviewArt(preview, DesignArt.Load(plateTarget ? "Lab/LabPlate" : mode == DropMode.Sauce ? "Lab/LabBowl" : pan ? "Lab/LabPan" : "Lab/LabPot"), 120, 40, 340, plateTarget ? 227 : 250, false);
                PreviewArt(preview, items[0].Art, 30, 30, 130, 130);
            },
            mode == DropMode.Plate ? "재료를 <color=#C8443A>끌어서</color>\n이름 자리에 놓기\n<color=#C8443A>→</color> <color=#1F7F8A>가운데에 딱</color>"
                : $"재료를 <color=#C8443A>끌어서</color>\n{target}에 놓기\n<color=#C8443A>→</color> <color=#1F7F8A>번호 순서대로</color>",
            $"제한 시간 {TimeLimitFor():0}초 · " + (mode == DropMode.Plate ? $"{order.Count}개 담기" : $"재료 {order.Count}개"),
            mode == DropMode.Plate ? "보기 좋은 떡이\n먹기도 좋지!" : "순서가\n맛을 바꿔!");
        limit = TimeLimitFor();
    }

    // 병에 든 양념은 부으면 국물 색이 바뀐다.
    private static readonly string[] PouredNames = { "간장", "미림", "식초", "꿀" };
    private float TimeLimitFor() => DropRules.TimeLimit(mode, order.Count);

    private void PickUp()
    {
        if (!running || finished || stirring || !PointerOnFrame(pad, out var p)) return;
        dragging = pieces.Where(x => !x.Placed).OrderBy(x => (x.Art.anchoredPosition - new Vector2(p.x, -p.y)).sqrMagnitude)
            .FirstOrDefault(x => (x.Art.anchoredPosition - new Vector2(p.x, -p.y)).magnitude < 110f);
        if (dragging != null) { dragging.Art.SetAsLastSibling(); dragging.Art.localScale = Vector3.one * 1.15f; }
    }

    private void Drop()
    {
        tracker.Release();
        var piece = dragging; dragging = null;
        if (piece == null || !running || finished) return;
        piece.Art.localScale = Vector3.one;
        PointerOnFrame(pad, out var p);
        if (mode == DropMode.Plate)
        {
            int slot = -1; float distance = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                float d = (p - slots[i].at).magnitude;
                if (d < DropRules.SlotRadius && d < distance && !pieces.Any(x => x.Slot == i)) { slot = i; distance = d; }
            }
            if (slot < 0) { Return(piece); return; }
            if (slots[slot].name != piece.Item.Name) { mistakes++; Bounce(piece, "자리가 달라요!"); return; }
            last = DropRules.PlaceScore(distance);
            places.Add(last);
            piece.Placed = true; piece.Slot = slot;
            piece.Art.sizeDelta = new Vector2(210, 210);
            piece.Art.GetComponentInChildren<TMP_Text>()?.gameObject.SetActive(false); // 자리 이름이 이미 접시에 있다.
            MoveTo(piece.Art, p);
            slots[slot].ring.color = new Color(1, 1, 1, 0f);
            ShowPop(last >= 4.5f ? "딱!" : last >= 3f ? "좋아!" : "삐뚤…", last >= 3f ? Color.white : DayLoopUI.Kit.Coin);
            if (places.Count >= order.Count) Finish();
            return;
        }
        if ((p - center).magnitude > radius) { Return(piece); return; }
        if (!DropRules.InOrder(order, next, piece.Item.Name, groups)) { mistakes++; Bounce(piece, "순서가 달라요!"); return; }
        // 묶음 안에서 순서를 바꿔 넣었으면 남은 순서도 맞춰 바꾼다.
        int at = order.IndexOf(piece.Item.Name, next);
        (order[next], order[at]) = (order[at], order[next]);
        next++;
        piece.Placed = true;
        if (piece.Item.Liquid || (LabItems.IsSeasoning(piece.Item.Name) && !plateTarget))
        {
            piece.Art.gameObject.SetActive(false);
            // 붓는 것은 국물 색을 바꾸고, 가루는 소스를 섞을 때만 조금 바꾼다.
            if (piece.Item.Liquid || mode == DropMode.Sauce || PouredNames.Contains(piece.Item.Name))
            {
                var tint = piece.Item.Art != null && piece.Item.Art.name.EndsWith("LabWater") ? piece.Item.Tint : AverageColor(piece.Item);
                var to = new Color(tint.r, tint.g, tint.b, .85f);
                liquidTo = liquidFrom.a <= 0f ? to : Color.Lerp(liquidTo, to, piece.Item.Liquid ? .5f : .3f);
                liquidFrom = liquidTo;
            }
            ShowPop(piece.Item.Liquid ? "졸졸~" : "솔솔~", Color.white);
        }
        else
        {
            float a = next * 2.1f;
            var spot = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .6f) * (next % 3 == 0 ? 40f : 150f);
            piece.Art.sizeDelta = new Vector2(150, 150);
            MoveTo(piece.Art, spot);
            piece.Art.GetComponentInChildren<TMP_Text>()?.gameObject.SetActive(false);
            ShowPop(pan ? "치익~" : plateTarget ? "톡!" : "퐁당!", Color.white);
        }
        if (liquid != null) liquid.color = liquidFrom;
        if (next >= order.Count)
        {
            if (mode == DropMode.Sauce)
            {
                stirring = true;
                spoon.gameObject.SetActive(true);
                spoon.SetAsLastSibling();
                guide.text = $"그릇 안을 <color=#C8443A>누른 채</color> 빙글빙글 {DropRules.StirTurns:0}바퀴 섞기";
            }
            else Finish();
        }
    }

    private static Color AverageColor(LabItem item)
    {
        var s = item.Art;
        if (s == null || !s.texture.isReadable) return new Color32(0xC8, 0x8E, 0x4A, 0xFF);
        var r = s.textureRect;
        var px = s.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        Color sum = Color.clear; float n = 0;
        for (int i = 0; i < px.Length; i += 37) if (px[i].a > .5f) { sum += px[i]; n++; }
        return n > 0 ? new Color(sum.r / n, sum.g / n, sum.b / n, 1f) : new Color32(0xC8, 0x8E, 0x4A, 0xFF);
    }

    private void Return(Piece piece) => MoveTo(piece.Art, piece.Home);
    private void Bounce(Piece piece, string message)
    {
        Return(piece);
        ShowPop(message, DayLoopUI.Kit.Alert);
    }
    private void ShowPop(string text, Color color)
    {
        pop.text = text; pop.color = color;
        pop.gameObject.SetActive(true); popTimer = .8f;
    }

    private float Score()
    {
        if (mode == DropMode.Plate) return DropRules.PlateScore(places, order.Count, mistakes);
        float o = DropRules.OrderScore(next, order.Count, mistakes);
        return mode == DropMode.Sauce ? DropRules.SauceScore(o, tracker.Turns, heldTime > 0 ? goodTime / heldTime : 0f) : o;
    }

    private void Finish()
    {
        if (finished) return;
        finished = true; running = false; stirring = false;
        float score = Score();
        int placed = mode == DropMode.Plate ? places.Count : next;
        var chips = new List<string> { $"{(mode == DropMode.Plate ? "담은 것" : "넣은 재료")} {placed}/{order.Count}" };
        if (mode == DropMode.Plate) chips.Add($"자리 맞춤 {(places.Count > 0 ? places.Average() / 5f * 100f : 0f):0}%");
        if (mode == DropMode.Sauce) chips.Add($"섞은 정도 {Mathf.Clamp01(tracker.Turns / DropRules.StirTurns) * 100f:0}%");
        chips.Add(mode == DropMode.Plate ? $"자리 실수 {mistakes}번" : $"순서 실수 {mistakes}번");
        ShowResult(score, chips.ToArray(), DropRules.ChefReview(mode, score, placed, order.Count, mistakes,
            mode == DropMode.Sauce && tracker.Turns < DropRules.StirTurns));
        Refresh();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        if (pad != null && pad.Held && PointerOnFrame(pad, out var p))
        {
            if (dragging != null) MoveTo(dragging.Art, p);
            if (stirring && running && !finished)
            {
                var dir = p - center;
                MoveTo(spoon, p + dir.normalized * 150f);
                spoon.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg + 180f);
                float turned = tracker.Feed(dir, dt, 30f, radius);
                if (turned > 0f || tracker.Speed > .05f)
                {
                    heldTime += dt;
                    if (StirRules.GoodSpeed(tracker.Speed)) goodTime += dt;
                }
                foreach (var piece in pieces.Where(x => x.Placed && x.Art.gameObject.activeSelf))
                    piece.Art.localScale = Vector3.one * Mathf.Lerp(1f, .35f, Mathf.Clamp01(tracker.Turns / DropRules.StirTurns));
                if (tracker.Turns >= DropRules.StirTurns) Finish();
            }
        }
        else tracker.Decay(dt);
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
        int placed = mode == DropMode.Plate ? places.Count : next;
        SetGauge(countGauge, order.Count > 0 ? placed / (float)order.Count : 0f, $"{placed} / {order.Count}", placed >= order.Count, false);
        if (mode == DropMode.Sauce)
        {
            float r = Mathf.Clamp01(tracker.Turns / DropRules.StirTurns);
            SetGauge(secondGauge, r, $"{r * 100f:0}%", r >= 1f, false);
        }
        else if (mode == DropMode.Plate)
        {
            float aim = places.Count > 0 ? places.Average() / 5f : 0f;
            SetGauge(secondGauge, aim, places.Count > 0 ? $"{aim * 100f:0}%" : "-", aim >= .8f, places.Count > 0 && aim < .5f);
        }
        else SetGauge(secondGauge, Mathf.Clamp01(mistakes / 4f), $"{mistakes}번", mistakes == 0 && next > 0, mistakes > 0);
        SetTimer(limit - elapsed, limit);
        string hint = mode == DropMode.Plate ? "이름 자리에\n살포시 놓아!"
            : stirring ? (tracker.Speed > StirRules.SpeedHigh ? "너무 빨라!\n튀겠어." : "빙글빙글\n골고루 섞어!")
            : next < order.Count ? $"다음은\n{order[next]}!" : "다 넣었어!";
        if (mode == DropMode.Plate && last >= 0f && last < 3f) hint = "자리 가운데에\n맞춰 봐!";
        chefLine.text = finished ? "완성!" : running ? hint : "준비되면\n[시작]을 눌러!";
    }
}
