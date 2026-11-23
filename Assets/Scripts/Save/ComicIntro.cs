using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 새 게임 첫 만화. 흰 종이 위 두 쪽 펼침면에 컷 자리를 잡아 두고, 누를 때마다 한 컷씩 나타난다.
/// 컷 그림의 말풍선은 비어 있고, 대사는 여기서 글자로 넣는다. 마지막 컷 뒤에 한 번 더 누르면 끝난다.
/// 그림은 Resources/UIDesign/Art/Intro/Cut*.png. 하나라도 없으면 TryPlay가 null을 돌려 기존 대화 인트로를 쓴다.
/// </summary>
public class ComicIntro : MonoBehaviour, IPointerClickHandler
{
    // 펼침면 좌표는 컷 그림의 원래 픽셀 단위, 왼쪽 위가 (0,0). 화면에 맞춰 통째로 줄인다.
    private const float PageWidth = 1660f, PageHeight = 1135f;
    private const float CharsPerSecond = 32f;

    public struct Line
    {
        public Rect box;     // 그림 안에서 말풍선 글자 칸 (픽셀, 왼쪽 위 기준)
        public string text;
        public bool caption; // 서술은 그림 아래 내레이션 칸을 사용한다.
        public bool speech;
        public Rect bubble;
        public Vector2 tailTip;
        public bool tailFromLeft;
    }

    public struct Cut
    {
        public string art;
        public Vector2 position; // 펼침면 위 왼쪽 위 좌표
        public Line[] lines;
        public Vector2 size;
        public Rect uv; // 비어 있으면 전체 그림, 아니면 정규화된 아틀라스 영역.
        public Cut(string art, float x, float y, params Line[] lines)
        { this.art = art; position = new Vector2(x, y); this.lines = lines; size = Vector2.zero; uv = default; }
    }

    private static Line Say(float x0, float y0, float x1, float y1, string text) => new Line { box = Rect.MinMaxRect(x0, y0, x1, y1), text = text };

    // 서로 이름을 부르지 않는다. 플레이어와 해달의 이름은 게임 안에서 따로 정해진다.
    private static readonly Cut[] Cuts =
    {
        new Cut("Cut1", 36, 36, Say(72, 88, 270, 200, "정신이 들어?\n내가 구해 왔어.")),
        new Cut("Cut2", 52, 483, Say(50, 68, 186, 158, "살려 줘서\n고마워!"), Say(524, 38, 666, 116, "이젠 좀\n괜찮아?")),
        new Cut("Cut3", 55, 789, Say(72, 30, 238, 125, "섬을 나가려면\n배가 필요해."), Say(278, 30, 508, 125, "내가 만들어 줄게.\n대신 내 꿈을 이뤄 줘!")),
        new Cut("Cut4", 879, 46, Say(505, 60, 665, 175, "내 식당을\n여는 거야!")),
        new Cut("Cut5", 884, 305, Say(60, 24, 240, 86, "식당은 어디 있는데?"), Say(410, 28, 530, 96, "지금부터\n지어야지!")),
        new Cut("Cut6a", 880, 531),
        new Cut("Cut6b", 1166, 527),
        new Cut("Cut7", 883, 735, Say(400, 270, 662, 327, "우리 식당, 완성!")),
    };

    private static readonly Color Paper = Color.white;
    private static readonly Color BubbleInk = new Color32(0x2A, 0x1E, 0x14, 0xFF);

    private Action _onFinished;
    private RectTransform _page;
    private CanvasGroup _group;
    private TextMeshProUGUI _hint;
    private RectTransform[] _cutViews;
    private TextMeshProUGUI[][] _texts;
    private int _shown = -1;     // 지금까지 나온 마지막 컷
    private Coroutine _typing;
    private bool _closing;
    private Cut[] _cuts = Cuts;
    private Vector2 _pageSize = new Vector2(PageWidth, PageHeight);
    private string _title;
    private readonly System.Collections.Generic.List<Sprite> _panelSprites = new System.Collections.Generic.List<Sprite>();
    public static ComicIntro Active { get; private set; }

    public static bool HasArt
    {
        get
        {
            foreach (var cut in Cuts)
                if (Resources.Load<Texture2D>("UIDesign/Art/Intro/" + cut.art) == null) return false;
            return true;
        }
    }

    /// <summary>만화를 띄운다. 그림이 없으면 아무것도 하지 않고 null.</summary>
    public static ComicIntro TryPlay(Action onFinished)
    {
        if (!HasArt) return null;
        return TryPlay(Cuts, new Vector2(PageWidth, PageHeight), null, onFinished);
    }

