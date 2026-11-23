using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>실제 세이브와 씬을 바꾸지 않고 주민 만화 10편과 오프닝을 렌더링한다.</summary>
public static class IslandComicValidation
{
    [MenuItem("Haedal/Validation/Island comic previews")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("플레이 모드를 종료한 뒤 실행하세요.");
        string output = Path.GetFullPath("Docs/Screenshots/IslandComic");
        Directory.CreateDirectory(output);
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(1920, 1080, 24);
        var previous = RenderTexture.active;
        try
        {
            var cameraObject = new GameObject("Comic preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene; camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white;
            camera.targetTexture = target;
            Capture(scene, camera, target, output, "opening", null, null);
            for (int island = 0; island < IslandStory.Islands.Length; island++)
                foreach (bool arrival in new[] { true, false })
                {
                    string path = arrival ? IslandStory.ArrivalDialogue(island) : IslandStory.DoneDialogue(island);
                    if (!IslandComic.TryBuild(path, out var cuts, out string title)) throw new InvalidOperationException(path);
                    Capture(scene, camera, target, output, IslandStory.Islands[island].DialogueKey + (arrival ? "_arrival" : "_done"), cuts, title);
                }
            File.WriteAllText(Path.Combine(output, "validation.txt"), "PASS: opening and 10 resident comics rendered at 1920x1080; all captions fit.");
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>()) camera.targetTexture = null;
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Capture(Scene scene, Camera camera, RenderTexture target, string output, string name, ComicIntro.Cut[] cuts, string title)
    {
        var canvas = DayLoopUI.CreateCanvas("Comic validation", 950, scene);
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            var comic = canvas.gameObject.AddComponent<ComicIntro>();
            if (cuts != null)
            {
                Field("_cuts").SetValue(comic, cuts);
                Field("_pageSize").SetValue(comic, IslandComic.PageSize);
                Field("_title").SetValue(comic, title);
            }
            typeof(ComicIntro).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(comic, new object[] { canvas.transform });
            foreach (var view in (RectTransform[])Field("_cutViews").GetValue(comic)) view.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            typeof(ComicIntro).GetMethod("Fit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(comic, null);
            foreach (var bubble in canvas.GetComponentsInChildren<ComicSpeechBubble>())
            {
                bubble.canvasRenderer.cull = false;
                bubble.SetAllDirty(); bubble.Rebuild(CanvasUpdate.PreRender);
                var geometry = bubble.canvasRenderer.GetMesh();
                if (geometry == null || geometry.vertexCount == 0) throw new InvalidOperationException(name + ": 말풍선 도형 누락");
            }
            foreach (var label in canvas.GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.maxVisibleCharacters = int.MaxValue; label.ForceMeshUpdate();
                if (label.isTextOverflowing) throw new InvalidOperationException(name + ": 대사 영역 넘침: " + label.text);
            }
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas.gameObject); }
    }

    private static FieldInfo Field(string name) => typeof(ComicIntro).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
}
