using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoutButton : MonoBehaviour
{
    [SerializeField] private ScoutData _scoutData;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private Currency _targetCurrency;

    [SerializeField] public Action OnClick;
    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        string minGrade = _scoutData.GradeDistribution.Keys.First().GradeName;
        string maxGrade = _scoutData.GradeDistribution.Keys.Last().GradeName;

        _iconImage.sprite = _scoutData.ScoutIcon;
        _typeText.text = _scoutData.ScoutName;
        _goldText.text = _scoutData.RequiredCurrencyCount.ToString();
        _gradeText.text = minGrade + "~" + maxGrade;
    }

    public void OnClickButton()
    {
        if (CurrencyManager.Instance.GetCurrency(_targetCurrency) > _scoutData.RequiredCurrencyCount)
        {
            var tx = new CurrencyTransaction(_targetCurrency, _scoutData.RequiredCurrencyCount * -1, TransactionSource.GotchaUse);
            CurrencyManager.Instance.ProcessTransaction(tx);
            if (ScoutManager.Instance != null)
            {
                ScoutManager.Instance.ChangeSection();
                ScoutManager.Instance.Scout(_scoutData);
            }
            
        }
    }

  
}