    /// <summary>오프닝과 같은 클릭·타이핑 연출로 주민 만화도 재생한다. art는 UIDesign/Art 기준 경로.</summary>
    public static ComicIntro TryPlay(Cut[] cuts, Vector2 pageSize, string title, Action onFinished)
    {
        if (Active != null || cuts == null || cuts.Length == 0) return null;
        foreach (var cut in cuts)
            if (Resources.Load<Texture2D>(ArtPath(cut.art)) == null) return null;
        var canvas = DayLoopUI.CreateCanvas("[ComicIntro]", 950, SceneManager.GetActiveScene());
        var intro = canvas.gameObject.AddComponent<ComicIntro>();
        Active = intro;
        intro._cuts = cuts;
        intro._pageSize = pageSize;
        intro._title = title;
        intro._onFinished = onFinished;
        intro.Build(canvas.transform);
        intro.Next();
        return intro;
    }

    private static string ArtPath(string art) => "UIDesign/Art/" + (art.Contains("/") ? art : "Intro/" + art);

    private void Build(Transform root)
    {
        _group = gameObject.AddComponent<CanvasGroup>();

        var paper = DayLoopUI.Panel(root, Paper, "Paper", false);
        DayLoopUI.Stretch(paper.rectTransform);
        paper.raycastTarget = true; // 화면 어디를 눌러도 넘어간다 (OnPointerClick)

        _page = DayLoopUI.Rect("Spread", root);
        DayLoopUI.Place(_page, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, _pageSize);
        // 책 가운데 접히는 선
        if (string.IsNullOrEmpty(_title))
        {
            var fold = DayLoopUI.Panel(_page, new Color(0, 0, 0, .12f), "Fold", false);
            fold.raycastTarget = false;
            DayLoopUI.Place(fold.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(3, _pageSize.y - 40));
        }
        else
        {
            var title = DayLoopUI.Text(_page, _title, 38, BubbleInk, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.raycastTarget = false;
            DayLoopUI.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -12), new Vector2(_pageSize.x - 72, 65));
        }

