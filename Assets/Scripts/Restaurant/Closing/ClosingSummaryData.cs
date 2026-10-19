using System.Collections.Generic;

// 하루 영업 결산 화면에 표시할 데이터 스냅샷.
public class ClosingSummaryData
{
    public int Day;

    public IReadOnlyCollection<SaleEntry> Sales;
    public int TotalSaleCount;
    public int TotalRevenue;

    public float TodayRating;
    public float PreviousRating;
    public float TotalRating;

    public int TotalIncome;
    public int TotalExpense;
    public int NetProfit;

    public int ServedCustomerCount;
    public int NotServedCustomerCount;
    public int TotalCustomerCount;
}
