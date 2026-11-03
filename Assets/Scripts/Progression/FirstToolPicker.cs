using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 첫 조리도구 선택 창. (첫 플레이 UI 개선안 03)
/// 설명은 위(안내 띠), 선택은 가운데(도구 4칸 + 상세), 실행은 아래(받기 버튼 하나)에 둔다.
/// 관리 창을 자동으로 여는 대신 이 창 하나만 띄워 첫 행동을 안내한다.
/// </summary>
public static class FirstToolPicker
{
    private static readonly int[] ToolIds = { 201, 202, 203, 204 };
    private static readonly string[] Names = { "프라이팬", "도마", "튀김기", "냄비" };
    private static readonly string[] Icons = { "Pan", "Board", "Fryer", "Pot" };
    private static readonly string[] Uses =
    {
        "굽고 볶는 요리를 만들 때 사용해요.",
        "회처럼 썰어 내는 요리를 만들 때 사용해요.",
        "바삭한 튀김 요리를 만들 때 사용해요.",
        "찜이나 탕처럼 끓이는 요리를 만들 때 사용해요.",
    };

    private static Canvas _canvas;
    private static Scene _scene;
    private static Action _onChosen;
    private static int _selected = 201;
    private static string _error;

    public static bool IsOpen => _canvas != null;

    /// <summary>첫 도구를 아직 고르지 않았을 때만 연다. 고르면 onChosen을 부른다.</summary>
    public static void Show(Scene scene, Action onChosen = null)
    {
        if (!GameSession.IsActive || GameSession.Current.progression.firstToolChosen) return;
        _scene = scene;
        _onChosen = onChosen;
        _selected = ToolIds.FirstOrDefault(HasStarterMenu);
        if (_selected == 0) _selected = ToolIds[0];
        _error = null;
        Build();
    }

