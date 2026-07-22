using System;
using UnityEngine;

/// <summary>
/// 대장간 자체 레벨 (강화 조건 검사 + 하단 바 표시용).
/// 경험치가 가득 차면 자동 레벨업. RestaurantLevelManager와 같은 PlayerPrefs 저장 방식.
/// 경험치 획득처(강화 성공 등)는 추후 연결.
/// </summary>
public class BlacksmithLevelManager : MonoBehaviour
{
    public static BlacksmithLevelManager Instance { get; private set; }

    private const string LevelKey = "BlacksmithLevel";
    private const string ExpKey = "BlacksmithExp";

    [SerializeField] private int _defaultLevel = 1;
    [SerializeField] private int _expPerLevel = 100; // 레벨당 필요 경험치 (예: 30/100)

    private int _currentLevel;
    private int _currentExp;

    public int CurrentLevel => _currentLevel;
    public int CurrentExp => _currentExp;
    public int ExpPerLevel => _expPerLevel;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _currentLevel = PlayerPrefs.GetInt(LevelKey, _defaultLevel);
        _currentExp = PlayerPrefs.GetInt(ExpKey, 0);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        _currentExp += amount;

        while (_currentExp >= _expPerLevel)
        {
            _currentExp -= _expPerLevel;
            _currentLevel++;
        }

        Save();
        OnChanged?.Invoke();
    }

    public void SetLevel(int level)
    {
        _currentLevel = Mathf.Max(1, level);
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>레벨과 경험치를 기본값으로 초기화한다 (테스트용).</summary>
    public void ResetToDefault()
    {
        _currentLevel = _defaultLevel;
        _currentExp = 0;
        Save();
        OnChanged?.Invoke();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(LevelKey, _currentLevel);
        PlayerPrefs.SetInt(ExpKey, _currentExp);
        PlayerPrefs.Save();
    }
}
