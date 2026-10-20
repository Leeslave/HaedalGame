using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>시안에서 내려받은 전용 에셋. 기존 씬의 UI 이미지/스프라이트 참조를 사용하지 않는다.</summary>
public static class DesignArt
{
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public static Sprite Load(string name)
    {
        if (sprites.TryGetValue(name, out var cached) && cached != null) return cached;
        var texture = Resources.Load<Texture2D>("UIDesign/Art/" + name);
        if (texture == null) return null;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
        sprite.name = "Design " + name; sprite.hideFlags = HideFlags.DontSave;
        sprites[name] = sprite; return sprite;
    }
    public static Sprite Food(RecipeData recipe)
    {
        if (recipe == null) return null;
        string name = recipe.RecipeName.Replace(" ", "");
        return name.Contains("고등어구이") ? Load("FoodGrilledFish") : name.Contains("광어회") ? Load("FoodSashimi") : name.Contains("조개찜") ? Load("FoodClams") : null;
    }

    public static void ButtonIcon(UnityEngine.UI.Button button, string label, float scale = 1.5f)
    {
        string name = label.StartsWith("적용") ? "Check" : label.Contains("기본값") || label.Contains("복원") || label.Contains("회전") || label.Contains("되돌리기") ? "Reset"
            : label == "관리" ? "Menu" : label.Contains("인석") ? "Furniture" : label.Contains("메뉴") ? "Menu" : label.Contains("재료") ? "Ingredient" : label.Contains("직원") ? "Staff" : label.Contains("가구") || label.Contains("배치") ? "Furniture"
            : label.Contains("도구") || label.Contains("대장간") ? "Tools" : label.Contains("엘프") ? "Elf" : label.Contains("상점") ? "Shop" : label.Contains("연구") ? "Research"
            : label.Contains("탐험") || label.Contains("선착장") ? "Explore" : label.Contains("집") || label.Contains("취침") ? "House" : label.Contains("설정") ? "Settings"
            : label.Contains("닫기") || label.Contains("종료") || label.Contains("취소") || label.Contains("섬으로") ? "Exit" : "Play";
        var icon = ReferenceTitleView.Picture(button.transform, "Icon" + name);
        icon.raycastTarget = false; icon.preserveAspect = true;
        icon.color = button.GetComponentInChildren<TMP_Text>().color;
        DayLoopUI.Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14 * scale, 0), new Vector2(22 * scale, 22 * scale));
        var text = button.GetComponentInChildren<TMP_Text>();
        DayLoopUI.Stretch(text.rectTransform, 48 * scale, 4 * scale, 8 * scale, 4 * scale);
        text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
    }
}
