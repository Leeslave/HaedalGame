using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>저장 파일과 열린 씬을 변경하지 않는 UI 미리보기. UIValidation에 결과를 쓴다.</summary>
public static class UIDesignValidation
{
    private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../UIValidation"));

    [MenuItem("Haedal/Validation/UI design previews")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        if (EditorApplication.isPlayingOrWillChangePlaymode || GameSession.IsActive)
        { File.WriteAllText(Output + "/result.txt", "SKIPPED: Exit play mode and end the active game session first."); return; }
        Scene preview = default;
        GamePhase originalPhase = GameFlow.Phase;
        RenderTexture target = null;
        try
        {
            var fixture = new ProgressionTests();
            foreach (string name in new[] {
                nameof(ProgressionTests.ShoppingCart_RejectsWholeOrder_WhenOneItemIsOutOfStock),
                nameof(ProgressionTests.ShoppingCart_RejectsWholeOrder_WhenGoldIsInsufficient),
                nameof(ProgressionTests.ShoppingCart_PurchasesAllItems_AndDeductsExactGoldAndStock),
                nameof(ProgressionTests.FurniturePurchase_ChargesOnce_AndRejectsInsufficientFunds),
                nameof(ProgressionTests.Expedition_ReturnsRewardsOnce_AndProtectsOperatingStaff) })
            {
                try { fixture.Setup(); typeof(ProgressionTests).GetMethod(name).Invoke(fixture, null); }
                finally { fixture.Teardown(); }
            }
            preview = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("UI preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview; camera.orthographic = true; camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x4F, 0xA3, 0xBD, 255);
            target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            const string headingSample = "해달식당 이어하기 새로하기 설정 게임 종료";
            const string bodySample = "마지막 저장 플레이 시간 아침 영업 밤";
            DesignFonts.Heading.TryAddCharacters(headingSample, out _);
            DesignFonts.Body.TryAddCharacters(bodySample, out _);
            if (!DesignFonts.Heading.HasCharacters(headingSample)) throw new InvalidOperationException("Jua 한글 글리프 누락");
            if (!DesignFonts.Body.HasCharacters(bodySample)) throw new InvalidOperationException("나눔고딕 한글 글리프 누락");
            var titleView = ReferenceTitleView.Create(preview, () => { }, () => { }, () => { }, () => { });
            var titleCanvas = titleView.GetComponent<Canvas>();
            titleCanvas.renderMode = RenderMode.ScreenSpaceCamera; titleCanvas.worldCamera = camera; titleCanvas.planeDistance = 10;
            titleCanvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            titleCanvas.GetComponent<CanvasScaler>().scaleFactor = 1.5f;
            titleView.RefreshSave();
            Capture(camera, target, "01-Title");
            UnityEngine.Object.DestroyImmediate(titleView.gameObject);
            var data = NewGameConfig.Load().CreateSaveData();
            data.day = 4; data.restaurantLevel = 2; data.progression.firstToolChosen = true;
            data.progression.tools.AddRange(new[] { 201, 202, 203, 204 });
            data.wallets[0].amount = 12480;
            GameSession.Begin(data, true); GameFlow.SetPhase(GamePhase.Preparation);
            var inventoryObject = new GameObject("UI preview inventory", typeof(IngredientInventoryService));
            SceneManager.MoveGameObjectToScene(inventoryObject, preview);
            var inventory = inventoryObject.GetComponent<IngredientInventoryService>();
            if (IngredientInventoryService.Instance == null)
                typeof(IngredientInventoryService).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, inventory);
            GameSession.Register(inventory);
            inventory.RestoreState(data);
            var elfObject = new GameObject("UI preview elf", typeof(ElfShopManager));
            SceneManager.MoveGameObjectToScene(elfObject, preview);
            var elf = elfObject.GetComponent<ElfShopManager>();
            Field(elf, "_database").SetValue(elf, GameDatabase.Recipes);
            Field(elf, "_elfConfig").SetValue(elf, Resources.Load<ElfShopConfigSO>("Data/ElfShopConfig"));
            typeof(ElfShopManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, elf);
            GameSession.Register(elf); elf.RestoreState(data);
            var islandOwner = new GameObject("Island preview", typeof(IslandDayHud));
            SceneManager.MoveGameObjectToScene(islandOwner, preview);
            Call(islandOwner.GetComponent<IslandDayHud>(), "Start");
            CaptureOwner(islandOwner, camera, target, "02-Island");
            var dayOwner = new GameObject("Day preview", typeof(DayCycleController));
            SceneManager.MoveGameObjectToScene(dayOwner, preview);
            var day = dayOwner.GetComponent<DayCycleController>();
            var prep = dayOwner.AddComponent<FirstDayGuide>(); prep.Init(day);
            CaptureOwnerCanvas(dayOwner, camera, target, "03-Preparation");
            UnityEngine.Object.DestroyImmediate(prep);
            foreach (var c in dayOwner.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject);
            typeof(DayCycleController).GetField("<Phase>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(day, DayLoopPhase.Open);
            var hud = dayOwner.AddComponent<RestaurantHud>(); hud.Init(day);
            CaptureOwnerCanvas(dayOwner, camera, target, "04-Operation");
            UnityEngine.Object.DestroyImmediate(hud);
            foreach (var c in dayOwner.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject);
            var placer = dayOwner.AddComponent<TablePlacementManager>();
            var toolbar = dayOwner.AddComponent<PlacementToolbar>();
            typeof(PlacementToolbar).GetMethod("Build", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(toolbar, new object[] { placer });
            Call(toolbar, "RefreshSeats");
            CaptureOwnerCanvas(dayOwner, camera, target, "05-Placement");
            UnityEngine.Object.DestroyImmediate(toolbar);
            foreach (var c in dayOwner.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject);
            var settlement = dayOwner.AddComponent<SettlementPanel>();
            settlement.Show(new DaySettlementRecord { day = 4, revenue = 1250, netProfit = 1250, goldAtClose = 12480, visitedCount = 24, servedCount = 23, notServedCount = 1, todayRating = 4.2f, totalRating = 3.9f }, "섬으로 돌아가기", () => { }, null);
            settlement.SetSaveState(SettlementPanel.SaveState.Saved);
            CaptureOwnerCanvas(dayOwner, camera, target, "06-Settlement");
            UnityEngine.Object.DestroyImmediate(dayOwner);
            foreach (string section in new[] { "메뉴", "재료", "직원", "연구", "상점", "식당", "가구", "주민", "집", "엘프", "대장간" })
            {
                var owner = new GameObject("UI preview", typeof(RestaurantManagementUI));
                SceneManager.MoveGameObjectToScene(owner, preview);
                var ui = owner.GetComponent<RestaurantManagementUI>();
                var canvas = DayLoopUI.CreateCanvas("UI preview canvas", 0, preview);
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                Field(ui, "canvas").SetValue(ui, canvas);
                Call(ui, "BuildWindow"); ui.Open(section);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/" + section + ".png", image.EncodeToPNG());
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                if (section == "대장간")
                {
                    GameSession.Current.progression.freeTime = true;
                    int recipeId = GameDatabase.Recipes.Recipes.First(r => RecipeResearchPlan.Find(r.RecipeId)?.steps?.Length > 0).RecipeId;
                    GameSession.Current.progression.research.Add(new ResearchEntry { recipeId = recipeId });
                    typeof(RestaurantManagementUI).GetMethod("BeginChallenge", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, new object[] { recipeId });
                    Capture(camera, target, "08-ResearchPractice");
                    Field(ui, "practiceRoot").SetValue(ui, null); Field(ui, "practice").SetValue(ui, null);
                }
                Field(ui, "canvas").SetValue(ui, null);
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
            ReferenceHouseView.Open(preview);
            var houseCanvas = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "Design / 09 House");
            PrepareCanvas(houseCanvas, camera, 1.5f);
            Capture(camera, target, "09-House");
            UnityEngine.Object.DestroyImmediate(houseCanvas.gameObject);
            SettingsWindow.Open(preview);
            var settingsCanvas = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "[Settings] Window");
            PrepareCanvas(settingsCanvas, camera, 1);
            Capture(camera, target, "10-Settings");
            UnityEngine.Object.DestroyImmediate(settingsCanvas.gameObject);
            GameSession.End();
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            UnityEngine.Object.DestroyImmediate(elfObject);
            File.WriteAllText(Output + "/result.txt", "PASS: 5 transaction tests (cart, furniture, expedition); Jua and NanumGothic Korean glyph checks. Rendered title, island, preparation, operation HUD, placement, settlement, research practice, 11 management tabs, house and settings at 1920x1080. No save files changed.");
        }
        catch (Exception error) { File.WriteAllText(Output + "/result.txt", "FAIL: " + error); Debug.LogException(error); }
        finally
        {
            GameSession.End(); GameFlow.SetPhase(originalPhase);
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        }
    }

    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static void PrepareCanvas(Canvas canvas, Camera camera, float scale)
    {
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = scale;
    }
    private static void CaptureOwner(GameObject owner, Camera camera, RenderTexture target, string name)
    {
        CaptureOwnerCanvas(owner, camera, target, name);
        UnityEngine.Object.DestroyImmediate(owner);
    }
    private static void CaptureOwnerCanvas(GameObject owner, Camera camera, RenderTexture target, string name)
    {
        foreach (var canvas in owner.GetComponentsInChildren<Canvas>(true)) PrepareCanvas(canvas, camera, 1);
        Capture(camera, target, name);
    }
    private static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private static void Capture(Camera camera, RenderTexture target, string name)
    {
        Canvas.ForceUpdateCanvases(); camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image);
    }
}
