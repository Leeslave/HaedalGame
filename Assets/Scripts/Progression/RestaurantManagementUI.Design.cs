using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 시안의 공통 탐색 띠 + 목록/상세 패널. 값과 거래는 기존 게임 서비스를 사용한다.
public partial class RestaurantManagementUI
{
    private string filter = "전체";
    private int selectedId = -1;
    private string selectedEmployee;
    private readonly Dictionary<int, int> cart = new Dictionary<int, int>();
    private int cartDay = -1;
    private readonly HashSet<string> expeditionStaff = new HashSet<string>();

    public static void OpenSection(string section)
    {
        var ui = FindFirstObjectByType<RestaurantManagementUI>();
        if (ui != null) ui.Open(section);
    }

    private void CloseWindow()
    {
        if (PayrollPending) return;
        window.gameObject.SetActive(false);
        StopPractice();
    }

    private static TMP_Text Copy(Transform parent, string text, float height = 42, float size = 23, Color? color = null)
    {
        var label = DayLoopUI.Text(parent, text, size, color ?? DayLoopUI.Kit.Ink, style: FontStyles.Bold);
        DayLoopUI.Layout(label, preferredHeight: height);
        return label;
    }

    private static RectTransform ColumnBody(Transform parent, string heading)
    {
        var panel = DayLoopUI.Card(parent, Color.white, heading, 3, 3);
        DayLoopUI.Layout(panel, flexibleWidth: 1, flexibleHeight: 1);
        DayLoopUI.Vertical(panel, 24, 14);
        if (!string.IsNullOrEmpty(heading)) Copy(panel.transform, heading, 48, 30);
        return ScrollList(panel.transform);
    }

    private RectTransform Split(out RectTransform detail, float height = 710)
    {
        var split = DayLoopUI.Rect("ListAndDetail", content);
        DayLoopUI.Layout(split, preferredHeight: height);
        var row = DayLoopUI.Horizontal(split, 0, 24);
        row.childForceExpandHeight = true;
        var list = DayLoopUI.Rect("List", split);
        DayLoopUI.Layout(list, preferredWidth: 933, flexibleWidth: 0, flexibleHeight: 1).minWidth = 933;
        DayLoopUI.Vertical(list, 0, 14);
        detail = ColumnBody(split, "");
        var detailPanel = detail.parent.parent.parent;
        DayLoopUI.Layout(detailPanel.GetComponent<RectTransform>(), preferredWidth: 405, flexibleWidth: 0, flexibleHeight: 1).minWidth = 0;
        return list;
    }

    private static RectTransform CardGrid(Transform parent, int columns = 4, float cellHeight = 240, int rows = 2)
    {
        var grid = DayLoopUI.Rect("ReferenceCards", parent);
        DayLoopUI.Layout(grid, preferredHeight: rows * cellHeight + (rows - 1) * 21 + 12);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2((933 - (columns - 1) * 18) / columns, cellHeight);
        layout.spacing = new Vector2(18, 21); layout.padding = new RectOffset(0, 0, 12, 0);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = columns;
        return grid;
    }

    private Button Tile(Transform grid, string name, string meta, bool selected, Action action, Sprite art = null, string flag = null)
    {
        var button = DayLoopUI.KitButton(grid, "", selected ? DayLoopUI.Kit.SeaTint : Color.white, DayLoopUI.Kit.Ink, 22.5f, () => action(), name);
        button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        if (selected) button.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        var body = DayLoopUI.Rect("TileBody", button.transform); DayLoopUI.Stretch(body, 15, 18, 15, 18);
        DayLoopUI.Vertical(body, 0, 5, TextAnchor.UpperCenter);
        var imageBox = DayLoopUI.Rect("Image", body); DayLoopUI.Layout(imageBox, preferredHeight: 108);
        var image = DayLoopUI.Panel(imageBox, art != null ? Color.white : DayLoopUI.Kit.Sand, "Art");
        image.sprite = art != null ? art : DayLoopUI.Circle;
        if (art != null && art.name.StartsWith("Design Icon")) image.color = DayLoopUI.Kit.Wood; image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = false;
        DayLoopUI.Place(image.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(108, 108));
        if (art == null)
        {
            var initial = DayLoopUI.Text(image.transform, name.Substring(0, 1), 39, DayLoopUI.Kit.Wood, TextAlignmentOptions.Center);
            initial.font = DesignFonts.Heading; initial.fontSharedMaterial = initial.font.material;
            DayLoopUI.Stretch(initial.rectTransform);
        }
        Copy(body, name, 34, 22.5f).alignment = TextAlignmentOptions.Center;
        Copy(body, meta, 54, 18, DayLoopUI.Kit.InkSoft).alignment = TextAlignmentOptions.Top;
        if (!string.IsNullOrEmpty(flag))
        {
            var chip = DayLoopUI.Chip(button.transform, flag, selected ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand, selected ? Color.white : DayLoopUI.Kit.Ink, 19.5f);
            var rect = (RectTransform)chip.transform.parent;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            DayLoopUI.Place(rect, Vector2.one, Vector2.one, new Vector2(-12, -12), new Vector2(chip.GetPreferredValues(flag).x + 32, 33));
        }
        return button;
    }

    private void Filters(Transform parent, params string[] labels)
    {
        var row = DayLoopUI.Rect("Filters", parent);
        DayLoopUI.Layout(row, preferredHeight: 44);
        DayLoopUI.Horizontal(row, 0, 10);
        foreach (string label in labels)
        {
            string value = label;
            bool active = filter == value;
            var button = DayLoopUI.KitButton(row, value, active ? DayLoopUI.Kit.Line : DayLoopUI.Kit.Sail,
                active ? Color.white : DayLoopUI.Kit.Ink, 20, () => { filter = value; page = 0; Refresh(); });
            DayLoopUI.Layout(button, preferredWidth: Mathf.Max(80, value.Length * 22 + 28), preferredHeight: 38);
        }
    }

    private static RectTransform ScrollList(Transform parent)
    {
        var host = DayLoopUI.Rect("ScrollList", parent);
        DayLoopUI.Layout(host, flexibleHeight: 1, flexibleWidth: 1, minHeight: 60);
        var scroll = host.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 34;
        var viewport = DayLoopUI.Panel(host, Color.clear, "Viewport", false);
        DayLoopUI.Stretch(viewport.rectTransform);
        viewport.gameObject.AddComponent<RectMask2D>();
        var items = DayLoopUI.Rect("Items", viewport.transform);
        items.anchorMin = new Vector2(0, 1); items.anchorMax = Vector2.one; items.pivot = new Vector2(.5f, 1);
        items.sizeDelta = Vector2.zero;
        var layout = DayLoopUI.Vertical(items, 6, 14);
        layout.padding = new RectOffset(6, 10, 6, 10);
        items.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = items; scroll.viewport = viewport.rectTransform;
        return items;
    }

