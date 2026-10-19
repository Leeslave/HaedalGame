using UnityEngine;

// 운영 중(Operation) 화면 UI 루트. PreOperationUIManager와 동일한 패턴:
// 이 스크립트는 항상 활성 상태인 매니저 오브젝트에 붙고, 실제로 켜고 끄는 대상은 uiRoot(Operation 캔버스)다.
public class OperationUIManager : MonoBehaviour
{
    public static OperationUIManager Instance;
    [SerializeField] private GameObject uiRoot;

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
}
