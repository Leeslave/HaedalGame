using UnityEngine;
using UnityEngine.SceneManagement;

public class RestaurantEntranceButton : MonoBehaviour
{
    private const string RestaurantSceneName = "Restaurant";

    // 인스펙터에서 레스토랑 버튼의 OnClick 이벤트에 연결
    public void OnClickRestaurantButton()
    {
        PopupManager.Instance.ShowConfirmPopup(
            "오늘의 장사를 시작하시겠습니까?",
            "예",
            "아니오",
            OnConfirmStartOperation
        );
    }

    private void OnConfirmStartOperation()
    {
        SceneManager.LoadScene(RestaurantSceneName);
    }
}
