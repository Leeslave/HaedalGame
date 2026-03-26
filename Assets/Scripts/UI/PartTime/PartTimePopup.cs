using UnityEngine;

public class PartTimePopup : UIPopup
{
    public override void Open()
    {
        base.Open();
        PopupManager.Instance.OpenPopup(this);
    }
}
