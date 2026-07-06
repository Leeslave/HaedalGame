using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class RecipeCsvMapper
{
    public static List<IngredientData> ToIngredients(CsvTable table, string ingredientIconFolderPath)
    {
        table.RequireColumns("ing_n", "ing_name", "rec_n");

        return table.MapRows(row =>
        {
            int ingredientId = row.GetInt("ing_n");
            string ingredientName = row.Get("ing_name");
            int recipeCode = row.GetInt("rec_n");

            Sprite icon = null;

#if UNITY_EDITOR
            icon = EditorSpriteFinder.FindSpriteById(ingredientIconFolderPath, ingredientId);
            if (icon == null)
                Debug.LogWarning($"Ingredient icon not found. ing_n={ingredientId}, name={ingredientName}");
#endif

            return new IngredientData(ingredientId, ingredientName, recipeCode, icon);
        });
    }

    public static List<RecipeData> ToRecipes(
        CsvTable table,
        Dictionary<int, IngredientData> ingredientByRecipeCode,
        string recipeIconFolderPath)
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

            List<RecipeCategory> categories = BuildCategories(row);

            string grade = row.Get("grade");
            string description = row.Get("description");

            List<RecipeIngredientRequirement> requirements = BuildRequirements(
                row,
                rawRecipeText,
                ingredientByRecipeCode,
                recipeName,
                recipeId);

            Sprite icon = null;

#if UNITY_EDITOR
            icon = EditorSpriteFinder.FindSpriteById(recipeIconFolderPath, recipeId);
            if (icon == null)
                Debug.LogWarning($"Recipe icon not found. menu_n={recipeId}, name={recipeName}");
#endif

            return new RecipeData(
                recipeId,
                recipeName,
                unlockType,
                classId,
                cookTime,
                rawRecipeText,
                requirements,
                icon,
                categories,
                grade,
                description);
        });
    }

    private static List<RecipeCategory> BuildCategories(CsvRow row)
    {
        List<RecipeCategory> result = new List<RecipeCategory>();

        IReadOnlyList<string> tokens = row.GetTokens("category", '|', ',', ' ', ';');

        for (int i = 0; i < tokens.Count; i++)
        {
            if (System.Enum.TryParse(tokens[i], true, out RecipeCategory category))
            {
                if (!result.Contains(category))
                    result.Add(category);
            }
            else
            {
                Debug.LogWarning($"Unknown recipe category token: '{tokens[i]}' / Row:{row.RowNumber}");
            }
        }

        return result;
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
                    $"Recipe Parse Warning - ingredient.csv �� rec_n={recipeCode} �� ���� / Row:{row.RowNumber} / Recipe:{recipeName}({recipeId})");
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