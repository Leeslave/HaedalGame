using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>별도 검증 프로젝트에서 시작 씬의 실제 Awake/Start/폰트 부트스트랩을 검사한다.</summary>
[InitializeOnLoad]
public static class UIDesignPlayValidation
{
    private const string Key = "Haedal.UIValidation.Play";
    private static double started;
    static UIDesignPlayValidation() { EditorApplication.update += Update; }

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/UIDesignCheckProject/"))
            throw new InvalidOperationException("별도 UI 검증 프로젝트에서만 실행할 수 있어요.");
        EditorSceneManager.OpenScene("Assets/Scenes/TEST/StartScene_TEST.unity");
        SessionState.SetInt(Key, 1); EditorApplication.EnterPlaymode();
    }

    private static void Update()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (started == 0) { started = EditorApplication.timeSinceStartup; return; }
        if (EditorApplication.timeSinceStartup - started < 5) return;
        try
        {
            if (stage == 1)
            {
                var title = UnityEngine.Object.FindFirstObjectByType<ReferenceTitleView>();
                if (title == null || !title.gameObject.activeInHierarchy) throw new Exception("실제 타이틀 UI 없음");
                foreach (string caption in new[] { "이어하기", "새로하기", "설정", "게임 종료" })
                {
                    var label = title.GetComponentsInChildren<TMP_Text>(true).First(t => t.text == caption);
                    label.ForceMeshUpdate(true, true);
                    if (!label.font.HasCharacters(caption) || label.textInfo.meshInfo.Sum(m => m.vertexCount) == 0 || label.GetComponentsInParent<CanvasGroup>(true).Any(g => g.alpha < .01f))
                        throw new Exception("시작 씬 한글 메시 누락: " + caption);
                }
                CaptureTitle(title);
                var data = NewGameConfig.Load().CreateSaveData(); data.introCompleted = true;
                data.progression.firstToolChosen = true; data.progression.tools.AddRange(new[] { 201, 202, 203, 204 });
                GameSession.Begin(data, true); GameFlow.SetPhase(GamePhase.DayStart);
                SessionState.SetInt(Key, 2); started = EditorApplication.timeSinceStartup;
                SceneManager.LoadScene(GameScenes.Island); return;
            }
            if (stage == 2)
            {
                if (UnityEngine.Object.FindFirstObjectByType<IslandDayHud>() == null) throw new Exception("섬 UI 설치 실패");
                if (!GameSession.IsDevSession) throw new Exception("검증 세션이 아니에요.");
                SessionState.SetInt(Key, 3); started = EditorApplication.timeSinceStartup;
                GameFlow.SetPhase(GamePhase.Preparation); SceneManager.LoadScene(GameScenes.Restaurant); return;
            }
            var prep = UnityEngine.Object.FindFirstObjectByType<FirstDayGuide>();
            if (prep == null || prep.GetComponentsInChildren<Canvas>().Length == 0) throw new Exception("영업 준비 UI 설치 실패");
            Finish("PASS: actual PlayMode startup created title with Korean glyph meshes; island and restaurant preparation UI installed. Used memory-only dev session; no save files written.");
        }
        catch (Exception error) { Finish("FAIL: " + error); }
    }

    private static void Finish(string result)
    {
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../UIValidation"));
        Directory.CreateDirectory(output); File.WriteAllText(output + "/play-result.txt", result);
        SessionState.SetInt(Key, 0); GameSession.End(); EditorApplication.Exit(result.StartsWith("PASS") ? 0 : 1);
    }

    private static void CaptureTitle(ReferenceTitleView title)
    {
        var owner = new GameObject("Title verification camera", typeof(Camera));
        var camera = owner.GetComponent<Camera>(); camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -100); camera.orthographicSize = 540;
        var target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target;
        var canvas = title.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = 1.5f;
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); screenshot.Apply();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../UIValidation"));
        Directory.CreateDirectory(output); File.WriteAllBytes(output + "/00-ActualStartup.png", screenshot.EncodeToPNG());
        RenderTexture.active = previous; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null; scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        camera.targetTexture = null; target.Release();
        UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(screenshot); UnityEngine.Object.Destroy(owner);
    }
}
