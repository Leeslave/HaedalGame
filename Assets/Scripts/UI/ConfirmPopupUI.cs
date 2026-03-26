using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : UIPopup
{
    private Action _onClickConfirmButton;
    private Action _onClickDenyButton;

    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _denyButton;

    [SerializeField] private TMP_Text _contentText;
    [SerializeField] private TMP_Text _subContentText;

    [SerializeField] private TMP_Text _confirmButtonText;
    [SerializeField] private TMP_Text _denyButtonText;

    private void OnEnable()
    {
        _confirmButton.onClick.AddListener(HandleClickConfirm);
        _denyButton.onClick.AddListener(HandleClickDeny);
    }

    private void OnDisable()
    {
        _confirmButton.onClick.RemoveListener(HandleClickConfirm);
        _denyButton.onClick.RemoveListener(HandleClickDeny);

        _onClickConfirmButton = null;
        _onClickDenyButton = null;
    }

    public void Bind(string content, string confirmText, string denyText, Action onConfirm, Action onDeny, string subContentText = "")
    {
        _contentText.text = content;
        _subContentText.text = subContentText;
        _confirmButtonText.text = confirmText;
        _denyButtonText.text = denyText;

        _onClickConfirmButton = onConfirm;
        _onClickDenyButton = onDeny;
    }

    private void HandleClickConfirm()
    {
        _onClickConfirmButton?.Invoke();
    }

    private void HandleClickDeny()
    {
        _onClickDenyButton?.Invoke();
    }
}