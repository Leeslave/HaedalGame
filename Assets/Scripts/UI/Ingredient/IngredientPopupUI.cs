using UnityEngine;

public class IngredientPopupUI : UIPopup
{
    public override void Open()
    {
        base.Open();
        PopupManager.Instance.OpenPopup(this);
    }
}
