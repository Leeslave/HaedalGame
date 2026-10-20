using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 환경 설정 창. 타이틀의 [설정] 버튼과 관리 창의 설정 탭에서 같은 창을 연다.
/// 소리와 화면 설정을 두 칸으로 한 번에 보여 준다.
///  - 소리: 전체 음량, 배경음악, 효과음, 대화 음량, 전체 음소거
///  - 화면: 화면 모드(전체 화면/창 모드), 해상도(16:9), 그래픽 품질(낮음/보통/높음)
/// 음량은 움직이는 즉시 미리 들려주고, [적용]을 눌러야 저장된다. [취소]는 창을 열기 전 값으로 되돌린다.
/// </summary>
public class SettingsWindow : MonoBehaviour
{
    private static SettingsWindow _instance;

    private RectTransform _root;
    private RectTransform _soundPage, _screenPage;
    private readonly List<Slider> _sliders = new List<Slider>();
    private readonly List<TMP_Text> _sliderValues = new List<TMP_Text>();
    private Button _muteButton;
    private RectTransform _muteKnob;
    private TMP_Text _modeValue, _resolutionValue;
    private readonly List<Button> _qualityButtons = new List<Button>();

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

        _muteButton.image.color = _working.muted ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Divider;
        DayLoopUI.Place(_muteKnob, new Vector2(_working.muted ? 1 : 0, .5f), new Vector2(_working.muted ? 1 : 0, .5f), new Vector2(_working.muted ? -7.5f : 7.5f, 0), new Vector2(42, 42));

