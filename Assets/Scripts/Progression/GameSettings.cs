using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 환경 설정(소리·화면) 값의 저장과 적용. 세이브 파일과 별개로 PlayerPrefs에 저장한다.
///  - 전체 음량·음소거는 AudioListener.volume으로 모든 소리에 적용된다.
///  - 배경음악·효과음·대화 음량은 채널별 배율이다. 소리를 내는 쪽(AudioSource)에서 BgmVolume 등을 곱해 쓴다.
///    (전체 음량은 AudioListener가 이미 곱하므로 채널 값에는 포함하지 않는다)
/// </summary>
public static class GameSettings
{
    [Serializable]
    public struct Values
    {
        public float master;
        public float bgm;
        public float sfx;
        public float voice;
        public bool muted;
        public bool fullscreen;
        public int width;       // 0이면 해상도를 바꾸지 않는다
        public int height;
        public int quality;
    }

    private const string MasterKey = "MasterVolume";
    private const string BgmKey = "BgmVolume";
    private const string SfxKey = "SfxVolume";
    private const string VoiceKey = "VoiceVolume";
    private const string MutedKey = "Muted";
    private const string FullscreenKey = "Fullscreen";
    private const string WidthKey = "ScreenWidth";
    private const string HeightKey = "ScreenHeight";
    private const string QualityKey = "QualityLevel";

    private static readonly Vector2Int[] WideResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
    };

    private static Values _current;
    private static bool _loaded;

    /// <summary>적용·저장된 설정이 바뀌면 호출된다.</summary>
    public static event Action OnChanged;

    public static Values Current
    {
        get
        {
            if (!_loaded) { _current = Load(); _loaded = true; }
            return _current;
        }
    }

    public static float BgmVolume => Current.muted ? 0f : Current.bgm;
    public static float SfxVolume => Current.muted ? 0f : Current.sfx;
    public static float VoiceVolume => Current.muted ? 0f : Current.voice;

    public static Values Defaults()
    {
        // 기본 해상도: 모니터가 허용하는 1920x1080 이하 중 가장 큰 16:9
        List<Vector2Int> sizes = ResolutionOptions();
        Vector2Int size = sizes[0];
        foreach (Vector2Int candidate in sizes)
        {
            if (candidate.x <= 1920) size = candidate;
        }

        return new Values
        {
            master = 1f,
            bgm = 0.8f,
            sfx = 0.8f,
            voice = 0.8f,
            muted = false,
            fullscreen = true,
            width = size.x,
            height = size.y,
            quality = QualitySettings.GetQualityLevel(),
        };
    }

    private static Values Load()
    {
        return new Values
        {
            master = PlayerPrefs.GetFloat(MasterKey, 1f),
            bgm = PlayerPrefs.GetFloat(BgmKey, 0.8f),
            sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f),
            voice = PlayerPrefs.GetFloat(VoiceKey, 0.8f),
            muted = PlayerPrefs.GetInt(MutedKey, 0) != 0,
            fullscreen = PlayerPrefs.HasKey(FullscreenKey) ? PlayerPrefs.GetInt(FullscreenKey) != 0 : Screen.fullScreen,
            width = PlayerPrefs.GetInt(WidthKey, 0),
            height = PlayerPrefs.GetInt(HeightKey, 0),
            quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
        };
    }

    /// <summary>씬 시작 시 저장된 설정을 적용한다.</summary>
    public static void ApplySaved()
    {
        Apply(Current, false);
    }

    /// <summary>소리만 미리 적용한다. (설정 창에서 슬라이더를 움직이는 동안)</summary>
    public static void PreviewAudio(Values values)
    {
        AudioListener.volume = values.muted ? 0f : Mathf.Clamp01(values.master);
    }

    public static void Apply(Values values, bool save)
    {
        values.master = Mathf.Clamp01(values.master);
        values.bgm = Mathf.Clamp01(values.bgm);
        values.sfx = Mathf.Clamp01(values.sfx);
        values.voice = Mathf.Clamp01(values.voice);
        values.quality = Mathf.Clamp(values.quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        PreviewAudio(values);

        if (values.width > 0 && values.height > 0)
        {
            if (Screen.width != values.width || Screen.height != values.height || Screen.fullScreen != values.fullscreen)
                Screen.SetResolution(values.width, values.height, values.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        else if (Screen.fullScreen != values.fullscreen)
        {
            Screen.fullScreen = values.fullscreen;
        }

        if (QualitySettings.names.Length > 0 && QualitySettings.GetQualityLevel() != values.quality)
            QualitySettings.SetQualityLevel(values.quality, true);

        _current = values;
        _loaded = true;

        if (save)
        {
            PlayerPrefs.SetFloat(MasterKey, values.master);
            PlayerPrefs.SetFloat(BgmKey, values.bgm);
            PlayerPrefs.SetFloat(SfxKey, values.sfx);
            PlayerPrefs.SetFloat(VoiceKey, values.voice);
            PlayerPrefs.SetInt(MutedKey, values.muted ? 1 : 0);
            PlayerPrefs.SetInt(FullscreenKey, values.fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, values.width);
            PlayerPrefs.SetInt(HeightKey, values.height);
            PlayerPrefs.SetInt(QualityKey, values.quality);
            PlayerPrefs.Save();
        }

        OnChanged?.Invoke();
    }

    /// <summary>선택 가능한 16:9 해상도. 모니터보다 큰 해상도는 뺀다.</summary>
    public static List<Vector2Int> ResolutionOptions()
    {
        Resolution display = Screen.currentResolution;
        List<Vector2Int> options = new List<Vector2Int>();
        foreach (Vector2Int size in WideResolutions)
        {
            if (display.width <= 0 || (size.x <= display.width && size.y <= display.height))
                options.Add(size);
        }
        if (options.Count == 0)
            options.Add(new Vector2Int(Screen.width, Screen.height));
        return options;
    }

    /// <summary>그래픽 품질 선택지 (표시 이름, 품질 레벨). 프로젝트의 Low/Medium/High를 낮음/보통/높음으로 보여 준다.</summary>
    public static List<KeyValuePair<string, int>> QualityOptions()
    {
        string[] names = QualitySettings.names;
        List<KeyValuePair<string, int>> options = new List<KeyValuePair<string, int>>();
        AddQuality(options, names, "Low", "낮음");
        AddQuality(options, names, "Medium", "보통");
        AddQuality(options, names, "High", "높음");
        if (options.Count < 3)
        {
            options.Clear();
            for (int i = 0; i < names.Length; i++)
                options.Add(new KeyValuePair<string, int>(names[i], i));
        }
        return options;
    }

    private static void AddQuality(List<KeyValuePair<string, int>> options, string[] names, string name, string label)
    {
        int index = Array.IndexOf(names, name);
        if (index >= 0)
            options.Add(new KeyValuePair<string, int>(label, index));
    }
}