    public static void Close()
    {
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_canvas.gameObject);
        }
        _canvas = null;
    }

    /// <summary>첫 도구로 받으면 오늘의 메뉴에 자동 등록되는 기본 메뉴. 없으면 null.</summary>
    public static RecipeData StarterMenu(int toolId)
        => GameDatabase.Recipes != null ? GameDatabase.Recipes.Recipes.FirstOrDefault(r => r.DefaultUnlock && r.ClassId == toolId) : null;

    private static bool HasStarterMenu(int toolId) => StarterMenu(toolId) != null;

    private static void Select(int toolId)
    {
        _selected = toolId;
        _error = null;
        Build();
    }

    private static void Take()
    {
        if (RestaurantProgress.TryBuyTool(_selected, out string error))
        {
            Close();
            _onChosen?.Invoke();
            return;
        }
        _error = error;
        Build();
    }

    // ───── 조립 ─────

    private static void Build()
    {
        // 버튼 클릭 중에 다시 그리므로 즉시 파괴하지 않고 숨긴 뒤 다음 프레임에 지운다.
        Close();
        _canvas = DayLoopUI.CreateCanvas("[FirstTool] Picker", 175, _scene);

        var dim = DayLoopUI.Panel(_canvas.transform, new Color(0.05f, 0.08f, 0.10f, 0.6f), "Dim", false);
        DayLoopUI.Stretch(dim.rectTransform);

        var card = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "Card", 4f, 8f);
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(960, 0));
        var layout = DayLoopUI.Vertical(card, 0, 22);
        layout.padding = new RectOffset(42, 42, 33, 39);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Header(card.transform);
        Notice(card.transform);
        ToolRow(card.transform);
        Detail(card.transform);
        Footer(card.transform);
    }

    private static void Header(Transform card)
    {
        var header = DayLoopUI.Rect("Header", card);
        DayLoopUI.Layout(header, preferredHeight: 66);
        DayLoopUI.Horizontal(header, 0, 12);
        var title = DayLoopUI.Text(header, "첫 조리도구 선택", 45, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(title, flexibleWidth: 1, preferredHeight: 64);
        IconButton(header, "Close", Close);
    }

    private static void Notice(Transform card)
    {
        var notice = DayLoopUI.Chunky(DayLoopUI.Panel(card, DayLoopUI.Kit.SeaTint, "Notice"), 2f, 0f);
        notice.GetComponent<Outline>().effectColor = new Color32(0xB9, 0xDD, 0xDF, 0xFF);
        DayLoopUI.Layout(notice, preferredHeight: 104);
        var row = DayLoopUI.Horizontal(notice, 0, 20);
        row.padding = new RectOffset(28, 24, 12, 12);
        var bulb = ReferenceTitleView.Picture(notice.transform, "IconIdea");
        bulb.color = DayLoopUI.Kit.SeaDeep; bulb.preserveAspect = true;
        DayLoopUI.Layout(bulb, preferredWidth: 54, preferredHeight: 54);
        var text = DayLoopUI.Text(notice.transform, "처음에는 도구 1개를 무료로 받아요.\n받은 도구의 기본 메뉴가 오늘의 메뉴에 자동으로 올라가요.", 24, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft);
        text.lineSpacing = 12f;
        DayLoopUI.Layout(text, flexibleWidth: 1, preferredHeight: 80);
    }

    private static void ToolRow(Transform card)
    {
        var row = DayLoopUI.Rect("Tools", card);
        DayLoopUI.Layout(row, preferredHeight: 178);
        DayLoopUI.Horizontal(row, 0, 18).childForceExpandWidth = true;
        for (int i = 0; i < ToolIds.Length; i++)
        {
            int id = ToolIds[i];
            bool selected = id == _selected;
            bool available = HasStarterMenu(id);
            var tile = DayLoopUI.KitButton(row, "", selected ? DayLoopUI.Kit.SeaTint : Color.white, DayLoopUI.Kit.Ink, 24, () => Select(id), Names[i]);
            tile.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            if (selected) { var outline = tile.GetComponent<Outline>(); outline.effectColor = DayLoopUI.Kit.Sea; outline.effectDistance = new Vector2(4, -4); }
            DayLoopUI.Layout(tile, flexibleWidth: 1, preferredHeight: 170);

            var icon = ReferenceTitleView.Picture(tile.transform, "Icon" + Icons[i]);
            icon.color = available ? DayLoopUI.Kit.Line : DayLoopUI.Kit.InkSoft; icon.preserveAspect = true;
            DayLoopUI.Place(icon.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(92, 92));
            var label = DayLoopUI.Text(tile.transform, Names[i], 25, available ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
            DayLoopUI.Place(label.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(170, 36));

            if (selected) CornerBadge(tile.transform, "IconCheck", DayLoopUI.Kit.Sea);
            else if (!available)
            {
                var chip = DayLoopUI.Chip(tile.transform, "연구 필요", DayLoopUI.Kit.Sand, DayLoopUI.Kit.InkSoft, 17);
                var rect = (RectTransform)chip.transform.parent;
                rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                DayLoopUI.Place(rect, Vector2.one, Vector2.one, new Vector2(-8, -8), new Vector2(chip.GetPreferredValues("연구 필요").x + 28, 28));
            }
        }
    }

    private static void Detail(Transform card)
    {
        int index = Array.IndexOf(ToolIds, _selected);
        string name = Names[index];
        RecipeData starter = StarterMenu(_selected);

        var panel = DayLoopUI.Chunky(DayLoopUI.Panel(card, Color.white, "Detail"), 2f, 0f);
        panel.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Divider;
        DayLoopUI.Layout(panel, preferredHeight: 196);
        var row = DayLoopUI.Horizontal(panel, 0, 28);
        row.padding = new RectOffset(28, 28, 22, 22);

        var plate = DayLoopUI.Panel(panel.transform, DayLoopUI.Kit.Sand, "Plate");
        plate.sprite = DayLoopUI.Circle; plate.type = Image.Type.Simple; plate.raycastTarget = false;
        DayLoopUI.Layout(plate, preferredWidth: 148, preferredHeight: 148);
        var art = ReferenceTitleView.Picture(plate.transform, "Icon" + Icons[index]);
        art.color = DayLoopUI.Kit.Line; art.preserveAspect = true;
        DayLoopUI.Place(art.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(104, 104));

        var texts = DayLoopUI.Rect("Texts", panel.transform);
        DayLoopUI.Layout(texts, flexibleWidth: 1, preferredHeight: 152);
        DayLoopUI.Vertical(texts, 0, 8, TextAnchor.MiddleLeft);
        DayLoopUI.Layout(DayLoopUI.Text(texts, name, 36, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 46);
        DayLoopUI.Layout(DayLoopUI.Text(texts, Uses[index], 23, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft), preferredHeight: 32);

        var gets = DayLoopUI.Panel(texts, starter != null ? DayLoopUI.Kit.SeaTint : DayLoopUI.Kit.WarnTint, "Gets");
        DayLoopUI.Layout(gets, preferredHeight: 52);
        string sea = DayLoopUI.Hex(DayLoopUI.Kit.Sea);
        string what = starter != null
            ? $"<color={sea}>받는 것:</color>  {name} + 기본 메뉴 '{starter.RecipeName}'"
            : $"<color={DayLoopUI.Hex(DayLoopUI.Kit.WarnInk)}>기본 메뉴가 없어 첫 도구로 고를 수 없어요.</color>";
        var getsText = DayLoopUI.Text(gets.transform, what, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Stretch(getsText.rectTransform, 18, 4, 14, 4);
    }

    private static void Footer(Transform card)
    {
        var rule = DayLoopUI.Panel(card, DayLoopUI.Kit.Divider, "Rule", false);
        DayLoopUI.Layout(rule, preferredHeight: 2);
        string note = string.IsNullOrEmpty(_error) ? "다른 도구는 나중에 대장간에서 살 수 있어요." : _error;
        var text = DayLoopUI.Text(card, note, 22, string.IsNullOrEmpty(_error) ? DayLoopUI.Kit.InkSoft : DayLoopUI.Kit.WarnInk, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Layout(text, preferredHeight: 32);

        bool available = HasStarterMenu(_selected);
        string name = Names[Array.IndexOf(ToolIds, _selected)];
        var take = DayLoopUI.KitButton(card, $"{name} 무료로 받기", available ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand,
            available ? Color.white : DayLoopUI.Kit.InkSoft, 32, Take, "Take");
        take.interactable = available;
        DayLoopUI.Layout(take, preferredHeight: 84);
    }

    // ───── 작은 부품 ─────

    private static void IconButton(Transform parent, string icon, UnityEngine.Events.UnityAction action)
    {
        // 시안 02-B: 모래색 네모 닫기 버튼 (66×66)
        var button = DayLoopUI.KitButton(parent, "", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, 10, action, icon);
        button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        DayLoopUI.Layout(button, preferredWidth: 66, preferredHeight: 66);
        var image = ReferenceTitleView.Picture(button.transform, "Icon" + icon);
        image.color = DayLoopUI.Kit.Line; image.preserveAspect = true;
        DayLoopUI.Place(image.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(34, 34));
    }

    private static void CornerBadge(Transform parent, string icon, Color color)
    {
        var badge = DayLoopUI.Chunky(DayLoopUI.Panel(parent, color, "Badge"), 3f, 0f);
        badge.GetComponent<Outline>().effectColor = Color.white;
        badge.sprite = DayLoopUI.Circle; badge.type = Image.Type.Simple; badge.raycastTarget = false;
        DayLoopUI.Place(badge.rectTransform, Vector2.one, new Vector2(.5f, .5f), new Vector2(-14, -14), new Vector2(44, 44));
        var mark = ReferenceTitleView.Picture(badge.transform, icon);
        mark.preserveAspect = true;
        DayLoopUI.Place(mark.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30));
    }
}
