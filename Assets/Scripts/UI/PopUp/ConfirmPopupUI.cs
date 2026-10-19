using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupUI : UIPopup
{
    private Action _onClickConfirmButton;
    private Action _onClickDenyButton;
    private bool _handled; // 닫힘 연출 중 버튼 연타로 콜백이 두 번 실행되는 것을 막는다.

    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _denyButton;

    [SerializeField] private TMP_Text _contentText;
    [SerializeField] private TMP_Text _subContentText;

    [SerializeField] private TMP_Text _confirmButtonText;
    [SerializeField] private TMP_Text _denyButtonText;

    [SerializeField] private UIPopupMotion _popupMotion;

    private void OnEnable()
    {
        _confirmButton.onClick.AddListener(HandleClickConfirm);
        _denyButton.onClick.AddListener(HandleClickDeny);

        if (_popupMotion != null)
            _popupMotion.PlayOpen();
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

        // 거절 문구가 비어 있으면 확인 버튼 하나짜리 알림 팝업으로 쓴다.
        _denyButton.gameObject.SetActive(!string.IsNullOrEmpty(denyText));
        _handled = false;

        _onClickConfirmButton = onConfirm;
        _onClickDenyButton = onDeny;
    }


    public void PlayClose(Action onClosed)
    {
        if (_popupMotion == null)
        {
            onClosed?.Invoke();
            return;
        }

        _popupMotion.PlayClose(onClosed);
    }

    private void HandleClickConfirm()
    {
        if (_handled) return;
        _handled = true;
        _onClickConfirmButton?.Invoke();
    }

    private void HandleClickDeny()
    {
        if (_handled) return;
        _handled = true;
        _onClickDenyButton?.Invoke();
    }
}