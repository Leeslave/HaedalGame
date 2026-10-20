using UnityEngine;

public class GachaPopupUI : UIPopup
{
    public override void Open()
    {
        if (GameSession.IsActive)
        {
            var management = FindFirstObjectByType<RestaurantManagementUI>();
            if (management != null) management.Open("직원");
            return;
        }
        base.Open();
        PopupManager.Instance.OpenPopup(this);
    }
}
