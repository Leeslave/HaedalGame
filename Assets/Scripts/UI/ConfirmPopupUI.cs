using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : UIPopup
{
    public Action OnClickConfirmButton;
    public Action OnClickDenyButton;

    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _denyButton;

    [SerializeField] private TMP_Text _contentText;
    [SerializeField] private TMP_Text _confirmButtonText;
    [SerializeField] private TMP_Text _denyButtonText;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(HandleClickConfirm);
        _denyButton.onClick.AddListener(HandleClickDeny);
    }

    public void Bind(string content, string denyText, string confirmText)
    {
        _contentText.text = content;
        _denyButtonText.text = denyText;
        _confirmButtonText.text = confirmText;

        OnClickConfirmButton = null;
        OnClickDenyButton = null;
    }

    private void HandleClickConfirm()
    {
        OnClickConfirmButton?.Invoke();
    }

    private void HandleClickDeny()
    {
        OnClickDenyButton?.Invoke();
    }

    private void OnDisable()
    {
        _confirmButton.onClick.RemoveListener(HandleClickConfirm);
        _denyButton.onClick.RemoveListener(HandleClickDeny);
    }
    
}