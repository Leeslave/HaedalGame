using UnityEngine;

public class GachaPopupUI : UIPopup
{
    public override void Open()
    {
        base.Open();
        PopupManager.Instance.OpenPopup(this);
    }
}
