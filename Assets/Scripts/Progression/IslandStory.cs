using UnityEngine;

/// <summary>
/// 섬 이야기 데이터. 섬을 직접 돌아다니는 대신 각 섬의 주민이 식당으로 찾아와 부탁하는 방식으로
/// 스토리 플롯(game_story_plot)의 다섯 섬을 잇는다. 진행 규칙은 RestaurantProgress의
/// TryExplore · TryDeliverQuest · TryEnding이 맡고, 이 클래스는 화면에 보여 줄 글만 가진다.
/// </summary>
public static class IslandStory
{
    public const int BoatPartsNeeded = 3;
    public const int RequestsPerIsland = 3;

    public readonly struct Island
    {
        public readonly string Name, Resident, Specialty, Arrival, Request, Done;
        public Island(string name, string resident, string specialty, string arrival, string request, string done)
        { Name = name; Resident = resident; Specialty = specialty; Arrival = arrival; Request = request; Done = done; }
    }

    public static readonly Island[] Islands =
    {
        new Island("요정의 숲", "숲의 요정", "버섯 · 과일",
            "해달의 오랜 친구인 요정이 식당 소식을 듣고 찾아왔어요.",
            "요정이 친구가 차린 식당의 요리를 맛보고 싶어 해요.",
            "요정이 식당에 재료를 대 주기로 했어요. 그런데 배를 만들 목재 이야기만 나오면 말을 돌려요."),
        new Island("돌고래의 바다", "돌고래 셰프", "해산물 · 해조류",
            "요리에 엄격하기로 소문난 돌고래 셰프가 손님으로 왔어요.",
            "돌고래 셰프가 실력을 보겠다며 요리 한 접시를 주문해요.",
            "돌고래 셰프가 근처 바다에 가라앉은 사람의 배 이야기를 들려줬어요. 잔해에 엔진이 남아 있대요."),
        new Island("해달 왕국", "왕국의 엔지니어", "기계 장치",
            "해달이 떠나온 왕국에서 옛 엔지니어 동료가 찾아왔어요. 표정이 좋지 않아요.",
            "동료는 해달이 떠난 뒤 일을 떠맡았다며 투덜대요. 따뜻한 한 끼로 마음을 풀어 주세요.",
            "동료가 해달의 마음을 이해하고 엔진 수리를 맡아 주기로 했어요."),
        new Island("곡물 섬", "곡물 섬 농부", "밀 · 쌀 · 옥수수",
            "곡물은 넘치지만 요리법이 부족한 곡물 섬의 농부가 찾아왔어요.",
            "농부가 곡물을 맛있게 먹을 수 있는 요리를 부탁해요.",
            "곡물 섬과 거래를 트고, 곡물을 묶던 튼튼한 밧줄도 얻을 수 있게 됐어요."),
        new Island("목장 섬", "목장 장인", "우유 · 치즈 · 버터",
            "훌륭한 유제품을 만드는 목장 섬의 장인이 찾아왔어요.",
            "장인이 자기 치즈가 어울릴 요리를 맛보고 싶어 해요.",
            "목장 섬과 교류가 생겼어요. 장인이 전통 방식으로 짠 천으로 돛을 만들어 주기로 했어요."),
    };

    /// <summary>RestaurantProgress.TryExplore와 같은 기준: 섬 번호(0부터)마다 식당 레벨 1, 3, 5, 7, 9.</summary>
    public static int RequiredLevel(int island) => Mathf.Min(10, 1 + island * 2);

    public const string EndingText =
        "모은 부품으로 드디어 배를 고쳤어요.\n떠나는 날, 그동안 만난 섬 주민과 손님들이 배웅을 나왔어요. " +
        "해달은 숨어 있다가 마지막에야 나타나 '퇴직 기념'이라며 작은 상자를 건넸어요.\n" +
        "배는 언제든 띄울 수 있어요. 식당은 계속 열어 둘 수 있어요.";
}
