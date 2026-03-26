using System.Collections.Generic;
using UnityEngine;

public class PartTimerAssignmentManager : MonoBehaviour
{
    public static PartTimerAssignmentManager Instance;

    [Header("References")]
    [SerializeField] private SnapScrollRect _ownedPartTimerScrollRect; 

    [Header("Owned PartTimers")]
    [SerializeField] private List<PartTimerData> _ownedPartTimers = new();

    [Header("Owned PartTimer UI")]
    [SerializeField] private List<OwnedPartTimerSlotUI> _ownedPartTimerSlots = new();

    [Header("Work Slots")]
    [SerializeField] private List<PartTimerSlot> _workSlots = new();
    [SerializeField] private CanvasGroup _kitchenSection;
    [SerializeField] private CanvasGroup _serverSection;


    [Header("Keyboard")]
    [SerializeField] private bool _useKeyboardSelection = true;
    [SerializeField] private KeyCode _upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode _downKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode _selectKey = KeyCode.Return;
    [SerializeField] private KeyCode _selectKey2 = KeyCode.Space;

    [Header("Top Tab Buttons")]
    [SerializeField] private TopTabButtonUI _serverTabButton;
    [SerializeField] private TopTabButtonUI _kitchenTabButton;

    [Header("Top Tab Colors")]
    [SerializeField] private Color _selectedButtonColor = Color.white;
    [SerializeField] private Color _unselectedButtonColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _selectedTextColor = Color.black;
    [SerializeField] private Color _unselectedTextColor = Color.white;

    private PartTimerSlot _selectedTargetSlot;
    private PartTimerData _selectedOwnedPartTimer;

    private int _ownedCursorIndex = -1;

