using UnityEngine;

public class GotchaButton : MonoBehaviour
{
    [SerializeField] private ScoutData _scoutData;

    [SerializeField] private Currency _targetCurrency;

    public void OnClickButton()
    {
        if (CurrencyManager.Instance.GetCurrency(_targetCurrency) > _scoutData.RequiredCurrencyCount)
        {
            var tx = new CurrencyTransaction(_targetCurrency, _scoutData.RequiredCurrencyCount, TransactionSource.GotchaUse);
            CurrencyManager.Instance.ProcessTransaction(tx);
        }
    }
}
