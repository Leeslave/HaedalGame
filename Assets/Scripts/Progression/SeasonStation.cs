using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>뿌리는 양념(소금·후추)인지, 바르는 양념(밀가루·계란·소스)인지.</summary>
public enum SeasonKind { Grain, Coat }

public class Seasoning
{
    public string Name, Art;
    public SeasonKind Kind;
    public Color32 Color;
}

/// <summary>
/// 염지·밑간·튀김옷 단계 점수 규칙. 화면과 떼어 두어 테스트에서 바로 확인한다.
/// 재료 그림을 칸으로 나누고, 누른 채 문지르면 붓 둘레 칸에 양념이 쌓인다.
/// 칸 양념이 0.5 이상이면 다 뿌린 칸이고, 덜 뿌린 칸은 그만큼만 쳐서 재료 칸 평균을 골고루 정도로 본다.
/// 그래서 살짝 문질러도 게이지가 조금씩 오른다.
/// 85%를 넘기면 만점, 평균 양이 1.4를 넘으면 너무 많아 점수가 깎인다.
/// </summary>
public static class SeasonRules
{
    public const float TargetCoverage = .85f, CoveredDose = .5f;
    public const float AmountLow = .5f, AmountHigh = 1.4f, AmountMax = 2.5f, MaxDose = 3f;
    public const float BrushRadius = .12f, Rate = 2.6f;
    private static readonly string[] RubWords = { "염지", "밑간", "묻히", "바르", "입히", "버무리" };
    // 긴 이름부터 찾아야 "된장소스"가 "소스"로 잘못 잡히지 않는다.
    private static readonly (string word, string art, SeasonKind kind, uint rgb)[] Known =
    {
        ("된장소스", "Ingredient/Doenjang", SeasonKind.Coat, 0xB07A3C),
        ("튀김가루", "Ingredient/Flour", SeasonKind.Coat, 0xF2D592),
        ("밀가루", "Ingredient/Flour", SeasonKind.Coat, 0xFFFDF6),
        ("계란", "Ingredient/Egg", SeasonKind.Coat, 0xF5C232),
        ("전분", "Ingredient/Starch", SeasonKind.Coat, 0xFFFFFF),
        ("소금", "Ingredient/Salt", SeasonKind.Grain, 0xFFFFFF),
        ("후추", "Ingredient/Pepper", SeasonKind.Grain, 0x2E241E),
        ("양념", "Ingredient/SoySauce", SeasonKind.Coat, 0x8A4B26),
        ("소스", "Ingredient/SoySauce", SeasonKind.Coat, 0x9A5A2E),
    };

    /// <summary>재료에 양념을 뿌리거나 바르는 단계인지. 양념을 섞어 만드는 단계는 아니다.</summary>
    public static bool IsRub(string text) => !string.IsNullOrEmpty(text) && RubWords.Any(text.Contains);

    /// <summary>단계 글에 나온 양념을 나온 순서대로. 하나도 없으면 소금.</summary>
    public static List<Seasoning> Parse(string text)
    {
        var found = new List<(int at, Seasoning s)>();
        var used = new bool[(text ?? "").Length];
        foreach (var k in Known)
        {
            for (int at = text?.IndexOf(k.word, StringComparison.Ordinal) ?? -1; at >= 0; at = text.IndexOf(k.word, at + 1, StringComparison.Ordinal))
            {
                if (Enumerable.Range(at, k.word.Length).Any(i => used[i])) continue;
                for (int i = at; i < at + k.word.Length; i++) used[i] = true;
                found.Add((at, Make(k)));
                break;
            }
        }
        var list = found.OrderBy(f => f.at).Select(f => f.s).ToList();
        if (list.Count == 0) list.Add(Make(Known.First(k => k.word == "소금")));
        return list;
    }
    private static Seasoning Make((string word, string art, SeasonKind kind, uint rgb) k) => new Seasoning
    {
        Name = k.word, Art = k.art, Kind = k.kind,
        Color = new Color32((byte)(k.rgb >> 16), (byte)(k.rgb >> 8), (byte)k.rgb, 255)
    };

