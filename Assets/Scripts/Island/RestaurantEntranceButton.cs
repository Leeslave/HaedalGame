using UnityEngine;

public class RestaurantEntranceButton : MonoBehaviour
{
    // 인스펙터에서 레스토랑 버튼의 OnClick 이벤트에 연결
    public void OnClickRestaurantButton()
    {
        if (GameSession.OperationCompletedToday)
        {
            PopupManager.Instance.ShowConfirmPopup(
                "오늘 영업은 이미 마쳤습니다.\n집에서 잠을 자면 다음 날이 시작됩니다.",
                "확인",
                "",
                null
            );
            return;
        }

        // 대장간에 맡긴 확장 공사로 휴업하는 날
        if (GameSession.IsActive && RestaurantRules.IsClosedForConstruction(GameSession.Current))
        {
            PopupManager.Instance.ShowConfirmPopup(
                $"오늘은 식당 확장 공사로 휴업해요.\n집에서 잠들면 다음 날 아침 Lv.{GameSession.Current.progression.pendingRestaurantLevel} 식당이 문을 열어요.",
                "확인",
                "",
                null
            );
            return;
        }

        // 섬 HUD가 있으면 오늘의 메뉴·직원·재료 부족을 보여 주는 식당 카드로 확인한다.
        if (IslandDayHud.TryShowRestaurantCard(OnConfirmStartOperation))
            return;

        PopupManager.Instance.ShowConfirmPopup(
            "오늘의 장사를 시작하시겠습니까?",
            "예",
            "아니오",
            OnConfirmStartOperation
        );
    }

    private void OnConfirmStartOperation()
    {
        GameFlow.TryEnterRestaurant();
    }
}
