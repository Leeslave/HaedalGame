using System;
using UnityEngine;

/// <summary>
/// 인게임 시간 시스템 플레이스홀더.
/// 실제 게임 시간 흐름 시스템 구현 전까지 사용.
/// AdvanceDay()를 호출하면 하루가 지나며 OnDayAdvanced 이벤트 발생.
/// </summary>
public class InGameTimeManager : MonoBehaviour
{
    public static InGameTimeManager Instance { get; private set; }

    private const string SaveKey = "InGameDay";

    private int _currentDay;
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

        _currentDay = PlayerPrefs.GetInt(SaveKey, 0);
    }

    // 추후 실제 게임 시간 시스템으로 교체 예정
    public void AdvanceDay()
    {
        _currentDay++;
        PlayerPrefs.SetInt(SaveKey, _currentDay);
        PlayerPrefs.Save();
        OnDayAdvanced?.Invoke(_currentDay);
    }
}
