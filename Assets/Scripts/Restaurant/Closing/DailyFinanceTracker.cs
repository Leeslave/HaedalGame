using UnityEngine;

// 오늘 하루치 수입/지출 집계. CurrencyManager를 거치는 모든 트랜잭션을 부호로 구분해 누적한다.
public class DailyFinanceTracker : MonoBehaviour
{
    public static DailyFinanceTracker Instance { get; private set; }

    private int _totalIncome = 0;
    private int _totalExpense = 0;

    public int TotalIncome => _totalIncome;
    public int TotalExpense => _totalExpense;
    public int NetProfit => _totalIncome - _totalExpense;

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

    private void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnTransactionProcessed -= HandleTransaction;
            CurrencyManager.Instance.OnTransactionProcessed += HandleTransaction;
        }
    }

    private void HandleTransaction(CurrencyTransaction tx)
    {
        int amount = tx.FinalAmount;
        if (amount >= 0) { _totalIncome += amount; }
        else { _totalExpense += -amount; }
    }

    public void ResetDay()
    {
        _totalIncome = 0;
        _totalExpense = 0;
    }
}
