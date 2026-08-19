using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 일회성 씬 구성 도구: Restaurant.unity에 운영 중(Operation) UI 캔버스를 만들고
// 2배속 버튼 + CurrencyUI를 배치한다. 이미 만들어져 있으면 중복 생성하지 않고 종료한다.
public static class RestaurantOperationUIBuilder
{
    private const string ScenePath = "Assets/Scenes/Restaurant.unity";
    private const string CurrencyPrefabPath = "Assets/Prefabs/UI/CurrencyUI.prefab";
    private const string GoldAssetPath = "Assets/Scriptable Object/Gold.asset";

    [MenuItem("Tools/Restaurant/Build Operation UI")]
    public static void BuildOperationUI()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (GameObject.Find("Operation") != null)
        {
            Debug.LogWarning("[RestaurantOperationUIBuilder] 'Operation' GameObject already exists in the scene. Skipping build to avoid duplicates.");
            return;
        }

        int uiLayer = LayerMask.NameToLayer("UI");

        // 1) Operation 캔버스 루트 (PreOperation과 동일한 설정: ScreenSpaceOverlay, ConstantPixelSize 800x600)
        GameObject canvasGO = new GameObject("Operation", typeof(RectTransform));
        canvasGO.layer = uiLayer;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referenceResolution = new Vector2(800, 600);
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.SetActive(false); // 운영 시작 전까지는 꺼져 있음

        // 2) 항상 활성 상태인 매니저 오브젝트 (PreOperationManager와 동일 패턴)
        GameObject managerGO = new GameObject("OperationManager");
        OperationUIManager opManager = managerGO.AddComponent<OperationUIManager>();
        SerializedObject opSO = new SerializedObject(opManager);
        opSO.FindProperty("uiRoot").objectReferenceValue = canvasGO;
        opSO.ApplyModifiedProperties();

        // 3) CurrencyUI 프리팹 인스턴스 (좌상단), Gold 재화 연결
        GameObject currencyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPrefabPath);
        if (currencyPrefab == null)
        {
            Debug.LogError("[RestaurantOperationUIBuilder] CurrencyUI 프리팹을 찾을 수 없습니다: " + CurrencyPrefabPath);
        }
        else
        {
            GameObject currencyInstance = (GameObject)PrefabUtility.InstantiatePrefab(currencyPrefab, canvasGO.transform);
            RectTransform currencyRect = currencyInstance.GetComponent<RectTransform>();
            currencyRect.anchorMin = new Vector2(0, 1);
            currencyRect.anchorMax = new Vector2(0, 1);
            currencyRect.pivot = new Vector2(0, 1);
            currencyRect.anchoredPosition = new Vector2(20, -20);

            Currency gold = AssetDatabase.LoadAssetAtPath<Currency>(GoldAssetPath);
            CurrencyUI currencyUI = currencyInstance.GetComponent<CurrencyUI>();
            if (gold != null && currencyUI != null)
            {
                SerializedObject curSO = new SerializedObject(currencyUI);
                curSO.FindProperty("_targetCurrency").objectReferenceValue = gold;
                curSO.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError("[RestaurantOperationUIBuilder] Gold 애셋 또는 CurrencyUI 컴포넌트를 찾을 수 없습니다.");
            }
        }

        // 4) 2배속 버튼 (우상단)
        GameObject buttonGO = new GameObject("SpeedButton", typeof(RectTransform));
        buttonGO.layer = uiLayer;
        buttonGO.transform.SetParent(canvasGO.transform, false);
        RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-20, -20);
        btnRect.sizeDelta = new Vector2(90, 50);

        Image btnImage = buttonGO.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = btnImage;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.layer = uiLayer;
        labelGO.transform.SetParent(buttonGO.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "x1";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.color = Color.white;

        RestaurantSpeedButton speedBtnScript = buttonGO.AddComponent<RestaurantSpeedButton>();
        SerializedObject sbSO = new SerializedObject(speedBtnScript);
        sbSO.FindProperty("speedButton").objectReferenceValue = button;
        sbSO.FindProperty("label").objectReferenceValue = tmp;
        sbSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[RestaurantOperationUIBuilder] Operation UI build complete.");
    }
}
