using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RecipeCsvImporterWindow : EditorWindow
{
    [SerializeField] private TextAsset _menuCsv;
    [SerializeField] private TextAsset _ingredientCsv;
    [SerializeField] private RecipeDatabaseSO _targetDatabase;
    [SerializeField] private string _createPath = "Assets/Data/Recipe/RecipeDatabaseSO.asset";

    [MenuItem("Tools/Recipe/CSV Importer")]
    public static void Open()
    {
        GetWindow<RecipeCsvImporterWindow>("Recipe CSV Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("CSV Source", EditorStyles.boldLabel);
        _menuCsv = (TextAsset)EditorGUILayout.ObjectField("Menu CSV", _menuCsv, typeof(TextAsset), false);
        _ingredientCsv = (TextAsset)EditorGUILayout.ObjectField("Ingredient CSV", _ingredientCsv, typeof(TextAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        _targetDatabase = (RecipeDatabaseSO)EditorGUILayout.ObjectField("Target Database", _targetDatabase, typeof(RecipeDatabaseSO), false);
        _createPath = EditorGUILayout.TextField("Create Path", _createPath);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_menuCsv == null || _ingredientCsv == null))
        {
            if (GUILayout.Button("Import CSV To SO", GUILayout.Height(32)))
                Import();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "menu.csv 필수 컬럼: menu_n, m_name, type_n, class_n, menu_t, recipe\n" +
            "ingredient.csv 필수 컬럼: ing_n, ing_name, rec_n\n" +
            "recipe 예시: 1003 1004 1004\n" +
            "중복되는 rec_n 값은 amount로 자동 합산됩니다.",
            MessageType.Info);
    }

    private void Import()
    {
        CsvTable ingredientTable = CsvReader.Read(_ingredientCsv.text);
        CsvTable menuTable = CsvReader.Read(_menuCsv.text);

        List<IngredientData> ingredients = RecipeCsvMapper.ToIngredients(ingredientTable);
        Dictionary<int, IngredientData> ingredientByRecipeCode = RecipeCsvMapper.BuildIngredientMapByRecipeCode(ingredients);
        List<RecipeData> recipes = RecipeCsvMapper.ToRecipes(menuTable, ingredientByRecipeCode);

        RecipeDatabaseSO database = _targetDatabase;

        if (database == null)
        {
            EnsureFolderExists(_createPath);

            database = ScriptableObject.CreateInstance<RecipeDatabaseSO>();
            database.SetData(ingredients, recipes);
            AssetDatabase.CreateAsset(database, _createPath);
        }
        else
        {
            Undo.RecordObject(database, "Import Recipe CSV");
            database.SetData(ingredients, recipes);
            EditorUtility.SetDirty(database);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);

        Debug.Log($"Recipe CSV Import Complete. Ingredients: {ingredients.Count}, Recipes: {recipes.Count}");
    }

    private static void EnsureFolderExists(string assetPath)
    {
        string folderPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        string[] parts = folderPath.Split('/');

        if (parts.Length == 0)
            return;

        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, parts[i]);

            currentPath = nextPath;
        }
    }
}
