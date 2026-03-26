using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PartTimerSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot Info")]
    [SerializeField] private PartTimerRole _slotRole;

    [Header("Current Data")]
    [SerializeField] private PartTimerData _currentPartTimer;

    [Header("UI")]
    [SerializeField] private GameObject _emptyObj;
    [SerializeField] private GameObject _partTimerInfoObj;
    [SerializeField] private GameObject _lockObj;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _cookingText;
    [SerializeField] private TMP_Text _servingText;
    [SerializeField] private TMP_Text _handyText;
    [SerializeField] private TMP_Text _hpText;

    public PartTimerRole SlotRole => _slotRole;
    public PartTimerData CurrentPartTimer => _currentPartTimer;
    public bool IsEmpty => _currentPartTimer == null;
    public bool IsLock;

    private void Start()
    {
        RefreshUI();
    }

    public void SetPartTimer(PartTimerData data)
    {
        _currentPartTimer = data;

        if (_currentPartTimer != null)
            _currentPartTimer.CurrentRole = _slotRole;

        RefreshUI();
    }

    public void Clear()
    {
        if (_currentPartTimer != null)
            _currentPartTimer.CurrentRole = PartTimerRole.None;

        _currentPartTimer = null;
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool hasPartTimer = _currentPartTimer != null;

        if (_lockObj != null)
            _lockObj.SetActive(IsLock);

        if (_emptyObj != null)
            _emptyObj.SetActive(!IsLock && !hasPartTimer);

        if (_partTimerInfoObj != null)
            _partTimerInfoObj.SetActive(!IsLock && hasPartTimer);

        if (IsLock || !hasPartTimer)
            return;

        _nameText.text = _currentPartTimer.serverName;
        _gradeText.text = _currentPartTimer.level;
        _servingText.text = _currentPartTimer.status.serving.ToString();
        _cookingText.text = _currentPartTimer.status.cooking.ToString();
        _handyText.text = _currentPartTimer.status.handy.ToString();
        _hpText.text = _currentPartTimer.status.hp.ToString();
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsLock)
            return;

        PartTimerAssignmentManager.Instance.OnClickWorkSlot(this);
    }

}