using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조리 도구별 현재 레벨과 사용 요리 횟수를 보관한다. (키: 도구 에셋 이름)
/// 저장은 통합 세이브(GameSession) 체크포인트에서 한다.
/// 사용 횟수는 식당에서 요리할 때 오르는 값 — 식당 조리 시스템이 생기면 AddUseCount()를 호출해 연결한다.
/// (미니게임과는 무관. 현재는 연결처 없음)
/// </summary>
public class CookwareLevelState : MonoBehaviour, ISaveParticipant
{
    public static CookwareLevelState Instance { get; private set; }

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
            return;
        }

        GameSession.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            GameSession.Unregister(this);
    }

    public void CaptureState(GameSaveData data)
    {
        data.cookware.Clear();

        HashSet<string> keys = new HashSet<string>(_levelCache.Keys);
        keys.UnionWith(_useCountCache.Keys);

        foreach (string key in keys)
        {
            _levelCache.TryGetValue(key, out int level);
            _useCountCache.TryGetValue(key, out int useCount);
            data.cookware.Add(new CookwareEntry { toolId = key, level = Mathf.Max(1, level), useCount = useCount });
        }
    }

    public void RestoreState(GameSaveData data)
    {
        _levelCache.Clear();
        _useCountCache.Clear();

        foreach (CookwareEntry entry in data.cookware)
        {
            if (entry == null || string.IsNullOrEmpty(entry.toolId))
                continue;

            _levelCache[entry.toolId] = Mathf.Max(1, entry.level);
            _useCountCache[entry.toolId] = Mathf.Max(0, entry.useCount);
        }

        OnChanged?.Invoke();
    }

    public int GetLevel(CookwareUpgradeSO tool)
    {
        if (tool == null)
            return 1;

        return _levelCache.TryGetValue(tool.name, out int level) ? level : 1;
    }

    public void SetLevel(CookwareUpgradeSO tool, int level)
    {
        if (tool == null)
            return;

        _levelCache[tool.name] = Mathf.Max(1, level);
        PushToSession();

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

        return _useCountCache.TryGetValue(tool.name, out int count) ? count : 0;
    }

    public void AddUseCount(CookwareUpgradeSO tool, int amount = 1)
    {
        if (tool == null || amount <= 0)
            return;

        _useCountCache[tool.name] = GetUseCount(tool) + amount;
        PushToSession();

        OnChanged?.Invoke();
    }

    /// <summary>강화 비용으로 사용횟수를 차감한다 (골드처럼 소모 자원으로 취급).</summary>
    public void ConsumeUseCount(CookwareUpgradeSO tool, int amount)
    {
        if (tool == null || amount <= 0)
            return;

        _useCountCache[tool.name] = Mathf.Max(0, GetUseCount(tool) - amount);
        PushToSession();

        OnChanged?.Invoke();
    }

    /// <summary>테스트용: 모든 도구 레벨과 사용 횟수를 초기화한다.</summary>
    public void ResetAll(IEnumerable<CookwareUpgradeSO> tools)
    {
        _levelCache.Clear();
        _useCountCache.Clear();
        PushToSession();

        OnChanged?.Invoke();
    }

    // 세션에 즉시 반영한다. 파일 저장은 체크포인트에서만 한다.
    private void PushToSession()
    {
        if (GameSession.IsActive)
            CaptureState(GameSession.Current);
    }
}
