using System.Collections.Generic;
using UnityEngine;
using System;

public enum CurrencyType
{
    Soft, Item, Stat
}

[Serializable]
public struct CurrencyId
{
    public string key;
}

[Serializable]
public struct CurrencyAmount
{
    public CurrencyId id;
    public int amount;
}

[Serializable]
public class CurrencyBundle
{
    public List<CurrencyAmount> list = new List<CurrencyAmount>();
}

public enum CurrencyChangeReason
{
    None,
    CookDish,
    SellDish,
    UpgradeKitchen,
    StoryReward,
    Debug
}

/*
    위의 선언들은 나중에 다른 코드로 빼낼 예정
    구현을 먼저 하기위한 상황
*/

//=============================================================
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private List<CurrencyConfig> currencyConfigs; // 재화 목록들
    private Dictionary<string, CurrencyConfig> configById = new();
    private Dictionary<string, int> balances = new();

    public event Action<CurrencyId, int, int, CurrencyChangeReason> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        // InitConfigDict();
        // InitBalances();
    }

    void InitConfigDict()
    {
        foreach (var cfg in currencyConfigs) { configById[cfg.id.key] = cfg; }
    }

    void InitBalance()
    {
        foreach (var cfg in currencyConfigs) { balances[cfg.id.key] = cfg.defaultValue; }
    }


}
