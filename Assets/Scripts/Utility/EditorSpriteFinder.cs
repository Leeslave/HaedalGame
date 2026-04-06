#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditorSpriteFinder
{
    public static Sprite FindSpriteById(string folderPath, int id)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"Invalid sprite folder path: {folderPath}");
            return null;
        }

        string idText = id.ToString();
        string[] guids = AssetDatabase.FindAssets($"{idText} t:Sprite", new[] { folderPath });

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (fileName == idText)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                    return sprite;
            }
        }

        return null;
    }
}
#endif