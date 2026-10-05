using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 당일 장부와 운영 측정값. (ECON-01, RESULT-01, RESULT-03)
/// 영업 시작부터 결산까지 실제 이벤트만 집계한다.
///  - 매출: 손님 결제 이벤트(CustomerAgent.OnAnyPaid)의 실제 지급액. 화면 효과는 별도 지급 경로가 아니다.
///  - 기타 수입·지출: 영업 중 CurrencyManager를 거친 골드 거래
///  - 손님: 생성(방문) / 계산 후 퇴장(대접) / 계산 없이 퇴장(미대접)
///  - 측정값: 평균 계산용 합계와 표본 수만 누적한다. (로그를 쌓지 않는다)
/// </summary>
public class DayLedger
{
    private class Timeline
    {
        public float spawned;
        public float menuChosen = -1f;
        public float orderTaken = -1f;
        public bool seatedCounted;
        public bool foodCounted;
    }

    private readonly Dictionary<int, DaySaleEntry> _sales = new Dictionary<int, DaySaleEntry>();
    private readonly List<int> _saleOrder = new List<int>();
    private readonly Dictionary<CustomerAgent, Timeline> _timelines = new Dictionary<CustomerAgent, Timeline>();

    public bool IsRecording { get; private set; }

    public int Revenue { get; private set; }
    public int OtherIncome { get; private set; }
    public int Expense { get; private set; }
    public int NetProfit => Revenue + OtherIncome - Expense;
    public int GoldAtOpen { get; private set; }

    public int Visited { get; private set; }
    public int Served { get; private set; }
    public int NotServed { get; private set; }
    public int ForcedLeave { get; private set; }
    public int PaymentCount { get; private set; }

    private float _firstSpawnTime = -1f;
    private float _firstRevenueSeconds = -1f;
    private float _seatWaitSum, _orderTakeSum, _foodWaitSum;
    private int _seatWaitSamples, _orderTakeSamples, _foodWaitSamples;

    /// <summary>마지막으로 진행(상태 변화·결제·퇴장)이 일어난 게임 시간. 마감 정지 감시에 쓴다.</summary>
    public float LastProgressTime { get; private set; }

    /// <summary>결제 1건마다 (손님, 금액, 오늘 첫 결제인지). 금액 팝업용.</summary>
    public event Action<CustomerAgent, int, bool> OnPayment;

    public void Begin(int goldAtOpen)
    {
        Stop();
        _sales.Clear();
        _saleOrder.Clear();
        _timelines.Clear();
        Revenue = OtherIncome = Expense = 0;
        Visited = Served = NotServed = ForcedLeave = PaymentCount = 0;
        _firstSpawnTime = _firstRevenueSeconds = -1f;
        _seatWaitSum = _orderTakeSum = _foodWaitSum = 0f;
        _seatWaitSamples = _orderTakeSamples = _foodWaitSamples = 0;
        GoldAtOpen = goldAtOpen;
        LastProgressTime = Time.time;

        CustomerAgent.OnAnySpawned += HandleSpawned;
        CustomerAgent.OnAnyStateChanged += HandleStateChanged;
        CustomerAgent.OnAnyPaid += HandlePaid;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnTransactionProcessed += HandleTransaction;
        IsRecording = true;
    }

    public void Stop()
    {
        if (!IsRecording)
            return;

        CustomerAgent.OnAnySpawned -= HandleSpawned;
        CustomerAgent.OnAnyStateChanged -= HandleStateChanged;
        CustomerAgent.OnAnyPaid -= HandlePaid;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnTransactionProcessed -= HandleTransaction;

        foreach (CustomerAgent customer in _timelines.Keys)
        {
            if (customer == null) continue;
            customer.OnOrderReceived -= HandleMenuChosen;
            customer.OnOrderTaken -= HandleOrderTaken;
            customer.OnExited -= HandleExited;
        }

        IsRecording = false;
    }

    public void MarkForcedLeave()
    {
        ForcedLeave++;
    }

    // ───── 이벤트 ─────

    private void HandleSpawned(CustomerAgent customer)
    {
        Visited++;
        Touch();
        if (_firstSpawnTime < 0f) _firstSpawnTime = Time.time;

        _timelines[customer] = new Timeline { spawned = Time.time };
        customer.OnOrderReceived += HandleMenuChosen;
        customer.OnOrderTaken += HandleOrderTaken;
        customer.OnExited += HandleExited;
    }

