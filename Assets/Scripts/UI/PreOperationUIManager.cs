using UnityEngine;

public class PreOperationUIManager : MonoBehaviour
{
    public static PreOperationUIManager Instance;
    [SerializeField] private GameObject uiRoot;

    // 영업 시작 전 추가 검사. 영업할 수 없는 이유를 돌려주면 시작하지 않는다. (null이면 통과)
    public static System.Func<string> StartBlocker;
    // StartBlocker가 막았을 때 이유를 받아 안내한다.
    public static System.Action<string> OnStartBlocked;

    void Awake()
    {
        Instance = this;
    }

    public void ShowUI() 
    { 
        uiRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();   
    }
    public void HideUI() 
    { 
        uiRoot.SetActive(false);
        Canvas.ForceUpdateCanvases(); 
    }

    // 하단 "자리 배치 하러가기" 버튼
    public void OnTablePlacementButtonClicked()
    {
        CameraController.Instance.MoveToZone(CameraZone.Restaurant);
        HideUI();
        TablePlacementManager.Instance.EnterPlacementMode();
    }

    // 하단 "시작" 버튼
    public void OnStartButtonClicked()
    {
        if (TableManager.Instance.GetPlacedTableCount() <= 0)
        {
            Debug.Log("테이블을 최소 1개 이상 배치해주세요.");
            // TODO: 경고 UI 표시
            OnStartBlocked?.Invoke("테이블을 최소 1개 이상 배치해주세요.");
            return;
        }

        string blockReason = StartBlocker?.Invoke();
        if (!string.IsNullOrEmpty(blockReason))
        {
            Debug.Log("영업을 시작할 수 없습니다: " + blockReason);
            OnStartBlocked?.Invoke(blockReason);
            return;
        }

        CameraController.Instance.MoveToZone(CameraZone.Restaurant);
        HideUI();
        RestaurantGameManager.instance.StartOperation();
    }
}