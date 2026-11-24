using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 연구 실습 단계마다 알맞은 전용 화면을 고른다.
/// 굽기·염지·씻기·손질·빚기·젓기·끓이기·졸이기·튀기기·넣기·양념 섞기·담기.
/// </summary>
public static class LabStations
{
    /// <summary>이 단계가 어느 화면에서 하는지. 테스트와 화면 고르기에 함께 쓴다.</summary>
    public static string StationFor(ResearchStep step)
    {
        if (step == null) return null;
        string kind = step.kind, text = step.text ?? "";
        switch (kind)
        {
            case "Grill": return "Grill";
            case "Wash": return "Wash";
            case "Cut": return "Cut";
            case "Shape": return "Shape";
            case "Fry": return "Fry";
            case "Boil": return "Boil";
            case "Simmer": return "Simmer";
            case "Plate": return "Plate";
            case "Stir": return text.Contains("졸이") ? "Simmer" : "Stir";
            case "Mix": return SeasonRules.IsRub(text) ? "Season" : "Sauce";
            case "Add":
                if (text.Contains("볶기") || text.Contains("볶아")) return "Stir"; // "볶은 해산물을 넣고"는 넣기
                if (SeasonRules.IsRub(text)) return "Season";
                return "Add";
        }
        return null;
    }

    /// <summary>전용 화면을 열었으면 true. 끝나면 onDone(점수 0~5)을 부른다.</summary>
    public static bool TryOpen(Transform screen, RecipeData recipe, ResearchStep[] steps, int index, Action<float> onDone, Action onExit)
    {
        switch (StationFor(steps[index]))
        {
            case "Grill": GrillStation.Open(screen, recipe, steps, index, onDone, onExit); return true;
            case "Season": SeasonStation.Open(screen, recipe, steps, index, onDone, onExit); return true;
            case "Wash": SeasonStation.Open(screen, recipe, steps, index, onDone, onExit, true); return true;
            case "Cut": CutStation.Open(screen, recipe, steps, index, onDone, onExit); return true;
            case "Shape": StirStation.Open(screen, recipe, steps, index, onDone, onExit, true); return true;
            case "Stir": StirStation.Open(screen, recipe, steps, index, onDone, onExit, false); return true;
            case "Boil": case "Simmer": case "Fry": HeatStation.Open(screen, recipe, steps, index, onDone, onExit); return true;
            case "Add": case "Sauce": case "Plate": DropStation.Open(screen, recipe, steps, index, onDone, onExit); return true;
        }
        return false;
    }
}

/// <summary>단계 글에 나온 재료 하나. 그림과 물처럼 부어 넣는 것인지.</summary>
public class LabItem
{
    public string Name;
    public Sprite Art;
    public Color Tint = Color.white;
    public bool Liquid;
    /// <summary>같은 묶음(예: "해산물"을 풀어 쓴 재료)끼리는 넣는 순서를 따지지 않는다. -1이면 묶음 없음.</summary>
    public int Group = -1;
    public override string ToString() => Name;
}

public static class LabItems
{
    // 재료 목록에 없는 말. 그림은 비슷한 것으로 대신한다.
    private static readonly (string word, string art, uint tint, bool liquid)[] Specials =
    {
        ("된장소스", "Ingredient/Doenjang", 0xFFFFFF, true), ("레몬소스", "Ingredient/Lemon", 0xFFFFFF, true),
        ("육수", "Lab/LabWater", 0xD9B77A, true), ("양념", "Ingredient/SoySauce", 0xFFFFFF, true), ("소스", "Ingredient/SoySauce", 0xFFFFFF, true),
        ("물", "Lab/LabWater", 0x9FD3EA, true), ("완자", "Lab/LabBall", 0xFFFFFF, false), ("반죽", "Lab/LabDough", 0xFFFFFF, false),
    };
    private static readonly (string word, string art)[] Dishes = { ("숙회", "Food/OctopusSukhoe"), ("구이", "Food/ScallopGrill"), ("회", "Food/FlounderSashimi") };

