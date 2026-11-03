using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 빌드에 포함된 섬과 식당의 공용 관리 창.
/// 왼쪽 나무 막대에 탭을 "식당 / 섬"으로 묶고, 메뉴 탭은 음식 카드, 집 탭은 미션·주급·직원 체력 카드로 보여 준다.
/// 섬에서는 오른쪽 아래 빠른 관리 막대로, 식당에서는 [관리] 버튼으로 연다.
/// 임금이 밀리면 "임금 정산" 화면으로 고정되고 정산 전에는 닫을 수 없다.
/// </summary>
public partial class RestaurantManagementUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform window, content;
    private TMP_Text message, title, info;
    private Button headerAction;
    private static int pendingPlacement;
    private string tab = "메뉴";
    private int page;
    private readonly List<Button> openers = new List<Button>();
    private ResearchPractice practice;
    private RectTransform practiceRoot;
    private RectTransform railGroups;
    private Image messageBar;
    private string expandedGroup;
    private static readonly string[] DestructiveActions = { "제외", "해고", "철거", "취소·환불", "돌아가기" };

    /// <summary>
    /// 관리 항목 10개를 세 묶음으로 접어 둔다. 한 번에 한 묶음만 펼치고, 상세는 항목을 고른 뒤 오른쪽에 보여 준다.
    /// (섹션 키는 저장·다른 화면에서 쓰는 기존 이름을 유지한다)
    /// </summary>
    private static readonly (string name, string summary, string icon, string[] sections)[] Groups =
    {
        ("영업 준비", "메뉴 · 재료 · 직원", "Menu", new[] { "메뉴", "재료", "직원" }),
        ("식당 꾸미기", "가구 · 조리도구 · 식당 증축", "Furniture", new[] { "가구", "식당", "대장간" }),
        ("섬 생활", "상점 · 연구 · 탐험 · 집", "Explore", new[] { "상점", "엘프", "연구", "주민", "집" }),
    };

    private static string GroupOf(string section) => Groups.FirstOrDefault(g => g.sections.Contains(section)).name;

    private static string TabLabel(string section) => section switch
    {
        "엘프" => "엘프 상점",
        "주민" => "탐험",
        "식당" => "조리도구",
        "가구" => "가구 배치",
        "대장간" => "식당 증축",
        _ => section,
    };

    private static string TabHint(string section) => section switch
    {
        "메뉴" => "판매할 음식",
        "재료" => "재고와 구매",
        "직원" => "배치와 고용",
        "가구" => "테이블 · 대기석 놓기",
        "식당" => "도구 구매와 강화",
        "대장간" => "레벨업 공사",
        "상점" => "재료 구매",
        "엘프" => "특별 재료 · 설계도",
        "연구" => "새 요리 배우기",
        "주민" => "직원 파견 · 수확",
        "집" => "할 일 · 주급 · 취침",
        _ => "",
    };

    private static string TabIcon(string section) => section switch
    {
        "메뉴" => "Menu",
        "재료" => "Ingredient",
        "직원" => "Staff",
        "가구" => "Furniture",
        "식당" => "Tools",
        "대장간" => "Hammer",
        "상점" => "Shop",
        "엘프" => "Elf",
        "연구" => "Research",
        "주민" => "Explore",
        "집" => "House",
        _ => "Play",
    };

    private void Start()
    {
        if (!GameSession.IsActive) return;
        GameSettings.ApplySaved();
        if (IngredientInventoryService.Instance == null) new GameObject("Ingredient inventory").AddComponent<IngredientInventoryService>();
        if (CurrencyManager.Instance == null) new GameObject("Currency wallets").AddComponent<CurrencyManager>();
        if (ElfShopManager.Instance == null) new GameObject("Elf shop").AddComponent<ElfShopManager>();
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name == "Lab") button.onClick.AddListener(() => Open("연구"));
            if (button.name == "House") button.onClick.AddListener(() => Open("집"));
        }
        canvas = DayLoopUI.CreateCanvas("Restaurant management", 140, gameObject.scene);
        bool restaurant = gameObject.scene.name == GameScenes.Restaurant;
        if (restaurant) BuildOpener(); // 섬 화면은 IslandDayHud의 메뉴 띠와 건물 표식으로 연다.
        BuildWindow();
        window.gameObject.SetActive(false);
        if (pendingPlacement > 0 && restaurant)
        { int count = pendingPlacement; pendingPlacement = 0; StartCoroutine(PlaceAfterStart(count)); }
        // 관리 창을 자동으로 열지 않는다. 첫날 안내는 섬의 첫 영업 준비 카드(IslandDayHud)가 맡고,
        // 첫 도구 없이 식당에 들어온 경우에만 도구 선택 창 하나를 띄운다.
        if (restaurant && !GameSession.Current.progression.firstToolChosen)
            FirstToolPicker.Show(gameObject.scene);
    }

    // 식당: 하단에 운영 시작 버튼이 있으므로 관리 버튼은 그 위에 배치한다.
    private void BuildOpener()
    {
        var opener = DayLoopUI.KitButton(canvas.transform, "관리", DayLoopUI.Kit.Sea, Color.white, 28, () => Open("메뉴"), "Opener");
        PlaceOpener(opener, false);
        openers.Add(opener);
    }

    // 영업 중에는 오른쪽 위 골드 알약 왼쪽에, 그 밖에는 운영 시작 버튼 위(오른쪽 아래)에 둔다.
    private static void PlaceOpener(Button opener, bool besideHud)
    {
        var rt = (RectTransform)opener.transform;
        if (besideHud)
            DayLoopUI.Place(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(24 + RestaurantHud.Current.TopRightWidth + 12), -24), new Vector2(150, 64));
        else
            DayLoopUI.Place(rt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 124), new Vector2(170, 66));
    }

    private void BuildWindow()
    {
        window = DayLoopUI.Rect("ManagementWindow", canvas.transform);
        DayLoopUI.Stretch(window);
        var dim = ReferenceTitleView.Picture(window, "IslandBackground");
        DayLoopUI.Stretch(dim.rectTransform);
        var rail = DayLoopUI.ScreenRail(window, "ManagementRail");
        rail.GetComponent<VerticalLayoutGroup>().spacing = 18f;
        // 시안 03-A: 제목 → 묶음 3개 → 맨 아래 [설정][닫기]. 닫기는 이 버튼 하나만 둔다.
        DayLoopUI.Layout(DayLoopUI.Text(rail, "관리", 54, Color.white), preferredHeight: 66);
        railGroups = DayLoopUI.Rect("Groups", rail);
        DayLoopUI.Vertical(railGroups, 0, 18);
        DayLoopUI.Spacer(rail);
        var footer = DayLoopUI.Rect("SettingsAndClose", rail);
        DayLoopUI.Layout(footer, preferredHeight: 66);
        var footerRow = DayLoopUI.Horizontal(footer, 0, 12);
        footerRow.childForceExpandWidth = true; footerRow.childForceExpandHeight = true;
        var settings = DayLoopUI.RailButton(footer, "설정", () => SettingsWindow.Open(gameObject.scene), height: 66);
        DayLoopUI.Layout(settings, flexibleWidth: 1, preferredHeight: 66);
        var close = DayLoopUI.KitButton(footer, "닫기", DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, 28, CloseWindow, "Close");
        DayLoopUI.Layout(close, flexibleWidth: 1, preferredHeight: 66);
        DesignArt.ButtonIcon(close, "닫기");

        var frame = DayLoopUI.Card(window, DayLoopUI.Kit.Sail, "ContentFrame", 4, 8);
        frame.rectTransform.anchorMin = new Vector2(0, 0);
        frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = new Vector2(450, 39);
        frame.rectTransform.offsetMax = new Vector2(-30, -30);
        // 제목 줄(66): 제목 42 + 아래 설명 한 줄 20. 오른쪽에 핵심 버튼 하나.
        title = DayLoopUI.Text(frame.transform, "", 42, DayLoopUI.Kit.Line);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = Vector2.one; title.rectTransform.pivot = new Vector2(0, 1);
        title.rectTransform.offsetMin = new Vector2(33, -73); title.rectTransform.offsetMax = new Vector2(-300, -27);
        info = DayLoopUI.Text(frame.transform, "", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        info.textWrappingMode = TextWrappingModes.NoWrap;
        info.rectTransform.anchorMin = new Vector2(0, 1); info.rectTransform.anchorMax = Vector2.one; info.rectTransform.pivot = new Vector2(0, 1);
        info.rectTransform.offsetMin = new Vector2(33, -101); info.rectTransform.offsetMax = new Vector2(-300, -73);
        headerAction = DayLoopUI.KitButton(frame.transform, "", DayLoopUI.Kit.Sea, Color.white, 28.5f, HeaderAction);
        DayLoopUI.Place((RectTransform)headerAction.transform, Vector2.one, Vector2.one, new Vector2(-33, -27), new Vector2(240, 66));
        // 본문은 제목 줄 아래부터, 맨 아래 안내 띠(60) 자리를 비워 두고 쌓는다. 겹치거나 넘치지 않는다.
        content = DayLoopUI.Rect("Rows", frame.transform);
        DayLoopUI.Stretch(content, 33, 122, 33, 102);
        DayLoopUI.Vertical(content, 0, 21);
        messageBar = DayLoopUI.Chunky(DayLoopUI.Panel(frame.transform, DayLoopUI.Kit.SeaTint, "MessageBar"), 3f, 0f);
        messageBar.GetComponent<Outline>().effectColor = new Color32(0xB9, 0xDD, 0xDF, 0xFF);
        messageBar.rectTransform.anchorMin = Vector2.zero; messageBar.rectTransform.anchorMax = new Vector2(1, 0);
        messageBar.rectTransform.offsetMin = new Vector2(33, 27); messageBar.rectTransform.offsetMax = new Vector2(-33, 87);
        var check = ReferenceTitleView.Picture(messageBar.transform, "IconCheck");
        check.color = DayLoopUI.Kit.SeaDeep; check.preserveAspect = true;
        DayLoopUI.Place(check.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(30, 30));
        message = DayLoopUI.Text(messageBar.transform, "", 21, DayLoopUI.Kit.SeaDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        message.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Stretch(message.rectTransform, 60, 0, 18, 0);
        messageBar.gameObject.SetActive(false);
    }

    // ───── 관리 묶음 (접고 펼치기) ─────

    /// <summary>왼쪽 띠를 지금 탭에 맞게 다시 그린다. 펼친 묶음 하나만 항목을 보여 준다.</summary>
    private void PaintTabs()
    {
        if (railGroups == null) return;
        foreach (Transform child in railGroups) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        foreach (var group in Groups)
        {
            if (group.name == expandedGroup) ExpandedGroup(group.name, group.summary, group.icon, group.sections);
            else CollapsedGroup(group.name, group.summary, group.icon, group.sections);
        }
    }

    private void ToggleGroup(string name, string[] sections)
    {
        if (expandedGroup == name) { expandedGroup = null; PaintTabs(); return; }
        expandedGroup = name;
        if (sections.Contains(tab)) PaintTabs();
        else Open(sections[0]);
    }

    private void CollapsedGroup(string name, string summary, string icon, string[] sections)
    {
        bool current = sections.Contains(tab);
        var button = DayLoopUI.KitButton(railGroups, "", DayLoopUI.Kit.WoodLight, Color.white, 28, () => ToggleGroup(name, sections), name);
        button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        DayLoopUI.Layout(button, preferredHeight: 93);
        if (current) button.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Awning;
        GroupHeading(button.transform, name, summary, icon, "ChevronDown", Color.white, new Color32(0xFF, 0xE9, 0xDF, 0xFF));
    }

    private void ExpandedGroup(string name, string summary, string icon, string[] sections)
    {
        var card = DayLoopUI.Card(railGroups, DayLoopUI.Kit.Sail, name, 4.5f, 6f);
        card.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        var layout = DayLoopUI.Vertical(card, 0, 9);
        layout.padding = new RectOffset(12, 12, 9, 15);

        // 펼친 묶음 머리(66): 아이콘 + 제목 + 접기 화살표. 설명은 본문 제목 아래에 쓴다.
        var header = DayLoopUI.Button(card.transform, "", Color.clear, Color.clear, 10, () => ToggleGroup(name, sections), "Header");
        header.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        DayLoopUI.Layout(header, preferredHeight: 66);
        RailIcon(header.transform, icon, DayLoopUI.Kit.Sea, 36, 9);
        var title = DayLoopUI.Text(header.transform, name, 32, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Stretch(title.rectTransform, 60, 0, 44, 0);
        RailIcon(header.transform, "ChevronUp", DayLoopUI.Kit.Line, 27, -9);

        foreach (string section in sections) GroupItem(card.transform, section);
    }

    private static void GroupHeading(Transform parent, string name, string summary, string icon, string chevron, Color ink, Color soft, Color? iconColor = null)
    {
        // 접힌 묶음(93): 제목 30 + 설명 18 한 줄. 글자를 자동으로 줄이지 않는다(설명은 짧게 유지).
        RailIcon(parent, icon, iconColor ?? ink, 36, 18);
        var title = DayLoopUI.Text(parent, name, 30, ink, TextAlignmentOptions.BottomLeft, FontStyles.Normal, "Title");
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.rectTransform.anchorMin = new Vector2(0, .5f); title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = new Vector2(66, -2); title.rectTransform.offsetMax = new Vector2(-48, -10);
        var sub = DayLoopUI.Text(parent, summary, 18, soft, TextAlignmentOptions.TopLeft, FontStyles.Bold, "Summary");
        sub.textWrappingMode = TextWrappingModes.NoWrap;
        sub.rectTransform.anchorMin = Vector2.zero; sub.rectTransform.anchorMax = new Vector2(1, .5f);
        sub.rectTransform.offsetMin = new Vector2(66, 10); sub.rectTransform.offsetMax = new Vector2(-48, -4);
        RailIcon(parent, chevron, ink, 27, -18);
    }

    private void GroupItem(Transform parent, string section)
    {
        bool selected = section == tab;
        Color ink = selected ? Color.white : DayLoopUI.Kit.Ink;
        var item = DayLoopUI.Button(parent, "", selected ? DayLoopUI.Kit.Sea : Color.white, Color.clear, 10, () => Open(section), section);
        DayLoopUI.Chunky(item.image, 2f, 0f);
        item.GetComponent<Outline>().effectColor = selected ? DayLoopUI.Kit.SeaDeep : DayLoopUI.Kit.Divider;
        item.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        // 항목(66): 아이콘 + 이름 한 줄. 설명은 본문 제목 아래에 쓴다.
        DayLoopUI.Layout(item, preferredHeight: 66);
        RailIcon(item.transform, TabIcon(section), selected ? Color.white : DayLoopUI.Kit.Wood, 30, 18);
        var title = DayLoopUI.Text(item.transform, TabLabel(section), 27, ink, TextAlignmentOptions.MidlineLeft);
        title.font = DesignFonts.Heading; title.fontSharedMaterial = title.font.material;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Stretch(title.rectTransform, 63, 0, 15, 0);
    }

    /// <summary>선 아이콘. x &gt; 0이면 왼쪽에서, x &lt; 0이면 오른쪽에서 띄우고, 0이면 가운데에 둔다.</summary>
    private static Image RailIcon(Transform parent, string icon, Color color, float size, float x)
    {
        var image = ReferenceTitleView.Picture(parent, "Icon" + icon);
        image.color = color; image.preserveAspect = true;
        float anchor = x > 0 ? 0f : x < 0 ? 1f : .5f;
        DayLoopUI.Place(image.rectTransform, new Vector2(anchor, .5f), new Vector2(anchor, .5f), new Vector2(x, 0), new Vector2(size, size));
        return image;
    }

    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    private bool PayrollPending => GameSession.IsActive && GameSession.Current.progression.unpaidEmployeeIds.Count > 0;
    private void Update()
    {
        if (window == null) return;
        bool hasMessage = !string.IsNullOrEmpty(message.text);
        if (messageBar.gameObject.activeSelf != hasMessage) messageBar.gameObject.SetActive(hasMessage);
        if (PayrollPending && (tab != "임금 정산" || !window.gameObject.activeSelf)) Open("임금 정산");
        bool usable = RestaurantProgress.CanManage || GameFlow.Phase == GamePhase.Operation;
        bool besideHud = RestaurantHud.Current != null && RestaurantHud.Current.IsShowing;
        foreach (var opener in openers) { opener.interactable = usable; PlaceOpener(opener, besideHud); }
        if (!RestaurantProgress.CanManage && GameFlow.Phase != GamePhase.Operation && window.gameObject.activeSelf) { window.gameObject.SetActive(false); StopPractice(); }
    }

    public void Open(string section)
    {
        if (window == null || (!RestaurantProgress.CanManage && !(GameFlow.Phase == GamePhase.Operation && section == "메뉴"))) return;
        if (PayrollPending) section = "임금 정산";
        string group = GroupOf(section);
        if (group != null) expandedGroup = group;
        tab = section; page = 0; selectedId = -1; selectedEmployee = null; filter = "전체"; StopPractice(); message.text = "";
        window.gameObject.SetActive(true); Refresh();
    }
    private void Clear()
    {
        StopPractice();
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var s = GameSession.Current;
        title.text = tab == "메뉴" ? "오늘의 메뉴" : tab == "재료" ? "재료 창고" : TabLabel(tab);
        string actionLabel = tab == "메뉴" ? "자동 편성" : tab == "재료" ? "상점에서 채우기" : tab == "직원" ? "직원 고용" : tab == "가구" ? "자리 배치 열기" : tab == "식당" ? "식당 증축" : tab == "연구" ? "연구소 가기" : tab == "집" ? "집으로 가기" : null;
        headerAction.gameObject.SetActive(actionLabel != null);
        if (actionLabel != null) DayLoopUI.SetButtonLabel(headerAction, actionLabel);
        info.text = $"{s.day}일차  ·  식당 Lv.{s.restaurantLevel}  ·  <color={DayLoopUI.Hex(DayLoopUI.Kit.GoldInk)}>{RestaurantProgress.Gold(GameSession.CaptureSnapshot()):N0}G</color>";
        PaintTabs();
    }
    private void Note(string value)
    {
        var text = DayLoopUI.Text(content, value, 23, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, preferredHeight: value.Contains("\n") ? 80 : 52);
    }
    private RectTransform Row(string label, params (string label, Action action)[] actions)
    {
        bool twoLines = label.Contains("\n");
        var row = DayLoopUI.Chunky(DayLoopUI.Panel(content, Color.white), 2f, 0f);
        DayLoopUI.Layout(row, preferredHeight: twoLines ? 90 : 72);
        var layout = DayLoopUI.Horizontal(row, 0, 10);
        layout.padding = new RectOffset(20, 14, 9, 13);
        var text = DayLoopUI.Text(row.transform, label, 23, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(text, flexibleWidth: 1, preferredHeight: twoLines ? 68 : 50);
        text.enableAutoSizing = true; text.fontSizeMin = 17; text.fontSizeMax = 23;
        foreach (var action in actions) ActionButton(row.transform, action.label, action.action, 135, 50);
        return row.rectTransform;
    }
    private Button ActionButton(Transform parent, string label, Action action, float width, float height)
    {
        bool destructive = DestructiveActions.Contains(label);
        var b = DayLoopUI.KitButton(parent, label, destructive ? DayLoopUI.Kit.Sand : DayLoopUI.Kit.Sea, destructive ? DayLoopUI.Kit.WarnInk : Color.white, 21, () => action(), label);
        DayLoopUI.Layout(b, preferredWidth: width, preferredHeight: height);
        return b;
    }
    private delegate bool Command(out string error);
    private void Run(Command action)
    {
        bool ok = action(out string error);
        Refresh(); message.text = ok ? "반영했어요." : error;
    }
    private void Pages(int total, int pageSize = 6)
    {
        int last = Mathf.Max(0, (total - 1) / pageSize);
        Row($"{page + 1} / {last + 1} 페이지", ("이전", () => { page = Mathf.Max(0, page - 1); Refresh(); }), ("다음", () => { page = Mathf.Min(last, page + 1); Refresh(); }));
    }
    private void Refresh()
    {
        if (!GameSession.IsActive) return;
        Clear();
        var s = GameSession.Current;
        var inventory = IngredientInventoryService.Instance;
        switch (tab)
        {
            case "메뉴":
                DesignedMenu(s); break;
            case "재료":
                DesignedIngredients(s); break;
            case "상점":
                DesignedShop(s); break;
            case "연구":
                DesignedResearch(s); break;
            case "엘프":
                DesignedElf(s); break;
            case "임금 정산":
                Note($"임금이 부족해요. 해고할 직원을 직접 선택한 뒤 남은 임금을 정산하세요.\n필요 {s.employees.Sum(e => e.wage):N0}G / 보유 {RestaurantProgress.Gold(s):N0}G · 미지급 해고 1명당 평점 -0.1");
                foreach (var employee in s.employees.Skip(page * 5).Take(5))
                {
                    string id = employee.instanceId;
                    Row($"{employee.name} [{employee.grade}] · 주급 {employee.wage:N0}G", ("해고", () => Run((out string e) => RestaurantProgress.TryDismiss(id, out e))));
                }
                Row("남은 직원 임금 정산", ("정산", () => { if (RestaurantProgress.TryPayWages(out string e)) Open("집"); else message.text = e; }));
                Pages(s.employees.Count, 5); break;
            case "직원":
                DesignedStaff(s); break;
            case "식당":
                DesignedTools(s); break;
            case "대장간":
                BlacksmithTab(s);
                break;
            case "주민":
                DesignedExplore(s); break;
            case "가구":
                DesignedFurniture(s); break;
            case "집":
                DesignedHome(s); break;
            case "설정":
                Row("소리·화면 설정", ("열기", () => SettingsWindow.Open(gameObject.scene)));
                Row("타이틀로 돌아가면 오늘의 저장하지 않은 진행을 잃어요.", ("돌아가기", () => { Clear(); Note("마지막 취침 이후 진행을 버리고 타이틀로 돌아갈까요?"); Row("", ("돌아가기", GameFlow.ReturnToTitle), ("계속하기", Refresh)); }));
                break;
        }
        // 묶음 띠에서 뺀 항목 설명은 본문 제목 아래 한 줄로 보여 준다.
        string hint = TabHint(tab);
        if (!string.IsNullOrEmpty(hint)) info.text = string.IsNullOrEmpty(info.text) ? hint : $"{hint}  ·  {info.text}";
    }
    private void ConfirmCancelResearch(int id)
    {
        ManagementDialog.Show(canvas.transform, "정말 연구를 취소하시겠습니까?\n투입된 식재료는 모두 돌려받지만, 펼쳐둔 레시피 도안은 파손되어 사라집니다.", () => {
            if (RestaurantProgress.TryCancelResearch(id, out string error))
            { Refresh(); message.text = "도안이 파손되고 식재료가 가방으로 반환되었습니다."; ManagementDialog.RefundEffect(canvas.transform); }
            else message.text = error;
        });
    }

    // ───── 메뉴 탭 ─────

    private void MenuSlots(GameSaveData s)
    {
        var strip = DayLoopUI.Chunky(DayLoopUI.Panel(content, DayLoopUI.Kit.Sand, "Slots"), 3f, 0f);
        DayLoopUI.Layout(strip, preferredHeight: 99);
        var layout = DayLoopUI.Horizontal(strip, 0, 15);
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childForceExpandWidth = true;
        foreach (int id in s.menuSlots)
        {
            if (id >= 0 && GameDatabase.TryGetRecipe(id, out var recipe))
            {
                var cell = DayLoopUI.Chunky(DayLoopUI.Panel(strip.transform, Color.white, "Slot"), 2f, 0f);
                DayLoopUI.Layout(cell, flexibleWidth: 1, preferredHeight: 72);
                var row = DayLoopUI.Horizontal(cell, 0, 8);
                row.padding = new RectOffset(10, 10, 6, 6);
                DayLoopUI.FoodIcon(cell.transform, recipe, 54);
                DayLoopUI.Layout(DayLoopUI.Text(cell.transform, recipe.RecipeName, 20, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 48);
            }
            else
            {
                var empty = DayLoopUI.Panel(strip.transform, new Color(1, 1, 1, .35f), "EmptySlot");
                DayLoopUI.Layout(empty, flexibleWidth: 1, preferredHeight: 72);
                var label = DayLoopUI.Text(empty.transform, "빈 슬롯", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
                DayLoopUI.Stretch(label.rectTransform);
            }
        }
    }

    // ───── 대장간 탭 ─────

    private static readonly string[] ToolNames = { "프라이팬", "도마", "튀김기", "냄비" };

    private void BlacksmithTab(GameSaveData s)
    {
        int level = s.restaurantLevel;
        int pending = s.progression.pendingRestaurantLevel;
        bool isMax = level >= RestaurantRules.MaxLevel;
        int next = Mathf.Min(level + 1, RestaurantRules.MaxLevel);

        var top = DayLoopUI.Rect("Restaurant level", content);
        DayLoopUI.Layout(top, preferredHeight: 338);
        var topLayout = DayLoopUI.Horizontal(top, 0, 18);
        topLayout.childForceExpandHeight = true;
        topLayout.padding = new RectOffset(0, 0, 0, 8);

        // 왼쪽: 현재 → 다음 레벨, 강화 조건, 강화 버튼, 공사 안내
        var upgrade = BlacksmithCard(top, "식당 레벨 강화", 560);
        var levels = DayLoopUI.Text(upgrade, isMax ? $"Lv.{level}  ·  최고 레벨" : pending > 0 ? $"Lv.{level}  →  Lv.{pending}  공사 중" : $"Lv.{level}  →  Lv.{next}",
            36, pending > 0 ? DayLoopUI.Kit.SeaDeep : DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(levels, preferredHeight: 46);

        if (pending > 0)
        {
            bool closedToday = RestaurantRules.IsClosedForConstruction(s);
            string when = closedToday
                ? $"오늘은 공사로 휴업해요. 집에서 잠들면 {s.progression.constructionUntilDay}일차 아침에 Lv.{pending} 식당이 문을 열어요."
                : $"내일({s.day + 1}일차) 하루 공사로 휴업하고, {s.progression.constructionUntilDay}일차 아침에 Lv.{pending} 식당이 문을 열어요.";
            var status = DayLoopUI.Text(upgrade, when, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DayLoopUI.Layout(status, preferredHeight: 96);
        }
        else if (!isMax)
        {
            float rating = Mathf.Round(RestaurantRules.RestaurantRating(s) * 10f) / 10f;
            float needRating = RestaurantRules.UpgradeRating(next);
            int gold = RestaurantProgress.Gold(GameSession.CaptureSnapshot());
            int cost = RestaurantRules.UpgradeCost(next);
            bool ratingMet = rating >= needRating, goldMet = gold >= cost;
            ConditionRow(upgrade, "식당 평점", $"{rating:0.0} / {needRating:0.0}", ratingMet);
            ConditionRow(upgrade, "공사 비용", $"{gold:N0} / {cost:N0}G", goldMet);
            DayLoopUI.Spacer(upgrade);
            var button = DayLoopUI.KitButton(upgrade, $"Lv.{next}로 강화", ratingMet && goldMet ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand,
                ratingMet && goldMet ? Color.white : DayLoopUI.Kit.InkSoft, 28, () => Run(RestaurantProgress.TryUpgrade), "Upgrade");
            DayLoopUI.Layout(button, preferredHeight: 56);
            string closed = s.progression.freeTime ? "지금 맡기면 내일 하루 쉬어요" : "지금 맡기면 오늘 하루 쉬어요";
            var note = DayLoopUI.Text(upgrade, $"공사 기간 {RestaurantRules.ConstructionDays(next)}일 (휴업) · {closed}", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            DayLoopUI.Layout(note, preferredHeight: 26);
        }
        else
        {
            DayLoopUI.Layout(DayLoopUI.Text(upgrade, "모든 확장을 마쳤어요.", 22, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 40);
        }

        // 오른쪽: 레벨 혜택 (현재 / 다음)
        int target = pending > 0 ? pending : next;
        bool hasNext = pending > 0 || !isMax;
        var benefits = BlacksmithCard(top, "레벨 혜택", -1);
        BenefitRow(benefits, "", $"현재 Lv.{level}", hasNext ? $"다음 Lv.{target}" : "-", true);
        BenefitRow(benefits, "손님 좌석", $"{RestaurantRules.SeatLimit(level)}개", hasNext ? Delta(RestaurantRules.SeatLimit(level), RestaurantRules.SeatLimit(target), "개") : "-");
        BenefitRow(benefits, "요리 공간", $"{RestaurantRules.CookingSpace(level)}개", hasNext ? Delta(RestaurantRules.CookingSpace(level), RestaurantRules.CookingSpace(target), "개") : "-");
        BenefitRow(benefits, "대기열", $"{RestaurantRules.WaitingLimit(level)}명", hasNext ? Delta(RestaurantRules.WaitingLimit(level), RestaurantRules.WaitingLimit(target), "명") : "-");
        BenefitRow(benefits, "레시피 등급", RestaurantRules.GradeRange(level), hasNext ? RestaurantRules.GradeRange(target) : "-");
        BenefitRow(benefits, "하루 방문 손님", $"{RestaurantRules.DailyVisitors(level)}명", hasNext ? Delta(RestaurantRules.DailyVisitors(level), RestaurantRules.DailyVisitors(target), "명") : "-");

        // 증축 화면에는 증축만 둔다. 조리도구 구매·강화는 조리도구 화면 한 곳에서 한다.
        Row($"조리도구 {s.progression.tools.Count} / 4 보유 · 구매와 강화는 조리도구 화면에서 해요.", ("조리도구", () => Open("식당")));
    }

    private static RectTransform BlacksmithCard(Transform parent, string heading, float width)
    {
        var card = DayLoopUI.Chunky(DayLoopUI.Panel(parent, DayLoopUI.Kit.Sail, heading), 3f, 5f);
        if (width > 0) DayLoopUI.Layout(card, preferredWidth: width);
        else DayLoopUI.Layout(card, flexibleWidth: 1);
        var layout = DayLoopUI.Vertical(card, 0, 6);
        layout.padding = new RectOffset(24, 24, 16, 18);
        DayLoopUI.Layout(DayLoopUI.Text(card.transform, heading, 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 36);
        return card.rectTransform;
    }

    private static void ConditionRow(Transform parent, string label, string value, bool met)
    {
        var row = DayLoopUI.Chunky(DayLoopUI.Panel(parent, Color.white, label), 2f, 0f);
        DayLoopUI.Layout(row, preferredHeight: 46);
        var layout = DayLoopUI.Horizontal(row, 0, 12);
        layout.padding = new RectOffset(16, 12, 6, 6);
        DayLoopUI.Layout(DayLoopUI.Text(row.transform, label, 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 40);
        DayLoopUI.Layout(DayLoopUI.Text(row.transform, value, 22, met ? DayLoopUI.Kit.Ink : DayLoopUI.Kit.WarnInk, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 220, preferredHeight: 40);
        DayLoopUI.Chip(row.transform, met ? "충족" : "부족", met ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sand, met ? Color.white : DayLoopUI.Kit.WarnInk, 18);
    }

    private static void BenefitRow(Transform parent, string label, string current, string next, bool header = false)
    {
        var row = DayLoopUI.Rect(string.IsNullOrEmpty(label) ? "Header" : label, parent);
        DayLoopUI.Horizontal(row, 0, 10);
        DayLoopUI.Layout(row, preferredHeight: header ? 30 : 34);
        Color soft = DayLoopUI.Kit.InkSoft;
        DayLoopUI.Layout(DayLoopUI.Text(row, label, 22, soft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 34);
        DayLoopUI.Layout(DayLoopUI.Text(row, current, header ? 19 : 22, header ? soft : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 150, preferredHeight: 34);
        DayLoopUI.Layout(DayLoopUI.Text(row, next, header ? 19 : 22, header ? soft : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 210, preferredHeight: 34);
    }

    /// <summary>"16개 (+4)" 형식. 늘지 않으면 값만.</summary>
    private static string Delta(int current, int next, string unit)
    {
        int delta = next - current;
        return delta > 0 ? $"{next}{unit} <color={DayLoopUI.Hex(DayLoopUI.Kit.GoodInk)}>(+{delta})</color>" : $"{next}{unit}";
    }

    // ───── 집 탭 ─────

    private void HouseTab(GameSaveData s)
    {
        Mission(s, out string missionName, out int target);
        int gold = RestaurantProgress.Gold(GameSession.CaptureSnapshot());
        int wages = s.employees.Sum(e => e.wage);
        int daysToWage = (7 - s.day % 7) % 7;

        var top = DayLoopUI.Rect("Top", content);
        DayLoopUI.Layout(top, preferredHeight: 214);
        var topLayout = DayLoopUI.Horizontal(top, 0, 18);
        topLayout.childForceExpandWidth = true;
        topLayout.childForceExpandHeight = true;
        topLayout.padding = new RectOffset(0, 0, 0, 8);

        var mission = HouseCard(top, $"미션 · {missionName}");
        var progressRow = DayLoopUI.Rect("Progress", mission);
        DayLoopUI.Horizontal(progressRow, 0, 10);
        DayLoopUI.Layout(progressRow, preferredHeight: 30);
        DayLoopUI.Layout(DayLoopUI.Text(progressRow, "진행", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        bool done = s.progression.missionProgress >= target;
        DayLoopUI.Layout(DayLoopUI.Text(progressRow, $"{s.progression.missionProgress:N0} / {target:N0}", 22, done ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 220);
        var bar = DayLoopUI.Bar(mission, target > 0 ? (float)s.progression.missionProgress / target : 0f, done ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sea);
        DayLoopUI.Layout(bar.transform.parent, preferredHeight: 16);
        DayLoopUI.Spacer(mission);
        var claim = DayLoopUI.KitButton(mission, $"보상 받기 · {200 + s.progression.missionIndex * 100:N0}G", done ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand, done ? Color.white : DayLoopUI.Kit.InkSoft, 23, () => Run(RestaurantProgress.TryClaimMission), "Claim");
        DayLoopUI.Layout(claim, preferredHeight: 54);

        var wage = HouseCard(top, "다음 주급");
        var amountRow = DayLoopUI.Rect("Amount", wage);
        DayLoopUI.Horizontal(amountRow, 0, 12, TextAnchor.LowerLeft);
        DayLoopUI.Layout(amountRow, preferredHeight: 52);
        var amount = DayLoopUI.Text(amountRow, $"{wages:N0}G", 44, DayLoopUI.Kit.GoldInk, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
        amount.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(amount, preferredHeight: 52);
        DayLoopUI.Layout(DayLoopUI.Text(amountRow, (daysToWage == 0 ? "오늘 밤 지급" : $"{daysToWage}일 뒤") + " · 7일마다", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.BottomLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 44);
        bool enough = gold >= wages;
        var wageNote = DayLoopUI.Text(wage, enough ? "보유 골드로 충분해요" : $"골드가 {wages - gold:N0}G 부족해요", 21, enough ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.WarnInk, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(wageNote, preferredHeight: 32);

        // 직원 체력
        var staff = DayLoopUI.Chunky(DayLoopUI.Panel(content, DayLoopUI.Kit.Sail, "Staff"), 3f, 0f);
        int shown = Mathf.Min(4, s.employees.Count);
        DayLoopUI.Layout(staff, preferredHeight: 86 + Mathf.Max(1, shown) * 46 + (s.employees.Count > shown ? 30 : 0));
        var staffLayout = DayLoopUI.Vertical(staff, 0, 8);
        staffLayout.padding = new RectOffset(24, 24, 16, 16);
        var staffHeader = DayLoopUI.Rect("Header", staff.transform);
        DayLoopUI.Horizontal(staffHeader, 0, 10);
        DayLoopUI.Layout(staffHeader, preferredHeight: 36);
        DayLoopUI.Layout(DayLoopUI.Text(staffHeader, "직원 체력", 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
        DayLoopUI.Layout(DayLoopUI.Text(staffHeader, "자고 나면 모두 회복해요", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 360);
        if (s.employees.Count == 0)
            DayLoopUI.Layout(DayLoopUI.Text(staff.transform, "아직 직원이 없어요. 직원 탭에서 스카우트할 수 있어요.", 21, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 38);
        foreach (var e in s.employees.Take(shown))
        {
            var row = DayLoopUI.Rect("Employee", staff.transform);
            DayLoopUI.Horizontal(row, 0, 14);
            DayLoopUI.Layout(row, preferredHeight: 38);
            DayLoopUI.Layout(DayLoopUI.Text(row, $"{e.name} <color={DayLoopUI.Hex(DayLoopUI.Kit.InkSoft)}>· {RoleLabel(e.role)} · {e.grade}등급</color>", 21, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1);
            float ratio = e.stamina < 0 ? 1f : Mathf.Clamp01(e.stamina / Mathf.Max(1f, e.hp));
            var stamina = DayLoopUI.Bar(row, ratio, ratio > .6f ? DayLoopUI.Kit.Kelp : ratio > .3f ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Alert);
            DayLoopUI.Layout(stamina.transform.parent, preferredWidth: 300, preferredHeight: 16);
            DayLoopUI.Layout(DayLoopUI.Text(row, $"{Mathf.RoundToInt(ratio * 100)}%", 20, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineRight, FontStyles.Bold), preferredWidth: 64);
        }
        if (s.employees.Count > shown)
            DayLoopUI.Layout(DayLoopUI.Text(staff.transform, $"외 {s.employees.Count - shown}명", 19, DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 24);

        // 취침
        var bottom = DayLoopUI.Rect("Sleep", content);
        DayLoopUI.Horizontal(bottom, 0, 20);
        DayLoopUI.Layout(bottom, preferredHeight: 104);
        string status = s.progression.freeTime ? "오늘 영업을 마쳤어요. 잠들면 지급일에 주급을 지급하고 다음 날로 넘어가요." : "영업 후 또는 공사 중에 잠들 수 있어요.";
        DayLoopUI.Layout(DayLoopUI.Text(bottom, $"{status}\n<size=85%><color={DayLoopUI.Hex(DayLoopUI.Kit.InkSoft)}>파일 저장은 취침할 때 이루어져요. 그전에 종료하면 마지막으로 저장한 아침으로 돌아가요.</color></size>", 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), flexibleWidth: 1, preferredHeight: 96);
        var sleep = DayLoopUI.KitButton(bottom, $"취침 · 저장\n<size=60%>{s.day + 1}일차 아침으로</size>", DayLoopUI.Kit.Night, Color.white, 32, () => { if (!RestaurantProgress.TrySleep(out string error)) message.text = error; }, "Sleep");
        DayLoopUI.Layout(sleep, preferredWidth: 340, preferredHeight: 92);
    }

    /// <summary>현재 미션 이름과 목표치. (보상 조건은 RestaurantProgress.TryClaimMission과 같다)</summary>
    public static void Mission(GameSaveData s, out string name, out int target)
    {
        int type = s.progression.missionIndex % 3;
        name = new[] { "손님 대접", "매출 달성", "새 요리 연구" }[type];
        target = type == 1 ? RestaurantProgress.MissionTarget * 100 : type == 2 ? 1 : RestaurantProgress.MissionTarget;
    }

    private static RectTransform HouseCard(Transform parent, string heading)
    {
        var card = DayLoopUI.Chunky(DayLoopUI.Panel(parent, DayLoopUI.Kit.Sail, heading), 3f, 5f);
        var layout = DayLoopUI.Vertical(card, 0, 8);
        layout.padding = new RectOffset(24, 24, 16, 18);
        DayLoopUI.Layout(DayLoopUI.Text(card.transform, heading, 27, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft, FontStyles.Bold), preferredHeight: 38);
        return card.rectTransform;
    }

    private static string RoleLabel(string role) => role == nameof(PartTimerRole.Serving) ? "홀" : role == nameof(PartTimerRole.Kitchen) ? "주방" : "대기";

    private void PlaceTable(int seats)
    {
        if (TablePlacementManager.Instance == null)
        {
            pendingPlacement = seats;
            if (!GameFlow.TryEnterRestaurant()) { pendingPlacement = 0; message.text = "오늘은 식당에 들어갈 수 없어요."; }
            return;
        }
        window.gameObject.SetActive(false);
        var placement = TablePlacementManager.Instance;
        PreOperationUIManager.Instance?.HideUI();
        CameraController.Instance?.MoveToZone(CameraZone.Restaurant);
        placement.EnterPlacementMode();
        if (seats == 1) placement.OnOneSeatButtonClicked();
        else if (seats == 2) placement.OnTwoSeatButtonClicked();
        else placement.OnFourSeatButtonClicked();
    }
    private System.Collections.IEnumerator PlaceAfterStart(int seats) { yield return null; PlaceTable(seats); }
    private void HeaderAction()
    {
        if (tab == "메뉴") Run(RestaurantProgress.TryAutoMenu);
        else if (tab == "재료") Open("상점");
        else if (tab == "직원") OpenRecruitment();
        else if (tab == "가구") OpenPlacement();
        else if (tab == "식당") Open("대장간");
        else if (tab == "집") { CloseWindow(); ReferenceHouseView.Open(gameObject.scene); }
        else if (tab == "연구")
        {
            var ready = GameSession.Current.progression.research.Find(x => x.remainingDays <= 0);
            if (ready != null) BeginChallenge(ready.recipeId); else message.text = "연구할 요리를 선택하고 재료를 예약해 주세요.";
        }
    }
    private void StopPractice()
    {
        if (practiceRoot != null) { practiceRoot.gameObject.SetActive(false); Destroy(practiceRoot.gameObject); practiceRoot = null; }
        if (practice == null) return;
        practice.enabled = false;
        Destroy(practice);
        practice = null;
    }
    private void BeginChallenge(int id)
    {
        if (!RestaurantProgress.CanResearch) { message.text = "실습은 자유시간에 할 수 있어요."; return; }
        var research = GameSession.Current.progression.research.Find(x => x.recipeId == id);
        if (research == null || research.remainingDays > 0) { message.text = "연구 시간이 남았어요."; return; }
        if (!GameDatabase.TryGetRecipe(id, out var recipe)) return;
        Clear();
        window.gameObject.SetActive(false);
        practiceRoot = DayLoopUI.Rect("ResearchPractice", canvas.transform);
        DayLoopUI.Stretch(practiceRoot);
        var background = DayLoopUI.Panel(practiceRoot, new Color32(0xF3, 0xE3, 0xC6, 255), "Backdrop", false);
        DayLoopUI.Stretch(background.rectTransform);
        var counter = DayLoopUI.Panel(practiceRoot, DayLoopUI.Kit.WoodLight, "Counter", false);
        DayLoopUI.Place(counter.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1920, 255));
        var counterEdge = DayLoopUI.Panel(practiceRoot, new Color32(0xB9, 0x82, 0x4F, 255), "CounterEdge", false);
        DayLoopUI.Place(counterEdge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0, 225), new Vector2(1920, 30));
        var chef = ReferenceTitleView.Picture(practiceRoot, "OtterChef");
        ReferenceTitleView.Place(chef.rectTransform, 705, 555, 315, 315);
        var rail = DayLoopUI.ScreenRail(practiceRoot, "ResearchRail");
        var heading = DayLoopUI.Card(rail, DayLoopUI.Kit.Sail, "ResearchMenu");
        DayLoopUI.Layout(heading, preferredHeight: 115); DayLoopUI.Vertical(heading, 18, 9);
        Copy(heading.transform, "연구 실습", 28, 21, DayLoopUI.Kit.Sea);
        Copy(heading.transform, recipe.RecipeName + " ★" + recipe.RecipeGrade, 42, 36);
        var steps = DayLoopUI.Rect("Steps", rail);
        DayLoopUI.Layout(steps, preferredHeight: 600); DayLoopUI.Vertical(steps, 0, 14);
        DayLoopUI.Spacer(rail);
        DayLoopUI.RailButton(rail, "닫기 · 예약 유지", () => { StopPractice(); window.gameObject.SetActive(true); Refresh(); });
        var stage = DayLoopUI.Rect("Stage", practiceRoot);
        DayLoopUI.Place(stage, new Vector2(0, 1), new Vector2(0, 1), new Vector2(465, -45), new Vector2(870, 480));
        DayLoopUI.Vertical(stage, 0, 0);
        var score = DayLoopUI.Card(practiceRoot, Color.white, "Score");
        DayLoopUI.Place(score.rectTransform, Vector2.one, Vector2.one, new Vector2(-45, -45), new Vector2(510, 0));
        DayLoopUI.Vertical(score, 33, 18);
        score.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        practice = gameObject.AddComponent<ResearchPractice>();
        practice.Begin(stage, recipe, average => {
            bool ok = RestaurantProgress.TryCompleteResearch(id, average, out string error);
            window.gameObject.SetActive(true); Refresh();
            message.text = $"실습 평균 {average:0.0}/5점 · " + (ok ? "새 요리를 배웠어요." : error);
        }, steps, score.transform);
    }
}
