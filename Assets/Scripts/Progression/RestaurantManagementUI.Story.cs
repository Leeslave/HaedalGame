using UnityEngine;

/// <summary>섬 소식 탭: 섬 주민의 부탁을 받고 요리를 전달해 섬 챕터와 배 부품을 모으고, 부품이 모이면 배를 고친다.</summary>
public partial class RestaurantManagementUI
{
    private static bool HasIslandNews(GameSaveData s)
    {
        var p = s.progression;
        if (!p.freeTime) return false;
        if (p.questRecipeId > 0) return true;
        if (!p.endingSeen && p.boatParts.Count >= IslandStory.BoatPartsNeeded) return true;
        return s.day >= 2 && !s.storyFlags.Contains("ExploreDay-" + s.day) && s.restaurantLevel >= IslandStory.RequiredLevel(p.islandChapter);
    }

    private void DesignedIslandNews(GameSaveData s)
    {
        var p = s.progression;
        int total = IslandStory.Islands.Length;
        int chapter = Mathf.Clamp(p.islandChapter, 0, total);
        int parts = p.boatParts.Count;
        info.text = $"배 부품 {Mathf.Min(parts, IslandStory.BoatPartsNeeded)} / {IslandStory.BoatPartsNeeded}  ·  교류한 섬 {chapter} / {total}";

        var list = Split(out var detail);
        var items = ScrollList(list);
        int current = Mathf.Min(chapter, total - 1);
        int shown = Mathf.Clamp(selectedId < 0 ? current : selectedId, 0, Mathf.Min(chapter, total - 1));
        for (int i = 0; i < total; i++)
        {
            int index = i;
            var island = IslandStory.Islands[i];
            bool known = i <= chapter;
            string status = i < chapter ? "교류 중"
                : i == chapter ? (s.restaurantLevel >= IslandStory.RequiredLevel(i) ? "주민이 찾아오고 있어요" : $"식당 Lv.{IslandStory.RequiredLevel(i)}에 소식이 와요")
                : "아직 소식이 없어요";
            var card = SelectCard(items, known ? island.Name : "알 수 없는 섬", known ? $"{status} · 특산물 {island.Specialty}" : status,
                shown == i, () => { selectedId = index; Refresh(); }, DesignArt.Load("IconExplore"));
            card.interactable = known;
        }

        var selected = IslandStory.Islands[shown];
        Copy(detail, selected.Name, 60, 33);
        Copy(detail, $"{selected.Resident} · 특산물 {selected.Specialty}", 36, 21, DayLoopUI.Kit.InkSoft);

        if (p.endingSeen)
        {
            Copy(detail, IslandStory.EndingText, 260, 21);
        }
        else if (shown < chapter)
        {
            Copy(detail, selected.Done, 130, 21);
        }
        else if (p.questRecipeId > 0 && GameDatabase.TryGetRecipe(p.questRecipeId, out var recipe))
        {
            Copy(detail, selected.Request, 100, 21);
            Copy(detail, $"부탁한 음식 · {recipe.RecipeName}", 42, 24);
            RecipeRequirements(detail, recipe);
        }
        else
        {
            Copy(detail, selected.Arrival, 100, 21);
        }

        if (chapter < total && !p.endingSeen)
            Copy(detail, $"이 섬과의 약속  {p.npcSatisfaction % IslandStory.RequestsPerIsland} / {IslandStory.RequestsPerIsland}", 42, 22);
        if (!p.freeTime)
            Copy(detail, "영업을 마친 자유시간에 주민을 만날 수 있어요.", 70, 21, DayLoopUI.Kit.WarnInk);
        DayLoopUI.Spacer(detail);

        if (!p.endingSeen && parts >= IslandStory.BoatPartsNeeded)
            DetailAction(detail, "배 고치기", () => Run(RestaurantProgress.TryEnding), true, p.freeTime);
        if (p.questRecipeId > 0)
            DetailAction(detail, "부탁한 음식 전달하기", DeliverIslandRequest, true, p.freeTime);
        else
            DetailAction(detail, p.endingSeen ? "주민 만나기 · 후일담" : "주민 만나기", () => Run(RestaurantProgress.TryExplore), p.endingSeen || parts < IslandStory.BoatPartsNeeded, p.freeTime);
    }

    /// <summary>전달 결과(설계도·섬 교류·배 부품)를 한 줄로 알려 준다.</summary>
    private void DeliverIslandRequest()
    {
        var p = GameSession.Current.progression;
        int chapter = p.islandChapter, parts = p.boatParts.Count;
        var blueprints = new System.Collections.Generic.HashSet<int>(p.blueprints);
        GameDatabase.TryGetRecipe(p.questRecipeId, out var dish);
        if (!RestaurantProgress.TryDeliverQuest(out string error)) { Refresh(); message.text = error; return; }
        p = GameSession.Current.progression;
        string text = dish != null ? $"{Josa(dish.RecipeName, "을", "를")} 전했어요." : "부탁한 음식을 전했어요.";
        foreach (int id in p.blueprints)
            if (!blueprints.Contains(id) && GameDatabase.TryGetRecipe(id, out var reward)) { text += $" 답례로 {reward.RecipeName} 설계도를 받았어요."; break; }
        if (p.islandChapter > chapter && chapter < IslandStory.Islands.Length)
            text += $" {Josa(IslandStory.Islands[chapter].Name, "과", "와")} 교류가 시작됐어요!";
        if (p.boatParts.Count > parts) text += $" 배 부품 {p.boatParts.Count}/{IslandStory.BoatPartsNeeded}";
        selectedId = -1; Refresh(); message.text = text;
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
