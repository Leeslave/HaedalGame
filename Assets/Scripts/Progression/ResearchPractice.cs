using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 연구 실습 UI. 위쪽에 6단계 진행(완료 단계는 점수)과 지금까지 평균을 보여 준다.
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
    private bool holding, transitioning;
    private Image fill;
    private readonly string[] names = { "씻기", "손질", "재료 섞기", "성형", "가열", "담기" };
    private readonly string[] hints = { "오염 지점", "칼질 간격", "레시피 재료", "누르고 놓기", "불 조절", "접시 순서" };
    private readonly System.Collections.Generic.List<float> scores = new System.Collections.Generic.List<float>();

    public void Begin(RectTransform parent, RecipeData data, Action<float> onFinished)
    {
        recipe = data; finished = onFinished;
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
        // 위쪽 단계 표시·안내 문구 아래로 조작 영역을 내린다.
        DayLoopUI.Place((RectTransform)button.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position + new Vector2(0, -70), new Vector2(230, 80));
        return button;
    }
    private void ShowPhase()
    {
        foreach (Transform child in surface) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        hits = mistakes = expected = 0; holding = transitioning = false; fill = null; value = 0;
        started = Time.unscaledTime; lastHit = -100;
        Steps();
        Label($"{recipe.RecipeName} · {phase + 1}/6 {names[phase]}", -124, 34);
        instruction = Label("", -186, 24);
        instruction.color = DayLoopUI.Kit.WoodLight;
        feedback = Label("", -566, 25);
        feedback.rectTransform.sizeDelta = new Vector2(520, 56);
        feedback.color = DayLoopUI.Kit.SeaDeep;
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
        var track = DayLoopUI.Chunky(DayLoopUI.Panel(surface, new Color32(0xD6, 0xEC, 0xF0, 0xFF)), 3f, 0f).rectTransform;
        DayLoopUI.Place(track, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 30), new Vector2(900, 64));
        var hot = DayLoopUI.Panel(track, new Color32(0xF6, 0xC3, 0xB5, 0xFF), "Hot").rectTransform;
        hot.anchorMin = new Vector2(.6f, 0); hot.anchorMax = new Vector2(1, 1); hot.offsetMin = hot.offsetMax = Vector2.zero;
        var target = DayLoopUI.Panel(track, new Color32(0x8F, 0xD0, 0xA3, 0xFF), "Target", false).rectTransform;
        target.anchorMin = new Vector2(.4f, 0); target.anchorMax = new Vector2(.6f, 1); target.offsetMin = target.offsetMax = Vector2.zero;
        fill = DayLoopUI.Panel(track, DayLoopUI.Kit.Line);
        fill.rectTransform.pivot = new Vector2(.5f, .5f);
        fill.rectTransform.sizeDelta = new Vector2(14, 80);
    }

    // 6단계 진행 표시: 끝난 단계는 점수, 지금 단계는 차양색, 남은 단계는 돛천색
    private void Steps()
    {
        var row = DayLoopUI.Rect("Steps", surface);
        DayLoopUI.Place(row, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(1180, 100));
        var layout = DayLoopUI.Horizontal(row, 0, 0, TextAnchor.UpperCenter);
        layout.childForceExpandWidth = true;
        float average = scores.Count > 0 ? scores.Average() : 0f;
        for (int i = 0; i < names.Length; i++)
        {
            bool done = i < scores.Count, current = i == phase;
            var step = DayLoopUI.Rect(names[i], row);
            DayLoopUI.Layout(step, flexibleWidth: 1, preferredHeight: 100);
            DayLoopUI.Vertical(step, 0, 2, TextAnchor.UpperCenter).childControlWidth = false;
            var dot = DayLoopUI.Chunky(DayLoopUI.Panel(step, done ? DayLoopUI.Kit.Kelp : current ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sail, "Dot"), 3f, 0f);
            dot.rectTransform.sizeDelta = new Vector2(46, 46);
            DayLoopUI.Layout(dot, preferredHeight: 46);
            var number = DayLoopUI.Text(dot.transform, done ? DayLoopUI.Glyph("✓", "V") : (i + 1).ToString(), 22, done || current ? Color.white : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            DayLoopUI.Stretch(number.rectTransform);
            var stepName = DayLoopUI.Text(step, names[i], 21, current ? DayLoopUI.Kit.WarnInk : done ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            stepName.rectTransform.sizeDelta = new Vector2(180, 26);
            var stepHint = DayLoopUI.Text(step, done ? $"{scores[i]:0.0}점" : hints[i], 16, done ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            stepHint.rectTransform.sizeDelta = new Vector2(180, 20);
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
        scores.Add(score);
        // 다음 단계는 명시적으로 진행해 결과와 조작 안내를 읽을 시간을 준다.
        var next = DayLoopUI.KitButton(surface, phase == 5 ? "결과 확인" : "다음 단계", phase == 5 ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sea, Color.white, 26, () => {
            if (++phase < names.Length) ShowPhase();
            else { var callback = finished; finished = null; callback?.Invoke(total / names.Length); Destroy(this); }
        });
        DayLoopUI.Place((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(220, 66));
    }
}