    private Button SelectCard(Transform parent, string heading, string meta, bool selected, Action action, Sprite sprite = null, float height = 110)
    {
        var button = DayLoopUI.KitButton(parent, "", selected ? DayLoopUI.Kit.SeaTint : Color.white,
            DayLoopUI.Kit.Ink, 24, () => action(), heading);
        if (selected) button.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        DayLoopUI.Layout(button, preferredHeight: height);
        // Empty button label has no layout responsibilities.
        button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        var row = DayLoopUI.Rect("Body", button.transform);
        DayLoopUI.Stretch(row, 18, 12, 18, 12);
        DayLoopUI.Horizontal(row, 0, 18);
        var icon = DayLoopUI.Panel(row, DayLoopUI.Kit.Sand, "Icon");
        icon.raycastTarget = false;
        DayLoopUI.Layout(icon, preferredWidth: 66, preferredHeight: 66);
        if (sprite != null) { icon.sprite = sprite; icon.type = Image.Type.Simple; icon.preserveAspect = true; icon.color = Color.white; }
        else
        {
            var initial = DayLoopUI.Text(icon.transform, heading.Length > 0 ? heading.Substring(0, 1) : "?", 30,
                DayLoopUI.Kit.WoodLight, TextAlignmentOptions.Center, FontStyles.Bold);
            DayLoopUI.Stretch(initial.rectTransform);
        }
        var texts = DayLoopUI.Rect("Labels", row);
        DayLoopUI.Layout(texts, flexibleWidth: 1, flexibleHeight: 1);
        DayLoopUI.Vertical(texts, 0, 2, TextAnchor.MiddleLeft);
        Copy(texts, heading, 36, 25);
        Copy(texts, meta, 36, 20, DayLoopUI.Kit.InkSoft);
        return button;
    }

    private void DetailAction(Transform parent, string label, Action action, bool primary = false, bool enabled = true)
    {
        var button = DayLoopUI.KitButton(parent, label, primary ? DayLoopUI.Kit.Awning : DayLoopUI.Kit.Sand,
            primary ? Color.white : DayLoopUI.Kit.Ink, 24, () => action());
        DayLoopUI.Layout(button, preferredHeight: 60);
        button.interactable = enabled;
    }

