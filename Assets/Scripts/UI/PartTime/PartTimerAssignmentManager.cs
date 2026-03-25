using System.Collections.Generic;
using UnityEngine;

public class PartTimerAssignmentManager : MonoBehaviour
{
    public static PartTimerAssignmentManager Instance;

    [Header("Owned PartTimers")]
    [SerializeField] private List<PartTimerData> _ownedPartTimers = new();

    [Header("Owned PartTimer UI")]
    [SerializeField] private GameObject _ownedPartTimerPanel;
    [SerializeField] private List<OwnedPartTimerSlotUI> _ownedPartTimerSlots = new();

    [Header("Work Slots")]
    [SerializeField] private List<PartTimerSlot> _workSlots = new();

    private PartTimerSlot _selectedTargetSlot;
    private PartTimerData _selectedOwnedPartTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_ownedPartTimerPanel != null)
            _ownedPartTimerPanel.SetActive(false);
    }

    public bool RegisterHiredPartTimer(PartTimerData candidateData)
    {
        if (candidateData == null)
            return false;

        PartTimerData newHire = ClonePartTimerData(candidateData);
        //newHire.CurrentRole = PartTimerRole.None;

        _ownedPartTimers.Add(newHire);
        RefreshOwnedPartTimerList();

        return true;
    }

    public void OnClickWorkSlot(PartTimerSlot clickedSlot)
    {
        if (clickedSlot == null || clickedSlot.IsLock)
            return;

        // 직원을 아직 선택 안 한 상태면 목록 오픈
        if (_selectedOwnedPartTimer == null)
        {
            _selectedTargetSlot = clickedSlot;
            OpenOwnedPartTimerList();
            return;
        }

        // 직원을 이미 선택한 상태면 이 슬롯에 배치/교체 시도
        RequestAssignOrSwap(_selectedOwnedPartTimer, clickedSlot);
    }

    public void OnClickOwnedPartTimer(OwnedPartTimerSlotUI ownedSlotUI)
    {
        if (ownedSlotUI == null || ownedSlotUI.IsEmpty)
            return;

        _selectedOwnedPartTimer = ownedSlotUI.Data;
        RefreshOwnedPartTimerList();
    }

    private void RequestAssignOrSwap(PartTimerData selectedPartTimer, PartTimerSlot targetSlot)
    {
        if (selectedPartTimer == null || targetSlot == null)
            return;

        PartTimerData targetPartTimer = targetSlot.CurrentPartTimer;

        string roleName = GetRoleText(targetSlot.SlotRole);
        string content;

        if (targetPartTimer == null)
        {
            content = $"{selectedPartTimer.serverName} 알바생을 {roleName}에 배치하겠습니까?";
        }
        else
        {
            if (targetPartTimer == selectedPartTimer)
            {
                ClearSelection();
                CloseOwnedPartTimerList();
                return;
            }

            content = $"{selectedPartTimer.serverName} 알바와 {targetPartTimer.serverName} 알바를 교대하겠습니까?";
        }

        PopupManager.Instance.ShowConfirmPopup(
            content,
            "네",
            "아니오",
            () => AssignOrSwap(selectedPartTimer, targetSlot),
            () =>
            {
                ClearSelection();
                CloseOwnedPartTimerList();
            });
    }

    private void AssignOrSwap(PartTimerData selectedPartTimer, PartTimerSlot targetSlot)
    {
        if (selectedPartTimer == null || targetSlot == null)
            return;

        PartTimerSlot sourceSlot = FindAssignedSlot(selectedPartTimer);
        PartTimerData targetPartTimer = targetSlot.CurrentPartTimer;

        // 1) 선택한 알바가 현재 아무 슬롯에도 없는 경우
        if (sourceSlot == null)
        {
            //if (targetPartTimer != null)
            //    targetPartTimer.CurrentRole = PartTimerRole.None;

            targetSlot.SetPartTimer(selectedPartTimer);
        }
        // 2) 같은 슬롯이면 무시
        else if (sourceSlot == targetSlot)
        {
            ClearSelection();
            CloseOwnedPartTimerList();
            return;
        }
        // 3) 빈 슬롯에 이동
        else if (targetPartTimer == null)
        {
            sourceSlot.Clear();
            targetSlot.SetPartTimer(selectedPartTimer);
        }
        // 4) 차있는 슬롯과 교체
        else
        {
            sourceSlot.SetPartTimer(targetPartTimer);
            targetSlot.SetPartTimer(selectedPartTimer);
        }

        RefreshAllWorkSlots();
        RefreshOwnedPartTimerList();
        ClearSelection();
        CloseOwnedPartTimerList();
    }

    public bool TryMoveOrSwap(PartTimerSlot fromSlot, PartTimerSlot toSlot)
    {
        if (fromSlot == null || toSlot == null)
            return false;

        if (fromSlot == toSlot)
            return false;

        if (fromSlot.IsLock || toSlot.IsLock)
            return false;

        PartTimerData fromData = fromSlot.CurrentPartTimer;
        PartTimerData toData = toSlot.CurrentPartTimer;

        if (fromData == null)
            return false;

        string content;
        string roleName = GetRoleText(toSlot.SlotRole);

        if (toData == null)
            content = $"{fromData.serverName} 알바생을 {roleName}에 배치하겠습니까?";
        else
            content = $"{fromData.serverName} 알바와 {toData.serverName} 알바를 교대하겠습니까?";

        PopupManager.Instance.ShowConfirmPopup(
            content,
            "네",
            "아니오",
            () =>
            {
                if (toData == null)
                {
                    fromSlot.Clear();
                    toSlot.SetPartTimer(fromData);
                }
                else
                {
                    fromSlot.SetPartTimer(toData);
                    toSlot.SetPartTimer(fromData);
                }

                RefreshAllWorkSlots();
                RefreshOwnedPartTimerList();
            });

        return true;
    }

    private PartTimerSlot FindAssignedSlot(PartTimerData data)
    {
        if (data == null)
            return null;

        for (int i = 0; i < _workSlots.Count; i++)
        {
            if (_workSlots[i] != null && _workSlots[i].CurrentPartTimer == data)
                return _workSlots[i];
        }

        return null;
    }

    private void OpenOwnedPartTimerList()
    {
        if (_ownedPartTimerPanel != null)
            _ownedPartTimerPanel.SetActive(true);

        RefreshOwnedPartTimerList();
    }

    private void CloseOwnedPartTimerList()
    {
        if (_ownedPartTimerPanel != null)
            _ownedPartTimerPanel.SetActive(false);
    }

    private void RefreshOwnedPartTimerList()
    {
        for (int i = 0; i < _ownedPartTimerSlots.Count; i++)
        {
            if (i < _ownedPartTimers.Count)
            {
                PartTimerData data = _ownedPartTimers[i];
                bool isSelected = data == _selectedOwnedPartTimer;
                _ownedPartTimerSlots[i].Bind(data, isSelected);
            }
            else
            {
                _ownedPartTimerSlots[i].SetEmpty();
            }
        }
    }

    private void RefreshAllWorkSlots()
    {
        for (int i = 0; i < _workSlots.Count; i++)
        {
            if (_workSlots[i] != null)
                _workSlots[i].RefreshUI();
        }
    }

    private void ClearSelection()
    {
        _selectedOwnedPartTimer = null;
        _selectedTargetSlot = null;
        RefreshOwnedPartTimerList();
    }

    private string GetRoleText(PartTimerRole role)
    {
        switch (role)
        {
            case PartTimerRole.Serving:
                return "홀";
            case PartTimerRole.Kitchen:
                return "주방";
            default:
                return "대기";
        }
    }

    private PartTimerData ClonePartTimerData(PartTimerData source)
    {
        PartTimerData newData = new PartTimerData();
        newData.serverName = source.serverName;
        newData.level = source.level;
      //  newData.CurrentRole = PartTimerRole.None;

        newData.status = new PartTimerStatus
        {
            serving = source.status.serving,
            cooking = source.status.cooking,
            handy = source.status.handy,
            hp = source.status.hp
        };

        return newData;
    }
}