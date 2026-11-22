using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 섬 이야기 데이터와 배 부품 규칙. 섬을 직접 돌아다니는 대신 각 섬의 주민이 식당으로 찾아와 부탁하는 방식으로
/// 스토리 플롯(game_story_plot)의 다섯 섬을 잇는다. 부탁 요청·전달은 RestaurantProgress의
/// TryExplore · TryDeliverQuest, 목재와 배 수리는 TryReceiveWood · TryEnding이 맡는다.
///
/// 배 부품(플롯 기준 4개): 돌고래의 바다에서 고장 난 엔진 → 해달 왕국에서 엔진 수리 → 곡물 섬 밧줄 → 목장 섬 돛,
/// 다섯 섬을 마친 뒤 해달과 화해하면 요정의 숲에서 선체 목재. 부품 목록은 섬 챕터와 목재 플래그로 다시 계산한다.
/// </summary>
public static class IslandStory
{
    public const int RequestsPerIsland = 3;
    public const string BrokenEngine = "BrokenEngine", Engine = "Engine", Rope = "Rope", Sail = "Sail", Wood = "Wood";
    public static readonly string[] RequiredParts = { Engine, Rope, Sail, Wood };
    public static int BoatPartsNeeded => RequiredParts.Length;
    public const string WoodFlag = "IslandStory.Wood";

    public readonly struct Island
    {
        public readonly string Name, Resident, Specialty, Arrival, Request, Done, DialogueKey;
        public Island(string name, string resident, string specialty, string arrival, string request, string done, string dialogueKey)
        { Name = name; Resident = resident; Specialty = specialty; Arrival = arrival; Request = request; Done = done; DialogueKey = dialogueKey; }
    }

    public static readonly Island[] Islands =
    {
        new Island("요정의 숲", "숲의 요정", "버섯 · 과일",
            "해달의 오랜 친구인 요정이 식당 소식을 듣고 찾아왔어요.",
            "요정이 친구가 차린 식당의 요리를 맛보고 싶어 해요.",
            "요정이 식당에 재료를 대 주기로 했어요. 그런데 배를 만들 목재 이야기만 나오면 말을 돌려요.", "fairy"),
        new Island("돌고래의 바다", "돌고래 셰프", "해산물 · 해조류",
            "요리에 엄격하기로 소문난 돌고래 셰프가 손님으로 왔어요.",
            "돌고래 셰프가 실력을 보겠다며 요리 한 접시를 주문해요.",
            "돌고래 셰프의 안내로 가라앉은 배에서 고장 난 엔진을 건져 왔어요.", "dolphin"),
        new Island("해달 왕국", "왕국의 엔지니어", "기계 장치",
            "해달이 떠나온 왕국에서 옛 엔지니어 동료가 찾아왔어요. 표정이 좋지 않아요.",
            "동료는 해달이 떠난 뒤 일을 떠맡았다며 투덜대요. 따뜻한 한 끼로 마음을 풀어 주세요.",
            "동료가 해달의 마음을 이해하고 고장 난 엔진을 고쳐 줬어요.", "kingdom"),
        new Island("곡물 섬", "곡물 섬 농부", "밀 · 쌀 · 옥수수",
            "곡물은 넘치지만 요리법이 부족한 곡물 섬의 농부가 찾아왔어요.",
            "농부가 곡물을 맛있게 먹을 수 있는 요리를 부탁해요.",
            "곡물 섬과 거래를 트고, 곡물을 묶던 튼튼한 밧줄을 얻었어요.", "grain"),
        new Island("목장 섬", "목장 장인", "우유 · 치즈 · 버터",
            "훌륭한 유제품을 만드는 목장 섬의 장인이 찾아왔어요.",
            "장인이 자기 치즈가 어울릴 요리를 맛보고 싶어 해요.",
            "목장 섬과 교류가 생기고, 장인이 전통 방식으로 짠 천으로 돛을 만들어 줬어요.", "ranch"),
    };

    /// <summary>섬 번호(0부터)마다 식당 레벨 1, 2, 3, 4, 5. 다섯 섬을 마친 뒤의 만남도 Lv.5 기준.</summary>
    public static int RequiredLevel(int island) => Mathf.Clamp(island + 1, 1, Islands.Length);

    public static string MetFlag(int island) => "IslandStory.Met-" + island;
    public static string ArrivalDialogue(int island) => $"Dialogue/Island/{Islands[island].DialogueKey}_arrival";
    public static string DoneDialogue(int island) => $"Dialogue/Island/{Islands[island].DialogueKey}_done";
    public const string WoodDialogue = "Dialogue/Island/climax_wood";
    public const string EndingDialogue = "Dialogue/Island/ending_farewell";
    /// <summary>엔딩 뒤 처음 이어하기를 누르면 나오는 섬 주민들의 후일담. (플롯 8. 에필로그)</summary>
    public const string EpilogueDialogue = "Dialogue/Island/epilogue";
    public static bool ShouldPlayEpilogue(GameSaveData s) => s?.progression != null && s.progression.endingSeen && !s.progression.epilogueSeen;

