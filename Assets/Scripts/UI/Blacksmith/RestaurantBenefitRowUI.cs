using TMPro;
using UnityEngine;

/// <summary>
/// 식당 레벨 혜택 표의 한 줄. 아이콘/라벨은 씬에서 고정 배치하고, 현재/다음 값만 코드로 갱신한다.
/// 예: 최대 좌석  "12석"  >  "16석 (+4석)"
/// </summary>
public class RestaurantBenefitRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _currentText;
    [SerializeField] private TMP_Text _nextText;

    public void SetValues(string currentText, string nextText)
    {
        if (_currentText != null)
            _currentText.text = currentText;

        if (_nextText != null)
            _nextText.text = nextText;
    }
}
