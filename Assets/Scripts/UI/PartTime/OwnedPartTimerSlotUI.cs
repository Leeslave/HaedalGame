using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class OwnedPartTimerSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject _emptyObj;
    [SerializeField] private GameObject _infoObj;
    [SerializeField] private GameObject _selectedObj;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _roleText;
    [SerializeField] private TMP_Text _servingText;
    [SerializeField] private TMP_Text _cookingText;
    [SerializeField] private TMP_Text _handyText;
    [SerializeField] private TMP_Text _hpText;

    private PartTimerData _data;

    public PartTimerData Data => _data;
    public bool IsEmpty => _data == null;

    public void Bind(PartTimerData data, bool isSelected)
    {
        _data = data;

        bool hasData = _data != null;

        if (_emptyObj != null)
            _emptyObj.SetActive(!hasData);

        if (_infoObj != null)
            _infoObj.SetActive(hasData);

        if (_selectedObj != null)
            _selectedObj.SetActive(hasData && isSelected);

        if (!hasData)
            return;

        _nameText.text = _data.serverName;
        _gradeText.text = _data.level;
        _servingText.text = _data.status.serving.ToString();
        _cookingText.text = _data.status.cooking.ToString();
        _handyText.text = _data.status.handy.ToString();
        _hpText.text = _data.status.hp.ToString();
        _roleText.text = GetRoleText(data.CurrentRole);
    }

    public void SetEmpty()
    {
        Bind(null, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsEmpty)
            return;

        PartTimerAssignmentManager.Instance.OnClickOwnedPartTimer(this);
    }

    private string GetRoleText(PartTimerRole role)
    {
        switch (role)
        {
            case PartTimerRole.Serving:
                return "»¶";
            case PartTimerRole.Kitchen:
                return "¡÷πÊ";
            default:
                return "¥Î±‚";
        }
    }
}