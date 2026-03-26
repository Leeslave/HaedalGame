using UnityEngine;

public abstract class UIPopup : MonoBehaviour
{
    public bool IsMenu = false;
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }


}