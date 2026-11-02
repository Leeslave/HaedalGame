using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [첫 플레이 UI 개선안 01] 첫날 섬 화면의 "첫 영업 준비" 안내.
///  - 관리 창을 자동으로 열지 않고, 지금 할 행동 하나만 카드로 보여 준다. (도구 받기 → 메뉴 확인 → 첫 영업)
///  - 안내 중에는 왼쪽 메뉴 띠를 숨기고 일차·골드 칩만 남긴다. [나중에 하기]를 누르면 평소 화면으로 돌아간다.
///  - 진행 기록은 세션의 튜토리얼 기록에 남겨 취침 저장과 함께 저장된다.
/// </summary>
public partial class IslandDayHud
{
    private const string OnboardingLaterId = "UIOnboarding.Later";
    private const string OnboardingMenuId = "UIOnboarding.MenuChecked";
    private const string OnboardingDoneId = "UIOnboarding.Done";
    private static readonly string[] OnboardingSteps = { "도구 받기", "메뉴 확인", "첫 영업" };

    private RectTransform _chips, _onboardingCard;
    private TMP_Text _chipDay, _chipGold;
    private int _onboardingStep = -1;

    /// <summary>지금 보여 줄 안내 단계. 0 도구 받기, 1 메뉴 확인, 2 첫 영업, 3 마침. 안내하지 않으면 -1.</summary>
    private static int OnboardingStep()
    {
        if (!GameSession.IsActive) return -1;
        GameSaveData s = GameSession.Current;
        if (s.day != 1 || s.HasTutorialCompleted(OnboardingDoneId) || s.HasTutorialCompleted(OnboardingLaterId)) return -1;
        if (GameSession.OperationCompletedToday) return 3;
        if (!s.progression.firstToolChosen) return 0;
        if (!s.HasTutorialCompleted(OnboardingMenuId)) return 1;
        return 2;
    }

