using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫날 섬의 "첫 영업 준비" 안내. (첫 플레이 UI 시안 v2 · 02-A)
///  - 관리 창을 자동으로 열지 않고, 왼쪽 띠 안에 3단계 체크리스트를 넣는다. 지도는 가리지 않는다.
///  - 지금 단계만 펼쳐 설명과 버튼 하나를 보여 주고, 나머지 단계는 한 줄로 접는다.
///  - 안내 중에는 띠의 오늘의 할 일·영업 준비 버튼 대신 이 카드가 들어간다. [나중에 하기]를 누르면 평소 띠로 돌아간다.
///  - 진행 기록은 세션의 튜토리얼 기록에 남아 취침 저장과 함께 저장된다.
/// </summary>
public partial class IslandDayHud
{
    private const string OnboardingLaterId = "UIOnboarding.Later";
    private const string OnboardingMenuId = "UIOnboarding.MenuChecked";
    private const string OnboardingDoneId = "UIOnboarding.Done";
    private static readonly string[] OnboardingSteps = { "첫 조리도구 받기", "오늘의 메뉴 확인", "식당에서 첫 영업" };

    // 1920×1080 기준 (시안 1280×720의 1.5배)
    private const float GuideInnerWidth = 294f;

    private RectTransform _guide;
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

    private void BuildGuide()
    {
        var card = DayLoopUI.Card(_railBody, DayLoopUI.Kit.Sail, "[Onboarding] Guide", 4.5f, 6f);
        card.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        _guide = card.rectTransform;
        _guide.SetSiblingIndex(1); // 하루 명판 바로 아래
        var layout = DayLoopUI.Vertical(card, 0, 9);
        layout.padding = new RectOffset(15, 15, 18, 10);
        _guide.gameObject.SetActive(false);
    }

    private void RefreshOnboarding(int day, int gold)
    {
        if (_guide == null) return;
        int step = FirstToolPicker.IsOpen ? _onboardingStep : OnboardingStep();
        bool active = step >= 0;
        _guide.gameObject.SetActive(active);
        if (_missionCard != null) _missionCard.gameObject.SetActive(!active && GameSession.IsActive);
        if (_prepareAction != null) _prepareAction.gameObject.SetActive(!active);
        if (_railHint != null) _railHint.gameObject.SetActive(!active);
        if (!active || step == _onboardingStep) { _onboardingStep = step; return; }
        _onboardingStep = step;
        RebuildGuide(step);
    }

    private void RebuildGuide(int step)
    {
        foreach (Transform child in _guide) { child.gameObject.SetActive(false); Destroy(child.gameObject); }

        // 머리: 제목 + 진행 칩
        var header = DayLoopUI.Rect("Header", _guide);
        DayLoopUI.Layout(header, preferredHeight: 42);
        DayLoopUI.Horizontal(header, 0, 8);
        var title = DayLoopUI.Text(header, step >= 3 ? "첫 영업 완료" : "첫 영업 준비", 32, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(title, flexibleWidth: 1, preferredHeight: 42);
        string progress = step >= 3 ? "완료" : $"{step + 1} / {OnboardingSteps.Length}";
        DayLoopUI.Chip(header, progress, step >= 3 ? DayLoopUI.Kit.Kelp : DayLoopUI.Kit.Sea, Color.white, 21);

        for (int i = 0; i < OnboardingSteps.Length; i++)
        {
            if (i == step) CurrentStep(i);
            else StepRow(i, i < step);
        }
        if (step >= 3) CurrentStep(3);

        string secondary = step >= 3 ? "닫기" : "나중에 하기";
        var later = DayLoopUI.Button(_guide, secondary, Color.clear, DayLoopUI.Kit.InkSoft, 20, () => {
            if (step >= 3) FinishOnboarding(); else GameSession.TryMarkTutorialCompleted(OnboardingLaterId);
            ForceRefresh();
        }, "Later");
        Body(later.GetComponentInChildren<TMP_Text>());
        DayLoopUI.Layout(later, preferredHeight: 54);
    }

    /// <summary>지금 단계: 흰 상자 안에 번호·제목, 설명, 버튼 하나.</summary>
    private void CurrentStep(int step)
    {
        string body, action;
        System.Action run;
        switch (step)
        {
            case 0:
                body = "도구를 받으면 그 도구로 만드는 기본 메뉴가 함께 생겨요.";
                action = "무료 도구 받기";
                run = () => FirstToolPicker.Show(gameObject.scene, ForceRefresh);
                break;
            case 1:
                string menu = FirstMenuName();
                body = menu != null ? $"'{menu}'{Particle(menu)} 오늘의 메뉴에 올라갔어요. 확인하고 필요하면 바꿔요." : "도구의 기본 메뉴가 오늘의 메뉴에 올라갔어요.";
                action = "메뉴 확인하기";
                run = () => { GameSession.TryMarkTutorialCompleted(OnboardingMenuId); RestaurantManagementUI.OpenSection("메뉴"); ForceRefresh(); };
                break;
            case 2:
                body = "식당에서 [운영 시작]을 누르면 손님이 들어와요.";
                action = "식당으로 가기";
                run = () => GameFlow.TryEnterRestaurant();
                break;
            default:
                body = "자유시간을 보낸 뒤 집에서 잠들면 2일차 아침으로 저장돼요.";
                action = "집으로 가기";
                run = () => { FinishOnboarding(); ReferenceHouseView.Open(gameObject.scene); };
                break;
        }

        var box = DayLoopUI.Chunky(DayLoopUI.Panel(_guide, Color.white, "CurrentStep"), 3f, 0f);
        box.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Sea;
        var v = DayLoopUI.Vertical(box, 15, 12);

        var head = DayLoopUI.Rect("Title", box.transform);
        DayLoopUI.Layout(head, preferredHeight: 42);
        DayLoopUI.Horizontal(head, 0, 12);
        if (step < 3) Number(head, step, DayLoopUI.Kit.Sea, Color.white, null);
        else Number(head, step, DayLoopUI.Kit.Kelp, Color.white, "IconCheck");
        var stepTitle = Jua(DayLoopUI.Text(head, step < 3 ? OnboardingSteps[step] : "잘했어요!", 29, DayLoopUI.Kit.Ink, TextAlignmentOptions.MidlineLeft));
        stepTitle.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(stepTitle, flexibleWidth: 1, preferredHeight: 42);

        var desc = Body(DayLoopUI.Text(box.transform, body, 20, new Color32(0x5E, 0x4A, 0x3A, 0xFF), TextAlignmentOptions.TopLeft));
        desc.lineSpacing = 10f;
        DayLoopUI.Layout(desc, preferredHeight: Mathf.Ceil(desc.GetPreferredValues(body, GuideInnerWidth, 0).y) + 4);

        var button = DayLoopUI.KitButton(box.transform, action, DayLoopUI.Kit.Sea, Color.white, 29, () => run(), "Primary");
        DayLoopUI.Layout(button, preferredHeight: 72);
        var chevron = ReferenceTitleView.Picture(button.transform, "IconChevronRight");
        chevron.preserveAspect = true;
        DayLoopUI.Place(chevron.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-18, 0), new Vector2(27, 27));
    }

