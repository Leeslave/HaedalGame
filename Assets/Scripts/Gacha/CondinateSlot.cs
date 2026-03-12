using TMPro;
using UnityEngine;
using UnityEngine.UI; // TextMeshPro를 쓰신다면 TMPro 네임스페이스를 추가하세요!
public class CondinateSlot : MonoBehaviour
{
    [Header("상태별 UI 패널")]
    public GameObject emptyImageObj;     // 가챠 실패/비어있을 때 뜨는 이미지
    public GameObject condinateInfoObj;  // 가챠 성공 시 뜨는 정보 패널

    [Header("Top Section")]
    public TextMeshProUGUI gradeText; // 등급 텍스트
    public TextMeshProUGUI nameText;  // 이름 텍스트

    [Header("Main Section - Ability")]
    public TextMeshProUGUI servingText; // 서빙 수치
    public TextMeshProUGUI cookingText; // 요리 수치
    public TextMeshProUGUI handyText;   // 솜씨 수치
    public TextMeshProUGUI hpText;      // 체력 수치

    public void SetEmpty()
    {
        emptyImageObj.SetActive(true);
        condinateInfoObj.SetActive(false);
    }

    public void SetData(string grade, string otterName, PartTimerStatus status)
    {
        emptyImageObj.SetActive(false);
        condinateInfoObj.SetActive(true);

        gradeText.text = grade;
        nameText.text = otterName;

        servingText.text = status.serving.ToString();
        cookingText.text = status.cooking.ToString();
        handyText.text = status.handy.ToString();
        hpText.text = status.hp.ToString();
    }
}