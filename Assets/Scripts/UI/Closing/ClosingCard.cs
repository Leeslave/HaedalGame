using TMPro;
using UnityEngine;

public class ClosingCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _headerTmp;
    [SerializeField] private TMP_Text _subTmp1;
    [SerializeField] private TMP_Text _subTmp2;
    [SerializeField] private TMP_Text _TotalTmp;

    [SerializeField] private string _headerText;
    [SerializeField] private string _sub1Text;
    [SerializeField] private string _sub2Text;
    [SerializeField] private string _totalText;
    [SerializeField] private string _unitText;



    [SerializeField] private int _sub1value;
    [SerializeField] private int _sub2value;
    [SerializeField] private int _totalvalue;


    private void OnEnable()
    {
     _headerTmp.text = _headerText;
     _subTmp1.text = _sub1Text +": " +_sub1value.ToString()+_unitText;
     _subTmp2.text = _sub2Text +": " +_sub2value.ToString()+_unitText;
     if (_unitText == "G")
     _subTmp2.text = _totalText + ": " + _sub2value.ToString() + _unitText;
    }

}