    public static string PartName(string part) => part switch
    {
        BrokenEngine => "엔진(고장)",
        Engine => "엔진",
        Rope => "밧줄",
        Sail => "돛",
        Wood => "선체 목재",
        _ => part,
    };

    /// <summary>섬 챕터(마친 섬 수)와 목재 플래그로 지금 가진 부품을 계산한다.</summary>
    public static List<string> PartsFor(int chapter, bool wood)
    {
        var parts = new List<string>();
        if (chapter >= 3) parts.Add(Engine); else if (chapter >= 2) parts.Add(BrokenEngine);
        if (chapter >= 4) parts.Add(Rope);
        if (chapter >= 5) parts.Add(Sail);
        if (wood) parts.Add(Wood);
        return parts;
    }

    /// <summary>세이브의 부품 목록을 규칙에 맞게 다시 맞춘다. (예전 "Part-0" 형식 세이브도 여기서 바뀐다)</summary>
    public static void SyncBoatParts(GameSaveData s)
    {
        var parts = PartsFor(s.progression.islandChapter, s.storyFlags.Contains(WoodFlag));
        s.progression.boatParts.Clear();
        s.progression.boatParts.AddRange(parts);
    }

    public static int ReadyParts(GameSaveData s) =>
        RequiredParts.Count(p => PartsFor(s.progression.islandChapter, s.storyFlags.Contains(WoodFlag)).Contains(p));
    public static bool ShipReady(GameSaveData s) => ReadyParts(s) >= BoatPartsNeeded;
    public static bool WoodReceived(GameSaveData s) => s.storyFlags.Contains(WoodFlag);

    /// <summary>
    /// 섬 직송: 섬과 교류하면 상점에 높은 등급 재료가 들어온다. (Lv.6부터 손님 기대 등급이 D라 F·E만으로는 평점 3.5를 넘기 어렵다)
    /// 등급별 값은 일반 가격의 2배(E)·4배(D), 품목·등급마다 하루 SupplyStock개.
    /// </summary>
    public readonly struct Supply
    {
        public readonly int Island, Grade;
        public readonly string Label;
        public readonly string[] Ingredients;
        public Supply(int island, string label, int grade, params string[] ingredients)
        { Island = island; Label = label; Grade = grade; Ingredients = ingredients; }
    }

    public const int SupplyStock = 30;
    private static readonly string[] Seafood = { "새우", "가리비", "오징어", "조개", "광어", "문어", "고등어", "연어", "바지락", "흰살생선" };
    private static readonly string[] Herbs = { "레몬", "마늘", "생강", "와사비", "겨자", "꿀" };
    private static readonly string[] Grains = { "튀김가루", "식초", "간장", "미림", "된장" };

    public static readonly Supply[] Supplies =
    {
        new Supply(0, "숲 재료", 1, Herbs),
        new Supply(1, "해산물", 1, Seafood),
        new Supply(2, "해산물", 2, Seafood),
        new Supply(3, "곡물 · 발효 양념", 2, Grains),
        new Supply(4, "계란", 2, "계란"),
        new Supply(4, "숲 재료", 2, Herbs),
    };

    /// <summary>교류 중인 섬 덕분에 상점에서 살 수 있는 이 재료의 최고 등급. 0이면 일반 F만.</summary>
    public static int SupplyGrade(GameSaveData s, string ingredientName)
    {
        int grade = 0;
        foreach (var supply in Supplies)
            if (supply.Island < s.progression.islandChapter && System.Array.IndexOf(supply.Ingredients, ingredientName) >= 0)
                grade = Mathf.Max(grade, supply.Grade);
        return grade;
    }

    /// <summary>이 섬과 교류하면 상점에 들어오는 재료 설명. (예: "해산물 E등급")</summary>
    public static string SupplyText(int island) => string.Join(" · ",
        Supplies.Where(x => x.Island == island).Select(x => $"{x.Label} {RestaurantRules.GradeLetter(x.Grade)}등급"));

    public const string ClimaxIntro =
        "다섯 섬과 모두 교류하게 됐어요. 배에 남은 건 선체가 될 목재뿐이에요.\n그런데 요정은 여전히 목재 이야기를 피하고, 해달은 어딘가 말이 없어요.";
    public const string ClimaxDone =
        "해달이 목재를 막아 달라고 부탁했던 사실을 털어놓았어요. 두 사람은 서로의 마음을 이해했고, 요정이 보내 준 선체 목재로 드디어 배가 완성됐어요.\n떠날 준비가 되면 [탈출하기]를 눌러요.";

    public const string EndingText =
        "완성한 배를 타고 섬을 떠났어요.\n떠나는 날, 그동안 만난 섬 주민과 손님들이 배웅을 나왔어요. " +
        "해달은 숨어 있다가 마지막에야 나타나 '퇴직 기념'이라며 작은 상자를 건넸어요.\n" +
        "식당은 계속 열어 둘 수 있어요. 타이틀에서 이어하기를 누르면 섬 주민들의 후일담을 볼 수 있어요.";
}
