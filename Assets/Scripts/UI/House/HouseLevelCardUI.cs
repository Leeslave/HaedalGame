using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집 UI 좌측 하단 "식당 레벨" 카드.
/// 현재 레벨을 표시하고, [레벨 정보] 버튼으로 상세 화면(대장간 식당 레벨 탭 등)을 연다.
/// </summary>
public class HouseLevelCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _levelText; // "Lv. 3"
    [SerializeField] private Button _levelInfoButton;

    [Header("레벨 정보 화면 (선택)")]
    [Tooltip("[레벨 정보] 클릭 시 열 오브젝트. 비우면 버튼이 비활성화된다.")]
    [SerializeField] private GameObject _levelInfoTarget;

    private void Awake()
    {
        if (_levelInfoButton != null)
            _levelInfoButton.onClick.AddListener(OpenLevelInfo);
    }

    private void OnDestroy()
    {
        if (_levelInfoButton != null)
            _levelInfoButton.onClick.RemoveListener(OpenLevelInfo);
    }

    public void Refresh()
    {
        int level = RestaurantLevelManager.Instance != null
            ? RestaurantLevelManager.Instance.CurrentLevel
            : 1;

        if (_levelText != null)
            _levelText.text = $"Lv. {level}";

        if (_levelInfoButton != null)
            _levelInfoButton.interactable = _levelInfoTarget != null;
    }

    private void OpenLevelInfo()
    {
        if (_levelInfoTarget != null)
            _levelInfoTarget.SetActive(true);
    }
}
