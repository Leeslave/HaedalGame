using UnityEngine;

public static class FoodSaveLoadManager
{
    private const string MenuSaveKey = "MENU_SAVE_DATA";

    public static void SaveMenu(MenuSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(MenuSaveKey, json);
        PlayerPrefs.Save();
    }

    public static MenuSaveData LoadMenu()
    {
        if (!PlayerPrefs.HasKey(MenuSaveKey))
            return null;

        string json = PlayerPrefs.GetString(MenuSaveKey);

        if (string.IsNullOrEmpty(json))
            return null;

        return JsonUtility.FromJson<MenuSaveData>(json);
    }
    public static void DeleteMenuSave()
    {
        if (PlayerPrefs.HasKey(MenuSaveKey))
        {
            PlayerPrefs.DeleteKey(MenuSaveKey);
            PlayerPrefs.Save();
        }
    }
}