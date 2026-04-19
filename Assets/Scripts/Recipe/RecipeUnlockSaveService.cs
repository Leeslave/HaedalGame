using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class RecipeUnlockSaveData
{
    public List<int> unlockedRecipeIdsInOrder = new List<int>();
}

public class RecipeUnlockSaveService : MonoBehaviour
{
    [SerializeField] private string _fileName = "recipe_unlocks.json";

    private string FilePath => Path.Combine(Application.persistentDataPath, _fileName);

    public void Save(RecipeUnlockSaveData data)
    {
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }

    public RecipeUnlockSaveData Load()
    {
        if (!File.Exists(FilePath))
            return null;

        string json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<RecipeUnlockSaveData>(json);
    }

    public bool HasSave()
    {
        return File.Exists(FilePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}