    private TopTabType _currentTopTab = TopTabType.Kitchen;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RefreshOwnedPartTimerList();
        SetTopTab(TopTabType.Kitchen);

    }

    private void Update()
    {
        HandleOwnedPartTimerKeyboard();
    }

    public bool RegisterHiredPartTimer(PartTimerData candidateData)
    {
        if (candidateData == null)
            return false;

        PartTimerData newHire = ClonePartTimerData(candidateData);
        newHire.CurrentRole = PartTimerRole.None;

        _ownedPartTimers.Add(newHire);

        if (_ownedCursorIndex < 0)
            _ownedCursorIndex = 0;

        RefreshOwnedPartTimerList();
        _ownedPartTimerScrollRect.ValidItemCount++;
        return true;
    }

    public void OnClickWorkSlot(PartTimerSlot clickedSlot)
    {
        if (clickedSlot == null || clickedSlot.IsLock)
            return;

        if (_selectedOwnedPartTimer == null)
        {
            _selectedTargetSlot = clickedSlot;
            OpenOwnedPartTimerList();
            return;
        }

        RequestAssignOrSwap(_selectedOwnedPartTimer, clickedSlot);
    }

    public void OnClickOwnedPartTimer(OwnedPartTimerSlotUI ownedSlotUI)
    {
        if (ownedSlotUI == null || ownedSlotUI.IsEmpty)
            return;

        _selectedOwnedPartTimer = ownedSlotUI.Data;
        _ownedCursorIndex = GetOwnedSlotIndex(ownedSlotUI);
        RefreshOwnedPartTimerList();
    }

    private void HandleOwnedPartTimerKeyboard()
    {
        if (!_useKeyboardSelection)
            return;


        int validCount = GetValidOwnedCount();
        if (validCount <= 0)
            return;

        if (_ownedCursorIndex < 0 || _ownedCursorIndex >= validCount)
            _ownedCursorIndex = GetInitialCursorIndex();

        if (Input.GetKeyDown(_upKey) || Input.GetKeyDown(KeyCode.W))
        {
            MoveOwnedCursor(-1, validCount);
        }
        else if (Input.GetKeyDown(_downKey) || Input.GetKeyDown(KeyCode.S))
        {
            MoveOwnedCursor(1, validCount);
        }

        if (Input.GetKeyDown(_selectKey) || Input.GetKeyDown(_selectKey2))
        {
            SelectOwnedCursor();
        }
    }

    private void MoveOwnedCursor(int direction, int validCount)
    {
        if (validCount <= 0)
            return;

        _ownedCursorIndex += direction;
        _ownedCursorIndex = Mathf.Clamp(_ownedCursorIndex, 0, validCount - 1);

        PartTimerData cursorData = _ownedPartTimers[_ownedCursorIndex];
        _selectedOwnedPartTimer = cursorData;
        RefreshOwnedPartTimerList();
    }

    private void SelectOwnedCursor()
    {
        if (_ownedCursorIndex < 0 || _ownedCursorIndex >= _ownedPartTimerSlots.Count)
            return;

        OwnedPartTimerSlotUI slotUI = _ownedPartTimerSlots[_ownedCursorIndex];
        if (slotUI == null || slotUI.IsEmpty)
            return;

        OnClickOwnedPartTimer(slotUI);
    }

    private int GetInitialCursorIndex()
    {
        if (_selectedOwnedPartTimer != null)
        {
            for (int i = 0; i < _ownedPartTimers.Count; i++)
            {
                if (_ownedPartTimers[i] == _selectedOwnedPartTimer)
                    return i;
            }
        }

        return _ownedPartTimers.Count > 0 ? 0 : -1;
    }

    private int GetValidOwnedCount()
    {
        return Mathf.Min(_ownedPartTimers.Count, _ownedPartTimerSlots.Count);
    }

    private int GetOwnedSlotIndex(OwnedPartTimerSlotUI target)
    {
        for (int i = 0; i < _ownedPartTimerSlots.Count; i++)
        {
            if (_ownedPartTimerSlots[i] == target)
                return i;
        }

        return -1;
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
            });
    }

    private void AssignOrSwap(PartTimerData selectedPartTimer, PartTimerSlot targetSlot)
    {
        if (selectedPartTimer == null || targetSlot == null)
            return;

        PartTimerSlot sourceSlot = FindAssignedSlot(selectedPartTimer);
        PartTimerData targetPartTimer = targetSlot.CurrentPartTimer;

        if (sourceSlot == null)
        {
            if (targetPartTimer != null)
                targetPartTimer.CurrentRole = PartTimerRole.None;

            targetSlot.SetPartTimer(selectedPartTimer);
        }
        else if (sourceSlot == targetSlot)
        {
            ClearSelection();
            return;
        }
        else if (targetPartTimer == null)
        {
            sourceSlot.Clear();
            targetSlot.SetPartTimer(selectedPartTimer);
        }
        else
        {
            sourceSlot.SetPartTimer(targetPartTimer);
            targetSlot.SetPartTimer(selectedPartTimer);
        }

        RefreshAllWorkSlots();
        RefreshOwnedPartTimerList();
        ClearSelection();
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

    public void OpenOwnedPartTimerList()
    {

        if (_ownedPartTimers.Count > 0)
        {
            if (_selectedOwnedPartTimer != null)
                _ownedCursorIndex = GetInitialCursorIndex();
            else
                _ownedCursorIndex = 0;
        }
        else
        {
            _ownedCursorIndex = -1;
        }

        RefreshOwnedPartTimerList();
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
        _ownedCursorIndex = _ownedPartTimers.Count > 0 ? 0 : -1;
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
        newData.CurrentRole = PartTimerRole.None;

        newData.status = new PartTimerStatus
        {
            serving = source.status.serving,
            cooking = source.status.cooking,
            handy = source.status.handy,
            hp = source.status.hp
        };

        return newData;
    }

    #region TopButton

    private void SetSectionState(CanvasGroup section, bool active)
    {
        if (section == null)
            return;

        section.alpha = active ? 1f : 0f;
        section.interactable = active;
        section.blocksRaycasts = active;
    }

    private void SetTabButtonState(TopTabButtonUI tabButton, bool selected)
    {
        if (tabButton == null)
            return;

        if (tabButton.background != null)
            tabButton.background.color = selected ? _selectedButtonColor : _unselectedButtonColor;

        if (tabButton.label != null)
            tabButton.label.color = selected ? _selectedTextColor : _unselectedTextColor;
    }

    public void SetTopTab(TopTabType tabType)
    {
        _currentTopTab = tabType;

        bool isServer = tabType == TopTabType.Server;
        bool isKitchen = tabType == TopTabType.Kitchen;

        SetSectionState(_kitchenSection, isKitchen);
        SetSectionState(_serverSection, isServer);

        SetTabButtonState(_serverTabButton, isServer);
        SetTabButtonState(_kitchenTabButton, isKitchen);
    }

    public void OnClickServerTopButton()
    {
        SetTopTab(TopTabType.Server);
    }

    public void OnClickKitchenTopButton()
    {
        SetTopTab(TopTabType.Kitchen);
    }
    #endregion
}