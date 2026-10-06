using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>연구 실습의 임시 UI. 재료 예약과 해금은 RestaurantProgress가 담당한다.</summary>
public class ResearchPractice : MonoBehaviour
{
    private RectTransform surface;
    private TMP_Text instruction, feedback;
    private RecipeData recipe;
    private Action<float> finished;
    private int phase, hits, mistakes, expected;
    private float total, started, lastHit, value;
    private bool holding, transitioning;
    private Image fill;
    private readonly string[] names = { "씻기", "손질", "재료 섞기", "성형", "가열", "담기" };

    public void Begin(RectTransform parent, RecipeData data, Action<float> onFinished)
    {
        recipe = data; finished = onFinished;
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
        hits = mistakes = expected = 0; holding = transitioning = false; fill = null; value = 0;
        started = Time.unscaledTime; lastHit = -100;
        Label($"{recipe.RecipeName} · {phase + 1}/6 {names[phase]}", -10, 32);
        instruction = Label("", -85);
        feedback = Label("", -465);
        switch (phase)
        {
            case 0:
                instruction.text = "오염된 곳 5개를 눌러 씻으세요. 12초 안에 모두 씻으면 5점이에요.";
                for (int i = 0; i < 5; i++)
                {
                    Button spot = null;
                    spot = Button("씻기", new Vector2((i % 3 - 1) * 280, 50 - i / 3 * 120), () => {
                        spot.interactable = false; spot.gameObject.SetActive(false);
                        if (++hits == 5) FinishPhase(Mathf.Clamp(5 - Mathf.Max(0, Time.unscaledTime - started - 12) / 4, 0, 5));
                    });
                }
                break;
            case 1:
                instruction.text = "0.4초 이상 간격을 두고 칼질 6번. 너무 빠르면 점수가 줄어요.";
                Button("칼질", Vector2.zero, () => {
                    if (Time.unscaledTime - lastHit < .4f) mistakes++;
                    lastHit = Time.unscaledTime; hits++;
                    feedback.text = $"손질 {hits}/6 · 실수 {mistakes}";
                    if (hits == 6) FinishPhase(Mathf.Max(0, 5 - mistakes));
                });
                break;
            case 2:
                var required = recipe.Requirements.Select(r => r.IngredientId).Distinct().ToList();
                var options = required.Concat(GameDatabase.Recipes.Ingredients.Select(i => i.IngredientId).Where(id => !required.Contains(id)).Take(2))
                    .OrderBy(_ => UnityEngine.Random.value).ToList();
                instruction.text = "이 요리에 들어가는 재료만 골라 넣으세요. 틀린 재료는 1점 감점돼요.";
                for (int i = 0; i < options.Count; i++)
                {
                    int id = options[i]; GameDatabase.Recipes.TryGetIngredientById(id, out var ingredient);
                    Button choice = null;
                    choice = Button(ingredient.IngredientName, new Vector2((i % 4 - 1.5f) * 285, 100 - i / 4 * 85), () => {
                        choice.interactable = false;
                        if (required.Contains(id)) hits++; else mistakes++;
                        feedback.text = $"올바른 재료 {hits}/{required.Count} · 실수 {mistakes}";
                        if (hits == required.Count) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    });
                }
                break;
            case 3:
                instruction.text = "반죽 버튼을 누르고 있다가 막대가 초록 구간에 도달하면 놓으세요.";
                Gauge();
                var press = Button("꾹 누르고 놓기", new Vector2(0, -80), () => { });
                var trigger = press.gameObject.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => { if (!transitioning) holding = true; }); trigger.triggers.Add(down);
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(_ => { if (holding && !transitioning) { holding = false; FinishPhase(TimingScore(value)); } }); trigger.triggers.Add(up);
                break;
            case 4:
                instruction.text = "불 조절 3번 · 막대가 초록 구간에 올 때 멈추세요.";
                Gauge();
                float heatScore = 0;
                Button("불 조절", new Vector2(0, -80), () => {
                    heatScore += TimingScore(value); hits++; started = Time.unscaledTime; value = 0;
                    feedback.text = $"불 조절 {hits}/3";
                    if (hits == 3) FinishPhase(heatScore / 3);
                });
                break;
            case 5:
                instruction.text = "접시 번호 1 → 2 → 3 → 4 순서로 담으세요. 잘못 누르면 1점 감점돼요.";
                int[] order = { 3, 1, 4, 2 };
                for (int i = 0; i < order.Length; i++)
                {
                    int number = order[i]; Button plate = null;
                    plate = Button(number.ToString(), new Vector2((i - 1.5f) * 280, 0), () => {
                        if (number != expected + 1) { mistakes++; feedback.text = $"다음 접시: {expected + 1} · 실수 {mistakes}"; return; }
                        expected++; plate.interactable = false;
                        if (expected == 4) FinishPhase(Mathf.Max(0, 5 - mistakes));
                    });
                }
                break;
        }
    }
    private void Gauge()
    {
        var track = DayLoopUI.Panel(surface, DayLoopUI.Wood).rectTransform;
        DayLoopUI.Place(track, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 60), new Vector2(900, 60));
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
        if (phase == 3 && holding) value = Mathf.Min(1, value + Time.unscaledDeltaTime * .3f);
        if (phase == 4) value = Mathf.PingPong((Time.unscaledTime - started) * .6f, 1);
        fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(value, .5f);
        fill.rectTransform.anchoredPosition = Vector2.zero;
        if (phase == 3 && holding && value >= 1) { holding = false; FinishPhase(0); }
    }
    private void FinishPhase(float score)
    {
        if (transitioning) return;
        transitioning = true; total += score;
        feedback.text = $"{names[phase]} {score:0.0}점 / 5점";
        // 다음 단계는 명시적으로 진행해 결과와 조작 안내를 읽을 시간을 준다.
        var next = DayLoopUI.Button(surface, phase == 5 ? "결과 확인" : "다음 단계", DayLoopUI.Wood, Color.white, 24, () => {
            if (++phase < names.Length) ShowPhase();
            else { var callback = finished; finished = null; callback?.Invoke(total / names.Length); Destroy(this); }
        });
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(200, 60));
    }
}
