using System;
using UnityEngine;

public class RestaurantGameManager : MonoBehaviour
{
    public static RestaurantGameManager instance { get; private set; }
    
    public SeatManager seatManager;
    public OrderManager orderManager;
    public RatingSystem ratingSystem;
    public CustomerSpawner customerSpawner;
    public KitchenSystem kitchenSystem;
    public HallSystem hallSystem;

    public Transform exitPoint;

    public MenuData menuData = new MenuData();

    private int pendingClosingDay;
    private float pendingTodayRating;
    private float pendingPreviousRating;

    // 영업 시작 버튼 연타로 손님 큐가 두 번 채워지는 것을 막는다.
    public bool IsOperating { get; private set; }

    // 손님 생성 직전에 발행된다. (손님이 0명이면 이 직후 바로 종료 처리가 이어진다)
    public event Action OnOperationStarted;

    // 설정되면 영업 종료(마지막 손님 퇴장) 처리를 이 핸들러에 맡긴다. 비어 있으면 기존 결산 흐름을 그대로 사용한다.
    public Action OperationEndHandler;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        seatManager = GetComponentInChildren<SeatManager>();
        orderManager = GetComponentInChildren<OrderManager>();
        ratingSystem = GetComponentInChildren<RatingSystem>();
        customerSpawner = GetComponentInChildren<CustomerSpawner>();
        kitchenSystem = GetComponentInChildren<KitchenSystem>();
        hallSystem = GetComponentInChildren<HallSystem>();
    }

    public void StartOperation()
    {
        if (IsOperating) { return; }
        IsOperating = true;
        IngredientInventoryService.Instance?.BeginOperation();

        if (OperationUIManager.Instance != null) { OperationUIManager.Instance.ShowUI(); }

        ServerManager.Instance.InitializeAgents();
        ChefManager.Instance.InitializeAgents();
        customerSpawner.OnAllCustomersHandled -= EndOperation;
        customerSpawner.OnAllCustomersHandled += EndOperation;
        OnOperationStarted?.Invoke();
        customerSpawner.StartGame();
    }

    // 마지막 손님이 나가면 CustomerSpawner.OnAllCustomersHandled를 통해 호출된다.
    public void EndOperation()
    {
        IsOperating = false;
        IngredientInventoryService.Instance?.EndOperation();

        if (OperationEndHandler != null)
        {
            OperationEndHandler();
            return;
        }

        if (OperationUIManager.Instance != null) { OperationUIManager.Instance.HideUI(); }
        RestaurantSpeedController.SetFastForward(false);        // Next Day에 배속이 켜져있는 문제 방지.

        pendingClosingDay = InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 0;
        pendingTodayRating = RestaurantRatingManager.Instance != null ? RestaurantRatingManager.Instance.TodayAverage : 0f;
        pendingPreviousRating = RestaurantRatingManager.Instance != null ? RestaurantRatingManager.Instance.RestaurantRating : 0f;

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOut(FinishEndOperation);
        }
        else
        {
            FinishEndOperation();
        }
    }

    private void FinishEndOperation()
    {
        if (InGameTimeManager.Instance != null) { InGameTimeManager.Instance.AdvanceDay(); }

        float totalRating = RestaurantRatingManager.Instance != null ? RestaurantRatingManager.Instance.RestaurantRating : pendingPreviousRating;

        ClosingSummaryData summary = new ClosingSummaryData();
        summary.Day = pendingClosingDay;
        summary.TodayRating = pendingTodayRating;
        summary.PreviousRating = pendingPreviousRating;
        summary.TotalRating = totalRating;

        if (DailySalesTracker.Instance != null)
        {
            summary.Sales = DailySalesTracker.Instance.Sales;
            summary.TotalSaleCount = DailySalesTracker.Instance.TotalCount;
            summary.TotalRevenue = DailySalesTracker.Instance.TotalRevenue;
        }

        if (DailyFinanceTracker.Instance != null)
        {
            summary.TotalIncome = DailyFinanceTracker.Instance.TotalIncome;
            summary.TotalExpense = DailyFinanceTracker.Instance.TotalExpense;
            summary.NetProfit = DailyFinanceTracker.Instance.NetProfit;
        }

        if (DailyCustomerTracker.Instance != null)
        {
            summary.ServedCustomerCount = DailyCustomerTracker.Instance.ServedCount;
            summary.NotServedCustomerCount = DailyCustomerTracker.Instance.NotServedCount;
            summary.TotalCustomerCount = DailyCustomerTracker.Instance.TotalCount;
        }

        if (ClosingReportManager.Instance != null)
        {
            ClosingReportManager.Instance.ShowReport(summary);
        }

        ResetDailyState();
    }

    // 당일 집계와 작업 배정 상태만 초기화한다. (골드·가구·직원 등 누적 자산은 건드리지 않는다)
    public void ResetDailyState()
    {
        if (DailySalesTracker.Instance != null) { DailySalesTracker.Instance.ResetDay(); }
        if (DailyCustomerTracker.Instance != null) { DailyCustomerTracker.Instance.ResetDay(); }
        if (DailyFinanceTracker.Instance != null) { DailyFinanceTracker.Instance.ResetDay(); }

        if (hallSystem != null) { hallSystem.ResetDay(); }
        if (kitchenSystem != null) { kitchenSystem.ResetDay(); }
    }
}
