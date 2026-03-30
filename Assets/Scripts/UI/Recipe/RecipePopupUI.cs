using UnityEngine;

public class RecipePopup : UIPopup
{
    public override void Open()
    {
        base.Open();
        PopupManager.Instance.OpenPopup(this);
    }
}
