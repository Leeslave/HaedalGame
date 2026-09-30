using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 개발 편의 메뉴. [Haedal > Dev Tools]
/// 씬 설정 작업은 여러 번 실행해도 결과가 같다. (이미 있는 오브젝트는 재사용하고 참조만 다시 연결)
/// </summary>
public static class HaedalDevTools
{
    private const string TitleScenePath = "Assets/Scenes/TEST/StartScene_TEST.unity";
    private const string PopupCanvasPrefabPath = "Assets/Prefabs/UI/Popup/PopupCanvas.prefab";

    // 타이틀 씬 버튼 오브젝트 이름
    private const string NewGameButtonName = "New Start";
    private const string ContinueButtonName = "Continue";
    private const string QuitButtonName = "Exit";

    [MenuItem("Haedal/Dev Tools/타이틀 씬 자동 설정", false, 0)]
    public static void SetupTitleScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("타이틀 씬 자동 설정", "플레이 모드를 종료한 뒤 실행하세요.", "확인");
            return;
        }

        if (!OpenTitleScene())
            return;

        StringBuilder report = new StringBuilder();

        // 1) 타이틀 컨트롤러 + 인트로
        TitleMenuController title = Object.FindFirstObjectByType<TitleMenuController>(FindObjectsInactive.Include);
        if (title == null)
        {
            GameObject go = new GameObject("TitleSystems");
            Undo.RegisterCreatedObjectUndo(go, "Create TitleSystems");
            title = go.AddComponent<TitleMenuController>();
            report.AppendLine("• TitleSystems 오브젝트 생성 (TitleMenuController)");
        }

        IntroController intro = Object.FindFirstObjectByType<IntroController>(FindObjectsInactive.Include);
        if (intro == null)
        {
            intro = Undo.AddComponent<IntroController>(title.gameObject);
            report.AppendLine("• IntroController 추가");
        }

        // 2) 버튼 연결
        Button newGame = FindButton(NewGameButtonName);
        Button cont = FindButton(ContinueButtonName);
        Button quit = FindButton(QuitButtonName);

        SerializedObject so = new SerializedObject(title);
        so.FindProperty("_newGameButton").objectReferenceValue = newGame;
        so.FindProperty("_continueButton").objectReferenceValue = cont;
        so.FindProperty("_quitButton").objectReferenceValue = quit;
        so.FindProperty("_intro").objectReferenceValue = intro;
        so.ApplyModifiedProperties();

        report.AppendLine($"• 버튼 연결: 새로하기 {Mark(newGame)} / 이어하기 {Mark(cont)} / 종료 {Mark(quit)}");

        // 코드에서 클릭을 연결하므로, 수동으로 OnClick에 넣어 둔 같은 메서드가 있으면 중복 호출되지 않게 지운다.
        int removed = RemoveManualTitleListeners(newGame) + RemoveManualTitleListeners(cont) + RemoveManualTitleListeners(quit);
        if (removed > 0)
            report.AppendLine($"• 버튼 OnClick에 수동 연결된 TitleMenuController 호출 {removed}개 제거 (중복 방지)");

        // 3) 확인 팝업
        if (Object.FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include) == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupCanvasPrefabPath);
            if (prefab != null)
            {
                GameObject popup = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(popup, "Create PopupCanvas");
                report.AppendLine("• PopupCanvas 프리팹 추가");
            }
            else
            {
                report.AppendLine($"• [실패] {PopupCanvasPrefabPath} 프리팹을 찾지 못했습니다.");
            }
        }

        // 4) 대화 시스템 (임시 인트로 재생용, DontDestroyOnLoad라 섬에서도 계속 사용)
        if (Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include) == null)
        {
            DialogueSystemBuilder.CreateDialogueSystem(new MenuCommand(null));
            report.AppendLine("• Dialogue System 추가");
        }

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        report.AppendLine(saved ? "• 씬 저장 완료" : "• [실패] 씬 저장 실패");

        Selection.activeGameObject = title.gameObject;
        Debug.Log("[DevTools] 타이틀 씬 자동 설정\n" + report);
        EditorUtility.DisplayDialog("타이틀 씬 자동 설정", report.ToString(), "확인");
    }

    [MenuItem("Haedal/Dev Tools/세이브 폴더 열기", false, 20)]
    public static void OpenSaveFolder()
    {
        string dir = Path.GetDirectoryName(SaveService.FilePath);
        Directory.CreateDirectory(dir);
        EditorUtility.RevealInFinder(SaveService.FilePath);
    }

    [MenuItem("Haedal/Dev Tools/세이브 삭제", false, 21)]
    public static void DeleteSave()
    {
        if (!SaveService.HasSaveFile())
        {
            EditorUtility.DisplayDialog("세이브 삭제", "삭제할 세이브 파일이 없습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("세이브 삭제",
                "진행 세이브(savegame.json)를 삭제합니다. 되돌릴 수 없습니다.\n\n" + SaveService.FilePath,
                "삭제", "취소"))
            return;

        foreach (string path in new[] { SaveService.FilePath, SaveService.FilePath + ".tmp", SaveService.FilePath + ".bak" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        Debug.Log("[DevTools] 세이브를 삭제했습니다.");
    }

    // ───── 내부 ─────

    private static bool OpenTitleScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path == TitleScenePath)
            return true;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        return true;
    }

    private static Button FindButton(string objectName)
    {
        List<Button> matches = new List<Button>();
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.gameObject.name == objectName)
                matches.Add(button);
        }

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[DevTools] 타이틀 씬에서 '{objectName}' 버튼을 찾지 못했습니다. 인스펙터에서 직접 연결하세요.");
            return null;
        }

        if (matches.Count > 1)
            Debug.LogWarning($"[DevTools] '{objectName}' 버튼이 {matches.Count}개입니다. 첫 번째를 연결합니다.", matches[0]);

        return matches[0];
    }

    private static int RemoveManualTitleListeners(Button button)
    {
        if (button == null)
            return 0;

        int removed = 0;
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentTarget(i) is TitleMenuController)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
                removed++;
            }
        }

        if (removed > 0)
            EditorUtility.SetDirty(button);

        return removed;
    }

    private static string Mark(Object target) => target != null ? "O" : "X(직접 연결 필요)";
}
