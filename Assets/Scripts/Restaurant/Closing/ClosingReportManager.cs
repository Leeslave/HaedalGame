using System;
using UnityEngine;

// 결산 데이터 준비 완료를 알리는 얇은 매니저. 결산 팝업 UI가 조립되면
// 이 이벤트를 구독해 화면을 채우면 된다.
public class ClosingReportManager : MonoBehaviour
{
    public static ClosingReportManager Instance { get; private set; }

    public Action<ClosingSummaryData> OnReportReady;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowReport(ClosingSummaryData data)
    {
        OnReportReady?.Invoke(data);
    }
}
