using TMPro;
using UnityEngine;

/// <summary>
/// 강화 조건 체크리스트의 한 줄. 라벨 / 수치 / 충족(✓·✗) 표시.
/// 미충족이면 수치를 붉은색으로 표시한다 (목업 기준).
/// </summary>
public class UpgradeConditionRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private GameObject _metMark;    // ✓
    [SerializeField] private GameObject _unmetMark;  // ✗

    [SerializeField] private Color _metColor = Color.black;
    [SerializeField] private Color _unmetColor = Color.red;

    public void Bind(UpgradeConditionResult result)
    {
        if (_labelText != null)
            _labelText.text = result.Label;

        if (_valueText != null)
        {
            _valueText.text = result.ValueText;
            _valueText.color = result.Met ? _metColor : _unmetColor;
        }

        if (_metMark != null)
            _metMark.SetActive(result.Met);

        if (_unmetMark != null)
            _unmetMark.SetActive(!result.Met);
    }
}