    /// <summary>단계 글에 가장 먼저 나온 재료 번호. 없으면 0(주재료).</summary>
    public static int TargetIndex(string text, IList<string> names)
    {
        int best = 0, bestAt = int.MaxValue;
        for (int i = 0; i < names.Count; i++)
        {
            int at = string.IsNullOrEmpty(names[i]) || text == null ? -1 : text.IndexOf(names[i], StringComparison.Ordinal);
            if (at >= 0 && at < bestAt) { best = i; bestAt = at; }
        }
        return best;
    }

    public static float TimeLimit(int layers) => 10f + 12f * Mathf.Max(1, layers);

    /// <summary>uv(0~1, 왼쪽 아래가 0) 둘레 칸에 dt초만큼 양념을 쌓는다. 가운데일수록 많이 쌓인다.</summary>
    public static void Sprinkle(float[] dose, int w, int h, Vector2 uv, float dt, float rate = Rate)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt((uv.x - BrushRadius) * w)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt((uv.x + BrushRadius) * w));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((uv.y - BrushRadius) * h)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt((uv.y + BrushRadius) * h));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Vector2.Distance(new Vector2((x + .5f) / w, (y + .5f) / h), uv) / BrushRadius;
                if (d >= 1f) continue;
                int i = y * w + x;
                dose[i] = Mathf.Min(MaxDose, dose[i] + rate * (1f - d * d) * dt);
            }
    }
    public static float Coverage(float[] dose, bool[] mask)
    {
        int total = 0; float covered = 0f;
        for (int i = 0; i < dose.Length; i++) if (mask[i]) { total++; covered += Mathf.Min(1f, dose[i] / CoveredDose); }
        return total > 0 ? covered / total : 0f;
    }
    public static float Amount(float[] dose, bool[] mask)
    {
        int total = 0; float sum = 0f;
        for (int i = 0; i < dose.Length; i++) if (mask[i]) { total++; sum += dose[i]; }
        return total > 0 ? sum / total : 0f;
    }
    /// <summary>둘레를 합쳐 양념이 가장 적은 재료 칸. 재료 칸이 없으면 -1.</summary>
    public static int Thinnest(float[] dose, bool[] mask, int w, int h, int reach = 3)
    {
        int best = -1; float bestSum = float.MaxValue;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[y * w + x]) continue;
                float sum = 0f; int n = 0;
                for (int yy = Mathf.Max(0, y - reach); yy <= Mathf.Min(h - 1, y + reach); yy++)
                    for (int xx = Mathf.Max(0, x - reach); xx <= Mathf.Min(w - 1, x + reach); xx++)
                        if (mask[yy * w + xx]) { sum += Mathf.Min(dose[yy * w + xx], CoveredDose); n++; }
                // 재료 끝자락보다 안쪽을 고르도록 칸 수가 많은 곳을 조금 더 친다.
                float score = sum / n - n * .001f;
                if (score < bestSum) { bestSum = score; best = y * w + x; }
            }
        return best;
    }
    /// <summary>그림이 없을 때 쓰는 타원 모양 재료 칸.</summary>
    public static bool[] EllipseMask(int w, int h)
    {
        var mask = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = ((x + .5f) / w - .5f) / .42f, v = ((y + .5f) / h - .5f) / .28f;
                mask[y * w + x] = u * u + v * v <= 1f;
            }
        return mask;
    }

    /// <summary>한 겹 점수(0~5): 골고루 50%면 0점, 85%면 5점. 평균 양이 1.4를 넘은 만큼 깎인다.</summary>
    public static float LayerScore(float coverage, float amount)
    {
        float cover = Mathf.Clamp01((coverage - .5f) / (TargetCoverage - .5f)) * 5f;
        return Mathf.Clamp(cover - Mathf.Max(0f, amount - AmountHigh) * 4f, 0f, 5f);
    }
    public static string AmountLabel(float amount, SeasonKind kind) =>
        amount < AmountLow ? "조금" : amount <= AmountHigh ? "딱 좋아요" : kind == SeasonKind.Grain ? "짜요!" : "너무 두꺼워요!";

    /// <summary>문지르는 동안 셰프가 하는 말. next는 다음 양념 이름(없으면 null).</summary>
    public static string ChefHint(float coverage, float amount, Seasoning s, string next)
    {
        if (amount > AmountHigh) return s.Kind == SeasonKind.Grain ? "너무 많아!\n짜지겠어." : "너무 두꺼워!\n이제 그만!";
        if (coverage >= TargetCoverage) return next != null ? $"좋아, 골고루 됐어!\n다음은 {next}!" : "좋아, 골고루 됐어!\n이제 끝내자.";
        if (coverage >= .45f) return "동그라미 친 곳에도\n골고루!";
        return "누른 채로\n쓱쓱 문질러 봐!";
    }
    public static string ChefReview(float score, float worstCoverage, float worstAmount, SeasonKind kind)
    {
        if (score >= 4.5f) return kind == SeasonKind.Grain ? "좋아! 간이 골고루 잘 뱄어. 맛있게 구워지겠다!" : "좋아! 빈틈없이 골고루 잘 입혔어!";
        if (worstAmount > AmountHigh) return kind == SeasonKind.Grain ? "너무 많이 뿌렸어. 짜지 않게 딱 적당히만!" : "너무 두껍게 발랐어. 얇고 고르게!";
        if (worstCoverage < TargetCoverage) return "덜 된 곳이 있어. 끝부분까지 골고루 문질러 줘.";
        return "잘했어! 조금만 더 골고루 하면 완벽해.";
    }
    /// <summary>받침 있는 말 뒤에는 a, 없으면 b(을/를, 으로/로).</summary>
    public static string Josa(string word, string a, string b)
    {
        if (string.IsNullOrEmpty(word)) return b;
        char c = word[word.Length - 1];
        if (c < 0xAC00 || c > 0xD7A3) return b;
        int final = (c - 0xAC00) % 28;
        if (a == "으로" && final == 8) return b; // ㄹ 받침은 "로"
        return final != 0 ? a : b;
    }
}

