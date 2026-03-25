using System;
using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;
    public GameObject ConfirmPopupPrefab;

    [SerializeField] private GameObject _dimPanel;

    private readonly Stack<UIPopup> _popupStack = new Stack<UIPopup>();

    public bool HasPopup => _popupStack.Count > 0;
    public UIPopup CurrentPopup => HasPopup ? _popupStack.Peek() : null;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RefreshDimPanel();
    }

    public void OpenPopup(UIPopup popup)
    {
        if (popup == null)
            return;

        if (_popupStack.Contains(popup))
            return;

        _popupStack.Push(popup);
        popup.Open();
        popup.transform.SetAsLastSibling();

        RefreshDimPanel();
    }

    public void ClosePopup(UIPopup popup)
    {
        if (popup == null || _popupStack.Count == 0)
            return;

        // 가장 위 팝업이면 바로 제거
        if (_popupStack.Peek() == popup)
        {
            _popupStack.Pop();
            popup.Close();
            RefreshDimPanel();
            return;
        }

        // 중간에 있는 팝업 제거
        Stack<UIPopup> tempStack = new Stack<UIPopup>();

        while (_popupStack.Count > 0)
        {
            UIPopup top = _popupStack.Pop();

            if (top == popup)
            {
                top.Close();
                break;
            }

            tempStack.Push(top);
        }

        while (tempStack.Count > 0)
        {
            _popupStack.Push(tempStack.Pop());
        }

        RefreshDimPanel();
    }

    public void CloseTopPopup()
    {
        if (_popupStack.Count == 0)
            return;

        UIPopup top = _popupStack.Pop();
        top.Close();

        RefreshDimPanel();
    }

    public void CloseAllPopup()
    {
        while (_popupStack.Count > 0)
        {
            UIPopup popup = _popupStack.Pop();
            if (popup != null)
                popup.Close();
        }

        RefreshDimPanel();
    }

    private void RefreshDimPanel()
    {
        if (_dimPanel != null)
            _dimPanel.SetActive(_popupStack.Count > 0);
    }

    public void ShowConfirmPopup(string content, string confirmText, string denyText, Action onConfirm, Action onDeny = null,string subContentText="")
    {
        GameObject popupObj = Instantiate(ConfirmPopupPrefab, transform);
        ConfirmPopupUI popup = popupObj.GetComponent<ConfirmPopupUI>();

        Action confirmAction = () =>
        {
            onConfirm?.Invoke();
            ClosePopup(popup);
            Destroy(popup.gameObject);
        };

        Action denyAction = () =>
        {
            onDeny?.Invoke();
            ClosePopup(popup);
            Destroy(popup.gameObject);
        };

        popup.Bind(content, confirmText, denyText, confirmAction, denyAction,subContentText);
        OpenPopup(popup);
    }
}