        _cutViews = new RectTransform[_cuts.Length];
        _texts = new TextMeshProUGUI[_cuts.Length][];
        for (int i = 0; i < _cuts.Length; i++)
        {
            var cut = _cuts[i];
            var sprite = DesignArt.Load(cut.art.Contains("/") ? cut.art : "Intro/" + cut.art);
            if (cut.uv.width > 0 && cut.uv.height > 0)
            {
                var texture = sprite.texture;
                sprite = Sprite.Create(texture, new Rect(cut.uv.x * texture.width, cut.uv.y * texture.height,
                    cut.uv.width * texture.width, cut.uv.height * texture.height), new Vector2(.5f, .5f), 100);
                sprite.hideFlags = HideFlags.DontSave;
                _panelSprites.Add(sprite);
            }
            var image = DayLoopUI.Panel(_page, Color.white, cut.art, false);
            image.sprite = sprite; image.type = Image.Type.Simple; image.raycastTarget = false;
            var size = cut.size.x > 0 && cut.size.y > 0 ? cut.size : sprite.rect.size;
            var view = image.rectTransform;
            DayLoopUI.Place(view, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(cut.position.x + size.x / 2, -cut.position.y - size.y / 2), size);
            _cutViews[i] = view;

            _texts[i] = new TextMeshProUGUI[cut.lines?.Length ?? 0];
            for (int j = 0; j < _texts[i].Length; j++)
            {
                var line = cut.lines[j];
                if (line.speech)
                {
                    var balloon = DayLoopUI.Rect("Speech balloon", view);
                    float top = Mathf.Min(0, line.bubble.yMin);
                    DayLoopUI.Place(balloon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -top), new Vector2(size.x, size.y - top));
                    var graphic = balloon.gameObject.AddComponent<ComicSpeechBubble>();
                    graphic.body = new Rect(line.bubble.x, line.bubble.y - top, line.bubble.width, line.bubble.height);
                    graphic.tailTip = line.tailTip - new Vector2(0, top); graphic.raycastTarget = false;
                    graphic.tailFromLeft = line.tailFromLeft;
                    graphic.SetAllDirty();
                }
                if (line.caption)
                {
                    var caption = DayLoopUI.Panel(view, new Color32(0xFC, 0xF5, 0xE9, 0xFF), "Caption", false);
                    caption.raycastTarget = false;
                    DayLoopUI.Place(caption.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                        new Vector2(line.box.xMin - 14, -line.box.yMin + 12), line.box.size + new Vector2(28, 24));
                }
                var label = DayLoopUI.Text(view, line.text, 28, BubbleInk, TextAlignmentOptions.Center, FontStyles.Bold);
                label.raycastTarget = false;
                label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 30;
                label.lineSpacing = -4f;
                label.textWrappingMode = line.caption || line.speech ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                if (line.caption) { label.alignment = TextAlignmentOptions.TopLeft; label.fontSizeMin = 22; label.fontSizeMax = 26; label.lineSpacing = 4; }
                if (line.speech) { label.fontSizeMin = 22; label.fontSizeMax = 28; label.lineSpacing = 0; }
                label.maxVisibleCharacters = 0;
                DayLoopUI.Place(label.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(line.box.xMin, -line.box.yMin), line.box.size);
                _texts[i][j] = label;
            }
            view.gameObject.SetActive(false);
        }

        _hint = DayLoopUI.Text(root, "눌러서 계속", 22, new Color(0, 0, 0, .45f), TextAlignmentOptions.BottomRight, FontStyles.Bold);
        _hint.raycastTarget = false;
        DayLoopUI.Place(_hint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 24), new Vector2(400, 40));

        var skip = DayLoopUI.Button(root, "건너뛰기", new Color(0, 0, 0, .06f), new Color(0, 0, 0, .55f), 22, Skip, "Skip");
        DayLoopUI.Place((RectTransform)skip.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(150, 52));

        Fit();
    }

    /// <summary>펼침면 전체가 화면 안에 들어오게 줄인다.</summary>
    private void Fit()
    {
        var area = ((RectTransform)transform).rect.size;
        if (area.x <= 0 || area.y <= 0) return;
        float scale = Mathf.Min((area.x - 60f) / _pageSize.x, (area.y - 110f) / _pageSize.y);
        _page.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }

    private void Update()
    {
        Fit();
        if (_hint != null)
            _hint.alpha = _typing != null ? 0f : .45f + Mathf.Sin(Time.unscaledTime * 3f) * .2f;
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Next();
#endif
    }

    public void OnPointerClick(PointerEventData eventData) => Next();

    /// <summary>글자가 찍히는 중이면 이 컷 대사를 다 보여 주고, 아니면 다음 컷. 마지막 컷 뒤면 끝낸다.</summary>
    public void Next()
    {
        if (_closing) return;
        if (_typing != null)
        {
            StopCoroutine(_typing); _typing = null;
            var view = _cutViews[_shown];
            view.GetComponent<CanvasGroup>().alpha = 1f;
            view.localScale = Vector3.one;
            foreach (var label in _texts[_shown]) label.maxVisibleCharacters = label.text.Length;
            return;
        }
        if (_shown + 1 >= _cuts.Length) { Close(); return; }
        _shown++;
        _typing = StartCoroutine(Reveal(_shown));
    }

    private IEnumerator Reveal(int index)
    {
        var view = _cutViews[index];
        view.gameObject.SetActive(true);
        var group = view.GetComponent<CanvasGroup>();
        if (group == null) group = view.gameObject.AddComponent<CanvasGroup>();
        const float duration = .35f;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / duration, 3f);
            group.alpha = k;
            view.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, k);
            yield return null;
        }
        group.alpha = 1f; view.localScale = Vector3.one;

        foreach (var label in _texts[index])
        {
            for (float shown = 0; shown < label.text.Length; shown += Time.unscaledDeltaTime * CharsPerSecond)
            {
                label.maxVisibleCharacters = Mathf.FloorToInt(shown);
                yield return null;
            }
            label.maxVisibleCharacters = label.text.Length;
            yield return new WaitForSecondsRealtime(.2f);
        }
        _typing = null;
    }

    public void Skip()
    {
        if (_typing != null) { StopCoroutine(_typing); _typing = null; }
        Close();
    }

    private void Close()
    {
        if (_closing) return;
        _closing = true;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        const float duration = .4f;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            _group.alpha = 1f - t / duration;
            yield return null;
        }
        Action callback = _onFinished;
        _onFinished = null;
        if (Active == this) Active = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    /// <summary>밖에서 인트로를 끊을 때. 완료 콜백은 부르지 않는다.</summary>
    public void Abort()
    {
        _onFinished = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        foreach (var sprite in _panelSprites)
        {
            if (sprite == null) continue;
            if (Application.isPlaying) Destroy(sprite);
            else DestroyImmediate(sprite);
        }
    }
}