    private void HandleStateChanged(CustomerAgent customer, CustomerState state)
    {
        Touch();
        if (!_timelines.TryGetValue(customer, out Timeline line))
            return;

        // 실내 좌석에 앉아 주문 단계에 들어간 시점 = 자리 대기 종료
        if (state == CustomerState.WaitingForOrder && !line.seatedCounted)
        {
            line.seatedCounted = true;
            _seatWaitSum += Time.time - line.spawned;
            _seatWaitSamples++;
        }
        else if (state == CustomerState.Eating && !line.foodCounted && line.orderTaken >= 0f)
        {
            line.foodCounted = true;
            _foodWaitSum += Time.time - line.orderTaken;
            _foodWaitSamples++;
        }
    }

    private void HandleMenuChosen(CustomerAgent customer)
    {
        Touch();
        if (_timelines.TryGetValue(customer, out Timeline line) && line.menuChosen < 0f)
            line.menuChosen = Time.time;
    }

    private void HandleOrderTaken(CustomerAgent customer)
    {
        Touch();
        if (!_timelines.TryGetValue(customer, out Timeline line) || line.orderTaken >= 0f)
            return;

        line.orderTaken = Time.time;
        if (line.menuChosen >= 0f)
        {
            _orderTakeSum += line.orderTaken - line.menuChosen;
            _orderTakeSamples++;
        }
    }

    private void HandleExited(CustomerAgent customer)
    {
        Touch();
        customer.OnOrderReceived -= HandleMenuChosen;
        customer.OnOrderTaken -= HandleOrderTaken;
        customer.OnExited -= HandleExited;
        _timelines.Remove(customer);

        if (customer.WasServed) Served++;
        else NotServed++;
    }

    private void HandlePaid(CustomerAgent customer, RecipeData recipe, int amount)
    {
        Touch();
        bool first = PaymentCount == 0;
        PaymentCount++;
        Revenue += amount;

        if (first && _firstSpawnTime >= 0f)
            _firstRevenueSeconds = Time.time - _firstSpawnTime;

        if (recipe != null)
        {
            if (!_sales.TryGetValue(recipe.RecipeId, out DaySaleEntry entry))
            {
                entry = new DaySaleEntry
                {
                    recipeId = recipe.RecipeId,
                    recipeName = string.IsNullOrEmpty(recipe.RecipeName) ? "메뉴 " + recipe.RecipeId : recipe.RecipeName,
                    unitPrice = Mathf.RoundToInt(recipe.Price),
                };
                _sales[recipe.RecipeId] = entry;
                _saleOrder.Add(recipe.RecipeId);
            }

            entry.count++;
            entry.revenue += amount;
        }

        OnPayment?.Invoke(customer, amount, first);
    }

    private void HandleTransaction(CurrencyTransaction tx)
    {
        if (tx.Currency == null || tx.Currency.CurrencyID != GameSession.GoldCurrencyId)
            return;

        // 손님 결제는 HandlePaid에서 같은 금액으로 집계한다. (이중 집계 방지)
        if (tx.Source == TransactionSource.CustomerPayment)
            return;

        int amount = tx.FinalAmount;
        if (amount >= 0) OtherIncome += amount;
        else Expense += -amount;
    }

    private void Touch()
    {
        LastProgressTime = Time.time;
    }

    // ───── 결과 ─────

    public DaySettlementRecord BuildRecord(int day, int goldAtClose)
    {
        DaySettlementRecord record = new DaySettlementRecord
        {
            day = day,
            revenue = Revenue,
            otherIncome = OtherIncome,
            expense = Expense,
            netProfit = NetProfit,
            goldAtOpen = GoldAtOpen,
            goldAtClose = goldAtClose,
            visitedCount = Visited,
            servedCount = Served,
            notServedCount = NotServed,
            forcedLeaveCount = ForcedLeave,
            seatWaitSum = _seatWaitSum,
            seatWaitSamples = _seatWaitSamples,
            orderTakeSum = _orderTakeSum,
            orderTakeSamples = _orderTakeSamples,
            foodWaitSum = _foodWaitSum,
            foodWaitSamples = _foodWaitSamples,
            firstRevenueSeconds = _firstRevenueSeconds,
        };

        foreach (int id in _saleOrder)
        {
            DaySaleEntry entry = _sales[id];
            record.sales.Add(new DaySaleEntry
            {
                recipeId = entry.recipeId,
                recipeName = entry.recipeName,
                count = entry.count,
                unitPrice = entry.unitPrice,
                revenue = entry.revenue,
            });
        }

        return record;
    }
}
