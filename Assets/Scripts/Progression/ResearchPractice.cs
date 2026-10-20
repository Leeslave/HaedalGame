using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>요리별 연구 실습 UI. 재료 예약과 해금은 RestaurantProgress가 담당한다.</summary>
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

    public void Begin(RectTransform parent, RecipeData data, Action<float> onFinished)
    {
        recipe = data; finished = onFinished;
        steps = RecipeResearchPlan.Find(data.RecipeId)?.steps;
        if (steps == null || steps.Length == 0) { onFinished?.Invoke(0); Destroy(this); return; }
        surface = DayLoopUI.Panel(parent, DayLoopUI.CreamDeep).rectTransform;
        DayLoopUI.Layout(surface, preferredHeight: 560);
        ShowPhase();
    }

    private TMP_Text Label(string text, float y, int size = 25)
    {
        var label = DayLoopUI.Text(surface, text, size, DayLoopUI.Ink, TextAlignmentOptions.Center);
        DayLoopUI.Place(label.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(1160, 70));
        return label;
    }
    private Button Button(string text, Vector2 position, Action action)
    {
        var button = DayLoopUI.Button(surface, text, DayLoopUI.Sea, Color.white, 24, () => { if (!transitioning) action(); });
        DayLoopUI.Place((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(230, 80));
        return button;
    }
    private void ShowPhase()
    {
        foreach (Transform child in surface) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        hits = mistakes = expected = 0; transitioning = false; fill = null; value = 0; heatLabel = null; heatElapsed = heatScore = 0; temperature = heat = .5f;
        started = Time.unscaledTime; lastHit = -100;
        Label($"{recipe.RecipeName} · {phase + 1}/{steps.Length}", -10, 32);
        Label(steps[phase].text, -70, 26);
        instruction = Label("", -135);
        feedback = Label("", -465);
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
                    heatLabel = Label("", -410, 22);
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
        var track = DayLoopUI.Panel(surface, DayLoopUI.Wood).rectTransform;
        DayLoopUI.Place(track, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(900, 60));
        var target = DayLoopUI.Panel(track, DayLoopUI.Good).rectTransform;
        target.anchorMin = new Vector2(.4f, 0); target.anchorMax = new Vector2(.6f, 1); target.offsetMin = target.offsetMax = Vector2.zero;
        fill = DayLoopUI.Panel(track, DayLoopUI.Coral);
        fill.rectTransform.pivot = new Vector2(.5f, .5f);
        fill.rectTransform.sizeDelta = new Vector2(12, 60);
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
        // 다음 단계는 명시적으로 진행해 결과와 조작 안내를 읽을 시간을 준다.
        var next = DayLoopUI.Button(surface, phase == steps.Length - 1 ? "결과 확인" : "다음 단계", DayLoopUI.Wood, Color.white, 24, () => {
            if (++phase < steps.Length) ShowPhase();
            else { var callback = finished; finished = null; callback?.Invoke(total / steps.Length); Destroy(this); }
        });
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(200, 60));
    }
}
