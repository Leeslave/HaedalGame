using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 환경 설정 창. 타이틀의 [설정] 버튼과 관리 창의 설정 탭에서 같은 창을 연다.
///  - 소리: 전체 음량, 배경음악, 효과음, 대화 음량, 전체 음소거
///  - 화면: 화면 모드(전체 화면/창 모드), 해상도(16:9), 그래픽 품질(낮음/보통/높음)
/// 음량은 움직이는 즉시 미리 들려주고, [적용]을 눌러야 저장된다. [취소]는 창을 열기 전 값으로 되돌린다.
/// </summary>
public class SettingsWindow : MonoBehaviour
{
    private static readonly Color Frame = new Color32(0x8A, 0x55, 0x30, 0xFF);
    private static readonly Color FrameDark = new Color32(0x5E, 0x3C, 0x27, 0xFF);
    private static readonly Color Paper = new Color32(0xFB, 0xF1, 0xDC, 0xFF);

    private static SettingsWindow _instance;

    private RectTransform _root;
    private RectTransform _soundPage, _screenPage;
    private Button _soundTab, _screenTab;
    private readonly List<Slider> _sliders = new List<Slider>();
    private readonly List<TMP_Text> _sliderValues = new List<TMP_Text>();
    private Button _muteButton;
    private TMP_Text _modeValue, _resolutionValue, _qualityValue;

    private GameSettings.Values _working;
    private List<Vector2Int> _resolutions;
    private List<KeyValuePair<string, int>> _qualities;

    public static bool IsOpen => _instance != null && _instance._root.gameObject.activeSelf;

    public static void Open(Scene scene)
    {
        if (_instance == null)
        {
            Canvas canvas = DayLoopUI.CreateCanvas("[Settings] Window", 200, scene);
            _instance = canvas.gameObject.AddComponent<SettingsWindow>();
            _instance.Build(canvas.transform);
        }
        _instance.Show();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Show()
    {
        _working = GameSettings.Current;
        _resolutions = GameSettings.ResolutionOptions();
        _qualities = GameSettings.QualityOptions();
        if (_working.width <= 0 || _working.height <= 0)
        {
            _working.width = Screen.width;
            _working.height = Screen.height;
        }
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        SelectTab(true);
        Refresh();
    }

    private void Close()
    {
        _root.gameObject.SetActive(false);
    }

    // ───── 버튼 동작 ─────

    private void Cancel()
    {
        GameSettings.PreviewAudio(GameSettings.Current);
        Close();
    }

    private void Confirm()
    {
        GameSettings.Apply(_working, true);
        Close();
    }

    private void ResetToDefaults()
    {
        _working = GameSettings.Defaults();
        GameSettings.PreviewAudio(_working);
        Refresh();
    }

    private void SetVolume(int channel, float value)
    {
        switch (channel)
        {
            case 0: _working.master = value; break;
            case 1: _working.bgm = value; break;
            case 2: _working.sfx = value; break;
            default: _working.voice = value; break;
        }
        _sliderValues[channel].text = Mathf.RoundToInt(value * 100f) + "%";
        GameSettings.PreviewAudio(_working);
    }

    private void ToggleMute()
    {
        _working.muted = !_working.muted;
        GameSettings.PreviewAudio(_working);
        Refresh();
    }

    private void StepMode(int step)
    {
        _working.fullscreen = !_working.fullscreen;
        Refresh();
    }

    private void StepResolution(int step)
    {
        int index = ResolutionIndex();
        index = (index + step + _resolutions.Count) % _resolutions.Count;
        _working.width = _resolutions[index].x;
        _working.height = _resolutions[index].y;
        Refresh();
    }

    private void StepQuality(int step)
    {
        int index = QualityIndex();
        index = Mathf.Clamp(index + step, 0, _qualities.Count - 1);
        _working.quality = _qualities[index].Value;
        Refresh();
    }

    private int ResolutionIndex()
    {
        int best = 0;
        for (int i = 0; i < _resolutions.Count; i++)
        {
            if (_resolutions[i].x == _working.width && _resolutions[i].y == _working.height) return i;
            if (_resolutions[i].x <= _working.width) best = i;
        }
        return best;
    }

    private int QualityIndex()
    {
        int best = 0;
        for (int i = 0; i < _qualities.Count; i++)
        {
            if (_qualities[i].Value <= _working.quality) best = i;
        }
        return best;
    }

    private void Refresh()
    {
        float[] volumes = { _working.master, _working.bgm, _working.sfx, _working.voice };
        for (int i = 0; i < _sliders.Count; i++)
        {
            _sliders[i].SetValueWithoutNotify(volumes[i]);
            _sliderValues[i].text = Mathf.RoundToInt(volumes[i] * 100f) + "%";
            _sliders[i].interactable = !_working.muted;
        }

        DayLoopUI.SetButtonLabel(_muteButton, _working.muted ? "켜짐" : "꺼짐");
        _muteButton.image.color = _working.muted ? DayLoopUI.Coral : DayLoopUI.CreamDeep;
        _muteButton.GetComponentInChildren<TMP_Text>().color = _working.muted ? Color.white : DayLoopUI.WoodDark;

        _modeValue.text = _working.fullscreen ? "전체 화면" : "창 모드";
        Vector2Int size = _resolutions[ResolutionIndex()];
        _resolutionValue.text = $"{size.x} x {size.y}";
        _qualityValue.text = _qualities.Count > 0 ? _qualities[QualityIndex()].Key : "-";
    }

    private void SelectTab(bool sound)
    {
        _soundPage.gameObject.SetActive(sound);
        _screenPage.gameObject.SetActive(!sound);
        PaintTab(_soundTab, sound);
        PaintTab(_screenTab, !sound);
    }

    private static void PaintTab(Button tab, bool selected)
    {
        tab.image.color = selected ? DayLoopUI.Sea : DayLoopUI.CreamDeep;
        tab.GetComponentInChildren<TMP_Text>().color = selected ? Color.white : DayLoopUI.WoodDark;
    }

    // ───── 조립 ─────

    private void Build(Transform canvas)
    {
        _root = DayLoopUI.Rect("Root", canvas);
        DayLoopUI.Stretch(_root);

        // 뒤쪽 화면 클릭을 막는 어두운 배경
        Image dim = DayLoopUI.Panel(_root, DayLoopUI.Dim, "Dim", false);
        DayLoopUI.Stretch(dim.rectTransform);

        // 나무 테두리 + 종이 안쪽
        Image frame = DayLoopUI.Panel(_root, Frame, "Frame");
        DayLoopUI.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1000, 700));
        DayLoopUI.AddOutline(frame, FrameDark, 4f);
        Image paper = DayLoopUI.Panel(frame.transform, Paper, "Paper");
        DayLoopUI.Stretch(paper.rectTransform, 18, 18, 18, 18);

