using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>연구 실습 결과 규칙. 메달과 셰프 한마디는 평균 점수로 정한다. (해금 기준 4점은 RestaurantProgress가 정한다)</summary>
public static class ResearchResultRules
{
    public const float UnlockScore = 4f;
    /// <summary>평균 4.75 이상 금, 4.4 이상 은, 4 이상 동. 4점 아래는 메달이 없다.</summary>
    public static string Medal(float average) => average >= 4.75f ? "금메달" : average >= 4.4f ? "은메달" : average >= UnlockScore ? "동메달" : null;
    public static string MedalArt(float average) => average >= 4.75f ? "Lab/LabMedalGold" : average >= 4.4f ? "Lab/LabMedalSilver" : average >= UnlockScore ? "Lab/LabMedalBronze" : "Lab/LabMedalNone";
    public static string ChefLine(float average, bool unlocked)
    {
        if (!unlocked) return "아깝다! 점수가 낮았던 단계를 신경 써서 다시 해 보자.";
        if (average >= 4.75f) return "완벽해! 이대로 손님상에 내도 되겠어.";
        if (average >= 4.4f) return "맛있다! 조금만 더 다듬으면 최고야.";
        return "좋아, 이제 이 요리를 만들 줄 알게 됐어!";
    }
}

/// <summary>
/// 연구 실습이 끝난 뒤 보여 주는 요리 완성 화면. 접시에 담긴 요리, 단계별 점수, 메달을 보여 주고
/// 해금됐으면 [메뉴에 올리기], 못 했으면 [다시 도전]을 고르게 한다.
/// </summary>
public class ResearchResultView : MonoBehaviour
{
    private const float FrameW = 1920f, FrameH = 1080f;
    private RectTransform frame, rays;

    public static ResearchResultView Open(Transform screen, RecipeData recipe, string[] stepTexts, float[] scores, float average, bool unlocked, string error,
        Action onPrimary, Action onClose)
    {
        var layer = DayLoopUI.Rect("ResearchResult", screen);
        DayLoopUI.Stretch(layer);
        layer.SetAsLastSibling();
        var view = layer.gameObject.AddComponent<ResearchResultView>();
        view.Build(recipe, stepTexts, scores, average, unlocked, error, onPrimary, onClose);
        return view;
    }

