using UnityEngine;

public abstract class UIPopup : MonoBehaviour
{
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnClickCloseButton()
    {
       // PopupManager.Instance.ClosePopup(this);
    }
}