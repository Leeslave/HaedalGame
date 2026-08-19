using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 결산 팝업 루트 컨트롤러. ClosingReportManager.OnReportReady를 구독해 화면을 채운다.
// 이 오브젝트는 항상 활성 상태를 유지해야 한다(SetActive로 끄면 Awake/Start/OnEnable이
// 다시는 호출되지 않아 이벤트 구독이 걸리지 않는다). 대신 CanvasGroup으로 보이기/가리기와
// 입력 차단을 처리한다.
public class ClosingPanelController : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Title")]
    [SerializeField] private TMP_Text dayTitleText;

    [Header("Sections")]
    [SerializeField] private FoodCard foodCard;
    [SerializeField] private ClosingStatCardUI ratingCard;
    [SerializeField] private ClosingStatCardUI moneyCard;
    [SerializeField] private ClosingStatCardUI customerCard;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        SetVisible(false);

        if (confirmButton != null) { confirmButton.onClick.AddListener(HandleConfirm); }
    }

    private void Start()
    {
        if (ClosingReportManager.Instance != null) { ClosingReportManager.Instance.OnReportReady += ShowReport; }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) { return; }
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void ShowReport(ClosingSummaryData data)
    {
        SetVisible(true);

        if (dayTitleText != null) { dayTitleText.text = "<" + data.Day + "일차 결산>"; }

        if (foodCard != null)
        {
            foodCard.Populate(data.Sales, data.TotalSaleCount, data.TotalRevenue);
        }

        if (ratingCard != null)
        {
            ratingCard.Setup(
                "<평점>",
                "오늘 평점: " + FormatRating(data.TodayRating),
                "기존 평점: " + FormatRating(data.PreviousRating),
                "총 평점: " + FormatRating(data.TotalRating));
        }

        if (moneyCard != null)
        {
            moneyCard.Setup(
                "<돈>",
                "총 수익: " + data.TotalIncome + "G",
                "총 지출: " + data.TotalExpense + "G",
                "총 이익: " + data.NetProfit + "G");
        }

        if (customerCard != null)
        {
            customerCard.Setup(
                "<손님>",
                "대접한 손님: " + data.ServedCustomerCount + "마리",
                "미대접 손님: " + data.NotServedCustomerCount + "마리",
                "총 손님: " + data.ServedCustomerCount + "/" + data.TotalCustomerCount);
        }
    }

    private void HandleConfirm()
    {
        SetVisible(false);

        if (ScreenFader.Instance != null) { ScreenFader.Instance.FadeIn(null); }
    }

    private string FormatRating(float rating)
    {
        return rating.ToString("F1");
    }
}