        _modeValue.text = _working.fullscreen ? "전체 화면" : "창 모드";
        Vector2Int size = _resolutions[ResolutionIndex()];
        _resolutionValue.text = $"{size.x} x {size.y}";
        for (int i = 0; i < _qualityButtons.Count; i++)
            DayLoopUI.SetButtonColors(_qualityButtons[i], i == QualityIndex() ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand,
                i == QualityIndex() ? Color.white : DayLoopUI.Kit.Ink);
    }

    // ───── 조립 ─────

    private void Build(Transform canvas)
    {
        _root = DayLoopUI.Rect("Root", canvas);
        DayLoopUI.Stretch(_root);

        // 뒤쪽 화면 클릭을 막는 어두운 배경
        Image dim = ReferenceTitleView.Picture(_root, "TitleBackground");
        DayLoopUI.Stretch(dim.rectTransform);

        var rail = DayLoopUI.ScreenRail(_root, "SettingsRail");
        DayLoopUI.Layout(DayLoopUI.Text(rail, "환경 설정", 54, Color.white, style: FontStyles.Bold), preferredHeight: 80);
        DayLoopUI.RailButton(rail, "적용\n<size=65%>바꾼 설정을 저장해요</size>", Confirm, true, 110);
        DayLoopUI.RailButton(rail, "기본값으로", ResetToDefaults);
        DayLoopUI.Spacer(rail);
        var cancel = DayLoopUI.RailButton(rail, "취소", Cancel);
        DayLoopUI.SetButtonColors(cancel, DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line);
        foreach (var icon in cancel.GetComponentsInChildren<Image>()) if (icon != cancel.image) icon.color = DayLoopUI.Kit.Line;

        Image frame = DayLoopUI.Card(_root, DayLoopUI.Kit.Sail, "Frame", 4f, 8f);
        frame.rectTransform.anchorMin = Vector2.zero; frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = new Vector2(450, 39); frame.rectTransform.offsetMax = new Vector2(-30, -30);

        // 소리 | 화면 두 칸을 한 번에 보여 준다. (탭 전환 없음)
        RectTransform columns = DayLoopUI.Rect("Columns", frame.transform);
        DayLoopUI.Stretch(columns, 45, 42, 45, 42);
        HorizontalLayoutGroup columnLayout = DayLoopUI.Horizontal(columns, 0, 27);
        columnLayout.childForceExpandWidth = true;
        columnLayout.childForceExpandHeight = true;

        _soundPage = Column(columns, "소리", "SoundPage");
        string[] labels = { "전체 음량", "배경 음악", "효과음", "대화 음량" };
        for (int i = 0; i < labels.Length; i++)
        {
            int channel = i;
            RectTransform row = Row(_soundPage, labels[i]);
            DayLoopUI.Layout(row, preferredHeight: 42);
            Slider slider = BuildSlider(row, value => SetVolume(channel, value));
            DayLoopUI.Layout(slider, flexibleWidth: 1, preferredHeight: 44);
            TMP_Text value = DayLoopUI.Text(row, "100%", 26, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            DesignFonts.Apply(value, true);
            DayLoopUI.Layout(value, preferredWidth: 66, preferredHeight: 42);
            _sliders.Add(slider);
            _sliderValues.Add(value);
        }
        DayLoopUI.Spacer(_soundPage);
        RectTransform muteRow = Row(_soundPage, "전체 음소거");
        _muteButton = DayLoopUI.KitButton(muteRow, "", DayLoopUI.Kit.Divider, DayLoopUI.Kit.Line, 24, ToggleMute, "Mute");
        _muteButton.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        DayLoopUI.Layout(_muteButton, preferredWidth: 126, preferredHeight: 60);
        foreach (var shadow in _muteButton.GetComponents<Shadow>()) if (!(shadow is Outline)) shadow.effectDistance = Vector2.zero;
        var knob = DayLoopUI.Chunky(DayLoopUI.Panel(_muteButton.transform, Color.white, "Knob"), 3, 0);
        knob.sprite = DayLoopUI.Circle; knob.type = Image.Type.Simple; knob.raycastTarget = false; _muteKnob = knob.rectTransform;

        _screenPage = Column(columns, "화면", "ScreenPage");
        _modeValue = Selector(_screenPage, "화면 모드", StepMode);
        _resolutionValue = Selector(_screenPage, "해상도", StepResolution);
        var qualityRow = Row(_screenPage, "그래픽 품질");
        string[] qualityNames = { "낮음", "보통", "높음" };
        for (int i = 0; i < qualityNames.Length; i++)
        {
            int index = i;
            var button = DayLoopUI.KitButton(qualityRow, qualityNames[i], DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 24, () => {
                if (_qualities.Count > 0) _working.quality = _qualities[Mathf.Min(index, _qualities.Count - 1)].Value;
                Refresh();
            });
            DayLoopUI.Layout(button, preferredWidth: 0, flexibleWidth: 1, preferredHeight: 63).minWidth = 0; _qualityButtons.Add(button);
        }
        DayLoopUI.Spacer(_screenPage);
        TMP_Text note = DayLoopUI.Text(_screenPage, "해상도는 16:9 비율만 보여요. 바꾼 설정은 [적용]을 눌러야 저장돼요.", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        DayLoopUI.Layout(note, preferredHeight: 60); note.fontSize = 19.5f;

        _root.gameObject.SetActive(false);
    }

    private static RectTransform Column(Transform parent, string title, string name)
    {
        Image column = DayLoopUI.Chunky(DayLoopUI.Panel(parent, Color.white, name), 3f, 0f);
        DayLoopUI.Layout(column, preferredWidth: 0, flexibleWidth: 1, flexibleHeight: 1).minWidth = 0;
        VerticalLayoutGroup layout = DayLoopUI.Vertical(column, 0, 21);
        layout.padding = new RectOffset(27, 27, 24, 24);
        TMP_Text heading = DayLoopUI.Text(column.transform, title, 32, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(heading, preferredHeight: 40);
        return column.rectTransform;
    }

    private static RectTransform Row(Transform page, string label)
    {
        RectTransform row = DayLoopUI.Rect(label, page);
        DayLoopUI.Layout(row, preferredHeight: 66);
        DayLoopUI.Horizontal(row, 0, 12);
        TMP_Text text = DayLoopUI.Text(row, label, 25, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, preferredWidth: 165, preferredHeight: 50).minWidth = 165;
        return row;
    }

    private static TMP_Text Selector(Transform page, string label, Action<int> step)
    {
        RectTransform row = Row(page, label);
        Button prev = DayLoopUI.KitButton(row, Glyph("◀", "<"), DayLoopUI.Kit.Wood, Color.white, 22, () => step(-1), "Prev");
        DayLoopUI.Layout(prev, preferredWidth: 66, preferredHeight: 66);
        Image box = DayLoopUI.Panel(row, DayLoopUI.Kit.Sail, "Value");
        DayLoopUI.Chunky(box, 2f, 0f);
        DayLoopUI.Layout(box, flexibleWidth: 1, preferredHeight: 66).minWidth = 0;
        TMP_Text value = DayLoopUI.Text(box.transform, "", 26, DayLoopUI.Kit.Line, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(value.rectTransform, 8, 0, 8, 0);
        Button next = DayLoopUI.KitButton(row, Glyph("▶", ">"), DayLoopUI.Kit.Wood, Color.white, 22, () => step(1), "Next");
        DayLoopUI.Layout(next, preferredWidth: 66, preferredHeight: 66);
        return value;
    }

    private static Slider BuildSlider(Transform parent, UnityAction<float> onChanged)
    {
        RectTransform root = DayLoopUI.Rect("Slider", parent);
        Slider slider = root.gameObject.AddComponent<Slider>();

        Image track = DayLoopUI.Panel(root, DayLoopUI.Kit.Divider, "Track");
        DayLoopUI.Chunky(track, 2f, 0f);
        CenterBar(track.rectTransform, 18f);

        RectTransform fillArea = DayLoopUI.Rect("Fill Area", root);
        CenterBar(fillArea, 18f);
        Image fill = DayLoopUI.Panel(fillArea, DayLoopUI.Kit.Sea, "Fill");
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(1f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        RectTransform handleArea = DayLoopUI.Rect("Handle Slide Area", root);
        DayLoopUI.Stretch(handleArea, 16, 2, 16, 2);
        Image handle = DayLoopUI.Panel(handleArea, Color.white, "Handle");
        handle.rectTransform.sizeDelta = new Vector2(34f, 0f);
        DayLoopUI.Chunky(handle, 2.5f, 3f);

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