    private static void DetailIcon(Transform parent, RecipeData recipe)
    {
        var holder = DayLoopUI.Rect("Portrait", parent);
        DayLoopUI.Layout(holder, preferredHeight: 126);
        var icon = DayLoopUI.FoodIcon(holder, recipe, 110);
        DayLoopUI.Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(110, 110));
    }

    private static string GradeInventory(int id)
    {
        if (IngredientInventoryService.IsUnlimited(id)) return "무제한 양념";
        var stacks = GameSession.Current.progression.ingredientGrades.Where(x => x.ingredientId == id && x.amount > 0).OrderBy(x => x.grade);
        string text = string.Join("   ", stacks.Select(x => $"{"FEDCBA"[Mathf.Clamp(x.grade, 0, 5)]} {x.amount}"));
        return string.IsNullOrEmpty(text) ? "보유 없음" : text;
    }

    private void RecipeRequirements(Transform parent, RecipeData recipe)
    {
        Copy(parent, "필요한 재료", 32, 23, DayLoopUI.Kit.InkSoft);
        foreach (var requirement in recipe.Requirements.Take(5))
            Copy(parent, $"{requirement.IngredientName} ×{requirement.Amount}\n<size=80%>{GradeInventory(requirement.IngredientId)}</size>", 58, 22);
    }

    private void DesignedMenu(GameSaveData s)
    {
        info.text = $"{s.menuSlots.Count(x => x >= 0)} / {RestaurantRules.MenuLimit(s.restaurantLevel)} 슬롯 · 변경 간격 30초";
        MenuSlots(s);
        Filters(content, "전체", "만들 수 있음", "구이", "회 · 숙회", "찜 · 탕", "튀김", "연구 필요");
        var list = Split(out var detail, 639);
        var recipes = GameDatabase.Recipes.Recipes.Where(r =>
            filter == "전체" || filter == "만들 수 있음" && s.unlockedRecipeIds.Contains(r.RecipeId) && CookwareRules.CanCook(s, r) && IngredientInventoryService.Instance.CraftableCount(r) > 0 ||
            filter == "연구 필요" && !s.unlockedRecipeIds.Contains(r.RecipeId) ||
            filter == "구이" && r.ClassId == 201 || filter == "회 · 숙회" && r.ClassId == 202 ||
            filter == "찜 · 탕" && (r.ClassId == 204 || r.ClassId == 205) || filter == "튀김" && r.ClassId == 203).ToList();
        var grid = CardGrid(list);
        foreach (var recipe in recipes.Skip(page * 8).Take(8))
        {
            int id = recipe.RecipeId;
            bool unlocked = s.unlockedRecipeIds.Contains(id);
            int count = IngredientInventoryService.Instance.CraftableCount(recipe);
            string state = s.menuSlots.Contains(id) ? "등록" : !unlocked ? "연구 필요" : !CookwareRules.CanCook(s, recipe) ? "도구 필요" : count == 0 ? "재료 부족" : "";
            Tile(grid, recipe.RecipeName, $"★{recipe.RecipeGrade} · {recipe.Price:N0}G · {count}인분", selectedId == id,
                () => { selectedId = id; Refresh(); }, DesignArt.Food(recipe), state);

        }
        Pager(list, recipes.Count, 8);
        var selected = recipes.FirstOrDefault(r => r.RecipeId == selectedId) ?? recipes.FirstOrDefault();
        if (selected == null) { Copy(detail, "이 분류에 메뉴가 없어요.", 80); return; }
        selectedId = selected.RecipeId;
        var detailHeader = DayLoopUI.Rect("SelectedMenuHeader", detail);
        DayLoopUI.Layout(detailHeader, preferredHeight: 126);
        DayLoopUI.Horizontal(detailHeader, 0, 18);
        DayLoopUI.FoodIcon(detailHeader, selected, 126);
        var detailLabels = DayLoopUI.Rect("Labels", detailHeader);
        DayLoopUI.Layout(detailLabels, flexibleWidth: 1, flexibleHeight: 1).minWidth = 0;
        DayLoopUI.Vertical(detailLabels, 0, 8, TextAnchor.MiddleLeft);
        Copy(detailLabels, selected.RecipeName, 48, 33);
        Copy(detailLabels, $"★{selected.RecipeGrade} · {selected.Price:N0}G\n조리 {selected.CookTime:0.#}초", 60, 18);
        RecipeRequirements(detail, selected);
        DayLoopUI.Spacer(detail);
        if (!s.unlockedRecipeIds.Contains(selectedId))
            DetailAction(detail, "연구 살펴보기", () => Open("연구"));
        else
        {
            bool registered = s.menuSlots.Contains(selectedId);
            int id = selectedId;
            DetailAction(detail, registered ? "메뉴에서 빼기" : "오늘 메뉴에 등록", () => {
                int slot = registered ? GameSession.Current.menuSlots.IndexOf(id) : GameSession.Current.menuSlots.IndexOf(-1);
                if (slot < 0 && !registered && GameSession.Current.menuSlots.Count < RestaurantRules.MenuLimit(GameSession.Current.restaurantLevel)) slot = GameSession.Current.menuSlots.Count;
                Run((out string error) => RestaurantProgress.TrySetMenu(slot, registered ? -1 : id, out error));
            });
        }
    }

    private void Pager(Transform parent, int count, int size)
    {
        int last = Mathf.Max(0, (count - 1) / size);
        var row = DayLoopUI.Rect("Pagination", parent);
        DayLoopUI.Layout(row, preferredHeight: 50);
        DayLoopUI.Horizontal(row, 0, 12);
        var previous = ActionButton(row, "이전", () => { page--; Refresh(); }, 100, 42);
        previous.interactable = page > 0;
        Copy(row, $"{page + 1} / {last + 1}", 42, 22).alignment = TextAlignmentOptions.Center;
        var next = ActionButton(row, "다음", () => { page++; Refresh(); }, 100, 42);
        next.interactable = page < last;
    }

    private void DesignedIngredients(GameSaveData s)
    {
        info.text = $"{s.ingredients.Count(x => x.amount > 0)}종 보유 · 등급별 재료 창고";
        Filters(content, "전체", "생선", "조개 · 해산물", "양념", "부족한 재료");
        var list = Split(out var detail, 783);
        var ingredients = GameDatabase.Recipes.Ingredients.Where(i => filter == "전체" ||
            filter == "생선" && IngredientGroup(i) == "생선" || filter == "조개 · 해산물" && IngredientGroup(i) == "조개 · 해산물" ||
            filter == "양념" && i.IsBasicSeasoning || filter == "부족한 재료" && IngredientInventoryService.Instance.GetCount(i.IngredientId) < 3).ToList();
        var table = DayLoopUI.Card(list, Color.white, "IngredientTable", 3, 0);
        DayLoopUI.Layout(table, flexibleHeight: 1); DayLoopUI.Vertical(table, 0, 0);
        var header = DayLoopUI.Rect("Columns", table.transform); DayLoopUI.Layout(header, preferredHeight: 48); DayLoopUI.Horizontal(header, 18, 15);
        DayLoopUI.Layout(Copy(header, "재료", 30, 21, DayLoopUI.Kit.InkSoft), flexibleWidth: 1);
        DayLoopUI.Layout(Copy(header, "등급별", 30, 21, DayLoopUI.Kit.InkSoft), preferredWidth: 192);
        DayLoopUI.Layout(Copy(header, "합계", 30, 21, DayLoopUI.Kit.InkSoft), preferredWidth: 72);
        DayLoopUI.Layout(Copy(header, "쓰는 곳", 30, 21, DayLoopUI.Kit.InkSoft), preferredWidth: 240);
        var items = ScrollList(table.transform);
        items.GetComponent<VerticalLayoutGroup>().spacing = 0;
        foreach (var ingredient in ingredients)
        {
            int id = ingredient.IngredientId, total = IngredientInventoryService.Instance.GetCount(id);
            var button = DayLoopUI.Button(items, "", selectedId == id ? DayLoopUI.Kit.SeaTint : total == 0 ? DayLoopUI.Kit.WarnTint : Color.white,
                DayLoopUI.Kit.Ink, 21, () => { selectedId = id; Refresh(); }, ingredient.IngredientName);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false); DayLoopUI.Layout(button, preferredHeight: 84);
            var row = DayLoopUI.Rect("Columns", button.transform); DayLoopUI.Stretch(row, 12, 6, 12, 6); DayLoopUI.Horizontal(row, 0, 15);
            var avatar = DayLoopUI.Panel(row, DayLoopUI.Kit.Sand, "Initial"); avatar.sprite = DayLoopUI.Circle; avatar.type = Image.Type.Simple;
            DayLoopUI.Layout(avatar, preferredWidth: 60, preferredHeight: 60);
            var initial = DayLoopUI.Text(avatar.transform, ingredient.IngredientName.Substring(0, 1), 27, DayLoopUI.Kit.Wood, TextAlignmentOptions.Center); DayLoopUI.Stretch(initial.rectTransform);
            DayLoopUI.Layout(Copy(row, ingredient.IngredientName, 64, 21), flexibleWidth: 1);
            DayLoopUI.Layout(Copy(row, GradeInventory(id), 64, 18), preferredWidth: 192);
            DayLoopUI.Layout(Copy(row, ingredient.IsBasicSeasoning ? "∞" : total.ToString(), 64, 27), preferredWidth: 72);
            string uses = string.Join(" · ", GameDatabase.Recipes.Recipes.Where(r => r.Requirements.Any(x => x.IngredientId == id)).Take(2).Select(r => r.RecipeName));
            DayLoopUI.Layout(Copy(row, uses, 64, 18, DayLoopUI.Kit.InkSoft), preferredWidth: 240);
        }
        var selected = ingredients.FirstOrDefault(i => i.IngredientId == selectedId) ?? ingredients.FirstOrDefault();
        if (selected == null) { Copy(detail, "이 분류에 재료가 없어요.", 80); return; }
        selectedId = selected.IngredientId;
        Copy(detail, selected.IngredientName, 60, 34);
        Copy(detail, GradeInventory(selectedId), 86, 24);
        Copy(detail, "만들 수 있는 메뉴", 42, 25);
        var recipes = GameDatabase.Recipes.Recipes.Where(r => r.Requirements.Any(x => x.IngredientId == selectedId)).ToList();
        foreach (var recipe in recipes.Take(5)) Copy(detail, $"{recipe.RecipeName} · {IngredientInventoryService.Instance.CraftableCount(recipe)}인분", 42, 22);
        if (recipes.Count == 0) Copy(detail, "연결된 메뉴가 없어요.", 42, 21, DayLoopUI.Kit.InkSoft);
        DayLoopUI.Spacer(detail);
        int ingredientId = selectedId;
        DetailAction(detail, "10개 → 다음 등급 합성", () => {
            bool ok = IngredientInventoryService.Instance.PromoteAll(ingredientId); Refresh();
            message.text = ok ? "합성했어요." : "같은 등급 재료 10개가 필요해요.";
        }, enabled: !selected.IsBasicSeasoning && RestaurantProgress.CanManage);
        DetailAction(detail, "상점에서 채우기", () => Open("상점"));
    }

    private void DesignedShop(GameSaveData s)
    {
        if (cartDay != s.day) { cart.Clear(); cartDay = s.day; }
        info.text = $"새 재고 · 오늘 재고 · 내일 아침 새로 들어와요 · {RestaurantProgress.Gold(s):N0}G";
        Filters(content, "전체", "생선", "조개 · 해산물", "양념", "오늘 메뉴에 필요");
        var list = Split(out var detail, 783);
        var ingredients = GameDatabase.Recipes.Ingredients.Where(i => !i.IsBasicSeasoning && (filter == "전체" ||
            filter == "생선" && IngredientGroup(i) == "생선" || filter == "조개 · 해산물" && IngredientGroup(i) == "조개 · 해산물" ||
            filter == "오늘 메뉴에 필요" && GameDatabase.Recipes.Recipes.Any(r => s.menuSlots.Contains(r.RecipeId) && r.Requirements.Any(q => q.IngredientId == i.IngredientId)))).ToList();
        var grid = CardGrid(list);
        foreach (var ingredient in ingredients.Skip(page * 8).Take(8))
        {
            int id = ingredient.IngredientId, available = Stock(s, id);
            cart.TryGetValue(id, out int quantity);
            var button = Tile(grid, ingredient.IngredientName + " F", $"{RestaurantProgress.RegularUnitPrice(ingredient)}G\n남은 {available}개", quantity > 0,
                () => { cart[id] = Mathf.Min(available, quantity + 1); Refresh(); }, flag: quantity > 0 ? $"담음 {quantity}" : available == 0 ? "다 팔림" : null);
            button.interactable = available > quantity;
        }
        Pager(list, ingredients.Count, 8);
        Copy(detail, "장바구니", 50, 32);
        var basket = ScrollList(detail);
        int total = 0;
        foreach (var pair in cart.Where(x => x.Value > 0).ToList())
        {
            if (!GameDatabase.Recipes.TryGetIngredientById(pair.Key, out var ingredient)) continue;
            int id = pair.Key, quantity = pair.Value;
            int cost = RestaurantProgress.RegularUnitPrice(ingredient) * quantity; total += cost;
            Copy(basket, $"{ingredient.IngredientName}  F · {cost:N0}G", 36, 22);
            var row = DayLoopUI.Rect("Quantity", basket);
            DayLoopUI.Layout(row, preferredHeight: 46); DayLoopUI.Horizontal(row, 0, 12);
            ActionButton(row, "−", () => { if (quantity <= 1) cart.Remove(id); else cart[id]--; Refresh(); }, 56, 42);
            Copy(row, quantity.ToString(), 42, 24);
            var plus = ActionButton(row, "+", () => { cart[id] = quantity + 1; Refresh(); }, 56, 42);
            plus.interactable = quantity < Stock(s, id);
        }
        if (cart.Count == 0) Copy(basket, "재료를 선택하면\n여기에 담겨요.", 100, 23, DayLoopUI.Kit.InkSoft);
        Copy(detail, $"합계  {total:N0}G", 46, 30);
        Copy(detail, $"산 뒤 보유  {RestaurantProgress.Gold(s) - total:N0}G", 34, 21, DayLoopUI.Kit.InkSoft);
        DetailAction(detail, "비우기", () => { cart.Clear(); Refresh(); }, enabled: cart.Count > 0);
        DetailAction(detail, $"사기 · {total:N0}G", () => {
            bool ok = RestaurantProgress.TryBuyIngredients(cart, out string error);
            if (ok) cart.Clear(); Refresh(); message.text = ok ? "재료를 구매했어요." : error;
        }, true, cart.Any(x => x.Value > 0) && total <= RestaurantProgress.Gold(s));
    }

    private static string IngredientGroup(IngredientData item)
    {
        if (item.IsBasicSeasoning) return "양념";
        return new[] { "조개", "가리비", "전복", "새우", "문어", "오징어", "게", "홍합" }.Any(x => item.IngredientName.Contains(x)) ? "조개 · 해산물" : "생선";
    }

    private void OpenRecruitment()
    {
        var dim = DayLoopUI.Panel(canvas.transform, DayLoopUI.Dim, "Recruitment", false); DayLoopUI.Stretch(dim.rectTransform);
        var card = DayLoopUI.Card(dim.transform, DayLoopUI.Kit.Sail, "RecruitmentCard");
        DayLoopUI.Place(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900, 440)); DayLoopUI.Vertical(card, 30, 18);
        Copy(card.transform, "직원 고용", 60, 42);
        Copy(card.transform, "스카우트로 후보를 만난 뒤 계약할 직원을 선택해요.", 42, 24);
        var choices = DayLoopUI.Rect("ScoutTiers", card.transform); DayLoopUI.Layout(choices, preferredHeight: 140); DayLoopUI.Horizontal(choices, 0, 18);
        for (int tier = 0; tier < 4; tier++)
        {
            int value = tier;
            ActionButton(choices, new[] { "일반\n100G", "고급\n500G", "특급\n2,000G", "최고\n5,000G" }[tier], () => {
                Destroy(dim.gameObject); Run((out string error) => RestaurantProgress.TryScout(value, out error));
            }, 192, 120);
        }
        DetailAction(card.transform, "닫기", () => Destroy(dim.gameObject));
    }

    private static int Stock(GameSaveData s, int id) => s.progression.regularStockDay == s.day
        ? s.progression.dailyStock.Find(x => x.itemId == id)?.currentStock ?? 50 : 50;

    private void DesignedStaff(GameSaveData s)
    {
        info.text = $"홀 {s.employees.Count(e => e.role == "Serving")}명 · 주방 {s.employees.Count(e => e.role == "Kitchen")}명 · 주급 {s.employees.Sum(e => e.wage):N0}G";
        var list = Split(out var detail, 852);
        var employees = s.employees.Concat(s.progression.candidates).ToList();
        var items = ScrollList(list);
        var grid = CardGrid(items, 2, 409.5f, Mathf.Max(2, Mathf.CeilToInt((employees.Count + 1) / 2f)));
        foreach (var employee in employees)
        {
            string id = employee.instanceId;
            var button = DayLoopUI.KitButton(grid, "", selectedEmployee == id ? DayLoopUI.Kit.SeaTint : Color.white, DayLoopUI.Kit.Ink, 22.5f,
                () => { selectedEmployee = id; Refresh(); }, employee.name);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            var body = DayLoopUI.Rect("StaffCard", button.transform); DayLoopUI.Stretch(body, 24, 24, 24, 24); DayLoopUI.Vertical(body, 0, 9);
            var top = DayLoopUI.Rect("PortraitAndName", body); DayLoopUI.Layout(top, preferredHeight: 108); DayLoopUI.Horizontal(top, 0, 18);
            var portrait = ReferenceTitleView.Picture(top, employee.role == "Kitchen" ? "OtterChef" : "OtterServer");
            portrait.preserveAspect = true; DayLoopUI.Layout(portrait, preferredWidth: 108, preferredHeight: 108);
            var name = DayLoopUI.Rect("Name", top); DayLoopUI.Layout(name, flexibleWidth: 1); DayLoopUI.Vertical(name, 0, 6, TextAnchor.MiddleLeft);
            Copy(name, employee.name, 42, 33); Copy(name, $"{RoleLabel(employee.role)} · {employee.grade}등급", 30, 21, DayLoopUI.Kit.InkSoft);
            float stamina = employee.stamina < 0 ? 1 : Mathf.Clamp01(employee.stamina / Mathf.Max(1, employee.hp));
            Copy(body, $"체력                                      {stamina * 100:0}%", 30, 21);
            var staminaBar = DayLoopUI.Bar(body, stamina, stamina < .8f ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Kelp); DayLoopUI.Layout(staminaBar.transform.parent, preferredHeight: 15);
            Copy(body, $"이동 속도                               {employee.serving:0}", 30, 21);
            Copy(body, employee.role == "Kitchen" ? $"조리 속도                               {employee.cooking:0}" : $"주문 응대                               {employee.handy:0}", 30, 21);
            DayLoopUI.Spacer(body);
            Copy(body, $"주급                                       {employee.wage:N0}G", 30, 21);
        }
        Tile(grid, "빈 자리", "눌러서 직원 고용", false, OpenRecruitment, DesignArt.Load("IconStaff"));
        var selected = employees.FirstOrDefault(e => e.instanceId == selectedEmployee) ?? employees.FirstOrDefault();
        if (selected == null) { Copy(detail, "스카우트로 직원을 만나 보세요.", 120, 26); return; }
        selectedEmployee = selected.instanceId;
        Copy(detail, selected.name, 60, 36);
        Copy(detail, $"{selected.grade}등급 · 주급 {selected.wage:N0}G", 42, 23);
        Copy(detail, $"요리 {selected.cooking:0}  ·  서빙 {selected.serving:0}\n솜씨 {selected.handy:0}  ·  최대 체력 {selected.hp:0}", 96, 23);
        float ratio = selected.stamina < 0 ? 1 : Mathf.Clamp01(selected.stamina / Mathf.Max(1, selected.hp));
        Copy(detail, $"체력 {ratio * 100:0}%", 36, 24);
        var bar = DayLoopUI.Bar(detail, ratio, ratio < .3f ? DayLoopUI.Kit.Alert : DayLoopUI.Kit.Kelp);
        DayLoopUI.Layout(bar.transform.parent, preferredHeight: 18);
        Copy(detail, "체력이 바닥나면 영업 중에 잠들어요. 집에서 자고 나면 모두 회복해요.", 96, 22, DayLoopUI.Kit.InkSoft);
        DayLoopUI.Spacer(detail);
        string employeeId = selected.instanceId;
        if (s.progression.candidates.Contains(selected)) DetailAction(detail, "고용 · " + selected.wage.ToString("N0") + "G", () => Run((out string error) => RestaurantProgress.TryHire(employeeId, out error)), true);
        else
        {
            DetailAction(detail, "홀에 배치", () => Run((out string error) => RestaurantProgress.TryAssign(employeeId, PartTimerRole.Serving, out error)));
            DetailAction(detail, "주방에 배치", () => Run((out string error) => RestaurantProgress.TryAssign(employeeId, PartTimerRole.Kitchen, out error)));
            DetailAction(detail, "대기", () => Run((out string error) => RestaurantProgress.TryAssign(employeeId, PartTimerRole.None, out error)));
            DetailAction(detail, "내보내기", () => ManagementDialog.Show(canvas.transform, selected.name + " 직원을 내보낼까요?", () => Run((out string error) => RestaurantProgress.TryDismiss(employeeId, out error))));
        }
    }

    private void DesignedResearch(GameSaveData s)
    {
        info.text = $"연구 칸 {s.progression.research.Count} / {RestaurantRules.ResearchSlots(s.restaurantLevel)} · 해금한 메뉴 {s.unlockedRecipeIds.Count}개";
        Filters(content, "전체", "연구 중", "시작할 수 있음", "잠김", "완료");
        var list = Split(out var detail, 783);
        var recipes = GameDatabase.Recipes.Recipes.Where(r => filter == "전체" || filter == "연구 중" && s.progression.research.Any(x => x.recipeId == r.RecipeId) ||
            filter == "시작할 수 있음" && s.progression.blueprints.Contains(r.RecipeId) && !s.unlockedRecipeIds.Contains(r.RecipeId) ||
            filter == "완료" && s.unlockedRecipeIds.Contains(r.RecipeId) || filter == "잠김" && !s.progression.blueprints.Contains(r.RecipeId) && !s.unlockedRecipeIds.Contains(r.RecipeId)).ToList();
        var items = ScrollList(list);
        foreach (var recipe in recipes)
        {
            int id = recipe.RecipeId;
            var pending = s.progression.research.Find(x => x.recipeId == id);
            string status = s.unlockedRecipeIds.Contains(id) ? "메뉴 해금" : pending != null ? pending.remainingDays > 0 ? $"연구 중 · {pending.remainingDays}일 남음" : "실습 준비 완료" : s.progression.blueprints.Contains(id) ? "재료 예약 가능" : $"설계도 필요 · {Mathf.RoundToInt(recipe.Price * 1.2f):N0}G";
            SelectCard(items, recipe.RecipeName, $"★{recipe.RecipeGrade} · {status}", selectedId == id, () => { selectedId = id; Refresh(); }, DesignArt.Food(recipe));
        }
        var selected = recipes.FirstOrDefault(r => r.RecipeId == selectedId) ?? recipes.FirstOrDefault();
        if (selected == null) { Copy(detail, "이 분류에 연구가 없어요.", 80); return; }
        selectedId = selected.RecipeId; int recipeId = selectedId;
        DetailIcon(detail, selected); Copy(detail, selected.RecipeName, 48, 30);
        Copy(detail, "실습 6단계", 34, 23);
        Copy(detail, "1 씻기  2 손질  3 재료 섞기\n4 성형  5 가열  6 담기", 68, 22);
        Copy(detail, "실습 평균 4점 이상이면 메뉴가 해금돼요.", 70, 22, DayLoopUI.Kit.InkSoft);
        RecipeRequirements(detail, selected);
        DayLoopUI.Spacer(detail);
        var research = s.progression.research.Find(x => x.recipeId == recipeId);
        if (s.unlockedRecipeIds.Contains(recipeId)) DetailAction(detail, "메뉴에서 보기", () => Open("메뉴"));
        else if (research != null)
        {
            DetailAction(detail, research.remainingDays > 0 ? $"{research.remainingDays}일 뒤 실습" : "실습 시작", () => BeginChallenge(recipeId), true, research.remainingDays <= 0 && RestaurantProgress.CanResearch);
            DetailAction(detail, "연구 취소 · 재료 환불", () => ConfirmCancelResearch(recipeId));
        }
        else if (s.progression.blueprints.Contains(recipeId)) DetailAction(detail, "재료 예약 · 연구 시작", () => Run((out string error) => RestaurantProgress.TryStartResearch(recipeId, out error)), true, RestaurantProgress.CanResearch);
        else DetailAction(detail, $"설계도 사기 · {Mathf.RoundToInt(selected.Price * 1.2f):N0}G", () => Run((out string error) => RestaurantProgress.TryBuyBlueprint(recipeId, out error)), true);
        if (!RestaurantProgress.CanResearch) message.text = "연구 실습은 영업을 마친 자유시간에 이용할 수 있어요.";
    }

    private void DesignedElf(GameSaveData s)
    {
        var elf = ElfShopManager.Instance;
        string elfName = elf.CurrentElfType.ToString() == "Red" ? "빨간" : elf.CurrentElfType.ToString() == "Yellow" ? "노란" : "초록";
        info.text = $"오늘의 {elfName} 엘프 · 물건마다 일일 재고가 있어요";
        var list = Split(out var detail);
        var items = ScrollList(list);
        var stocks = elf.ElfStocks.ToList();
        foreach (var stock in stocks)
        {
            int index = stocks.IndexOf(stock);
            string name = stock.IngredientID.ToString(); Sprite icon = null;
            if (stock.ItemType == ElfShopItemType.Recipe && GameDatabase.TryGetRecipe(stock.IngredientID, out var recipe)) { name = recipe.RecipeName + " 설계도"; icon = DesignArt.Food(recipe); }
            else if (GameDatabase.Recipes.TryGetIngredientById(stock.IngredientID, out var ingredient)) { name = ingredient.IngredientName; }
            SelectCard(items, name, $"{elf.GetElfUnitPrice(stock):N0}G · " + (stock.CurrentStock > 0 ? $"재고 {stock.CurrentStock}" : "팔림"), selectedId == index, () => { selectedId = index; Refresh(); }, icon);
        }
        if (stocks.Count == 0) { Copy(detail, "오늘 판매 중인 물건이 없어요.", 90); return; }
        selectedId = Mathf.Clamp(selectedId, 0, stocks.Count - 1);
        var chosen = stocks[selectedId];
        if (chosen.ItemType == ElfShopItemType.Recipe && GameDatabase.TryGetRecipe(chosen.IngredientID, out var selected))
        {
            DetailIcon(detail, selected); Copy(detail, selected.RecipeName, 60, 30);
            Copy(detail, $"설계도 · ★{selected.RecipeGrade}", 34, 23);
            RecipeRequirements(detail, selected);
            Copy(detail, "설계도를 산 뒤 연구소에서 재료를 예약하고 실습하면 메뉴를 배워요.", 100, 22, DayLoopUI.Kit.InkSoft);
        }
        else if (GameDatabase.Recipes.TryGetIngredientById(chosen.IngredientID, out var ingredient))
        { Copy(detail, ingredient.IngredientName, 70, 34); Copy(detail, GradeInventory(ingredient.IngredientId), 100); }
        DayLoopUI.Spacer(detail);
        DetailAction(detail, $"사기 · {elf.GetElfUnitPrice(chosen):N0}G", () => {
            bool ok = elf.PurchaseFromElf(chosen, 1); Refresh(); message.text = ok ? "구매했어요." : "재고 또는 골드가 부족해요.";
        }, true, chosen.CurrentStock > 0);
    }

    private void DesignedFurniture(GameSaveData s)
    {
        int seats = PlacedTable.Active.Sum(t => t.tableData.chairTiles.Length);
        info.text = $"좌석 {seats} / 최대 {RestaurantRules.SeatLimit(s.restaurantLevel)}석 · 테이블 {PlacedTable.Active.Count}개";
        Filters(content, "전체", "테이블", "대기석", "테이블 색상", "창고에 있음");
        var list = Split(out var detail, 783);
        var grid = CardGrid(list);
        foreach (int size in new[] { 1, 2, 4 })
        {
            int count = size, owned = RestaurantProgress.FurnitureCount(s, size);
            int placed = PlacedTable.Active.Count(t => t.tableData.chairTiles.Length == size);
            if (filter != "전체" && filter != "테이블" && !(filter == "창고에 있음" && owned > 0)) continue;
            Tile(grid, size + "인석 테이블", $"배치 {placed} · 창고 {owned}", selectedId == size, () => { selectedId = count; Refresh(); }, DesignArt.Load("IconFurniture"), owned > 0 ? $"창고 {owned}" : null);
        }
        if (filter == "전체" || filter == "대기석")
            Tile(grid, "입구 대기 의자", "입구에 놓인 대기석", selectedId == 100, () => { selectedId = 100; Refresh(); }, DesignArt.Load("IconFurniture"));
        if (filter == "전체" || filter == "테이블")
        {
            var locked = Tile(grid, "6인석 테이블", "식당 레벨 3에 열려요", false, () => { selectedId = 6; Refresh(); }, DesignArt.Load("IconFurniture"), "식당 Lv.3");
            locked.interactable = false;
        }
        if (filter == "전체" || filter == "테이블 색상")
        for (int skin = 0; skin < 3; skin++)
        {
            int value = skin; bool owned = skin < 2 || (s.progression.unlockedTableSkins?.Contains(skin) ?? false);
            var tile = Tile(grid, new[] { "나무색", "바다색", "차양색" }[skin], owned ? "보유 · 바꿀 수 있음" : "120G", selectedId == 200 + skin,
                () => { selectedId = 200 + value; Refresh(); }, flag: s.progression.tableSkin == skin ? "적용 중" : null);
            var swatch = tile.transform.Find("TileBody/Image/Art").GetComponent<Image>(); swatch.sprite = DayLoopUI.Rounded; swatch.type = Image.Type.Sliced;
            swatch.color = skin == 0 ? DayLoopUI.Kit.WoodLight : skin == 1 ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Awning;
            foreach (Transform child in swatch.transform) child.gameObject.SetActive(false);
        }
        if (selectedId < 0) selectedId = 4;
        if (selectedId >= 200)
        {
            int skin = selectedId - 200;
            Copy(detail, new[] { "나무색", "바다색", "차양색" }[Mathf.Clamp(skin, 0, 2)], 60, 33);
            Copy(detail, "테이블 색상", 40, 21, DayLoopUI.Kit.InkSoft);
            DayLoopUI.Spacer(detail);
            bool owned = skin < 2 || (s.progression.unlockedTableSkins?.Contains(skin) ?? false);
            DetailAction(detail, owned ? "색상 적용" : "사기 · 120G", () => {
                if (!owned) { Run((out string error) => RestaurantProgress.TryBuyTableSkin(skin, out error)); return; }
                GameSession.Current.progression.tableSkin = skin;
                if (TableSaveLoadManager.Instance != null) TableSaveLoadManager.Instance.RememberEdit();
                foreach (var table in PlacedTable.Active) table.SetSkin(skin);
                TableSaveLoadManager.Instance?.SavePlacement(); Refresh();
            }, true);
            return;
        }
        if (selectedId == 100) { Copy(detail, "입구 대기 의자", 60, 33); Copy(detail, $"현재 식당 대기열 {RestaurantRules.WaitingLimit(s.restaurantLevel)}자리", 80, 23); DetailAction(detail, "자리 배치 열기", () => OpenPlacement()); return; }
        int chosen = selectedId == 1 || selectedId == 2 ? selectedId : 4;
        Copy(detail, chosen + "인석 테이블", 60, 33);
        Copy(detail, $"좌석 {chosen} · 창고에 {RestaurantProgress.FurnitureCount(s, chosen)}개", 60, 23);
        Copy(detail, $"배치함   {PlacedTable.Active.Count(t => t.tableData.chairTiles.Length == chosen)}개\n창고      {RestaurantProgress.FurnitureCount(s, chosen)}개\n더 사기  {chosen * 40}G", 150, 24);
        DayLoopUI.Spacer(detail);
        DetailAction(detail, $"더 사기 · {chosen * 40}G", () => Run((out string error) => RestaurantProgress.TryBuyFurniture(chosen, out error)));
        DetailAction(detail, "놓으러 가기", () => PlaceTable(chosen), true, RestaurantProgress.FurnitureCount(s, chosen) > 0);
    }

    private void OpenPlacement()
    {
        CloseWindow();
        if (PreOperationUIManager.Instance != null) PreOperationUIManager.Instance.OnTablePlacementButtonClicked();
        else if (!GameFlow.TryEnterRestaurant()) { window.gameObject.SetActive(true); message.text = "오늘은 식당에 들어갈 수 없어요."; }
    }

    private void DesignedExplore(GameSaveData s)
    {
        var trip = s.progression.expedition ?? new ExpeditionState();
        int away = s.employees.Count(e => ReferenceExpedition.IsAway(s, e.instanceId));
        info.text = $"선착장 · 배 1척 · 나간 직원 {away}명";
        if (trip.destination >= 0)
        {
            bool ready = ReferenceExpedition.Returned(s);
            string rewards = string.Join("  ", ReferenceExpedition.Rewards(s).Select(x => GameDatabase.Recipes.TryGetIngredientById(x.Key, out var item) ? $"{item.IngredientName} F {x.Value}" : ""));
            Row(ready ? ReferenceExpedition.Names[trip.destination] + " 탐험이 끝났어요\n" + rewards : $"{ReferenceExpedition.Names[trip.destination]} 탐험 중 · {trip.returnDay}일차 아침에 돌아와요",
                (ready ? "받기" : "탐험 중", () => Run(RestaurantProgress.TryClaimExpedition)));
        }
        var list = Split(out var detail, trip.destination >= 0 ? 741 : 852);
        Copy(list, "갈 수 있는 곳", 40, 23, DayLoopUI.Kit.InkSoft);
        string[] resources = { "고등어 · 광어 · 새우  F~E등급", "조개 · 가리비  F~E등급", "전복 · 문어 · 식당 레벨 3에 열려요" };
        int destination = Mathf.Clamp(selectedId, 0, 2);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var button = SelectCard(list, ReferenceExpedition.Names[i], resources[i] + "\n" + (i == 0 ? "반나절 · 직원 1명" : i == 1 ? "하루 · 직원 1~2명" : "이틀 · 직원 2명"), destination == i,
                () => { selectedId = index; expeditionStaff.Clear(); Refresh(); }, DesignArt.Load("IconExplore"), 156);
            button.interactable = i < 2 || s.restaurantLevel >= 3;
        }
        Copy(detail, ReferenceExpedition.Names[destination] + "로 보내기", 60, 33);
        Copy(detail, destination == 2 ? "이틀" : destination == 0 ? "반나절" : "하루", 36, 21, DayLoopUI.Kit.InkSoft);
        int max = destination == 0 ? 1 : 2;
        Copy(detail, $"보낼 직원  {expeditionStaff.Count} / {max}명", 42, 23);
        var staffGrid = DayLoopUI.Rect("ExpeditionStaff", detail);
        var layout = staffGrid.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(100, 126); layout.spacing = new Vector2(9, 9);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 3;
        DayLoopUI.Layout(staffGrid, preferredHeight: Mathf.CeilToInt(s.employees.Count / 3f) * 135);
        foreach (var employee in s.employees)
        {
            string id = employee.instanceId;
            var button = DayLoopUI.KitButton(staffGrid, employee.name, expeditionStaff.Contains(id) ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand,
                expeditionStaff.Contains(id) ? Color.white : DayLoopUI.Kit.Ink, 19.5f, () => {
                    if (expeditionStaff.Contains(id)) expeditionStaff.Remove(id); else if (expeditionStaff.Count < max) expeditionStaff.Add(id); Refresh();
                });
            var text = button.GetComponentInChildren<TMP_Text>(); DayLoopUI.Stretch(text.rectTransform, 4, 84, 4, 4);
            var avatar = ReferenceTitleView.Picture(button.transform, employee.role == "Kitchen" ? "OtterChef" : "OtterServer"); avatar.preserveAspect = true;
            ReferenceTitleView.Place(avatar.rectTransform, 16, 8, 68, 72);
            button.interactable = !ReferenceExpedition.IsAway(s, id);
        }
        Copy(detail, $"돌아오는 때  {s.day + ReferenceExpedition.Duration(destination)}일차 아침", 56, 22);
        Copy(detail, "예상 수확  " + (destination == 0 ? "고등어 · 광어 · 새우" : destination == 1 ? "조개 · 가리비 6~10개" : "전복 · 문어 6~10개"), 76, 22);
        Copy(detail, "선택한 직원은 탐험 중 영업에 나오지 않아요.", 90, 21, DayLoopUI.Kit.WarnInk);
        DayLoopUI.Spacer(detail);
        DetailAction(detail, "출발", () => { bool ok = RestaurantProgress.TryDepart(destination, expeditionStaff.ToList(), out string error); if (ok) expeditionStaff.Clear(); Refresh(); message.text = ok ? "탐험을 출발했어요." : error; },
            true, trip.destination < 0 && expeditionStaff.Count > 0);
    }

    private void DesignedHome(GameSaveData s)
    {
        info.text = $"{s.day}일차 {(s.progression.freeTime ? "밤" : "아침")} · Lv.{s.restaurantLevel} · 밤에 집에서 잠들면 저장돼요";
        var grid = DayLoopUI.Rect("HomeCards", content); DayLoopUI.Layout(grid, preferredHeight: 852);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(669, 414); layout.spacing = new Vector2(24, 24);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 2;
        var todos = ReferenceHomeCard(grid, "오늘의 할 일");
        Copy(todos, (s.menuSlots.Any(x => x >= 0) ? "완료  " : "□  ") + "오늘의 메뉴 정하기", 60, 24);
        Copy(todos, "□  상점에서 재료 채우기", 60, 24, DayLoopUI.Kit.SeaDeep);
        Copy(todos, "□  식당에서 영업 시작", 60, 24);
        DetailAction(todos, "영업 준비", () => { if (!GameFlow.TryEnterRestaurant()) message.text = "오늘은 영업을 시작할 수 없어요."; });
        var mission = ReferenceHomeCard(grid, "미션"); Mission(s, out string name, out int target);
        Copy(mission, $"{name}   {s.progression.missionProgress} / {target}", 48, 26);
        var progress = DayLoopUI.Bar(mission, target > 0 ? (float)s.progression.missionProgress / target : 0, DayLoopUI.Kit.Sea); DayLoopUI.Layout(progress.transform.parent, preferredHeight: 18);
        Copy(mission, $"보상 {200 + s.progression.missionIndex * 100:N0}G", 42, 22, DayLoopUI.Kit.GoldInk);
        DetailAction(mission, "보상 받기", () => Run(RestaurantProgress.TryClaimMission), enabled: s.progression.missionProgress >= target);
        var wage = ReferenceHomeCard(grid, "다음 주급"); int wages = s.employees.Sum(x => x.wage);
        Copy(wage, $"{wages:N0}G", 72, 51, DayLoopUI.Kit.GoldInk);
        Copy(wage, $"{(7 - s.day % 7) % 7}일 뒤 · 7일마다", 42, 22, DayLoopUI.Kit.InkSoft);
        Copy(wage, $"직원 {s.employees.Count}명", 42, 23);
        Copy(wage, RestaurantProgress.Gold(s) >= wages ? $"보유 골드 {RestaurantProgress.Gold(s):N0}G로 충분해요" : "주급을 준비해 주세요", 68, 23, DayLoopUI.Kit.GoodInk);
        var save = ReferenceHomeCard(grid, "저장");
        string saved = DateTime.TryParse(s.savedAtUtc, out var time) ? time.ToLocalTime().ToString("yyyy.MM.dd HH:mm") : "저장 기록 없음";
        Copy(save, "마지막 저장  " + saved, 64, 23);
        Copy(save, $"플레이 시간  {(int)(s.playTimeSeconds / 3600)}시간 {(int)(s.playTimeSeconds / 60) % 60}분", 48, 23);
        Copy(save, "저장은 하루에 한 번, 밤에 집에서 잠들 때 돼요. 영업 중에 끄면 그날 아침부터 다시 해요.", 120, 21, DayLoopUI.Kit.InkSoft);
    }

    private static RectTransform ReferenceHomeCard(Transform parent, string heading)
    {
        var card = DayLoopUI.Card(parent, Color.white, heading, 3, 0); DayLoopUI.Vertical(card, 27, 18);
        Copy(card.transform, heading, 48, 33); return card.rectTransform;
    }

    private void DesignedTools(GameSaveData s)
    {
        info.text = $"식당 Lv.{s.restaurantLevel} · 평점 {RestaurantRules.RestaurantRating(s):0.0} · 조리도구 {s.progression.tools.Count} / 4";
        var list = Split(out var detail);
        var levelCard = DayLoopUI.Card(list, Color.white, "RestaurantLevel");
        DayLoopUI.Layout(levelCard, preferredHeight: 230);
        DayLoopUI.Vertical(levelCard, 20, 8);
        var level = levelCard.rectTransform;
        Copy(level, $"해달식당 Lv.{s.restaurantLevel}", 44, 30);
        Copy(level, $"최대 {RestaurantRules.SeatLimit(s.restaurantLevel)}석 · 하루 손님 {RestaurantRules.DailyVisitors(s.restaurantLevel)}명\n주방 {RestaurantRules.CookingSpace(s.restaurantLevel)}명 · 품질 {RestaurantRules.GradeRange(s.restaurantLevel)}", 76, 25);
        if (s.restaurantLevel < RestaurantRules.MaxLevel)
            Copy(level, $"다음 레벨: 평점 {RestaurantRules.UpgradeRating(s.restaurantLevel + 1):0.0} · 공사 {RestaurantRules.UpgradeCost(s.restaurantLevel + 1):N0}G", 46, 23, DayLoopUI.Kit.InkSoft);
        var items = ScrollList(list);
        for (int i = 201; i <= 204; i++)
        {
            int id = i;
            var definition = CookwareRules.Definition(id);
            string state = s.progression.tools.Contains(id) ? $"Lv.{CookwareRules.Level(s, id)} · 사용 중" : s.progression.firstToolChosen ? "미보유 · 500G" : "첫 도구 무료";
            SelectCard(items, ToolNames[id - 201], state, selectedId == id, () => { selectedId = id; Refresh(); }, DesignArt.Load("IconTools"));
        }
        selectedId = Mathf.Clamp(selectedId, 201, 204); int toolId = selectedId;
        var tool = CookwareRules.Definition(toolId);
        int currentLevel = CookwareRules.Level(s, toolId);
        Copy(detail, ToolNames[toolId - 201], 64, 34);
        bool owned = s.progression.tools.Contains(toolId);
        Copy(detail, owned ? $"현재 Lv.{currentLevel}" : "아직 보유하지 않았어요", 48, 26);
        if (tool != null)
        {
            Copy(detail, tool.Description, 100, 22, DayLoopUI.Kit.InkSoft);
            if (owned && tool.TryGetLevelData(currentLevel + 1, out var next))
            {
                Copy(detail, $"Lv.{currentLevel + 1}로 강화하면", 44, 25);
                Copy(detail, $"사용 가능한 재료 {tool.GetMaxIngredientCount(currentLevel)} → {next.MaxIngredientCount}종", 54, 23);
                foreach (var condition in next.UpgradeConditions)
                {
                    string label = condition.Type == UpgradeConditionType.Gold ? "골드" : condition.Type == UpgradeConditionType.RestaurantLevel ? "식당 레벨" : condition.Type == UpgradeConditionType.BlacksmithLevel ? "대장간 레벨" : "도구 사용 횟수";
                    Copy(detail, $"{label}  {condition.Amount:N0}", 40, 22);
                }
            }
        }
        DayLoopUI.Spacer(detail);
        if (!owned) DetailAction(detail, s.progression.firstToolChosen ? "도구 구매 · 500G" : "첫 도구 선택 · 무료", () => Run((out string error) => RestaurantProgress.TryBuyTool(toolId, out error)), true);
        else DetailAction(detail, "강화하기", () => Run((out string error) => RestaurantProgress.TryUpgradeTool(toolId, out error)), true, tool != null && currentLevel < tool.MaxLevel);
        DetailAction(detail, "식당 확장 · 대장간", () => Open("대장간"));
    }
}
