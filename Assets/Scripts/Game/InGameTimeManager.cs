using System;
using UnityEngine;

/// <summary>
/// 현재 일차 보관. 값의 기준은 GameSession이며 파일 저장은 체크포인트에서만 한다.
/// 날짜 증가는 하루 종료 처리(잠자기)만 수행해야 한다.
/// </summary>
public class InGameTimeManager : MonoBehaviour, ISaveParticipant
{
    public static InGameTimeManager Instance { get; private set; }

    private int _currentDay = 1;
    public int CurrentDay => _currentDay;

    public Action<int> OnDayAdvanced;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    // 추후 실제 게임 시간 시스템으로 교체 예정
    public void AdvanceDay()
    {
        _currentDay++;
        OnDayAdvanced?.Invoke(_currentDay);
    }

    public void CaptureState(GameSaveData data)
    {
        data.day = _currentDay;
    }

    public void RestoreState(GameSaveData data)
    {
        _currentDay = Mathf.Max(1, data.day);
    }
}