        // 제목 리본
        Image ribbon = DayLoopUI.Panel(frame.transform, FrameDark, "Ribbon");
        DayLoopUI.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(380, 78));
        DayLoopUI.AddOutline(ribbon, Frame, 3f);
        TMP_Text title = DayLoopUI.Text(ribbon.transform, "환경 설정", 36, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(title.rectTransform);

        Button close = DayLoopUI.Button(frame.transform, "X", DayLoopUI.Coral, Color.white, 30, Cancel, "Close");
        DayLoopUI.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12, -12), new Vector2(64, 64));
        DayLoopUI.AddOutline(close.image, FrameDark, 3f);

        // 탭
        RectTransform tabs = DayLoopUI.Rect("Tabs", paper.transform);
        DayLoopUI.Place(tabs, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(880, 64));
        DayLoopUI.Horizontal(tabs, 0, 14, TextAnchor.MiddleCenter);
        _soundTab = DayLoopUI.Button(tabs, "소리", DayLoopUI.Sea, Color.white, 26, () => SelectTab(true), "SoundTab");
        DayLoopUI.Layout(_soundTab, preferredWidth: 200, preferredHeight: 58);
        _screenTab = DayLoopUI.Button(tabs, "화면", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, 26, () => SelectTab(false), "ScreenTab");
        DayLoopUI.Layout(_screenTab, preferredWidth: 200, preferredHeight: 58);

        // 페이지
        _soundPage = Page(paper.transform, "SoundPage");
        string[] labels = { "전체 음량", "배경 음악", "효과음", "대화 음량" };
        for (int i = 0; i < labels.Length; i++)
        {
            int channel = i;
            RectTransform row = Row(_soundPage, labels[i]);
            Slider slider = BuildSlider(row, value => SetVolume(channel, value));
            DayLoopUI.Layout(slider, flexibleWidth: 1, preferredHeight: 40);
            TMP_Text value = DayLoopUI.Text(row, "100%", 24, DayLoopUI.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            DayLoopUI.Layout(value, preferredWidth: 90, preferredHeight: 50);
            _sliders.Add(slider);
            _sliderValues.Add(value);
        }
        RectTransform muteRow = Row(_soundPage, "전체 음소거");
        RectTransform spacer = DayLoopUI.Rect("Spacer", muteRow);
        DayLoopUI.Layout(spacer, flexibleWidth: 1, preferredHeight: 10);
        _muteButton = DayLoopUI.Button(muteRow, "꺼짐", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, 22, ToggleMute, "Mute");
        DayLoopUI.Layout(_muteButton, preferredWidth: 120, preferredHeight: 50);

        _screenPage = Page(paper.transform, "ScreenPage");
        _modeValue = Selector(_screenPage, "화면 모드", StepMode);
        _resolutionValue = Selector(_screenPage, "해상도", StepResolution);
        _qualityValue = Selector(_screenPage, "그래픽 품질", StepQuality);
        TMP_Text note = DayLoopUI.Text(_screenPage, "화면 설정은 [적용]을 누르면 바뀌어요. 게임 화면은 16:9 비율로 표시돼요.", 21, DayLoopUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        DayLoopUI.Layout(note, preferredHeight: 50);

        // 하단 버튼
        RectTransform footer = DayLoopUI.Rect("Footer", paper.transform);
        DayLoopUI.Place(footer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(880, 66));
        DayLoopUI.Horizontal(footer, 0, 16, TextAnchor.MiddleCenter);
        Button reset = DayLoopUI.Button(footer, "기본값으로", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, 24, ResetToDefaults, "Reset");
        DayLoopUI.Layout(reset, preferredWidth: 200, preferredHeight: 62);
        RectTransform gap = DayLoopUI.Rect("Gap", footer);
        DayLoopUI.Layout(gap, flexibleWidth: 1, preferredHeight: 10);
        Button cancel = DayLoopUI.Button(footer, "취소", DayLoopUI.Wood, Color.white, 26, Cancel, "Cancel");
        DayLoopUI.Layout(cancel, preferredWidth: 170, preferredHeight: 62);
        Button apply = DayLoopUI.Button(footer, "적용", DayLoopUI.Sea, Color.white, 26, Confirm, "Apply");
        DayLoopUI.Layout(apply, preferredWidth: 170, preferredHeight: 62);

        _root.gameObject.SetActive(false);
    }

    private static RectTransform Page(Transform parent, string name)
    {
        RectTransform page = DayLoopUI.Rect(name, parent);
        DayLoopUI.Place(page, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(880, 400));
        DayLoopUI.Vertical(page, 0, 14);
        return page;
    }

    private static RectTransform Row(Transform page, string label)
    {
        Image row = DayLoopUI.Panel(page, DayLoopUI.Cream, label);
        DayLoopUI.Layout(row, preferredHeight: 66);
        HorizontalLayoutGroup layout = DayLoopUI.Horizontal(row, 0, 18);
        layout.padding = new RectOffset(26, 22, 8, 8);
        TMP_Text text = DayLoopUI.Text(row.transform, label, 26, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, preferredWidth: 200, preferredHeight: 50);
        return row.rectTransform;
    }

    private static TMP_Text Selector(Transform page, string label, Action<int> step)
    {
        RectTransform row = Row(page, label);
        Button prev = DayLoopUI.Button(row, Glyph("◀", "<"), DayLoopUI.Wood, Color.white, 22, () => step(-1), "Prev");
        DayLoopUI.Layout(prev, preferredWidth: 64, preferredHeight: 50);
        TMP_Text value = DayLoopUI.Text(row, "", 26, DayLoopUI.WoodDark, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(value, flexibleWidth: 1, preferredHeight: 50);
        Button next = DayLoopUI.Button(row, Glyph("▶", ">"), DayLoopUI.Wood, Color.white, 22, () => step(1), "Next");
        DayLoopUI.Layout(next, preferredWidth: 64, preferredHeight: 50);
        return value;
    }

    private static Slider BuildSlider(Transform parent, UnityAction<float> onChanged)
    {
        RectTransform root = DayLoopUI.Rect("Slider", parent);
        Slider slider = root.gameObject.AddComponent<Slider>();

        Image track = DayLoopUI.Panel(root, DayLoopUI.CreamDeep, "Track");
        CenterBar(track.rectTransform, 16f);

        RectTransform fillArea = DayLoopUI.Rect("Fill Area", root);
        CenterBar(fillArea, 16f);
        Image fill = DayLoopUI.Panel(fillArea, DayLoopUI.Sea, "Fill");
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(1f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = DayLoopUI.Rect("Handle Slide Area", root);
        DayLoopUI.Stretch(handleArea, 16, 4, 16, 4);
        Image handle = DayLoopUI.Panel(handleArea, Color.white, "Handle");
        handle.rectTransform.sizeDelta = new Vector2(32f, 0f);
        DayLoopUI.AddOutline(handle, DayLoopUI.Wood, 2f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private static void CenterBar(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }

    private static string Glyph(string glyph, string fallback)
    {
        TMP_FontAsset font = DayLoopUI.Font;
        return font != null && font.HasCharacters(glyph) ? glyph : fallback;
    }
}
