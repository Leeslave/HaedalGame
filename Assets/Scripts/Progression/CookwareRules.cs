using System.Linq;
using UnityEngine;

public static class CookwareRules
{
    private static readonly string[] Names = { "FryingPanUpgradeSO", "CuttingBoardUpgradeSO", "DeepfryerUpgradeSO", "PotUpgradeSO" };
    public static CookwareUpgradeSO Definition(int id) => id >= 201 && id <= 204 ? Resources.Load<CookwareUpgradeSO>("Data/Tools/" + Names[id - 201]) : null;
    public static CookwareEntry Entry(GameSaveData data, int id)
    {
        if (id < 201 || id > 204) return null;
        var entry = data.cookware.Find(e => e.toolId == Names[id - 201]);
        if (entry == null) { entry = new CookwareEntry { toolId = Names[id - 201] }; data.cookware.Add(entry); }
        return entry;
    }
    public static bool CanCook(GameSaveData data, RecipeData recipe)
    {
        if (!RestaurantProgress.HasTool(data, recipe)) return false;
        int[] ids = recipe.ClassId == 205 ? new[] { 201, 202 } : new[] { recipe.ClassId };
        int needed = recipe.Requirements.Count(r => !IngredientInventoryService.IsUnlimited(r.IngredientId));
        foreach (int id in ids)
        {
            var definition = Definition(id);
            var entry = Entry(data, id);
            if (definition == null || entry == null || definition.GetMaxIngredientCount(entry.level) < needed) return false;
        }
        return true;
    }
    public static void RecordUse(RecipeData recipe)
    {
        if (!GameSession.IsActive) return;
        int[] ids = recipe.ClassId == 205 ? new[] { 201, 202 } : new[] { recipe.ClassId };
        foreach (int id in ids)
        {
            if (CookwareLevelState.Instance != null) CookwareLevelState.Instance.AddUseCount(Definition(id));
            else { var e = Entry(GameSession.Current, id); if (e != null) e.useCount++; }
        }
    }
}
