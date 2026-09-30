using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>인스펙터에서 키와 스프라이트를 짝지어 두기 위한 한 줄.</summary>
[Serializable]
public class DialogueSpriteEntry
{
    [Tooltip("JSON의 background나 portrait에 적을 키.")]
    public string key = "";

    [Tooltip("그 키가 가리킬 실제 스프라이트.")]
    public Sprite sprite;
}

/// <summary>
/// JSON에 적힌 이미지 키를 실제 스프라이트로 바꿔준다.
/// 찾는 순서는 (1) 등록해둔 표 (2) Resources 경로 (3) 스프라이트 시트 안의 낱장 이름 순이고,
/// 끝까지 못 찾으면 null을 돌려줘서 배경은 기본 스타일로, 초상화는 빈 채로 넘어간다.
/// </summary>
public class DialogueAssetResolver
{
    // Resources를 매 줄마다 뒤지지 않도록 찾은 결과를 기억해둔다. 못 찾은 키도 null로 기억한다.
    private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> _registered = new Dictionary<string, Sprite>();

    // Resources 아래에서 이미지를 찾을 때 앞에 붙일 기본 폴더.
    private readonly List<string> _resourceFolders = new List<string>();

    public DialogueAssetResolver(IEnumerable<string> resourceFolders = null)
    {
        if (resourceFolders != null)
        {
            foreach (string folder in resourceFolders)
            {
                if (!string.IsNullOrWhiteSpace(folder))
                    _resourceFolders.Add(folder.Trim().TrimEnd('/'));
            }
        }

        // 기본 폴더가 하나도 없으면 Resources 최상단에서 그대로 찾는다.
        if (_resourceFolders.Count == 0)
            _resourceFolders.Add("");
    }

    /// <summary>키와 스프라이트를 직접 등록한다. Resources보다 먼저 검사한다.</summary>
    public void Register(string key, Sprite sprite)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _registered[key.Trim()] = sprite;
        _cache.Remove(key.Trim());
    }

    /// <summary>인스펙터에 적어둔 목록을 한 번에 등록한다.</summary>
    public void RegisterAll(IEnumerable<DialogueSpriteEntry> entries)
    {
        if (entries == null)
            return;

        foreach (DialogueSpriteEntry entry in entries)
        {
            if (entry != null)
                Register(entry.key, entry.sprite);
        }
    }

    /// <summary>키에 해당하는 스프라이트를 찾는다. 못 찾으면 null.</summary>
    public Sprite Resolve(string key) => Resolve(key, true);

    /// <param name="logMissing">못 찾았을 때 경고를 남길지. 검사 도구는 따로 보고하므로 꺼서 쓴다.</param>
    public Sprite Resolve(string key, bool logMissing)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string trimmed = key.Trim();

        if (_cache.TryGetValue(trimmed, out Sprite cached))
            return cached;

        Sprite found = Find(trimmed);
        _cache[trimmed] = found;

        if (found == null && logMissing)
            Debug.LogWarning($"[Dialogue] 이미지 '{trimmed}'를 찾지 못했다. 등록 목록이나 Resources 경로를 확인한다.");

        return found;
    }

    private Sprite Find(string key)
    {
        if (_registered.TryGetValue(key, out Sprite registered) && registered != null)
            return registered;

        for (int i = 0; i < _resourceFolders.Count; i++)
        {
            string folder = _resourceFolders[i];
            string path = string.IsNullOrEmpty(folder) ? key : folder + "/" + key;

            Sprite direct = Resources.Load<Sprite>(path);

            if (direct != null)
                return direct;

            // 스프라이트 시트로 잘라 쓴 경우엔 파일이 아니라 낱장 이름으로 적었을 수 있다.
            Sprite fromSheet = FindInSheet(path, key);

            if (fromSheet != null)
                return fromSheet;
        }

        return null;
    }

    /// <summary>"폴더/시트이름/낱장이름" 형태로 적힌 키를 시트 안에서 찾아본다.</summary>
    private static Sprite FindInSheet(string path, string key)
    {
        int separator = path.LastIndexOf('/');

        if (separator <= 0)
            return null;

        string sheetPath = path.Substring(0, separator);
        string spriteName = path.Substring(separator + 1);

        Sprite[] sheet = Resources.LoadAll<Sprite>(sheetPath);

        if (sheet == null || sheet.Length == 0)
            return null;

        for (int i = 0; i < sheet.Length; i++)
        {
            if (sheet[i] != null && sheet[i].name == spriteName)
                return sheet[i];
        }

        return null;
    }

    /// <summary>기억해둔 결과를 지운다. 실행 중에 이미지를 갈아끼울 때 쓴다.</summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
