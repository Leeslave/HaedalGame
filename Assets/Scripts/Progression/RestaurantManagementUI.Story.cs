using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 섬 소식 탭: 섬 주민의 부탁을 받고 요리를 전달해 섬 챕터와 배 부품을 모은다.
/// 다섯 섬을 마치면 마지막 이야기(해달과 화해 → 요정의 목재)를 거쳐 배를 완성한다.
/// 첫 만남·섬 교류 완료·목재·배 완성 때 대화 장면(Resources/Dialogue/Island)을 띄운다.
/// </summary>
public partial class RestaurantManagementUI
{
    private const int ClimaxCard = 5;

    private static bool HasIslandNews(GameSaveData s)
    {
        var p = s.progression;
        if (!p.freeTime || p.endingSeen) return false;
        if (p.questRecipeId > 0) return true;
        if (p.islandChapter >= IslandStory.Islands.Length) return !IslandStory.WoodReceived(s) || IslandStory.ShipReady(s);
        return s.day >= 2 && !s.storyFlags.Contains("ExploreDay-" + s.day) && s.restaurantLevel >= IslandStory.RequiredLevel(p.islandChapter);
    }

    private void DesignedIslandNews(GameSaveData s)
    {
        var p = s.progression;
        int total = IslandStory.Islands.Length;
        int chapter = Mathf.Clamp(p.islandChapter, 0, total);
        bool allIslands = chapter >= total;
        var owned = IslandStory.PartsFor(chapter, IslandStory.WoodReceived(s));
        info.text = $"배 부품 {IslandStory.ReadyParts(s)} / {IslandStory.BoatPartsNeeded}  ·  교류한 섬 {chapter} / {total}";

        var list = Split(out var detail);
        var items = ScrollList(list);
        int last = allIslands ? ClimaxCard : chapter;
        int shown = selectedId < 0 ? last : Mathf.Clamp(selectedId, 0, last);
        for (int i = 0; i < total; i++)
        {
            int index = i;
            var island = IslandStory.Islands[i];
            bool known = i <= chapter;
            string status = i < chapter ? "교류 중"
                : s.restaurantLevel >= IslandStory.RequiredLevel(i) ? "주민이 찾아오고 있어요" : $"식당 Lv.{IslandStory.RequiredLevel(i)}에 소식이 와요";
            var card = SelectCard(items, known ? island.Name : "알 수 없는 섬", known ? $"{status} · 특산물 {island.Specialty}" : "아직 소식이 없어요",
                shown == i, () => { selectedId = index; Refresh(); }, DesignArt.Load("IconExplore"));
            card.interactable = known;
        }
        if (allIslands)
            SelectCard(items, "마지막 이야기", p.endingSeen ? "배웅을 받으며 떠났어요" : IslandStory.WoodReceived(s) ? "배가 완성됐어요 · 탈출할 수 있어요" : "해달과 선체 목재",
                shown == ClimaxCard, () => { selectedId = ClimaxCard; Refresh(); }, DesignArt.Load("IconExplore"));

        if (shown == ClimaxCard) ClimaxDetail(s, detail);
        else IslandDetail(s, detail, shown, chapter);

        Copy(detail, "배 부품  " + (owned.Count == 0 ? "아직 없음" : string.Join(" · ", owned.Select(IslandStory.PartName))), 60, 21, DayLoopUI.Kit.InkSoft);
        if (!p.freeTime && !p.endingSeen)
            Copy(detail, "영업을 마친 자유시간에 주민을 만날 수 있어요.", 70, 21, DayLoopUI.Kit.WarnInk);
        DayLoopUI.Spacer(detail);

        if (p.questRecipeId > 0)
            DetailAction(detail, "부탁한 음식 전달하기", DeliverIslandRequest, true, p.freeTime);
        else if (shown == ClimaxCard && !IslandStory.WoodReceived(s))
            DetailAction(detail, "해달과 이야기하기", () => RunStory(RestaurantProgress.TryReceiveWood, IslandStory.WoodDialogue, "선체 목재로 배가 완성됐어요. 떠날 준비가 되면 탈출하기를 눌러요."), true, p.freeTime);
        else if (shown == ClimaxCard && !p.endingSeen)
            DetailAction(detail, "탈출하기", () => RunStory(RestaurantProgress.TryEnding, IslandStory.EndingDialogue, "섬 주민들의 배웅을 받으며 바다로 떠났어요."), true, p.freeTime && IslandStory.ShipReady(s));
        else if (!p.endingSeen || allIslands)
            DetailAction(detail, p.endingSeen ? "주민 만나기 · 후일담" : "주민 만나기", MeetIslandResident, !allIslands, p.freeTime);
    }