/// <summary>
/// 연구 실습의 염지(밑간·튀김옷·양념 바르기) 단계 화면. 도마 위 재료에
/// 양념통을 누른 채 문질러 골고루 뿌리고, 덜 된 곳은 빨간 동그라미로 알려 준다.
/// 양념이 여러 가지면 차례로 한 겹씩 입힌다.
/// </summary>
public class SeasonStation : LabStation
{
    private const int GridW = 40, GridH = 40, TexSize = 160;
    private const float FishX = 650f, FishY = 245f, FishSize = 620f;
    private RectTransform fishRect, shaker, ring, ringTagRect;
    private TMP_Text coverValue, amountValue, coverTitle, amountTitle, amountHint, sprinkle, guide, layerText;
    private Image coverFill, amountFill, shakerArt;
    private RectTransform coverNeedle, amountNeedle;
    private Button nextButton;
    private RubPad pad;
    private Texture2D overlay;
    private Color32[] pixels;
    private byte[] noise;
    private bool[] mask;
    private List<Seasoning> layers;
    private float[][] doses;
    private readonly List<float> scores = new List<float>(), coverages = new List<float>(), amounts = new List<float>();
    private string targetName;
    private int layer;
    private float elapsed, limit, redraw, ringCheck;
    private bool dirty;

    public static SeasonStation Open(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        var station = Create<SeasonStation>("SeasonStage", screen);
        station.Build(recipe, steps, index, onDone, onExit);
        return station;
    }