    /// <summary>
    /// 단계 글에 나온 재료를 나온 순서대로. "해산물"은 레시피의 해산물로 풀어 쓴다.
    /// 하나도 없으면 레시피 주재료. dishes가 켜지면 "회·숙회·구이"도 요리 그림으로 잡는다(담기).
    /// </summary>
    public static List<LabItem> FromText(RecipeData recipe, string text, bool dishes = false)
    {
        text ??= "";
        var found = new List<(int at, LabItem item)>();
        var used = new bool[text.Length];
        bool Take(string word, Func<LabItem> make)
        {
            if (string.IsNullOrEmpty(word)) return false;
            for (int at = text.IndexOf(word, StringComparison.Ordinal); at >= 0; at = text.IndexOf(word, at + 1, StringComparison.Ordinal))
            {
                if (Enumerable.Range(at, word.Length).Any(i => used[i])) continue;
                for (int i = at; i < at + word.Length; i++) used[i] = true;
                found.Add((at, make()));
                return true;
            }
            return false;
        }
        var names = new List<(string name, IngredientData data)>();
        if (GameDatabase.Recipes != null)
            foreach (var ing in GameDatabase.Recipes.Ingredients) if (!string.IsNullOrEmpty(ing.IngredientName)) names.Add((ing.IngredientName, ing));
        // 긴 말부터 잡아야 "된장소스"가 "된장"으로, "숙회"가 "회"로 잘못 잡히지 않는다.
        var words = new List<(string word, Func<LabItem> make)>();
        foreach (var s in Specials) { var k = s; words.Add((k.word, () => Special(k))); }
        if (dishes) foreach (var d in Dishes) { var k = d; words.Add((k.word, () => new LabItem { Name = k.word, Art = DesignArt.Load(k.art) })); }
        foreach (var n in names) { var k = n; words.Add((k.name, () => new LabItem { Name = k.name, Art = DesignArt.Ingredient(k.data) })); }
        words.Add(("해산물", null));
        foreach (var w in words.OrderByDescending(w => w.word.Length))
        {
            if (w.word == "해산물")
            {
                int at = text.IndexOf("해산물", StringComparison.Ordinal);
                if (at < 0 || used[at]) continue;
                for (int i = at; i < at + 3; i++) used[i] = true;
                int k = 0;
                foreach (var main in MainIngredients(recipe).Take(3)) { main.Group = at; found.Add((at + k++, main)); }
                continue;
            }
            Take(w.word, w.make);
        }
        var list = found.OrderBy(f => f.at).Select(f => f.item).ToList();
        // 같은 재료가 두 번 나오면 한 번만.
        list = list.GroupBy(i => i.Name).Select(g => g.First()).ToList();
        if (list.Count == 0) list.AddRange(MainIngredients(recipe).Take(1));
        return list;
    }

    private static LabItem Special((string word, string art, uint tint, bool liquid) k) => new LabItem
    {
        Name = k.word, Art = DesignArt.Load(k.art), Liquid = k.liquid,
        Tint = new Color32((byte)(k.tint >> 16), (byte)(k.tint >> 8), (byte)k.tint, 255)
    };

    private static readonly string[] SeasoningNames = { "소금", "후추", "설탕", "간장", "미림", "식초", "된장", "마늘", "생강", "꿀", "겨자", "와사비", "밀가루", "전분", "식용유", "레몬", "튀김가루", "계란" };
    /// <summary>양념·가루처럼 통째로 놓지 않고 뿌리거나 붓는 재료.</summary>
    public static bool IsSeasoning(string name) => SeasoningNames.Contains(name);

    /// <summary>레시피 재료 중 기본 양념이 아닌 것.</summary>
    public static IEnumerable<LabItem> MainIngredients(RecipeData recipe)
    {
        if (recipe == null || GameDatabase.Recipes == null) yield break;
        foreach (var need in recipe.Requirements)
        {
            if (!GameDatabase.Recipes.TryGetIngredientById(need.IngredientId, out var ing) || ing.IsBasicSeasoning) continue;
            yield return new LabItem { Name = ing.IngredientName, Art = DesignArt.Ingredient(ing) };
        }
    }
}

/// <summary>
/// 원을 그리며 젓거나 빚는 동작을 잰다. 가운데를 기준으로 돈 각도를 모으고,
/// 1초에 몇 바퀴 도는지(속도)를 부드럽게 따라간다.
/// </summary>
public class CircleTracker
{
    public float Turns { get; private set; }
    public float Speed { get; private set; }   // 바퀴/초
    private float lastAngle; private bool has;

    public void Release() { has = false; }
    /// <summary>offset: 가운데에서 손가락까지. radius 밖이거나 너무 안쪽이면 세지 않는다.</summary>
    public float Feed(Vector2 offset, float dt, float minRadius, float maxRadius)
    {
        float r = offset.magnitude;
        if (r < minRadius || r > maxRadius || dt <= 0f) { has = false; Speed = Mathf.MoveTowards(Speed, 0f, dt * 2f); return 0f; }
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        float turned = 0f;
        if (has)
        {
            turned = Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle)) / 360f;
            if (turned > .25f) turned = 0f; // 한 번에 크게 건너뛰면(손을 뗐다 다시 댐) 세지 않는다.
        }
        lastAngle = angle; has = true;
        Turns += turned;
        Speed = Mathf.Lerp(Speed, turned / dt, 1f - Mathf.Exp(-4f * dt));
        return turned;
    }
    public void Decay(float dt) => Speed = Mathf.MoveTowards(Speed, 0f, dt * 2f);
}

/// <summary>누르고 있는 동안 손가락(마우스) 자리를 기억한다. 누를 때와 뗄 때를 알린다.</summary>
public class LabPointer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public bool Held;
    public Vector2 Screen;
    public Camera Cam;
    public event Action Down, Up;
    public void OnPointerDown(PointerEventData e) { Held = true; Screen = e.position; Cam = e.pressEventCamera; Down?.Invoke(); }
    public void OnDrag(PointerEventData e) { Screen = e.position; Cam = e.pressEventCamera; }
    public void OnPointerUp(PointerEventData e) { Screen = e.position; Held = false; Up?.Invoke(); }
}
