using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집 UI "알바 스카우트"의 등급 카드 1개 (신입/숙련/베테랑/마스터).
/// 실제 스카우트 진행은 가챠 화면에서 하므로, 여기서는 요약만 보여주고 클릭 시 가챠 UI를 연다.
/// </summary>
public class HouseScoutCardUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;  // "신입"
    [SerializeField] private TMP_Text _costText;  // "100G"
    [SerializeField] private TMP_Text _gradeText; // "F~D" (선택)
    [SerializeField] private Button _button;

    private Action _onClick;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(ScoutData data, Action onClick)
    {
        _onClick = onClick;

        if (data == null)
            return;

        if (_iconImage != null)
        {
            _iconImage.sprite = data.ScoutIcon;
            _iconImage.enabled = data.ScoutIcon != null;
        }

        if (_nameText != null)
            _nameText.text = data.ScoutName;

        if (_costText != null)
            _costText.text = $"{data.RequiredCurrencyCount:N0}G";

        if (_gradeText != null)
            _gradeText.text = BuildGradeRange(data);
    }

    /// <summary>등급 분포의 처음~마지막 등급을 "F~D" 형태로 표기한다.</summary>
    private static string BuildGradeRange(ScoutData data)
    {
        if (data.GradeDistribution == null || data.GradeDistribution.Count == 0)
            return string.Empty;

        string min = null;
        string max = null;

        foreach (var pair in data.GradeDistribution)
        {
            if (pair.Key == null)
                continue;

            if (min == null)
                min = pair.Key.GradeName;

            max = pair.Key.GradeName;
        }

        if (string.IsNullOrEmpty(min))
            return string.Empty;

        return min == max ? min : $"{min}~{max}";
    }

    private void HandleClick()
    {
        _onClick?.Invoke();
    }
}