    // ───────────── 화면 만들기 ─────────────
    private void Build(RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        string text = steps[index].text;
        layers = SeasonRules.Parse(text);
        doses = layers.Select(_ => new float[GridW * GridH]).ToArray();
        limit = SeasonRules.TimeLimit(layers.Count);
        BuildStage(recipe, onDone, layers.Select(l => l.Art).Concat(new[] { "Ingredient/Salt", "Ingredient/Pepper", "Ingredient/Lemon" }).ToArray());

        // 도마와 재료
        Art("Lab/LabBoard", 480, 262, 960, 585, true);
        Sprite target = TargetSprite(recipe, steps, text);
        var fish = Art(null, FishX, FishY, FishSize, FishSize, true);
        fish.sprite = target; fish.preserveAspect = true; fish.enabled = target != null;
        fishRect = fish.rectTransform;
        fish.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        overlay = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        pixels = new Color32[TexSize * TexSize];
        var random = new System.Random(7);
        noise = new byte[TexSize * TexSize];
        random.NextBytes(noise);
        var grains = DayLoopUI.Rect("Seasoning", fishRect).gameObject.AddComponent<RawImage>();
        grains.texture = overlay; grains.raycastTarget = false;
        DayLoopUI.Stretch(grains.rectTransform);
        mask = target != null ? ReadMask(target) : SeasonRules.EllipseMask(GridW, GridH);

        ring = Art("Lab/LabRing", 0, 0, 220, 170).rectTransform;
        var ringTag = DayLoopUI.Card(ring, DayLoopUI.Kit.Alert, "RingTag", 3f, 0f);
        DayLoopUI.Place(ringTag.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 0), new Vector2(0, 6), new Vector2(230, 44));
        var ringText = DayLoopUI.Text(ringTag.transform, "여기 덜 됐어요", 22, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(ringText.rectTransform);
        ringTag.raycastTarget = ringText.raycastTarget = false;
        ringTagRect = ringTag.rectTransform;
        ring.gameObject.SetActive(false);

        // 문지르는 자리: 도마 전체를 누를 수 있다.
        var padImage = DayLoopUI.Panel(frame, new Color(0, 0, 0, 0), "RubPad", false);
        TopLeft(padImage.rectTransform, 480, 262, 960, 585);
        padImage.raycastTarget = true;
        pad = padImage.gameObject.AddComponent<RubPad>();

        shaker = DayLoopUI.Rect("Shaker", frame);
        DayLoopUI.Place(shaker, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(370, 200), new Vector2(190, 190));
        shakerArt = DayLoopUI.Panel(shaker, Color.white, "Art", false);
        shakerArt.preserveAspect = true; shakerArt.raycastTarget = false;
        DayLoopUI.Stretch(shakerArt.rectTransform);
        sprinkle = Label(frame, "", 44, Color.white, 0, 0, 220, 70, true);
        Outline(sprinkle, .28f);

        BuildHud(recipe, steps, index, onExit);
        BuildGauges();
        BuildChef(60, 600, 345, 200, 470);

        var layerCard = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Layers", 3f, 6f);
        TopLeft(layerCard.rectTransform, 1460, 455, 435, 84);
        layerText = Label(layerCard.transform, "", 28, DayLoopUI.Kit.Ink, 20, 0, 395, 84, true);
        layerCard.gameObject.SetActive(layers.Count > 1);

        nextButton = DayLoopUI.KitButton(frame, "다 뿌렸어요", DayLoopUI.Kit.Sea, Color.white, 40, Next, "Done");
        TopLeft((RectTransform)nextButton.transform, 1530, 905, 355, 126);
        guide = BuildInstruction("");

        var first = layers[0];
        ShowLayer();
        Refresh();
        string verb = first.Kind == SeasonKind.Grain ? "뿌려요" : "발라요";
        string all = string.Join(", ", layers.Select(l => l.Name));
        ShowIntro(steps, index,
            $"도마 위 {targetName}에 {all}{SeasonRules.Josa(layers.Last().Name, "을", "를")} 골고루 {verb}. 덜 된 곳이 없게, 너무 많지도 않게!\n다 되면 오른쪽 아래 버튼을 눌러요.",
            preview => {
                PreviewArt(preview, DesignArt.Load("Lab/LabBoard"), 20, 40, 400, 244, false);
                PreviewArt(preview, target, 70, 20, 290, 290);
                var jar = PreviewArt(preview, DesignArt.Load(first.Art), 330, 20, 150, 150);
                jar.rectTransform.localEulerAngles = new Vector3(0, 0, 30);
            },
            $"양념통을 <color=#C8443A>누른 채</color>\n{targetName} 위를 문지르기\n<color=#C8443A>→</color> 골고루 <color=#1F7F8A>{SeasonRules.TargetCoverage * 100f:0}%</color> 넘기기",
            $"제한 시간 {limit:0}초 · " + (first.Kind == SeasonKind.Grain ? "너무 많이 뿌리면 짜요" : "너무 두꺼우면 감점"),
            first.Kind == SeasonKind.Grain ? "끝부분까지\n골고루 뿌려 줘!" : "얇고 고르게\n입혀 줘!");
    }