    private void IslandDetail(GameSaveData s, RectTransform detail, int shown, int chapter)
    {
        var p = s.progression;
        var island = IslandStory.Islands[shown];
        Copy(detail, island.Name, 60, 33);
        Copy(detail, $"{island.Resident} · 특산물 {island.Specialty}", 36, 21, DayLoopUI.Kit.InkSoft);
        string supply = IslandStory.SupplyText(shown);
        if (shown < chapter)
        {
            Copy(detail, island.Done, 110, 21);
            Copy(detail, $"상점 섬 직송  {supply}", 36, 21, DayLoopUI.Kit.Sea);
            return;
        }
        Copy(detail, $"교류하면 상점에  {supply}", 36, 21, DayLoopUI.Kit.InkSoft);
        if (p.questRecipeId > 0 && GameDatabase.TryGetRecipe(p.questRecipeId, out var recipe))
        {
            Copy(detail, island.Request, 100, 21);
            Copy(detail, $"부탁한 음식 · {recipe.RecipeName}", 42, 24);
            RecipeRequirements(detail, recipe);
        }
        else Copy(detail, island.Arrival, 100, 21);
        Copy(detail, $"이 섬과의 약속  {p.npcSatisfaction % IslandStory.RequestsPerIsland} / {IslandStory.RequestsPerIsland}", 42, 22);
    }

    private static void ClimaxDetail(GameSaveData s, RectTransform detail)
    {
        Copy(detail, "마지막 이야기", 60, 33);
        Copy(detail, "해달 · 요정의 숲", 36, 21, DayLoopUI.Kit.InkSoft);
        string text = s.progression.endingSeen ? IslandStory.EndingText
            : IslandStory.WoodReceived(s) ? IslandStory.ClimaxDone : IslandStory.ClimaxIntro;
        Copy(detail, text, s.progression.endingSeen ? 260 : 150, 21);
    }

    private void MeetIslandResident()
    {
        var s = GameSession.Current;
        int chapter = s.progression.islandChapter;
        bool firstMeeting = chapter < IslandStory.Islands.Length && !s.storyFlags.Contains(IslandStory.MetFlag(chapter));
        RunStory(RestaurantProgress.TryExplore, firstMeeting ? IslandStory.ArrivalDialogue(chapter) : null, "주민의 부탁을 들었어요.");
    }

    /// <summary>전달 결과(설계도·섬 교류·배 부품)를 한 줄로 알려 주고, 섬 교류가 시작되면 대화 장면을 띄운다.</summary>
    private void DeliverIslandRequest()
    {
        var p = GameSession.Current.progression;
        int chapter = p.islandChapter, ready = IslandStory.ReadyParts(GameSession.Current);
        var parts = p.boatParts.ToList();
        var blueprints = new System.Collections.Generic.HashSet<int>(p.blueprints);
        GameDatabase.TryGetRecipe(p.questRecipeId, out var dish);
        if (!RestaurantProgress.TryDeliverQuest(out string error)) { Refresh(); message.text = error; return; }
        var s = GameSession.Current; p = s.progression;
        string text = dish != null ? $"{Josa(dish.RecipeName, "을", "를")} 전했어요." : "부탁한 음식을 전했어요.";
        foreach (int id in p.blueprints)
            if (!blueprints.Contains(id) && GameDatabase.TryGetRecipe(id, out var reward)) { text += $" 답례로 {reward.RecipeName} 설계도를 받았어요."; break; }
        bool advanced = p.islandChapter > chapter && chapter < IslandStory.Islands.Length;
        if (advanced) text += $" {Josa(IslandStory.Islands[chapter].Name, "과", "와")} 교류가 시작됐어요! 상점 섬 직송: {IslandStory.SupplyText(chapter)}.";
        string newPart = p.boatParts.FirstOrDefault(x => !parts.Contains(x));
        if (newPart == IslandStory.BrokenEngine) text += " 고장 난 엔진을 얻었어요. 해달 왕국에서 고칠 수 있을 거예요.";
        else if (newPart != null) text += $" 배 부품: {IslandStory.PartName(newPart)} ({IslandStory.ReadyParts(s)}/{IslandStory.BoatPartsNeeded})";
        selectedId = -1;
        PlayStory(advanced ? IslandStory.DoneDialogue(chapter) : null, text);
    }

    /// <summary>규칙을 적용하고, 성공하면 대화 장면을 띄운 뒤 화면을 새로 그린다.</summary>
    private void RunStory(Command action, string dialogue, string done)
    {
        if (!action(out string error)) { Refresh(); message.text = error; return; }
        selectedId = -1;
        PlayStory(dialogue, done);
    }

    private void PlayStory(string dialogue, string done)
    {
        Action finish = () => { if (this == null || window == null) return; Refresh(); message.text = done; };
        var manager = DialogueManager.Instance;
        if (string.IsNullOrEmpty(dialogue) || manager == null || manager.IsPlaying || !manager.PlayFromResources(dialogue, finish))
        {
            if (!string.IsNullOrEmpty(dialogue)) Debug.LogWarning($"[IslandStory] 대화 장면을 재생하지 못했어요: {dialogue}");
            finish();
            return;
        }
        Refresh(); message.text = done;
    }

    /// <summary>마지막 글자 받침에 맞는 조사를 붙인다. (예: 찜을, 구이를, 숲과, 바다와)</summary>
    private static string Josa(string word, string withFinal, string withoutFinal)
    {
        if (string.IsNullOrEmpty(word)) return word;
        char last = word[word.Length - 1];
        bool final = last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
        return word + (final ? withFinal : withoutFinal);
    }
}
