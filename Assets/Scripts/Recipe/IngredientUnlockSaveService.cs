using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class IngredientUnlockSaveData
{
    public List<int> unlockedIngredientIdsInOrder = new List<int>();
}

public class IngredientUnlockSaveService : MonoBehaviour
{
    [SerializeField] private string _fileName = "ingredient_unlocks.json";

    private string FilePath => Path.Combine(Application.persistentDataPath, _fileName);

    public void Save(IngredientUnlockSaveData data)
    {
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }

    public IngredientUnlockSaveData Load()
    {
        if (!File.Exists(FilePath))
            return null;

        string json = File.ReadAllText(FilePath);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<IngredientUnlockSaveData>(json);
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
