using System;
using UnityEngine;

#region Currency SO
[CreateAssetMenu(fileName = "Currency", menuName = "Game Data/Currency/Currency")]
public class Currency : ScriptableObject
{
    [Header("�ĺ� �� �⺻ ����")]
    [Tooltip("Key�� ����� ID")]
    public string CurrencyID;
    [Tooltip("���� ȭ�鿡 ǥ�õ� ���� �̸�")]
    public string DisplayName;
    [TextArea]
    public string Description; // �����̳� �κ��丮���� ������ ����

    [Header("�ð� ��� (UI)")]
    [Tooltip("UI�� ǥ�õ� ������")]
    public Sprite Icon;

    [Tooltip("�ִ� ���� �ѵ�")]
    public int MaxCapacity = Int32.MaxValue;

    [Tooltip("�ּ� ���� �ѵ�")]
    public int MinCapacity = Int32.MinValue;
}
#endregion 

#region CurrencyTransaction
/// <summary>
/// ��ȭ�� ȹ��/�Һ� ��ó
/// </summary>
public enum TransactionSource{TestGet, TestUse, GotchaUse, ShopPurchase, BlacksmithUpgrade}
public struct CurrencyTransaction
{
    [Header("Data")]
    public Currency Currency;
    public int Amount;              
    public TransactionSource Source;  
    public float Multiplier;


    public CurrencyTransaction(Currency currency, int amount, TransactionSource source, float multiplier = 1f)
    {
        this.Currency = currency;
        this.Amount = amount;
        this.Source = source;
        this.Multiplier = multiplier;
    }

    // ���� ��� �ݾ� (�⺻�ݾ� * ����)
    public int FinalAmount => Mathf.RoundToInt(Amount * Multiplier);
}
#endregion