    /// <summary>지금이 아닌 단계: 번호(끝난 단계는 체크)와 이름 한 줄.</summary>
    private void StepRow(int step, bool done)
    {
        var row = DayLoopUI.Rect("Step " + (step + 1), _guide);
        DayLoopUI.Layout(row, preferredHeight: 66);
        var h = DayLoopUI.Horizontal(row, 0, 15);
        h.padding = new RectOffset(15, 15, 0, 0);
        if (done) Number(row, step, DayLoopUI.Kit.Kelp, Color.white, "IconCheck");
        else Number(row, step, DayLoopUI.Kit.Sand, DayLoopUI.Kit.InkSoft, null, true);
        var label = Jua(DayLoopUI.Text(row, OnboardingSteps[step], 26, done ? DayLoopUI.Kit.GoodInk : DayLoopUI.Kit.InkSoft, TextAlignmentOptions.MidlineLeft));
        label.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(label, flexibleWidth: 1, preferredHeight: 40);
    }

    private static void Number(Transform parent, int step, Color fill, Color ink, string icon, bool outlined = false)
    {
        var dot = DayLoopUI.Panel(parent, fill, "Number");
        if (outlined) { DayLoopUI.Chunky(dot, 3f, 0f); dot.GetComponent<Outline>().effectColor = new Color32(0xC9, 0xAE, 0x82, 0xFF); }
        dot.sprite = DayLoopUI.Circle; dot.type = Image.Type.Simple; dot.raycastTarget = false;
        DayLoopUI.Layout(dot, preferredWidth: 42, preferredHeight: 42);
        if (icon != null)
        {
            var check = ReferenceTitleView.Picture(dot.transform, icon);
            check.preserveAspect = true;
            DayLoopUI.Place(check.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(27, 27));
            return;
        }
        var number = Jua(DayLoopUI.Text(dot.transform, (step + 1).ToString(), 24, ink, TextAlignmentOptions.Center));
        DayLoopUI.Stretch(number.rectTransform);
    }

    // DayLoopUI.Text는 28 이상만 제목 글꼴(Jua)을 쓰므로, 시안대로 글꼴을 직접 정한다.
    private static T Jua<T>(T text) where T : TMP_Text
    {
        text.font = DesignFonts.Heading; text.fontSharedMaterial = text.font.material;
        return text;
    }

    private static T Body<T>(T text) where T : TMP_Text
    {
        text.font = DesignFonts.Body; text.fontSharedMaterial = text.font.material;
        return text;
    }

    private static string FirstMenuName()
    {
        var slots = GameSession.Current.menuSlots;
        if (!slots.Any(x => x >= 0)) return null;
        return GameDatabase.TryGetRecipe(slots.First(x => x >= 0), out var recipe) ? recipe.RecipeName : null;
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
