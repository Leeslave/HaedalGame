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
