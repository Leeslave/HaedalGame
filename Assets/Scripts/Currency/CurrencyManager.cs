using System;
using System.Collections.Generic;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
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
        else Destroy(gameObject);

        InitializeWallets();
    }


    public void ProcessTransaction(CurrencyTransaction tx)
    {
        tx = ApplyGlobalModifiers(tx);
        int finalAmount = tx.FinalAmount;

        if(!_wallets.ContainsKey(tx.Currency))
            _wallets[tx.Currency] = 0;

        _wallets[tx.Currency] += finalAmount;
        Mathf.Clamp(_wallets[tx.Currency], tx.Currency.MinCapacity, tx.Currency.MaxCapacity);

        Debug.Log($"[{tx.Source}] {tx.Currency.CurrencyID} {finalAmount} ������. (���� �ܾ�: {_wallets[tx.Currency]})");

        // UI ������Ʈ ���
        OnCurrencyChanged?.Invoke(tx.Currency, _wallets[tx.Currency]);
        OnTransactionProcessed?.Invoke(tx);
    }

    public int GetCurrency(Currency currency)
    {
        return _wallets.ContainsKey(currency) ? _wallets[currency] : -1;
    }

    // ����(���ϱ� ����)
    private CurrencyTransaction ApplyGlobalModifiers(CurrencyTransaction tx)
    {
        return tx;
    }
}
