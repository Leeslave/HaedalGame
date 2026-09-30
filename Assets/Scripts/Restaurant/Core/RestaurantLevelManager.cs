using System;
using UnityEngine;

public class RestaurantLevelManager : MonoBehaviour, ISaveParticipant
{
    public static RestaurantLevelManager Instance { get; private set; }

    [SerializeField] private int _defaultLevel = 1;

    private int _currentLevel;

    public int CurrentLevel => _currentLevel;

    public Action<int> OnLevelChanged;

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

        _currentLevel = _defaultLevel;
        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    public void CaptureState(GameSaveData data)
    {
        data.restaurantLevel = _currentLevel;
    }

    public void RestoreState(GameSaveData data)
    {
        _currentLevel = Mathf.Max(1, data.restaurantLevel);
        OnLevelChanged?.Invoke(_currentLevel);
    }

    public void SetLevel(int level)
    {
        if (level == _currentLevel) return;
        _currentLevel = Mathf.Max(1, level);
        OnLevelChanged?.Invoke(_currentLevel);
    }

    public void LevelUp()
    {
        SetLevel(_currentLevel + 1);
    }
}
