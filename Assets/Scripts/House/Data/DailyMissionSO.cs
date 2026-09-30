using UnityEngine;

/// <summary>
/// 오늘의 미션 데이터 (집 UI 표시용).
/// 미션 진행 시스템이 아직 없으므로 진행도는 HouseMissionPanelUI의 스텁 값을 사용한다.
/// 실제 시스템 연결 시 진행도만 그쪽에서 읽도록 교체하면 된다.
/// </summary>
[CreateAssetMenu(fileName = "DailyMissionSO", menuName = "Game Data/House/Daily Mission")]
public class DailyMissionSO : ScriptableObject
{
    [SerializeField] private string _missionName = "해물 파스타를 20번 판매하기";
    [SerializeField] private int _targetCount = 20;

    [Header("보상")]
    [SerializeField] private Currency _rewardCurrency;
    [SerializeField] private int _rewardAmount = 20;

    public string MissionName => _missionName;
    public int TargetCount => Mathf.Max(1, _targetCount);
    public Currency RewardCurrency => _rewardCurrency;
    public int RewardAmount => _rewardAmount;
}
