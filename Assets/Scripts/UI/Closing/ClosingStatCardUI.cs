using TMPro;
using UnityEngine;

// 결산 팝업의 통계 카드(<평점>/<돈>/<손님>) 공용 컨트롤러. ClosingCard 프리팹에 부착된다.
public class ClosingStatCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text subText1;
    [SerializeField] private TMP_Text subText2;
    [SerializeField] private TMP_Text subText3;

    public void Setup(string title, string line1, string line2, string line3)
    {
        if (headerText != null) { headerText.text = title; }
        if (subText1 != null) { subText1.text = line1; }
        if (subText2 != null) { subText2.text = line2; }
        if (subText3 != null) { subText3.text = line3; }
    }
}
