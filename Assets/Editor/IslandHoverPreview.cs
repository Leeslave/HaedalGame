using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Render the actual MainIsland scene and pointer-enter signs without changing the saved scene.</summary>
public static class IslandHoverPreview
{
    [MenuItem("Haedal/Validation/Main Island hover captures")]
    public static void Capture()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainIsland.unity");
        RenderTexture target = null;
        try
        {
            var objects = scene.GetRootGameObjects();
            var camera = objects.SelectMany(o => o.GetComponentsInChildren<Camera>(true)).First();
            camera.scene = scene;
            target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            foreach (var canvas in objects.SelectMany(o => o.GetComponentsInChildren<Canvas>(true)))
            {
                if (canvas.name.Contains("Popup")) { canvas.gameObject.SetActive(false); continue; }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 100;
            }
            var signs = objects.SelectMany(o => o.GetComponentsInChildren<IslandMenuButtonHoverSign>(true)).ToArray();
            if (signs.Length != 3) throw new Exception("Expected three building hover signs.");
            foreach (var sign in signs)
                typeof(IslandMenuButtonHoverSign).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sign, null);
            string output = Path.GetFullPath("UIValidation/IslandHover");
            Directory.CreateDirectory(output);
            foreach (var sign in signs)
            {
                sign.OnPointerEnter(null);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(output, sign.name + ".png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.active = previous;
                sign.OnPointerExit(null);
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: MainIsland scene rendered with each of its three pointer-enter banners.");
        }
        finally
        {
            foreach (var camera in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>(true)))
                camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
