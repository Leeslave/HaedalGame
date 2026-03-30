using System.Collections.Generic;
using UnityEngine;

public static class RecipeCsvMapper
{
    public static List<IngredientData> ToIngredients(CsvTable table)
    {
        table.RequireColumns("ing_n", "ing_name", "rec_n");

        return table.MapRows(row =>
        {
            int ingredientId = row.GetInt("ing_n");
            string ingredientName = row.Get("ing_name");
            int recipeCode = row.GetInt("rec_n");

            return new IngredientData(ingredientId, ingredientName, recipeCode);
        });
    }

    public static List<RecipeData> ToRecipes(CsvTable table, Dictionary<int, IngredientData> ingredientByRecipeCode)
    {
        table.RequireColumns("menu_n", "m_name", "type_n", "class_n", "menu_t", "recipe");


        return table.MapRows(row =>
        {
            int recipeId = row.GetInt("menu_n");
            string recipeName = row.Get("m_name");
            int unlockType = row.GetInt("type_n");
            int classId = row.GetInt("class_n");
            float cookTime = row.GetFloat("menu_t");
            string rawRecipeText = row.Get("recipe");

            List<RecipeIngredientRequirement> requirements = BuildRequirements(
                row,
                rawRecipeText,
                ingredientByRecipeCode,
                recipeName,
                recipeId);

            return new RecipeData(
                recipeId,
                recipeName,
                unlockType,
                classId,
                cookTime,
                rawRecipeText,
                requirements);
        });
    }

    public static Dictionary<int, IngredientData> BuildIngredientMapByRecipeCode(List<IngredientData> ingredients)
    {
        Dictionary<int, IngredientData> result = new Dictionary<int, IngredientData>();

        for (int i = 0; i < ingredients.Count; i++)
        {
            IngredientData ingredient = ingredients[i];

            if (result.ContainsKey(ingredient.RecipeCode))
            {
                Debug.LogWarning($"Duplicate rec_n found in ingredient.csv: {ingredient.RecipeCode}");
                continue;
            }

            result.Add(ingredient.RecipeCode, ingredient);
        }

        return result;
    }

    private static List<RecipeIngredientRequirement> BuildRequirements(
        CsvRow row,
        string rawRecipeText,
        Dictionary<int, IngredientData> ingredientByRecipeCode,
        string recipeName,
        int recipeId)
    {
        List<RecipeIngredientRequirement> result = new List<RecipeIngredientRequirement>();

        if (string.IsNullOrWhiteSpace(rawRecipeText))
            return result;

        List<int> recipeCodes = row.GetIntList("recipe", ' ', '\t', ',', ';', '|');
        Dictionary<int, int> amountByRecipeCode = new Dictionary<int, int>();

        for (int i = 0; i < recipeCodes.Count; i++)
        {
            int recipeCode = recipeCodes[i];

            if (amountByRecipeCode.ContainsKey(recipeCode))
                amountByRecipeCode[recipeCode]++;
            else
                amountByRecipeCode.Add(recipeCode, 1);
        }

        foreach (KeyValuePair<int, int> pair in amountByRecipeCode)
        {
            int recipeCode = pair.Key;
            int amount = pair.Value;

            if (!ingredientByRecipeCode.TryGetValue(recipeCode, out IngredientData ingredient))
            {
                Debug.LogWarning(
                    $"Recipe Parse Warning - ingredient.csv 에 rec_n={recipeCode} 가 없음 / Row:{row.RowNumber} / Recipe:{recipeName}({recipeId})");
                continue;
            }

            result.Add(new RecipeIngredientRequirement(
                ingredient.IngredientId,
                ingredient.RecipeCode,
                ingredient.IngredientName,
                amount));
        }

        return result;
    }
}
