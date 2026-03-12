using UnityEngine;

public class ScoutButton : MonoBehaviour
{
    [SerializeField] private ScoutData _scoutData;

    [SerializeField] private Currency _targetCurrency;
    
    public void OnClickButton()
    {
        if (CurrencyManager.Instance.GetCurrency(_targetCurrency) > _scoutData.RequiredCurrencyCount)
        {
            var tx = new CurrencyTransaction(_targetCurrency, _scoutData.RequiredCurrencyCount * -1, TransactionSource.GotchaUse);
            CurrencyManager.Instance.ProcessTransaction(tx);
            ScoutManager.Instance.Scout(_scoutData);
        }
    }

  
}
