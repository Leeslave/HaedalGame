using System;
using UnityEngine;

public class RestaurantLevelManager : MonoBehaviour
{
    public static RestaurantLevelManager Instance { get; private set; }

    private const string SaveKey = "RestaurantLevel";

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

        _currentLevel = PlayerPrefs.GetInt(SaveKey, _defaultLevel);
    }

    public void SetLevel(int level)
    {
        if (level == _currentLevel) return;
        _currentLevel = Mathf.Max(1, level);
        PlayerPrefs.SetInt(SaveKey, _currentLevel);
        PlayerPrefs.Save();
        OnLevelChanged?.Invoke(_currentLevel);
    }

    public void LevelUp()
    {
        SetLevel(_currentLevel + 1);
    }
}