    private void BuildOnboardingChips()
    {
        _chips = DayLoopUI.Rect("[Onboarding] Chips", _canvas.transform);
        DayLoopUI.Place(_chips, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(0, 72));
        DayLoopUI.Horizontal(_chips, 0, 16).childForceExpandHeight = true;
        _chips.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        _chipDay = StatusChip(_chips, DayLoopUI.Kit.Coin, "Day");
        _chipGold = StatusChip(_chips, DayLoopUI.Kit.Coin, "Gold", true);

        var settings = DayLoopUI.KitButton(_chips, "", DayLoopUI.Kit.Sail, DayLoopUI.Kit.Line, 10, () => SettingsWindow.Open(gameObject.scene), "Settings");
        settings.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
        DayLoopUI.Layout(settings, preferredWidth: 72, preferredHeight: 72);
        var gear = ReferenceTitleView.Picture(settings.transform, "IconSettings");
        gear.color = DayLoopUI.Kit.Wood; gear.preserveAspect = true;
        DayLoopUI.Place(gear.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 40));
        _chips.gameObject.SetActive(false);
    }

    private static TMP_Text StatusChip(Transform parent, Color dot, string name, bool coin = false)
    {
        var chip = DayLoopUI.Card(parent, DayLoopUI.Kit.Sail, name, 3f, 5f);
        chip.raycastTarget = false;
        var row = DayLoopUI.Horizontal(chip, 0, 14);
        row.padding = new RectOffset(22, 28, 10, 10);
        var mark = DayLoopUI.Chunky(DayLoopUI.Panel(chip.transform, dot, "Mark"), coin ? 3f : 0f, 0f);
        if (coin) mark.GetComponent<Outline>().effectColor = new Color32(0xC9, 0x8A, 0x12, 0xFF);
        mark.sprite = DayLoopUI.Circle; mark.type = Image.Type.Simple; mark.raycastTarget = false;
        DayLoopUI.Layout(mark, preferredWidth: 34, preferredHeight: 34);
        var text = DayLoopUI.Text(chip.transform, "", 28, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(text, preferredHeight: 40);
        return text;
    }

    private void RefreshOnboarding(int day, int gold)
    {
        if (_chips == null) return;
        int step = FirstToolPicker.IsOpen ? _onboardingStep : OnboardingStep();
        bool active = step >= 0;
        _rail.gameObject.SetActive(!active);
        _chips.gameObject.SetActive(active);
        if (active)
        {
            _chipDay.text = day + "일차";
            _chipGold.text = gold >= 0 ? DayLoopUI.Gold(gold) : "-";
            FitChip(_chipDay);
            FitChip(_chipGold);
        }
        if (step == _onboardingStep && (_onboardingCard != null) == active) return;
        _onboardingStep = step;
        if (_onboardingCard != null) { _onboardingCard.gameObject.SetActive(false); Destroy(_onboardingCard.gameObject); _onboardingCard = null; }
        if (active) BuildOnboardingCard(step);
    }

    private static void FitChip(TMP_Text text)
    {
        DayLoopUI.Layout(text, preferredWidth: text.GetPreferredValues(text.text).x + 4, preferredHeight: 40);
    }

    private void BuildOnboardingCard(int step)
    {
        string title, body, action;
        System.Action run;
        switch (step)
        {
            case 0:
                title = "첫 조리도구를 골라요";
                body = "도구를 받으면 만들 수 있는 메뉴도 생겨요.";
                action = "무료 도구 받기";
                run = () => FirstToolPicker.Show(gameObject.scene, ForceRefresh);
                break;
            case 1:
                string menu = FirstMenuName();
                title = "오늘의 메뉴를 확인해요";
                body = menu != null ? $"'{menu}'{Particle(menu)} 오늘의 메뉴에 올라갔어요." : "도구의 기본 메뉴가 오늘의 메뉴에 올라갔어요.";
                action = "메뉴 확인하기";
                run = () => { GameSession.TryMarkTutorialCompleted(OnboardingMenuId); RestaurantManagementUI.OpenSection("메뉴"); ForceRefresh(); };
                break;
            case 2:
                title = "첫 영업을 시작해요";
                body = "식당에서 [운영 시작]을 누르면 손님이 들어와요.";
                action = "식당으로 가기";
                run = () => GameFlow.TryEnterRestaurant();
                break;
            default:
                title = "첫 영업을 마쳤어요";
                body = "집에서 잠들면 2일차 아침으로 저장돼요.";
                action = "집으로 가기";
                run = () => { FinishOnboarding(); ReferenceHouseView.Open(gameObject.scene); };
                break;
        }

        var card = DayLoopUI.Card(_canvas.transform, DayLoopUI.Kit.Sail, "[Onboarding] Card", 4f, 8f);
        _onboardingCard = card.rectTransform;
        // 아래 가운데: 왼쪽 아래의 식당 표식과 위쪽 건물 표식을 가리지 않는다.
        DayLoopUI.Place(_onboardingCard, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 40), new Vector2(680, 0));
        var layout = DayLoopUI.Vertical(card, 0, 12, TextAnchor.UpperCenter);
        layout.padding = new RectOffset(40, 40, 30, 24);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string progress = step >= 3 ? "완료" : $"{step + 1} / {OnboardingSteps.Length}";
        Line(card.transform, $"첫 영업 준비 · {progress}", 22, DayLoopUI.Kit.InkSoft, 30, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Line(card.transform, title, 44, DayLoopUI.Kit.Ink, 58, TextAlignmentOptions.Center, FontStyles.Bold);
        Line(card.transform, body, 24, DayLoopUI.Kit.SeaDeep, 36, TextAlignmentOptions.Center, FontStyles.Normal);
        DayLoopUI.Spacer(card.transform, height: 6);

        // 실행은 아래 한 줄: 보조 행동은 작게, 핵심 행동 하나만 크게.
        var actions = DayLoopUI.Rect("Actions", card.transform);
        DayLoopUI.Layout(actions, preferredHeight: 84);
        DayLoopUI.Horizontal(actions, 0, 14).childForceExpandHeight = true;
        string secondary = step >= 3 ? "닫기" : "나중에 하기";
        var later = DayLoopUI.KitButton(actions, secondary, DayLoopUI.Kit.Sand, DayLoopUI.Kit.Ink, 25, () => {
            if (step >= 3) FinishOnboarding(); else GameSession.TryMarkTutorialCompleted(OnboardingLaterId);
            ForceRefresh();
        }, "Later");
        DayLoopUI.Layout(later, preferredWidth: 180, preferredHeight: 78);
        var primary = DayLoopUI.KitButton(actions, action, DayLoopUI.Kit.Sea, Color.white, 32, () => run(), "Primary");
        DayLoopUI.Layout(primary, flexibleWidth: 1, preferredHeight: 78);
        var chevron = ReferenceTitleView.Picture(primary.transform, "IconChevronRight");
        chevron.preserveAspect = true;
        DayLoopUI.Place(chevron.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-24, 0), new Vector2(32, 32));

        var rule = DayLoopUI.Panel(card.transform, DayLoopUI.Kit.Divider, "Rule", false);
        DayLoopUI.Layout(rule, preferredHeight: 2);
        StepTracker(card.transform, step);
    }

    private static void Line(Transform parent, string value, float size, Color color, float height, TextAlignmentOptions align, FontStyles style)
    {
        var text = DayLoopUI.Text(parent, value, size, color, align, style);
        // 긴 문장은 줄바꿈하고 그만큼 높이를 늘린다. (카드 안쪽 폭 600)
        DayLoopUI.Layout(text, preferredHeight: Mathf.Max(height, text.GetPreferredValues(value, 600, 0).y + 4));
    }

    private static void StepTracker(Transform parent, int step)
    {
        var row = DayLoopUI.Rect("Steps", parent);
        DayLoopUI.Layout(row, preferredHeight: 54);
        var layout = DayLoopUI.Horizontal(row, 0, 12, TextAnchor.MiddleCenter);
        layout.padding = new RectOffset(0, 0, 4, 2);
        for (int i = 0; i < OnboardingSteps.Length; i++)
        {
            bool done = i < step, now = i == step;
            if (i > 0)
            {
                var arrow = ReferenceTitleView.Picture(row, "IconChevronRight");
                arrow.color = DayLoopUI.Kit.InkSoft; arrow.preserveAspect = true;
                DayLoopUI.Layout(arrow, preferredWidth: 20, preferredHeight: 20);
                DayLoopUI.Spacer(row, 14, 20);
            }
            var dot = DayLoopUI.Panel(row, done ? DayLoopUI.Kit.Kelp : now ? DayLoopUI.Kit.Sea : DayLoopUI.Kit.Sand, "Dot");
            dot.sprite = DayLoopUI.Circle; dot.type = Image.Type.Simple; dot.raycastTarget = false;
            DayLoopUI.Layout(dot, preferredWidth: 38, preferredHeight: 38);
            if (done)
            {
                var check = ReferenceTitleView.Picture(dot.transform, "IconCheck");
                check.preserveAspect = true;
                DayLoopUI.Place(check.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26));
            }
            else
            {
                var number = DayLoopUI.Text(dot.transform, (i + 1).ToString(), 22, now ? Color.white : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
                DayLoopUI.Stretch(number.rectTransform);
            }
            var label = DayLoopUI.Text(row, OnboardingSteps[i], 22, now ? DayLoopUI.Kit.SeaDeep : done ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            DayLoopUI.Layout(label, preferredWidth: label.GetPreferredValues(OnboardingSteps[i]).x + 4, preferredHeight: 38);
        }
    }

    private static string FirstMenuName()
    {
        int id = GameSession.Current.menuSlots.FirstOrDefault(x => x >= 0);
        return GameSession.Current.menuSlots.Any(x => x >= 0) && GameDatabase.TryGetRecipe(id, out var recipe) ? recipe.RecipeName : null;
    }

    /// <summary>받침이 있으면 "이", 없으면 "가".</summary>
    private static string Particle(string word)
    {
        char last = word[word.Length - 1];
        return last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0 ? "이" : "가";
    }

    private static void FinishOnboarding() => GameSession.TryMarkTutorialCompleted(OnboardingDoneId);

    private void ForceRefresh()
    {
        _onboardingStep = -2;
        _nextRefresh = 0f;
        Refresh();
    }
}
