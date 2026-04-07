using UnityEngine;

public class PreOperationUIManager : MonoBehaviour
{
    [SerializeField] private GameObject uiRoot;

    public void ShowUI() { uiRoot.SetActive(true);  }
    public void HideUI() { uiRoot.SetActive(false); }

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
            return;
        }

        CameraController.Instance.MoveToZone(CameraZone.Restaurant);
        HideUI();
        // TODO: GameManager.Instance.StartOperation()
    }
}