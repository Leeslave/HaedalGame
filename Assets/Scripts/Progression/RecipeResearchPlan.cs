using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public class ResearchStep { public string kind; public string text; }
[Serializable] public class ResearchCost { public int ingredientId; public int amount; }
[Serializable] public class RecipeResearchPlan
{
    public int recipeId;
    public ResearchCost[] requirements;
    public ResearchStep[] steps;
    [Serializable] private class Database { public RecipeResearchPlan[] plans; }
    private static Database database;
    public static RecipeResearchPlan Find(int id)
    {
        if (database == null)
        {
            var text = Resources.Load<TextAsset>("Data/RecipeResearchPlans");
            if (text == null) return null;
            database = JsonUtility.FromJson<Database>(text.text);
        }
        return database.plans.FirstOrDefault(p => p.recipeId == id);
    }
    public static IEnumerable<RecipeIngredientRequirement> Requirements(RecipeData recipe)
    {
        var plan = Find(recipe.RecipeId);
        if (plan == null) return recipe.Requirements;
        return plan.requirements.Select(r => {
            GameDatabase.Recipes.TryGetIngredientById(r.ingredientId, out var ingredient);
            return new RecipeIngredientRequirement(r.ingredientId, ingredient.RecipeCode, ingredient.IngredientName, r.amount);
        });
    }
    public static string CostLabel(RecipeData recipe) => string.Join(" · ", Requirements(recipe)
        .Where(r => !IngredientInventoryService.IsUnlimited(r.IngredientId)).Select(r => r.IngredientName + " " + r.Amount));
}
