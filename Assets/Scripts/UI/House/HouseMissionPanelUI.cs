using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집 UI "오늘의 미션" 패널. 기획서 기준 읽기 전용(클릭 반응 없음).
/// 미션 진행 시스템이 아직 없어 진행도는 스텁 값을 사용한다 —
/// 시스템 연결 시 SetProgress()를 호출하도록 바꾸면 된다.
/// </summary>
public class HouseMissionPanelUI : MonoBehaviour
{
    [SerializeField] private DailyMissionSO _mission;

    [Header("UI")]
    [SerializeField] private TMP_Text _missionNameText;
    [SerializeField] private Image _progressFill;      // fillAmount
    [SerializeField] private TMP_Text _progressText;   // "8 / 20"
    [SerializeField] private TMP_Text _rewardText;     // "20"
    [SerializeField] private Image _rewardIcon;

    [Header("진행도 (스텁 — 시스템 연결 전 테스트용)")]
    [SerializeField] private int _currentProgress = 0;

    public void Refresh()
    {
        if (_mission == null)
            return;

        if (_missionNameText != null)
            _missionNameText.text = _mission.MissionName;

        int target = _mission.TargetCount;
        int current = Mathf.Clamp(_currentProgress, 0, target);

        if (_progressFill != null)
            _progressFill.fillAmount = (float)current / target;

        if (_progressText != null)
            _progressText.text = $"{current} / {target}";

        if (_rewardText != null)
            _rewardText.text = _mission.RewardAmount.ToString();

        if (_rewardIcon != null)
        {
            Sprite icon = _mission.RewardCurrency != null ? _mission.RewardCurrency.Icon : null;
            _rewardIcon.sprite = icon;
            _rewardIcon.enabled = icon != null;
        }
    }

    /// <summary>미션 진행 시스템이 생기면 이 메서드로 진행도를 갱신한다.</summary>
    public void SetProgress(int value)
    {
        _currentProgress = Mathf.Max(0, value);
        Refresh();
    }
}