    private Sprite TargetSprite(RecipeData recipe, ResearchStep[] steps, string text)
    {
        if (recipe == null || recipe.Requirements.Count == 0) { targetName = "재료"; return null; }
        var names = recipe.Requirements.Select(r => r.IngredientName).ToList();
        int index = SeasonRules.TargetIndex(text, names);
        targetName = names[index];
        // 뒤에 굽는 단계가 있는 주재료라면 구이 직전 모습(손질된 살)을 보여 준다.
        if (index == 0 && steps.Any(s => s.kind == "Grill")) { var raw = RawFood(recipe); if (raw != null) return raw; }
        return DesignArt.Ingredient(recipe.Requirements[index].IngredientId) ?? RawFood(recipe) ?? DesignArt.Food(recipe);
    }

    // 그림의 투명하지 않은 곳만 재료 칸으로 친다. 그림 텍스처를 읽을 수 없어 작은 렌더 텍스처에 옮겨 읽는다.
    private static bool[] ReadMask(Sprite sprite)
    {
        var rt = RenderTexture.GetTemporary(GridW, GridH, 0, RenderTextureFormat.ARGB32);
        var tex = sprite.texture;
        var rect = sprite.textureRect;
        Graphics.Blit(tex, rt, new Vector2(rect.width / tex.width, rect.height / tex.height), new Vector2(rect.x / tex.width, rect.y / tex.height));
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var read = new Texture2D(GridW, GridH, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, GridW, GridH), 0, 0);
        read.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        var data = read.GetPixels32();
        Destroy(read);
        var mask = data.Select(p => p.a > 140).ToArray();
        return mask.Count(m => m) >= 20 ? mask : SeasonRules.EllipseMask(GridW, GridH);
    }

    private void BuildGauges()
    {
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Gauges", 3f, 6f);
        TopLeft(card.rectTransform, 1460, 150, 435, 285);
        coverTitle = Label(card.transform, "", 24, DayLoopUI.Kit.Ink, 24, 16, 260, 34);
        coverValue = Label(card.transform, "", 34, DayLoopUI.Kit.Ink, 240, 12, 172, 40, false, TextAlignmentOptions.MidlineRight);
        coverFill = GaugeBar(card.transform, 60, SeasonRules.TargetCoverage, 1f, out coverNeedle);
        Label(card.transform, $"{SeasonRules.TargetCoverage * 100f:0}% 넘으면 성공", 18, DayLoopUI.Kit.InkSoft, 24, 94, 387, 26, false, TextAlignmentOptions.MidlineRight);
        amountTitle = Label(card.transform, "", 24, DayLoopUI.Kit.Ink, 24, 140, 200, 34);
        amountValue = Label(card.transform, "", 30, DayLoopUI.Kit.Ink, 180, 136, 232, 40, false, TextAlignmentOptions.MidlineRight);
        amountFill = GaugeBar(card.transform, 184, SeasonRules.AmountLow / SeasonRules.AmountMax, SeasonRules.AmountHigh / SeasonRules.AmountMax, out amountNeedle);
        amountHint = Label(card.transform, "", 18, DayLoopUI.Kit.InkSoft, 24, 218, 387, 26, false, TextAlignmentOptions.MidlineRight);
    }

    /// <summary>지금 겹(양념)에 맞춰 양념통 그림, 게이지 이름, 버튼, 안내 줄을 바꾼다.</summary>
    private void ShowLayer()
    {
        var s = layers[layer];
        bool grain = s.Kind == SeasonKind.Grain, last = layer == layers.Count - 1;
        shakerArt.sprite = DesignArt.Load(s.Art);
        coverTitle.text = grain ? "골고루 뿌린 정도" : "골고루 바른 정도";
        amountTitle.text = $"{s.Name} 양";
        amountHint.text = grain ? "너무 많이 뿌리면 짜요" : "너무 두꺼우면 감점";
        DayLoopUI.SetButtonLabel(nextButton, last ? (grain ? "다 뿌렸어요" : "다 발랐어요") : $"다음: {layers[layer + 1].Name}  ▶");
        guide.text = $"{s.Name}{(grain ? "통" : "")}{SeasonRules.Josa(s.Name + (grain ? "통" : ""), "을", "를")} 누른 채 {targetName} 위로 문지르기 <color=#C8443A>→</color> {SeasonRules.TargetCoverage * 100f:0}% 넘기기";
        layerText.text = string.Join("   ", layers.Select((l, i) => i < layer ? $"<color=#3B8A5A>{DayLoopUI.Glyph("✓", "V")} {l.Name}</color>" : i == layer ? $"<color=#C8443A>▶ {l.Name}</color>" : $"<color=#8A7A6A>{l.Name}</color>"));
        dirty = true;
    }

    // ───────────── 문지르기 ─────────────
    private void Next()
    {
        if (!running || finished) return;
        ScoreLayer(layer);
        if (layer < layers.Count - 1) { layer++; ShowLayer(); Refresh(); return; }
        Finish();
    }

    private void ScoreLayer(int i)
    {
        float coverage = SeasonRules.Coverage(doses[i], mask), amount = SeasonRules.Amount(doses[i], mask);
        coverages.Add(coverage); amounts.Add(amount);
        scores.Add(SeasonRules.LayerScore(coverage, amount));
    }

    private void Finish()
    {
        if (finished) return;
        finished = true; running = false;
        // 시간이 다 돼 아직 못 한 겹은 지금 상태대로(대개 0점) 매긴다.
        for (int i = scores.Count; i < layers.Count; i++) ScoreLayer(i);
        float score = scores.Average();
        nextButton.gameObject.SetActive(false);
        ring.gameObject.SetActive(false);
        sprinkle.gameObject.SetActive(false);
        var chips = layers.Count == 1
            ? new[] { $"골고루 {coverages[0] * 100f:0}%", $"양 {SeasonRules.AmountLabel(amounts[0], layers[0].Kind)}" }
            : layers.Select((l, i) => $"{l.Name} {coverages[i] * 100f:0}%").Take(3).ToArray();
        int worst = Enumerable.Range(0, layers.Count).OrderBy(i => scores[i]).First();
        ShowResult(score, chips, SeasonRules.ChefReview(score, coverages[worst], amounts[worst], layers[worst].Kind));
        Refresh();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        bool rubbing = false;
        if (pad != null && pad.Held && fishRect != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, pad.Screen, pad.Cam, out var onFrame))
            {
                // 양념통은 손가락 오른쪽 위에서 거꾸로 기울어 손가락 자리로 쏟는다.
                shaker.anchoredPosition = onFrame + new Vector2(85, 105);
                sprinkle.rectTransform.anchoredPosition = new Vector2(onFrame.x + FrameW * .5f + 170, onFrame.y - FrameH * .5f + 150);
            }
            if (running && !finished && RectTransformUtility.ScreenPointToLocalPointInRectangle(fishRect, pad.Screen, pad.Cam, out var local))
            {
                var r = fishRect.rect;
                var uv = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
                if (uv.x > -.1f && uv.x < 1.1f && uv.y > -.1f && uv.y < 1.1f)
                {
                    SeasonRules.Sprinkle(doses[layer], GridW, GridH, uv, dt);
                    rubbing = true; dirty = true;
                }
            }
        }
        else if (shaker != null) shaker.anchoredPosition = Vector2.Lerp(shaker.anchoredPosition, new Vector2(370, 200), 1f - Mathf.Exp(-10f * dt));
        if (shaker != null)
        {
            float wiggle = rubbing ? Mathf.Sin(Time.unscaledTime * 30f) * 6f : 0f;
            shaker.localEulerAngles = new Vector3(0, 0, (pad != null && pad.Held ? 140f : 12f) + wiggle);
            sprinkle.text = layers[layer].Kind == SeasonKind.Grain ? "솔솔~" : "쓱쓱~";
            sprinkle.gameObject.SetActive(rubbing);
        }
        if (running && !finished)
        {
            elapsed += dt;
            if (elapsed >= limit) Finish();
        }
        redraw -= dt;
        if (dirty && redraw <= 0f) { Redraw(); redraw = .06f; dirty = false; }
        Refresh();
    }

    // 양념 칸 값을 그림으로. 알갱이 양념은 알갱이 수가, 바르는 양념은 진하기가 양을 따라간다.
    private void Redraw()
    {
        for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color32(0, 0, 0, 0);
        for (int l = 0; l <= layer && l < layers.Count; l++)
        {
            var s = layers[l]; var dose = doses[l];
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    int p = y * TexSize + x;
                    float d = Sample(dose, (x + .5f) / TexSize, (y + .5f) / TexSize);
                    if (d <= .02f) continue;
                    if (s.Kind == SeasonKind.Grain)
                    {
                        if (Grain(dose, x, y)) pixels[p] = s.Color;
                        else if (x > 0 && y < TexSize - 1 && Grain(dose, x - 1, y + 1)) pixels[p] = Blend(pixels[p], new Color32(70, 48, 30, 255), .45f);
                    }
                    else pixels[p] = Blend(pixels[p], s.Color, Mathf.Clamp01(d * .75f) * .82f);
                }
        }
        overlay.SetPixels32(pixels);
        overlay.Apply(false);
    }
    private bool Grain(float[] dose, int x, int y)
    {
        float d = Sample(dose, (x + .5f) / TexSize, (y + .5f) / TexSize);
        return noise[y * TexSize + x] < Mathf.Clamp01(d * .2f) * 255f;
    }
    private static float Sample(float[] dose, float u, float v)
    {
        float fx = Mathf.Clamp(u * GridW - .5f, 0f, GridW - 1f), fy = Mathf.Clamp(v * GridH - .5f, 0f, GridH - 1f);
        int x0 = (int)fx, y0 = (int)fy, x1 = Mathf.Min(x0 + 1, GridW - 1), y1 = Mathf.Min(y0 + 1, GridH - 1);
        float tx = fx - x0, ty = fy - y0;
        float a = Mathf.Lerp(dose[y0 * GridW + x0], dose[y0 * GridW + x1], tx), b = Mathf.Lerp(dose[y1 * GridW + x0], dose[y1 * GridW + x1], tx);
        return Mathf.Lerp(a, b, ty);
    }
    private static Color32 Blend(Color32 under, Color32 over, float alpha)
    {
        if (alpha <= 0f) return under;
        float ua = under.a / 255f, oa = alpha, outA = oa + ua * (1f - oa);
        if (outA <= 0f) return under;
        byte Mix(byte u, byte o) => (byte)Mathf.Clamp((o * oa + u * ua * (1f - oa)) / outA, 0, 255);
        return new Color32(Mix(under.r, over.r), Mix(under.g, over.g), Mix(under.b, over.b), (byte)(outA * 255f));
    }

    private void Refresh()
    {
        if (coverValue == null) return;
        var s = layers[layer];
        float coverage = SeasonRules.Coverage(doses[layer], mask), amount = SeasonRules.Amount(doses[layer], mask);
        coverValue.text = $"{coverage * 100f:0}%";
        coverValue.color = coverage >= SeasonRules.TargetCoverage ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink;
        DayLoopUI.SetBar(coverFill, coverage);
        coverFill.color = coverage >= SeasonRules.TargetCoverage ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Coin;
        SetNeedle(coverNeedle, coverage);
        amountValue.text = SeasonRules.AmountLabel(amount, s.Kind);
        amountValue.color = amount > SeasonRules.AmountHigh ? DayLoopUI.Kit.WarnInk : amount >= SeasonRules.AmountLow ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink;
        float bar = amount / SeasonRules.AmountMax;
        DayLoopUI.SetBar(amountFill, bar);
        amountFill.color = amount > SeasonRules.AmountHigh ? DayLoopUI.Kit.Alert : DayLoopUI.Kit.Coin;
        SetNeedle(amountNeedle, bar);
        SetTimer(limit - elapsed, limit);
        nextButton.interactable = running && !finished;
        chefLine.text = finished ? "다 됐다!" : running ? SeasonRules.ChefHint(coverage, amount, s, layer < layers.Count - 1 ? layers[layer + 1].Name : null) : "준비되면\n[시작]을 눌러!";

        // 덜 된 곳 동그라미: 조금 해 본 뒤부터, 85%를 넘기 전까지.
        ringCheck -= Time.unscaledDeltaTime;
        if (ringCheck <= 0f)
        {
            ringCheck = .3f;
            bool show = running && !finished && elapsed > 2.5f && coverage < SeasonRules.TargetCoverage && coverage > .15f;
            int cell = show ? SeasonRules.Thinnest(doses[layer], mask, GridW, GridH) : -1;
            show &= cell >= 0;
            if (show)
            {
                float u = (cell % GridW + .5f) / GridW, v = (cell / GridW + .5f) / GridH;
                float ringY = FishY + (1f - v) * FishSize;
                ring.anchoredPosition = new Vector2(FishX + u * FishSize, -ringY);
                // 동그라미가 도마 위쪽 끝이면 이름표를 아래에 달아 선반을 가리지 않는다.
                bool below = ringY < 360f;
                ringTagRect.anchorMin = ringTagRect.anchorMax = new Vector2(.5f, below ? 0f : 1f);
                ringTagRect.pivot = new Vector2(.5f, below ? 1f : 0f);
                ringTagRect.anchoredPosition = new Vector2(0, below ? -6f : 6f);
            }
            ring.gameObject.SetActive(show);
        }
        if (ring.gameObject.activeSelf) ring.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * .05f);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    /// <summary>도마를 누르고 있는 동안 손가락(마우스) 자리를 기억한다.</summary>
    private class RubPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public bool Held;
        public Vector2 Screen;
        public Camera Cam;
        public void OnPointerDown(PointerEventData e) { Held = true; Screen = e.position; Cam = e.pressEventCamera; }
        public void OnDrag(PointerEventData e) { Screen = e.position; Cam = e.pressEventCamera; }
        public void OnPointerUp(PointerEventData e) { Held = false; }
    }
}
