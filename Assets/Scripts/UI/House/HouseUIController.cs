using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 해달이네 집 화면(로비형 UI) 루트 컨트롤러.
/// 하위 패널(알바 현황 / 재고 / 스카우트 / 미션 / 식당 레벨) 갱신을 조율하고 닫기를 처리한다.
/// 기획서 기준 이 화면에서 직접 편집은 하지 않고, 각 패널은 상세 화면으로 이동시킨다.
/// </summary>
public class HouseUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("Panels")]
    [SerializeField] private HouseStaffPanelUI _staffPanel;
    [SerializeField] private HouseInventoryPanelUI _inventoryPanel;
    [SerializeField] private HouseScoutPanelUI _scoutPanel;
    [SerializeField] private HouseMissionPanelUI _missionPanel;
    [SerializeField] private HouseLevelCardUI _levelCard;

    private void Awake()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Close);

        Unsubscribe();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        // 씬 로드 순서상 OnEnable이 매니저 Awake보다 먼저 돌 수 있어 한 번 더 시도한다.
        Subscribe();
        RefreshAll();
    }

    public void Open()
    {
        if (_root != null)
            _root.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>모든 패널을 현재 게임 상태로 갱신한다.</summary>
    public void RefreshAll()
    {
        if (_staffPanel != null)
            _staffPanel.Refresh();

        if (_inventoryPanel != null)
            _inventoryPanel.Refresh();

        if (_scoutPanel != null)
            _scoutPanel.Refresh();

        if (_missionPanel != null)
            _missionPanel.Refresh();

        if (_levelCard != null)
            _levelCard.Refresh();
    }

    private void Subscribe()
    {
        // -= 후 += : 여러 번 호출돼도 중복 구독되지 않는다.
        if (IngredientInventoryService.Instance != null)
        {
            IngredientInventoryService.Instance.OnChanged -= RefreshInventory;
            IngredientInventoryService.Instance.OnChanged += RefreshInventory;
        }

        if (RestaurantLevelManager.Instance != null)
        {
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;
            RestaurantLevelManager.Instance.OnLevelChanged += HandleLevelChanged;
        }

        if (PartTimerAssignmentManager.Instance != null)
        {
            PartTimerAssignmentManager.Instance.OnAssignmentChanged -= RefreshStaff;
            PartTimerAssignmentManager.Instance.OnAssignmentChanged += RefreshStaff;
        }
    }

    private void Unsubscribe()
    {
        if (IngredientInventoryService.Instance != null)
            IngredientInventoryService.Instance.OnChanged -= RefreshInventory;

        if (RestaurantLevelManager.Instance != null)
            RestaurantLevelManager.Instance.OnLevelChanged -= HandleLevelChanged;

        if (PartTimerAssignmentManager.Instance != null)
            PartTimerAssignmentManager.Instance.OnAssignmentChanged -= RefreshStaff;
    }

    private void RefreshStaff()
    {
        if (_staffPanel != null)
            _staffPanel.Refresh();
    }

    private void RefreshInventory()
    {
        if (_inventoryPanel != null)
            _inventoryPanel.Refresh();
    }

    // 식당 레벨이 오르면 알바 슬롯 잠금과 레벨 카드가 함께 바뀐다.
    private void HandleLevelChanged(int level)
    {
        if (_staffPanel != null)
            _staffPanel.Refresh();

        if (_levelCard != null)
            _levelCard.Refresh();
    }
}
