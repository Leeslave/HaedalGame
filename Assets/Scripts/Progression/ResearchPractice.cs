using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 요리별 연구 실습 UI. 위쪽에 요리의 단계 진행(완료 단계는 점수)과 지금까지 평균을 보여 준다.
/// 재료 예약과 해금은 RestaurantProgress가 담당한다.
/// </summary>
public class ResearchPractice : MonoBehaviour
{
    private RectTransform surface;
    private TMP_Text instruction, feedback;
    private RecipeData recipe;
    private Action<float> finished;
    private int phase, hits, mistakes, expected;
    private float total, started, lastHit, value;
    private bool transitioning;
    private Image fill;
    private ResearchStep[] steps;
    private string Kind => steps[phase].kind;
    private int Mode => Kind == "Wash" ? 0 : Kind == "Cut" ? 1 : Kind == "Mix" || Kind == "Add" ? 2 : Kind == "Shape" ? 3 : Kind == "Plate" ? 5 : 4;
    private float temperature = .5f, heat = .5f, heatScore, heatElapsed;
    private TMP_Text heatLabel;
    private readonly System.Collections.Generic.List<float> scores = new System.Collections.Generic.List<float>();

    public void Begin(RectTransform parent, RecipeData data, Action<float> onFinished)
    {
        recipe = data; finished = onFinished;
        steps = RecipeResearchPlan.Find(data.RecipeId)?.steps;
        if (steps == null || steps.Length == 0) { onFinished?.Invoke(0); Destroy(this); return; }
        surface = DayLoopUI.Chunky(DayLoopUI.Panel(parent, DayLoopUI.Kit.Sail), 3f, 0f).rectTransform;
        DayLoopUI.Layout(surface, preferredHeight: 640);
        ShowPhase();
    }

