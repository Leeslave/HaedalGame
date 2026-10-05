using System;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager : MonoBehaviour, ISaveParticipant
{
    public static CurrencyManager Instance { get; private set; }
    public Action<Currency, int> OnCurrencyChanged;
    public Action<CurrencyTransaction> OnTransactionProcessed;

    private Dictionary<Currency, int> _wallets = new Dictionary<Currency, int>();
    [SerializeField] private List<Currency> _allCurrencies;

    private void InitializeWallets()
    {
        foreach (var currency in _allCurrencies)
        {
            _wallets[currency] = 0;
        }
    }

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
            return;
        }

        InitializeWallets();
        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    public void ProcessTransaction(CurrencyTransaction tx)
    {
        tx = ApplyGlobalModifiers(tx);
        int finalAmount = tx.FinalAmount;

        if(!_wallets.ContainsKey(tx.Currency))
            _wallets[tx.Currency] = 0;

        _wallets[tx.Currency] = Mathf.Clamp(_wallets[tx.Currency] + finalAmount, tx.Currency.MinCapacity, tx.Currency.MaxCapacity);

        Debug.Log($"[{tx.Source}] {tx.Currency.CurrencyID} {finalAmount} 처리. (현재 잔액: {_wallets[tx.Currency]})");

        // UI 갱신 알림
        OnCurrencyChanged?.Invoke(tx.Currency, _wallets[tx.Currency]);
        OnTransactionProcessed?.Invoke(tx);
    }

    public int GetCurrency(Currency currency)
    {
        return _wallets.ContainsKey(currency) ? _wallets[currency] : -1;
    }

    /// <summary>재화 ID로 잔액을 조회한다. 없는 재화면 -1.</summary>
    public int GetCurrencyById(string currencyId)
    {
        Currency currency = FindCurrency(currencyId);
        return currency != null ? GetCurrency(currency) : -1;
    }

    // 전역 보정(배수 적용 등)
    private CurrencyTransaction ApplyGlobalModifiers(CurrencyTransaction tx)
    {
        return tx;
    }

    public void CaptureState(GameSaveData data)
    {
        data.wallets.Clear();
        foreach (KeyValuePair<Currency, int> pair in _wallets)
        {
            if (pair.Key == null) continue;
            data.wallets.Add(new WalletEntry { currencyId = pair.Key.CurrencyID, amount = pair.Value });
        }
    }

    public void RestoreState(GameSaveData data)
    {
        InitializeWallets();

        foreach (WalletEntry entry in data.wallets)
        {
            Currency currency = FindCurrency(entry.currencyId);
            if (currency == null)
            {
                Debug.LogWarning($"[CurrencyManager] 세이브의 재화 ID '{entry.currencyId}'가 _allCurrencies에 없습니다.");
                continue;
            }
            _wallets[currency] = entry.amount;
        }

        // 복원은 거래가 아니므로 OnTransactionProcessed(재무 집계)는 발행하지 않는다.
        foreach (KeyValuePair<Currency, int> pair in new List<KeyValuePair<Currency, int>>(_wallets))
            OnCurrencyChanged?.Invoke(pair.Key, pair.Value);
    }

    private Currency FindCurrency(string currencyId)
    {
        foreach (Currency currency in _allCurrencies)
        {
            if (currency != null && currency.CurrencyID == currencyId)
                return currency;
        }
        return null;
    }
}
