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
    // 요리·재료 그림 (Resources/UIDesign/Art/Food, Ingredient). 이름은 띄어쓰기를 뺀 한글 이름으로 찾는다.
    // 그림이 없는 요리·재료는 null을 돌려주고, 화면은 첫 글자 동그라미로 대신 보여 준다.
    private static readonly Dictionary<string, string> FoodArt = new Dictionary<string, string>
    {
        { "가리비구이", "ScallopGrill" }, { "고등어구이", "MackerelGrill" }, { "고등어된장조림", "MackerelDoenjang" },
        { "광어회", "FlounderSashimi" }, { "문어숙회", "OctopusSukhoe" }, { "새우구이", "ShrimpGrill" },
        { "새우완자탕", "ShrimpBallSoup" }, { "새우튀김", "ShrimpTempura" }, { "생선가스", "FishCutlet" },
        { "연어간장구이", "SalmonSoyGrill" }, { "오징어숙회", "SquidSukhoe" }, { "조개찜", "ClamSteam" },
    };
    private static readonly Dictionary<string, string> IngredientArt = new Dictionary<string, string>
    {
        { "새우", "Shrimp" }, { "가리비", "Scallop" }, { "오징어", "Squid" }, { "조개", "Clam" }, { "광어", "Flounder" },
        { "문어", "Octopus" }, { "고등어", "Mackerel" }, { "연어", "Salmon" }, { "바지락", "ManilaClam" }, { "흰살생선", "WhiteFish" },
        { "소금", "Salt" }, { "설탕", "Sugar" }, { "식용유", "Oil" }, { "전분", "Starch" }, { "후추", "Pepper" }, { "밀가루", "Flour" },
        { "식초", "Vinegar" }, { "와사비", "Wasabi" }, { "간장", "SoySauce" }, { "미림", "Mirin" }, { "레몬", "Lemon" }, { "마늘", "Garlic" },
        { "된장", "Doenjang" }, { "생강", "Ginger" }, { "계란", "Egg" }, { "꿀", "Honey" }, { "겨자", "Gyeoja" },
    };
    public static Sprite Food(RecipeData recipe) =>
        recipe != null && FoodArt.TryGetValue(recipe.RecipeName.Replace(" ", ""), out var file) ? Load("Food/" + file) : null;
    public static Sprite Ingredient(IngredientData ingredient) =>
        ingredient != null && IngredientArt.TryGetValue(ingredient.IngredientName.Replace(" ", ""), out var file) ? Load("Ingredient/" + file) : null;
    public static Sprite Ingredient(int ingredientId) =>
        GameDatabase.Recipes != null && GameDatabase.Recipes.TryGetIngredientById(ingredientId, out var item) ? Ingredient(item) : null;

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