    private TMP_Text Label(string text, float y, int size = 25)
    {
        var label = DayLoopUI.Text(surface, text, size, DayLoopUI.Kit.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Place(label.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(1160, 56));
        return label;
    }
    private Button Button(string text, Vector2 position, Action action)
    {
        var button = DayLoopUI.KitButton(surface, text, DayLoopUI.Kit.Sea, Color.white, 26, () => { if (!transitioning) action(); });
        DayLoopUI.Place((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(230, 80));
        return button;
    }
    private void ShowPhase()
    {
        foreach (Transform child in surface) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        hits = mistakes = expected = 0; transitioning = false; fill = null; value = 0; heatLabel = null; heatElapsed = heatScore = 0; temperature = heat = .5f;
        started = Time.unscaledTime; lastHit = -100;
        Steps();
        Label($"{recipe.RecipeName} · {phase + 1}/{steps.Length}", -120, 32);
        Label(steps[phase].text, -166, 24);
        instruction = Label("", -210, 22);
        instruction.color = DayLoopUI.Kit.WoodLight;
        feedback = Label("", -566, 25);
        feedback.rectTransform.sizeDelta = new Vector2(520, 56);
        feedback.color = DayLoopUI.Kit.SeaDeep;
        switch (Mode)
        {
            case 0:
                instruction.text = "오염된 부분을 누른 채 문질러 씻으세요.";
                for (int i = 0; i < 5; i++)
                {
                    var spot = Button("문지르기", new Vector2((i % 3 - 1) * 280, 20 - i / 3 * 110), () => { });
                    var gesture = spot.gameObject.AddComponent<ResearchGesture>();
                    gesture.Scrub(100, () => {
                        spot.gameObject.SetActive(false);
                        if (++hits == 5) FinishPhase(Mathf.Clamp(5 - Mathf.Max(0, Time.unscaledTime - started - 20) / 8, 0, 5));
                    });
                }
                break;
            case 1:
                instruction.text = "가이드의 위쪽에서 아래쪽까지 드래그하여 4번 써세요.";
                for (int i = 0; i < 4; i++)
                {
                    var guide = Button("↓\n칼질", new Vector2((i - 1.5f) * 250, -10), () => { });
                    guide.gameObject.AddComponent<ResearchGesture>().Stroke(new Vector2(0, -1), 50, () => {
                        guide.interactable = false; guide.gameObject.SetActive(false);
                        if (++hits == 4) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    }, () => { mistakes++; feedback.text = "가이드 방향으로 썰어 주세요."; });
                }
                break;
            case 2:
                var required = recipe.Requirements.Select(r => r.IngredientId).Distinct().ToList();
                var options = required.Concat(GameDatabase.Recipes.Ingredients.Select(i => i.IngredientId).Where(id => !required.Contains(id)).Take(2))
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                instruction.text = "순서대로 넣으세요: " + string.Join(" → ", recipe.Requirements.Select(r => r.IngredientName).Distinct());
                for (int i = 0; i < options.Count; i++)
                {
                    int id = options[i]; GameDatabase.Recipes.TryGetIngredientById(id, out var ingredient);
                    Button choice = null;
                    choice = Button(ingredient.IngredientName, new Vector2((i % 4 - 1.5f) * 285, 20 - i / 4 * 85), () => {
                        choice.interactable = false;
                        if (hits < required.Count && id == required[hits]) hits++; else { mistakes++; choice.interactable = true; }
                        feedback.text = $"올바른 재료 {hits}/{required.Count} · 실수 {mistakes}";
                        if (hits == required.Count) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    });
                }
                break;
            case 3:
                instruction.text = "반죽을 오른쪽 가이드 방향으로 드래그해 완자를 빚으세요.";
                var dough = Button("반죽  →", new Vector2(-160, 0), () => { });
                dough.gameObject.AddComponent<ResearchGesture>().Stroke(Vector2.right, 150,
                    () => FinishPhase(Mathf.Max(0, 5 - mistakes)), () => { mistakes++; feedback.text = "가이드를 따라 길게 빚어 주세요."; });
                break;
            case 4:
                instruction.text = Kind == "Grill" ? "앞면과 뒷면을 각각 초록 구간에서 뒤집고 꺼내세요." :
                    Kind == "Fry" ? "기름 온도를 초록 구간에 유지하고 8초 이상 튀기세요." :
                    Kind == "Simmer" ? "소스 농도가 초록 구간에 도달하면 꺼내세요." :
                    Kind == "Stir" ? "젓기 버튼을 일정한 간격으로 눌러 8번 저으세요." : "물의 양을 맞추고 끓는 온도를 8초 이상 유지하세요.";
                Gauge();
                if (Kind == "Stir")
                {
                    Button("젓기", new Vector2(0, -90), () => {
                        if (hits > 0 && (Time.unscaledTime - lastHit < .4f || Time.unscaledTime - lastHit > 1.2f)) mistakes++;
                        lastHit = Time.unscaledTime;
                        if (++hits >= 8) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    });
                }
                else if (Kind == "Grill" || Kind == "Simmer")
                {
                    Button turn = null;
                    turn = Button(Kind == "Grill" ? "뒤집기" : "꺼내기", new Vector2(0, -90), () => {
                        heatScore += TimingScore(value); hits++; started = Time.unscaledTime; value = 0;
                        if (Kind == "Simmer" || hits >= 2) FinishPhase(heatScore / hits);
                        else { feedback.text = "뒷면을 구워 주세요."; turn.GetComponentInChildren<TMP_Text>().text = "꺼내기"; }
                    });
                }
                else
                {
                    heatLabel = Label("", -470, 22);
                    heatLabel.rectTransform.sizeDelta = new Vector2(1160, 40);
                    Button("불 낮추기", new Vector2(-280, -90), () => heat = Mathf.Clamp01(heat - .15f));
                    Button("불 높이기", new Vector2(0, -90), () => heat = Mathf.Clamp01(heat + .15f));
                    Button("꺼내기", new Vector2(280, -90), () => FinishPhase(heatElapsed < 8 ? 0 : 5 * heatScore / Mathf.Max(1, heatElapsed)));
                    if (Kind == "Boil" || Kind == "Add")
                        Button("물 맞추기", new Vector2(0, 0), () => { temperature = .5f; heat = .5f; });
                }
                break;
            case 5:
                instruction.text = "재료를 1 → 2 → 3 → 4 순서로 접시 쪽(아래)으로 드래그하세요.";
                int[] order = { 3, 1, 4, 2 };
                for (int i = 0; i < order.Length; i++)
                {
                    int number = order[i]; Button plate = null;
                    plate = Button(number.ToString() + " ↓", new Vector2((i - 1.5f) * 280, 0), () => { });
                    plate.gameObject.AddComponent<ResearchGesture>().Stroke(Vector2.down, 70, () => {
                        if (number != expected + 1) { mistakes++; feedback.text = $"다음 재료: {expected + 1}"; return; }
                        expected++; plate.gameObject.SetActive(false);
                        if (expected == 4) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    }, () => { mistakes++; });
                }
                break;
        }
    }
    private void Gauge()
    {
        var track = DayLoopUI.Chunky(DayLoopUI.Panel(surface, new Color32(0xD6, 0xEC, 0xF0, 0xFF)), 3f, 0f).rectTransform;
        DayLoopUI.Place(track, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(900, 64));
        var hot = DayLoopUI.Panel(track, new Color32(0xF6, 0xC3, 0xB5, 0xFF), "Hot").rectTransform;
        hot.anchorMin = new Vector2(.6f, 0); hot.anchorMax = new Vector2(1, 1); hot.offsetMin = hot.offsetMax = Vector2.zero;
        var target = DayLoopUI.Panel(track, new Color32(0x8F, 0xD0, 0xA3, 0xFF), "Target", false).rectTransform;
        target.anchorMin = new Vector2(.4f, 0); target.anchorMax = new Vector2(.6f, 1); target.offsetMin = target.offsetMax = Vector2.zero;
        fill = DayLoopUI.Panel(track, DayLoopUI.Kit.Line);
        fill.rectTransform.pivot = new Vector2(.5f, .5f);
        fill.rectTransform.sizeDelta = new Vector2(14, 80);
    }

    // 단계 종류를 진행 표시용 짧은 이름으로 바꾼다.
    private static string StepLabel(string kind)
    {
        switch (kind)
        {
            case "Wash": return "씻기";
            case "Cut": return "손질";
            case "Mix": return "섞기";
            case "Add": return "넣기";
            case "Shape": return "성형";
            case "Grill": return "굽기";
            case "Fry": return "튀기기";
            case "Simmer": return "졸이기";
            case "Stir": return "젓기";
            case "Boil": return "끓이기";
            case "Plate": return "담기";
            default: return "단계";
        }
    }

    // 단계 진행 표시: 끝난 단계는 점수, 지금 단계는 차양색, 남은 단계는 돛천색 (요리마다 단계 수가 다르다)
    private void Steps()
    {
        var row = DayLoopUI.Rect("Steps", surface);
        DayLoopUI.Place(row, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(1180, 92));
        var layout = DayLoopUI.Horizontal(row, 0, 0, TextAnchor.UpperCenter);
        layout.childForceExpandWidth = true;
        float average = scores.Count > 0 ? scores.Average() : 0f;
        for (int i = 0; i < steps.Length; i++)
        {
            bool done = i < scores.Count, current = i == phase;
            var step = DayLoopUI.Rect("Step " + (i + 1), row);
            DayLoopUI.Layout(step, flexibleWidth: 1, preferredHeight: 92);
            DayLoopUI.Vertical(step, 0, 2, TextAnchor.UpperCenter).childControlWidth = false;
            var dot = DayLoopUI.Chunky(DayLoopUI.Panel(step, done ? DayLoopUI.Kit.Kelp : current ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sail, "Dot"), 3f, 0f);
            dot.rectTransform.sizeDelta = new Vector2(42, 42);
            DayLoopUI.Layout(dot, preferredHeight: 42);
            var number = DayLoopUI.Text(dot.transform, done ? DayLoopUI.Glyph("✓", "V") : (i + 1).ToString(), 21, done || current ? Color.white : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            DayLoopUI.Stretch(number.rectTransform);
            var stepName = DayLoopUI.Text(step, StepLabel(steps[i].kind), 20, current ? DayLoopUI.Kit.WarnInk : done ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            stepName.rectTransform.sizeDelta = new Vector2(140, 24);
            if (done)
            {
                var score = DayLoopUI.Text(step, $"{scores[i]:0.0}점", 16, DayLoopUI.Kit.GoodInk, TextAlignmentOptions.Center, FontStyles.Bold);
                score.rectTransform.sizeDelta = new Vector2(140, 20);
            }
        }
        if (scores.Count == 0) return;
        var summary = DayLoopUI.Chip(surface, $"지금까지 평균 {average:0.0} / 5 · 4점 이상 해금", average >= 4f ? DayLoopUI.Kit.GoodTint : DayLoopUI.Kit.Sand, average >= 4f ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink, 17, "Average");
        var summaryRect = (RectTransform)summary.transform.parent;
        var fit = summaryRect.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        DayLoopUI.Place(summaryRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 26), Vector2.zero);
    }
    public static float TimingScore(float position) => Mathf.Clamp(5 - Mathf.Max(0, Mathf.Abs(position - .5f) - .1f) * 12.5f, 0, 5);
    private void Update()
    {
        if (transitioning || fill == null) return;
        if (Mode == 4)
        {
            if (Kind == "Grill" || Kind == "Simmer") value = Mathf.Clamp01((Time.unscaledTime - started) * .1f);
            else if (Kind == "Stir") value = Mathf.PingPong((Time.unscaledTime - lastHit) * .7f, 1);
            else
            {
                float dt = Time.unscaledDeltaTime;
                temperature = Mathf.Clamp01(temperature + (heat - .45f) * dt * .3f);
                heatElapsed += dt;
                if (temperature >= .4f && temperature <= .6f) heatScore += dt;
                value = temperature;
                if (heatLabel != null) heatLabel.text = $"온도 {temperature*200:0}° · 화력 {heat*100:0}% · {heatElapsed:0.0}초 / 8초";
            }
        }
        fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(value, .5f);
        fill.rectTransform.anchoredPosition = Vector2.zero;
    }
    private void FinishPhase(float score)
    {
        if (transitioning) return;
        transitioning = true; total += score;
        feedback.text = $"{steps[phase].text} {score:0.0}점 / 5점";
        scores.Add(score);
        // 다음 단계는 명시적으로 진행해 결과와 조작 안내를 읽을 시간을 준다.
        bool last = phase == steps.Length - 1;
        var next = DayLoopUI.KitButton(surface, last ? "결과 확인" : "다음 단계", last ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sea, Color.white, 26, () => {
            if (++phase < steps.Length) ShowPhase();
            else { var callback = finished; finished = null; callback?.Invoke(total / steps.Length); Destroy(this); }
        });
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(220, 66));
    }
}