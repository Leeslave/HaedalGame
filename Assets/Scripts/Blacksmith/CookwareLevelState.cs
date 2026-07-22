using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조리 도구별 현재 레벨과 사용 요리 횟수를 보관한다.
/// RestaurantLevelManager와 동일하게 PlayerPrefs로 저장한다 (키: 도구 에셋 이름).
/// 사용 횟수는 식당에서 요리할 때 오르는 값 — 식당 조리 시스템이 생기면 AddUseCount()를 호출해 연결한다.
/// (미니게임과는 무관. 현재는 연결처 없음)
/// </summary>
public class CookwareLevelState : MonoBehaviour
{
    public static CookwareLevelState Instance { get; private set; }

    private const string LevelKeyPrefix = "CookwareLevel_";
    private const string UseCountKeyPrefix = "CookwareUseCount_";

    private readonly Dictionary<string, int> _levelCache = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _useCountCache = new Dictionary<string, int>();

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
        }
    }

    public int GetLevel(CookwareUpgradeSO tool)
    {
        if (tool == null)
            return 1;

        string key = tool.name;

        if (!_levelCache.TryGetValue(key, out int level))
        {
            level = PlayerPrefs.GetInt(LevelKeyPrefix + key, 1);
            _levelCache[key] = level;
        }

        return level;
    }

    public void SetLevel(CookwareUpgradeSO tool, int level)
    {
        if (tool == null)
            return;

        level = Mathf.Max(1, level);

        string key = tool.name;
        _levelCache[key] = level;

        PlayerPrefs.SetInt(LevelKeyPrefix + key, level);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    public void LevelUp(CookwareUpgradeSO tool)
    {
        SetLevel(tool, GetLevel(tool) + 1);
    }

    public int GetUseCount(CookwareUpgradeSO tool)
    {
        if (tool == null)
            return 0;

        string key = tool.name;

        if (!_useCountCache.TryGetValue(key, out int count))
        {
            count = PlayerPrefs.GetInt(UseCountKeyPrefix + key, 0);
            _useCountCache[key] = count;
        }

        return count;
    }

    public void AddUseCount(CookwareUpgradeSO tool, int amount = 1)
    {
        if (tool == null || amount <= 0)
            return;

        string key = tool.name;
        int count = GetUseCount(tool) + amount;

        _useCountCache[key] = count;

        PlayerPrefs.SetInt(UseCountKeyPrefix + key, count);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    /// <summary>강화 비용으로 사용횟수를 차감한다 (골드처럼 소모 자원으로 취급).</summary>
    public void ConsumeUseCount(CookwareUpgradeSO tool, int amount)
    {
        if (tool == null || amount <= 0)
            return;

        string key = tool.name;
        int count = Mathf.Max(0, GetUseCount(tool) - amount);

        _useCountCache[key] = count;

        PlayerPrefs.SetInt(UseCountKeyPrefix + key, count);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    /// <summary>테스트용: 모든 캐시/저장을 초기화한다. (PlayerPrefs 키는 도구별이라 개별 삭제)</summary>
    public void ResetAll(IEnumerable<CookwareUpgradeSO> tools)
    {
        if (tools != null)
        {
            foreach (CookwareUpgradeSO tool in tools)
            {
                if (tool == null)
                    continue;

                PlayerPrefs.DeleteKey(LevelKeyPrefix + tool.name);
                PlayerPrefs.DeleteKey(UseCountKeyPrefix + tool.name);
            }
        }

        _levelCache.Clear();
        _useCountCache.Clear();
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }
}
