using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 집 UI "식당 알바 현황" 패널.
/// 주방/서빙 그룹별로 슬롯을 생성해 배치된 알바 · 빈칸(+) · 잠금을 표시한다.
/// 빈칸이나 알바를 누르면 기존 알바 관리 팝업(PartTimePopup)이 열린다.
/// </summary>
public class HouseStaffPanelUI : MonoBehaviour
{
    /// <summary>역할별 슬롯 그룹 (주방 최대 6, 서빙 최대 10 등).</summary>
    [Serializable]
    public class StaffGroup
    {
        [SerializeField] private PartTimerRole _role = PartTimerRole.Kitchen;
        [SerializeField] private string _title = "주방 담당";
        [SerializeField] private TMP_Text _titleText;   // "주방 담당 (최대 6명)"
        [SerializeField] private Transform _slotRoot;
        [SerializeField] private int _maxCount = 6;

        // 슬롯별 해금에 필요한 식당 레벨. 값이 현재 레벨보다 크면 잠금 표시.
        // 배열 길이가 maxCount보다 짧으면 나머지는 잠금 없음(0)으로 간주.
        [SerializeField] private int[] _slotUnlockLevels = new int[0];

        public PartTimerRole Role => _role;
        public string Title => _title;
        public TMP_Text TitleText => _titleText;
        public Transform SlotRoot => _slotRoot;
        public int MaxCount => _maxCount;

        public int GetUnlockLevel(int index)
        {
            if (_slotUnlockLevels == null || index < 0 || index >= _slotUnlockLevels.Length)
                return 0;

            return _slotUnlockLevels[index];
        }
    }

    [SerializeField] private HouseStaffSlotUI _slotPrefab;
    [SerializeField] private List<StaffGroup> _groups = new List<StaffGroup>();

    [Header("알바 관리 팝업")]
    [SerializeField] private PartTimePopup _partTimePopup;

    private readonly List<HouseStaffSlotUI> _spawnedSlots = new List<HouseStaffSlotUI>();

    public void Refresh()
    {
        ClearSlots();

        if (_slotPrefab == null)
            return;

        int restaurantLevel = RestaurantLevelManager.Instance != null
            ? RestaurantLevelManager.Instance.CurrentLevel
            : 1;

        for (int g = 0; g < _groups.Count; g++)
        {
            StaffGroup group = _groups[g];

            if (group == null || group.SlotRoot == null)
                continue;

            if (group.TitleText != null)
                group.TitleText.text = $"{group.Title} (최대 {group.MaxCount}명)";

            List<PartTimerData> assigned = PartTimerAssignmentManager.Instance != null
                ? PartTimerAssignmentManager.Instance.GetAssignedPartTimers(group.Role)
                : new List<PartTimerData>();

            for (int i = 0; i < group.MaxCount; i++)
            {
                HouseStaffSlotUI slot = Instantiate(_slotPrefab, group.SlotRoot);
                _spawnedSlots.Add(slot);

                int requiredLevel = group.GetUnlockLevel(i);

                if (requiredLevel > restaurantLevel)
                {
                    slot.BindLocked(requiredLevel);
                    continue;
                }

                if (i < assigned.Count)
                    slot.BindFilled(assigned[i], null, OpenPartTimePopup);
                else
                    slot.BindEmpty(OpenPartTimePopup);
            }
        }
    }

    /// <summary>기존 알바 관리 UI를 연다.</summary>
    private void OpenPartTimePopup()
    {
        if (_partTimePopup != null)
            _partTimePopup.Open();
        else
            Debug.LogWarning("[House] PartTimePopup 미할당 - 알바 관리 화면을 열 수 없습니다", this);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
    }
}
