using System;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼이 가리킬 대상(UI 버튼 / 월드 오브젝트)을 문자열 키로 찾을 수 있게 모아두는 전역 레지스트리.
/// 튜토리얼 데이터(SO)가 특정 씬의 오브젝트를 직접 참조하지 않아도 되므로,
/// 씬이 바뀌거나 프리팹이 새로 생성돼도 키만 같으면 그대로 동작한다.
/// </summary>
public static class TutorialTargetRegistry
{
    // 같은 키를 가진 대상이 여러 개일 수 있어(리스트 슬롯 등) 리스트로 보관하고, 가장 마지막에 켜진 것을 우선한다.
    private static readonly Dictionary<string, List<TutorialTarget>> _targets = new Dictionary<string, List<TutorialTarget>>();

    /// <summary>새 대상이 등록될 때 알린다. (튜토리얼이 대상 등장을 기다릴 때 사용)</summary>
    public static event Action<TutorialTarget> OnTargetRegistered;

    public static void Register(TutorialTarget target)
    {
        if (target == null || string.IsNullOrEmpty(target.Key))
            return;

        if (!_targets.TryGetValue(target.Key, out List<TutorialTarget> list))
        {
            list = new List<TutorialTarget>();
            _targets.Add(target.Key, list);
        }

        if (list.Contains(target))
            return;

        list.Add(target);
        OnTargetRegistered?.Invoke(target);
    }

    public static void Unregister(TutorialTarget target)
    {
        if (target == null || string.IsNullOrEmpty(target.Key))
            return;

        if (!_targets.TryGetValue(target.Key, out List<TutorialTarget> list))
            return;

        list.Remove(target);

        if (list.Count == 0)
            _targets.Remove(target.Key);
    }

    /// <summary>키에 해당하는, 현재 화면에 살아있는 대상을 찾는다. 없으면 null.</summary>
    public static TutorialTarget Find(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (!_targets.TryGetValue(key, out List<TutorialTarget> list))
            return null;

        // 뒤에서부터 = 가장 나중에 켜진 대상 우선 (팝업 위에 팝업이 뜬 경우 등)
        for (int i = list.Count - 1; i >= 0; i--)
        {
            TutorialTarget target = list[i];

            if (target == null)
            {
                list.RemoveAt(i);
                continue;
            }

            if (target.isActiveAndEnabled)
                return target;
        }

        return null;
    }

    public static bool Contains(string key)
    {
        return Find(key) != null;
    }

    /// <summary>[디버그용] 현재 화면에 등록되어 있는 키 목록.</summary>
    public static List<string> GetActiveKeys()
    {
        List<string> keys = new List<string>();

        foreach (KeyValuePair<string, List<TutorialTarget>> pair in _targets)
        {
            if (Find(pair.Key) != null)
                keys.Add(pair.Key);
        }

        keys.Sort();
        return keys;
    }

    /// <summary>도메인 리로드 없이 플레이 모드를 반복할 때를 대비한 초기화.</summary>
    public static void Clear()
    {
        _targets.Clear();
        OnTargetRegistered = null;
    }
}
