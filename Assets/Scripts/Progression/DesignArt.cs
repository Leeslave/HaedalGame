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

    // 플레이씬 시안(Figma 386:73)에서 2x로 내보낸 늘림용 조각의 9-slice 테두리.
    // 값은 2x 텍스처 픽셀 기준이며 Unity 순서(왼쪽, 아래, 오른쪽, 위)다. 모서리 반경·외곽선·그림자 두께를 시안 수치에서 계산했다.
    public static readonly IReadOnlyDictionary<string, Vector4> SliceBorders = new Dictionary<string, Vector4>
    {
        { "PlayHud/SignPaper", new Vector4(12, 12, 12, 12) },        // 영업 팻말 끼운 종이, 반경 6
        { "PlayHud/MenuCardPaper", new Vector4(10, 10, 10, 10) },    // 탁상 메뉴판 종이, 반경 5
        { "PlayHud/MenuCardStand", new Vector4(8, 8, 8, 8) },        // 메뉴판 받침목, 반경 4
        { "PlayHud/HudButtonFrame", new Vector4(18, 18, 18, 18) },   // 우측 HUD 나무 테, 반경 9
        { "PlayHud/HudGoldFrame", new Vector4(18, 18, 18, 18) },
        { "PlayHud/HudSpeedFrame", new Vector4(18, 18, 18, 18) },
        { "PlayHud/HudButtonInner", new Vector4(10, 10, 10, 10) },   // 나무 테 안쪽 종이, 반경 5
        { "PlayHud/PortraitFrame", new Vector4(26, 26, 26, 26) },    // 직원 초상 배경, 반경 13
        { "PlayHud/HealthTrack", new Vector4(10, 10, 10, 10) },      // 체력 게이지, 높이 10·반경 5 (가로로만 늘린다)
        { "PlayHud/HealthFill", new Vector4(10, 10, 10, 10) },
        { "PlayHud/PatienceBar", new Vector4(6, 6, 6, 6) },          // 손님 인내심, 반경 3
        // 확인/건너뛰기 버튼: 반경 23 + 바깥 외곽선 4, 아래로 6 밀린 그림자까지 포함.
        { "PlayHud/ButtonPrimary", new Vector4(54, 66, 54, 54) },
        { "PlayHud/ButtonSecondary", new Vector4(54, 66, 54, 54) },
        // 감열지 빌지: 아래 톱니(16~18px)는 늘리지 않는다. 가로로 늘리면 톱니가 같이 늘어나므로 폭은 시안(352) 그대로 쓴다.
        { "PlayHud/ReceiptPaper", new Vector4(8, 20, 8, 8) },
        { "PlayHud/ReceiptPaperOpen", new Vector4(10, 22, 12, 10) },
    };
    private static readonly Dictionary<string, Sprite> slicedSprites = new Dictionary<string, Sprite>();

    /// <summary>9-slice 테두리를 넣은 시안 조각. 테두리가 등록되지 않은 이름은 Load와 같다.
    /// 2x 텍스처라 pixelsPerUnit을 200으로 두어, Image.Type.Sliced에서 모서리가 시안 크기(1x) 그대로 보인다.</summary>
    public static Sprite Sliced(string name)
    {
        if (!SliceBorders.TryGetValue(name, out var border)) return Load(name);
        if (slicedSprites.TryGetValue(name, out var cached) && cached != null) return cached;
        var texture = Resources.Load<Texture2D>("UIDesign/Art/" + name);
        if (texture == null) return null;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 200, 0, SpriteMeshType.FullRect, border);
        sprite.name = "Design Sliced " + name; sprite.hideFlags = HideFlags.DontSave;
        slicedSprites[name] = sprite; return sprite;
    }

    /// <summary>Image에 9-slice 조각을 넣고 Sliced로 바꾼다. 가운데는 채우고 색은 건드리지 않는다.
    /// borderScale은 모서리를 시안보다 작게 그릴 때 쓴다 (예: 0.7이면 모서리·그림자를 70% 크기로).</summary>
    public static void ApplySliced(UnityEngine.UI.Image image, string name, float borderScale = 1f)
    {
        image.sprite = Sliced(name);
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.fillCenter = true;
        // DayLoopUI.Panel이 둥근 도형용으로 바꿔 둔 배수를 되돌린다.
        image.pixelsPerUnitMultiplier = 1f / Mathf.Max(.01f, borderScale);
    }

    /// <summary>레이아웃에서 빠져 부모를 inset만큼 안쪽으로 채우는 9-slice 장식 (맨 뒤에 깔린다).</summary>
    public static UnityEngine.UI.Image Backing(Transform parent, string piece, float left, float top, float right, float bottom, string name = "Paper")
    {
        var image = DayLoopUI.Panel(parent, Color.white, name, false);
        ApplySliced(image, piece);
        image.raycastTarget = false;
        image.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        DayLoopUI.Stretch(image.rectTransform, left, top, right, bottom);
        image.transform.SetAsFirstSibling();
        return image;
    }

    /// <summary>시안의 나무 테 카드 (영업 팻말·탁상 메뉴판·우측 HUD 칸). 나무 테 안쪽 8에 종이를 끼운다.
    /// 반환한 나무 테에 레이아웃 그룹과 내용을 붙인다. 내용 여백은 테 두께(8)를 감안해 잡는다.</summary>
    public static UnityEngine.UI.Image WoodCard(Transform parent, string name, string paper = "PlayHud/HudButtonInner",
        string frame = "PlayHud/HudButtonFrame", float paperBottom = 8f, bool pins = false)
    {
        var card = DayLoopUI.Panel(parent, Color.white, name, false);
        ApplySliced(card, frame);
        card.raycastTarget = false;
        Backing(card.transform, paper, 8f, 8f, 8f, paperBottom);
        if (pins)
            foreach (float side in new[] { 0f, 1f })
            {
                var pin = DayLoopUI.Panel(card.transform, Color.white, "BrassPin", false);
                pin.sprite = Load("PlayHud/BrassPin");
                pin.raycastTarget = false;
                pin.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                DayLoopUI.Place(pin.rectTransform, new Vector2(side, 1f), new Vector2(side, 1f), new Vector2(side == 0f ? 16f : -16f, -16f), new Vector2(8f, 8f));
            }
        return card;
    }

    /// <summary>나무 테 + 안쪽 종이로 된 버튼 (우측 HUD 배속 칸 모양). 글자는 진갈색.</summary>
    public static UnityEngine.UI.Button WoodButton(Transform parent, string label, float fontSize, UnityEngine.Events.UnityAction onClick,
        string name, string frame = "PlayHud/HudButtonFrame")
    {
        var button = DayLoopUI.Button(parent, label, Color.white, DayLoopUI.Kit.Ink, fontSize, onClick, name);
        ApplySliced(button.image, frame);
        Backing(button.transform, "PlayHud/HudButtonInner", 8f, 8f, 8f, 8f);
        return button;
    }
    // 요리·재료 그림 (Resources/UIDesign/Art/Food, Ingredient). 이름은 띄어쓰기를 뺀 한글 이름으로 찾는다.
    // 그림이 없는 요리·재료는 null을 돌려주고, 화면은 첫 글자 동그라미로 대신 보여 준다.
    private static readonly Dictionary<string, string> FoodArt = new Dictionary<string, string>
    {
        { "가리비구이", "ScallopGrill" }, { "고등어구이", "MackerelGrill" }, { "고등어된장조림", "MackerelDoenjang" },
        { "광어회", "FlounderSashimi" }, { "문어숙회", "OctopusSukhoe" }, { "새우구이", "ShrimpGrill" },
        { "새우완자탕", "ShrimpBallSoup" }, { "새우튀김", "ShrimpTempura" }, { "생선가스", "FishCutlet" },
        { "연어간장구이", "SalmonSoyGrill" }, { "오징어숙회", "SquidSukhoe" }, { "조개찜", "ClamSteam" },
        // 아래 10개는 기존 요리·재료 그림을 조합한 임시 그림이다. 디자이너 그림이 나오면 같은 이름 파일만 바꾼다.
        { "가리비간장구이", "ScallopSoyGrill" }, { "광어간장찜", "FlounderSoySteam" }, { "문어초무침", "OctopusChomuchim" },
        { "바다의진미플래터", "SeaPlatter" }, { "바지락술찜", "ClamSakeSteam" }, { "연어스테이크레몬소스", "SalmonLemon" },
        { "해물된장구이모둠", "SeafoodDoenjangGrill" }, { "해물크림스튜", "SeafoodCreamStew" }, { "해물튀김모둠", "SeafoodTempura" },
        { "해산물전골", "SeafoodHotpot" },
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