    private void Build(RecipeData recipe, string[] stepTexts, float[] scores, float average, bool unlocked, string error, Action onPrimary, Action onClose)
    {
        var root = (RectTransform)transform;
        var floor = DayLoopUI.Panel(root, new Color32(0xE2, 0xB5, 0x7E, 0xFF), "Floor", false);
        DayLoopUI.Stretch(floor.rectTransform);
        // 조리대 배경을 깔고 어둡게 덮은 뒤, 그 위 frame에 접시와 결과 카드를 그린다.
        frame = DayLoopUI.Rect("Back", root);
        DayLoopUI.Place(frame, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(FrameW, FrameH));
        Art("Lab/LabKitchen", 960, 540, FrameW, FrameH);
        var dim = DayLoopUI.Panel(root, new Color(.16f, .09f, .05f, .55f), "Dim", false);
        DayLoopUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        frame = DayLoopUI.Rect("Frame", root);
        DayLoopUI.Place(frame, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(FrameW, FrameH));

        // 왼쪽: 빛, 접시, 요리
        rays = Art("Lab/LabRays", 540, 610, 1000, 1000).rectTransform;
        Art("Lab/LabPlate", 540, 600, 900, 600);
        var food = Art(null, 540, 585, 560, 560);
        food.sprite = LabStation.DishArt(recipe); food.preserveAspect = true;
        food.enabled = food.sprite != null;
        food.rectTransform.localEulerAngles = new Vector3(0, 0, -8);
        if (!unlocked) food.color = new Color(.85f, .82f, .78f);

        var banner = DayLoopUI.Card(frame, unlocked ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.WoodLight, "Banner", 4f, 6f);
        TopLeft(banner.rectTransform, 225, 42, 630, 114);
        var title = DayLoopUI.Text(banner.transform, unlocked ? "요리 완성!" : "다시 도전!", 60, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
        DayLoopUI.Stretch(title.rectTransform);

        // 오른쪽: 결과 카드
        var card = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "ResultCard", 4f, 8f);
        TopLeft(card.rectTransform, 1110, 42, 810, 840);
        var medal = DayLoopUI.Panel(card.transform, Color.white, "Medal", false);
        medal.sprite = DesignArt.Load(ResearchResultRules.MedalArt(average)); medal.raycastTarget = false; medal.preserveAspect = true;
        TopLeft(medal.rectTransform, 40, 30, 120, 150);
        Label(card.transform, recipe != null ? recipe.RecipeName : "새 요리", 58, DayLoopUI.Kit.Ink, 186, 34, 590, 70);
        string medalName = ResearchResultRules.Medal(average);
        Label(card.transform, $"평균 {average:0.00} / 5점" + (medalName != null ? " · " + medalName : ""), 34,
            unlocked ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.WarnInk, 188, 104, 590, 46);
        string ingredients = recipe != null ? string.Join(" · ", recipe.Requirements.Select(r => $"{r.IngredientName} {r.Amount}")) : "";
        Label(card.transform, ingredients, 21, DayLoopUI.Kit.InkSoft, 190, 152, 590, 30);
        var rule = DayLoopUI.Panel(card.transform, DayLoopUI.Kit.Divider, "Divider", false);
        TopLeft(rule.rectTransform, 40, 200, 730, 4);

        int count = Mathf.Min(stepTexts.Length, scores.Length);
        float rowH = Mathf.Clamp(360f / Mathf.Max(1, count), 52f, 76f);
        for (int i = 0; i < count; i++)
        {
            float y = 220 + i * rowH;
            var step = Label(card.transform, $"{i + 1}. {stepTexts[i]}", 26, DayLoopUI.Kit.Ink, 44, y, 455, rowH);
            step.overflowMode = TextOverflowModes.Ellipsis; step.textWrappingMode = TextWrappingModes.NoWrap;
            string verdict = GrillRules.Verdict(scores[i]);
            Color badgeColor = scores[i] >= 4.5f ? DayLoopUI.Kit.Coin : scores[i] >= 3.5f ? DayLoopUI.Kit.Kelp : scores[i] >= 2f ? DayLoopUI.Kit.Divider : DayLoopUI.Kit.Alert;
            var badge = DayLoopUI.Card(card.transform, badgeColor, "Badge" + i, 0f, 0f);
            TopLeft(badge.rectTransform, 510, y + (rowH - 46) / 2, 170, 46);
            var badgeText = DayLoopUI.Text(badge.transform, verdict, 27, scores[i] >= 3.5f && scores[i] < 4.5f || scores[i] < 2f ? Color.white : DayLoopUI.Kit.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            DayLoopUI.Stretch(badgeText.rectTransform);
            Label(card.transform, scores[i].ToString("0.0"), 32, DayLoopUI.Kit.Ink, 690, y, 80, rowH, TextAlignmentOptions.MidlineRight);
        }

        var note = DayLoopUI.Chunky(DayLoopUI.Panel(card.transform, unlocked ? DayLoopUI.Kit.Sand : DayLoopUI.Kit.WarnTint, "Note"), 3f, 0f);
        TopLeft(note.rectTransform, 40, 600, 730, 104);
        if (unlocked)
        {
            var tag = DayLoopUI.Card(note.transform, DayLoopUI.Kit.Awning, "New", 0f, 0f);
            TopLeft(tag.rectTransform, 22, 32, 86, 40);
            var tagText = DayLoopUI.Text(tag.transform, "NEW", 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, "Label");
            DayLoopUI.Stretch(tagText.rectTransform);
            Label(note.transform, "새 메뉴 등록! 오늘 메뉴에 올리면 바로 팔 수 있어요.", 24, DayLoopUI.Kit.Ink, 126, 16, 590, 36);
            Label(note.transform, recipe != null ? $"★{recipe.RecipeGrade} · 판매가 {recipe.Price:0}G" : "", 22, DayLoopUI.Kit.InkSoft, 126, 54, 590, 32);
        }
        else
            Label(note.transform, $"평균 {ResearchResultRules.UnlockScore:0}점 이상이면 배울 수 있어요.\n예약한 재료는 그대로라 바로 다시 도전할 수 있어요.", 24,
                DayLoopUI.Kit.WarnInk, 28, 10, 680, 84);
        if (!unlocked && !string.IsNullOrEmpty(error) && !error.Contains("4점")) Label(card.transform, error, 20, DayLoopUI.Kit.WarnInk, 40, 708, 730, 28);

        var primary = DayLoopUI.KitButton(card.transform, unlocked ? "▶  메뉴에 올리기" : "▶  다시 도전", DayLoopUI.Kit.Awning, Color.white, 36, () => Close(onPrimary), "Primary");
        TopLeft((RectTransform)primary.transform, 40, 736, 440, 84);
        var close = DayLoopUI.KitButton(card.transform, "닫기", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 34, () => Close(onClose), "Close");
        TopLeft((RectTransform)close.transform, 500, 736, 270, 84);

        // 셰프 한마디
        var talk = DayLoopUI.Card(frame, DayLoopUI.Kit.Sail, "Talk", 4f, 8f);
        TopLeft(talk.rectTransform, 54, 918, 990, 144);
        var face = DayLoopUI.Card(talk.transform, new Color32(0xF5, 0xD6, 0xB8, 0xFF), "Face", 4f, 0f);
        DayLoopUI.Place(face.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, 0), new Vector2(108, 108));
        face.gameObject.AddComponent<RectMask2D>();
        var portrait = DayLoopUI.Panel(face.transform, Color.white, "Chef", false);
        portrait.sprite = DesignArt.Load("OtterChef"); portrait.raycastTarget = false;
        DayLoopUI.Place(portrait.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(14, -14), new Vector2(156, 156));
        Label(talk.transform, "해달 셰프", 22, DayLoopUI.Kit.Sea, 154, 22, 300, 30);
        Label(talk.transform, ResearchResultRules.ChefLine(average, unlocked), 34, DayLoopUI.Kit.Ink, 154, 58, 810, 60);
    }

    private void Close(Action next)
    {
        gameObject.SetActive(false); Destroy(gameObject);
        next?.Invoke();
    }
    private void Update()
    {
        if (rays != null) rays.localEulerAngles = new Vector3(0, 0, Time.unscaledTime * 6f);
    }

    // ───────────── 도우미 ─────────────
    private void TopLeft(RectTransform rect, float x, float y, float w, float h) =>
        DayLoopUI.Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));
    /// <summary>가운데 좌표(cx, cy) 기준으로 그림을 놓는다.</summary>
    private Image Art(string asset, float cx, float cy, float w, float h)
    {
        var image = DayLoopUI.Panel(frame, Color.white, asset ?? "Art", false);
        if (asset != null) image.sprite = DesignArt.Load(asset);
        image.raycastTarget = false;
        DayLoopUI.Place(image.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(cx, -cy), new Vector2(w, h));
        return image;
    }
    private TMP_Text Label(Transform parent, string text, float size, Color color, float x, float y, float w, float h,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var label = DayLoopUI.Text(parent, text, size, color, align, FontStyles.Bold, size >= 28 ? "Label" : "Text");
        TopLeft(label.rectTransform, x, y, w, h);
        label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = 30f;
        return label;
    }
}
