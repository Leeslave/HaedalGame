using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 준비 화면 안내 카드와 첫 손님 안내. (GUIDE-01)
///  - 준비 카드(매일): 날짜·보유 골드·오늘의 메뉴·배치 직원·이용 가능 좌석, 영업 가능 여부와 이유
///  - 첫날(안내 미완료): 첫 손님 한 명을 따라 입장 → 주문 → 조리 → 운반 → 식사 → 계산을 짧게 설명
///    새 단계가 나오면 게임을 잠깐 멈추고, [확인]을 누르면 이어서 진행한다. 건너뛰기 가능.
///    완료 여부는 세션에 기록되어 결산 체크포인트와 함께 저장된다. (새 게임에서만 다시 보인다)
///  - 직원 잠듦(한 번만): 영업 중 처음으로 직원이 지쳐 잠들면 게임을 멈추고 깨우는 방법을 알려 준다.
///  - 첫 손님만 식사 시간을 줄여 첫 수익까지의 시간을 앞당긴다. (일반 영업 속도는 그대로)
/// </summary>
public class FirstDayGuide : MonoBehaviour
{
    private const float FirstGuestEatSeconds = 4f;
    private const string StaffSleepGuideId = "DayLoopTEST.StaffSleepGuide";

    private enum Step { None, Enter, Waiting, Choosing, OrderReady, Cooking, Carrying, Eating, Paid, Done }

    private DayCycleController _day;
    private Canvas _canvas;

    // 준비 카드
    private RectTransform _prepCard;
    private TMP_Text _prepTitle, _prepBody, _prepStatus, _prepIntro;
    private Button _prepToggle;
    private bool _prepCollapsed;
    private float _nextPrepRefresh;

    // 영업 안내
    private RectTransform _guideBar;
    private TMP_Text _guideStep, _guideText, _guideHint;
    private Button _continueButton;
    private System.Action _onContinue;
    private bool _guideActive;
    private bool _sleepGuideActive;
    private float _nextSleepCheck;
    private CustomerAgent _followed;
    private Step _step;
    private int _paidAmount;
    private float _nextFollowSearch;

    public void Init(DayCycleController day)
    {
        _day = day;
        Build();
        _day.OnPhaseChanged += HandlePhaseChanged;
        _day.Ledger.OnPayment += HandlePayment;
        CustomerAgent.OnAnySpawned += HandleSpawned;
        HandlePhaseChanged(_day.Phase);
    }

    private void OnDestroy()
    {
        if (_day != null)
        {
            _day.OnPhaseChanged -= HandlePhaseChanged;
            _day.Ledger.OnPayment -= HandlePayment;
        }
        CustomerAgent.OnAnySpawned -= HandleSpawned;
    }

    private static bool GuideCompleted
    {
        get
        {
            if (GameSession.TryGetTutorialCompleted(DayLoopSave.FirstDayGuideId, out bool completed))
                return completed;
            return true; // 세션이 없으면 안내하지 않는다.
        }
    }

    private bool ShouldGuide => _day.Day == 1 && !GuideCompleted;

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        _prepCard.gameObject.SetActive(phase == DayLoopPhase.Preparation);
        if (phase == DayLoopPhase.Preparation)
            RefreshPrep();

        if (phase == DayLoopPhase.Open && ShouldGuide)
        {
            _guideActive = true;
            _step = Step.None;
            SetGuide("오늘의 첫 손님을 기다리는 중이에요", "손님 응대는 직원이 자동으로 해요. 첫 손님을 따라가며 짧게 알려 드릴게요.", "");
        }

        if (phase == DayLoopPhase.Settlement)
        {
            EndGuide(false);
            EndSleepGuide(false);
        }

        RefreshGuideBar();
    }

    private void RefreshGuideBar()
    {
        _guideBar.gameObject.SetActive(_guideActive || _sleepGuideActive);
    }

    private void Update()
    {
        if (_prepCard.gameObject.activeSelf && Time.unscaledTime >= _nextPrepRefresh)
        {
            _nextPrepRefresh = Time.unscaledTime + 0.5f;
            RefreshPrep();
        }

        if (_guideActive)
            UpdateGuide();
        else
            CheckStaffSleep();
    }

    // ───── 준비 카드 ─────

    private void RefreshPrep()
    {
        // 테이블 배치 모드(카메라가 식당 구역)에서는 화면을 가리지 않게 숨긴다.
        bool placing = CameraController.Instance != null && CameraController.Instance.GetCurZone() == CameraZone.Restaurant;
        _prepCard.GetComponent<CanvasGroup>().alpha = placing ? 0f : 1f;
        _prepCard.GetComponent<CanvasGroup>().blocksRaycasts = !placing;

        _prepTitle.text = $"{_day.Day}일차 영업 준비";

        StringBuilder sb = new StringBuilder();
        int gold = _day.CurrentGold;
        sb.Append("<color=#7D6654>보유 골드</color>   <b>").Append(gold >= 0 ? DayLoopUI.Gold(gold) : "-").Append("</b>\n");

        MenuManager menu = MenuManager.Instance;
        sb.Append("<color=#7D6654>오늘의 메뉴</color>   ");
        if (menu != null && menu.DailyFoods.Count > 0)
        {
            for (int i = 0; i < menu.DailyFoods.Count; i++)
            {
                RecipeData food = menu.DailyFoods[i];
                if (i > 0) sb.Append(", ");
                sb.Append("<b>").Append(food.RecipeName).Append("</b> ").Append(Mathf.RoundToInt(food.Price)).Append("G");
            }
        }
        else
        {
            sb.Append("<b>없음</b>");
        }
        sb.Append('\n');

        _day.CountStaff(out int servers, out int chefs);
        sb.Append("<color=#7D6654>직원</color>   홀 <b>").Append(servers).Append("명</b> • 주방 <b>").Append(chefs).Append("명</b>\n");

        int tables = TableManager.Instance != null ? TableManager.Instance.GetPlacedTableCount() : 0;
        SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
        sb.Append("<color=#7D6654>좌석</color>   테이블 <b>").Append(tables).Append("개</b> • <b>")
          .Append(seats != null ? seats.IndoorSeatCount : 0).Append("석</b>");
        if (seats != null && seats.WaitingBenchCount > 0)
            sb.Append(" (입구 대기 ").Append(seats.WaitingBenchCount).Append("자리)");
        _prepBody.text = sb.ToString();

        string block = _day.GetStartBlockReason();
        if (string.IsNullOrEmpty(block))
        {
            _prepStatus.text = "준비 완료 — 아래 [운영 시작]을 누르면 손님이 들어와요.";
            _prepStatus.color = DayLoopUI.Good;
        }
        else
        {
            _prepStatus.text = "영업할 수 없어요: " + block;
            _prepStatus.color = DayLoopUI.Warn;
        }

        bool firstDay = ShouldGuide;
        _prepIntro.gameObject.SetActive(firstDay && !_prepCollapsed);
        if (firstDay)
            _prepIntro.text = "첫날은 기본 메뉴와 직원이 이미 배치되어 있어 바로 영업할 수 있어요. 주문·조리·서빙은 직원이 자동으로 처리하고, 첫 손님이 오면 순서대로 짧게 안내해 드려요.";

        _prepBody.gameObject.SetActive(!_prepCollapsed);
        DayLoopUI.SetButtonLabel(_prepToggle, _prepCollapsed ? "펼치기" : "접기");
    }

    private void TogglePrep()
    {
        _prepCollapsed = !_prepCollapsed;
        RefreshPrep();
    }

    // ───── 첫 손님 안내 ─────

    private void HandleSpawned(CustomerAgent customer)
    {
        if (!_guideActive || _followed != null) return;
        _followed = customer;
        _followed.OverrideEatDuration(FirstGuestEatSeconds);
    }

    private void HandlePayment(CustomerAgent customer, int amount, bool first)
    {
        if (!_guideActive || customer != _followed) return;
        _paidAmount = amount;
        SetStep(Step.Paid);
    }

    private void UpdateGuide()
    {
        if (_onContinue != null) return;   // 안내를 읽는 중 (게임 정지)

        // 따라가던 손님이 계산 없이 나가면 아직 남아 있는 다른 손님을 따라간다.
        if (_followed == null || (_followed.State == CustomerState.Exit && !_followed.WasServed))
        {
            _followed = null;
            _step = Step.None;
            if (Time.unscaledTime >= _nextFollowSearch)
            {
                _nextFollowSearch = Time.unscaledTime + 0.5f;
                foreach (CustomerAgent candidate in FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None))
                {
                    if (candidate.State != CustomerState.Exit && candidate.State != CustomerState.Paying)
                    {
                        _followed = candidate;
                        break;
                    }
                }
            }
            return;
        }

        switch (_followed.State)
        {
            case CustomerState.Enter:
            case CustomerState.Seating:
                SetStep(Step.Enter);
                break;
            case CustomerState.WaitingRoom:
                SetStep(Step.Waiting);
                break;
            case CustomerState.WaitingForOrder:
                SetStep(_followed.coc.GetOrderData() == null ? Step.Choosing : Step.OrderReady);
                break;
            case CustomerState.WaitingForFood:
                SetStep(IsCarried(_followed) ? Step.Carrying : Step.Cooking);
                break;
            case CustomerState.Eating:
                SetStep(Step.Eating);
                break;
        }
    }

    private bool IsCarried(CustomerAgent customer)
    {
        foreach (ServerAgent server in _day.Servers)
        {
            if (server != null && server.CarriedFood != null && server.CurrentCustomer == customer)
                return true;
        }
        return false;
    }

    private void SetStep(Step step)
    {
        if (step <= _step) return;   // 앞 단계로 돌아가지 않는다
        _step = step;

        switch (step)
        {
            case Step.Enter:
                SetGuide("1. 손님 입장", "왼쪽 입구로 들어온 손님이 빈 자리로 가요.", "자리가 없으면 입구 대기열에서 기다려요.");
                break;
            case Step.Waiting:
                SetGuide("1. 자리 대기", "빈 자리가 없어 입구에서 기다리는 중이에요.", "발밑 게이지가 줄어들면 기다리다 돌아갈 수 있어요.");
                break;
            case Step.Choosing:
                SetGuide("2. 메뉴 고르기", "자리에 앉은 손님이 오늘의 메뉴 중에서 고르고 있어요.", "");
                break;
            case Step.OrderReady:
                SetGuide("3. 주문 접수 (자동)", "홀 직원이 주문을 받으러 가요. 직접 할 일은 없어요.", "급한 손님을 클릭하면 그 손님부터 응대해요.");
                break;
            case Step.Cooking:
                SetGuide("4. 조리 (자동)", "주방 직원이 주문한 요리를 만들고 있어요.", "직원 발밑에 지금 하는 일이 표시돼요.");
                break;
            case Step.Carrying:
                SetGuide("5. 음식 운반 (자동)", "홀 직원이 음식을 들고 손님 자리로 가요.", "직원 머리 위에 운반 중인 음식이 보여요.");
                break;
            case Step.Eating:
                SetGuide("6. 식사", "음식이 식탁에 놓이고 손님이 식사 중이에요.", "");
                break;
            case Step.Paid:
                SetGuide("7. 계산 완료!", $"첫 수익 +{_paidAmount}G를 벌었어요. 나머지 손님도 같은 순서로 자동 응대돼요.", "위쪽 배속 버튼(x1·x2·x3)으로 빠르게, [|| 정지]로 일시정지할 수 있어요.");
                WaitForContinue(() => EndGuide(true));
                return;
        }

        WaitForContinue(null);
    }

    // 게임을 멈추고 [확인]을 기다린다. 확인하면 then을 실행하고 이어서 진행한다.
    private void WaitForContinue(System.Action then)
    {
        _onContinue = then ?? (() => { });
        _continueButton.gameObject.SetActive(true);
        _day.SetGuidePaused(true);
    }

    private void Continue()
    {
        System.Action then = _onContinue;
        ClearContinue();
        then?.Invoke();
    }

    private void ClearContinue()
    {
        _onContinue = null;
        if (_continueButton != null) _continueButton.gameObject.SetActive(false);
        _day.SetGuidePaused(false);
    }

    private void SetGuide(string step, string text, string hint)
    {
        _guideStep.text = step;
        _guideText.text = text;
        _guideHint.text = hint;
        _guideHint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
    }

    private void SkipGuide()
    {
        if (_sleepGuideActive) EndSleepGuide(true);
        else EndGuide(true);
    }

    private void EndGuide(bool completed)
    {
        if (completed)
            GameSession.TryMarkTutorialCompleted(DayLoopSave.FirstDayGuideId);

        if (_guideActive) ClearContinue();
        _guideActive = false;
        if (_guideBar != null) RefreshGuideBar();
    }

    // ───── 직원 잠듦 안내 ─────

    private void CheckStaffSleep()
    {
        if (_sleepGuideActive || Time.unscaledTime < _nextSleepCheck) return;
        if (_day.Phase != DayLoopPhase.Open && _day.Phase != DayLoopPhase.Closing) return;
        _nextSleepCheck = Time.unscaledTime + 0.5f;

        if (!GameSession.TryGetTutorialCompleted(StaffSleepGuideId, out bool completed) || completed) return;

        foreach (PartTimerAgent agent in FindObjectsByType<PartTimerAgent>(FindObjectsSortMode.None))
        {
            if (!agent.IsSleeping) continue;

            _sleepGuideActive = true;
            string role = agent is ChefAgent ? "주방" : "홀";
            SetGuide("직원 잠듦", $"지친 {role} 직원이 잠들었어요. 자는 동안에는 일이 멈춰요.",
                "잠든 직원(발밑에 '잠듦' 표시)을 클릭하면 깨울 수 있어요. 체력이 낮을수록 잘 잠들어요.");
            RefreshGuideBar();
            WaitForContinue(() => EndSleepGuide(true));
            return;
        }
    }

    private void EndSleepGuide(bool completed)
    {
        if (completed)
            GameSession.TryMarkTutorialCompleted(StaffSleepGuideId);

        if (_sleepGuideActive && _onContinue != null) ClearContinue();
        _sleepGuideActive = false;
        if (_guideBar != null) RefreshGuideBar();
    }

    // ───── 조립 ─────

    private void Build()
    {
        _canvas = DayLoopUI.CreateCanvas("[DayLoop] Guide", 60, gameObject.scene);
        _canvas.transform.SetParent(transform, false);

        // 준비 카드: 화면 오른쪽 가운데 (기존 준비 UI의 메뉴판·버튼과 겹치지 않는 자리)
        Image card = DayLoopUI.Panel(_canvas.transform, DayLoopUI.Cream, "PrepCard");
        _prepCard = card.rectTransform;
        DayLoopUI.Place(_prepCard, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40, 40), new Vector2(600, 0));
        DayLoopUI.AddOutline(card, DayLoopUI.Wood, 2.5f);
        card.gameObject.AddComponent<CanvasGroup>();
        DayLoopUI.Vertical(card, 26, 12);
        ContentSizeFitter fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform header = DayLoopUI.Rect("Header", card.transform);
        DayLoopUI.Horizontal(header, 0, 10);
        DayLoopUI.Layout(header, preferredHeight: 48);
        _prepTitle = DayLoopUI.Text(header, "", 34, DayLoopUI.WoodDark, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_prepTitle, flexibleWidth: 1, preferredHeight: 48);
        _prepToggle = DayLoopUI.Button(header, "접기", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, 20, TogglePrep, "Toggle");
        DayLoopUI.Layout(_prepToggle, preferredWidth: 96, preferredHeight: 42);

        _prepIntro = DayLoopUI.Text(card.transform, "", 22, DayLoopUI.Ink, TextAlignmentOptions.TopLeft);
        DayLoopUI.Layout(_prepIntro, preferredHeight: 96);

        _prepBody = DayLoopUI.Text(card.transform, "", 24, DayLoopUI.Ink, TextAlignmentOptions.TopLeft);
        _prepBody.lineSpacing = 18f;
        DayLoopUI.Layout(_prepBody, preferredHeight: 170);

        _prepStatus = DayLoopUI.Text(card.transform, "", 23, DayLoopUI.Good, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        DayLoopUI.Layout(_prepStatus, preferredHeight: 64);

        // 영업 안내 바: 화면 아래 가운데
        Image bar = DayLoopUI.Panel(_canvas.transform, DayLoopUI.Cream, "GuideBar");
        _guideBar = bar.rectTransform;
        DayLoopUI.Place(_guideBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(1080, 150));
        DayLoopUI.AddOutline(bar, DayLoopUI.Coral, 3f);
        HorizontalLayoutGroup row = DayLoopUI.Horizontal(bar, 0, 20);
        row.padding = new RectOffset(30, 22, 14, 14);

        RectTransform texts = DayLoopUI.Rect("Texts", bar.transform);
        DayLoopUI.Vertical(texts, 0, 2, TextAnchor.MiddleLeft);
        DayLoopUI.Layout(texts, flexibleWidth: 1, preferredHeight: 122);
        _guideStep = DayLoopUI.Text(texts, "", 22, DayLoopUI.Coral, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_guideStep, preferredHeight: 30);
        _guideText = DayLoopUI.Text(texts, "", 28, DayLoopUI.Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        DayLoopUI.Layout(_guideText, preferredHeight: 44);
        _guideHint = DayLoopUI.Text(texts, "", 21, DayLoopUI.InkSoft, TextAlignmentOptions.MidlineLeft);
        DayLoopUI.Layout(_guideHint, preferredHeight: 30);

        _continueButton = DayLoopUI.Button(bar.transform, "확인", DayLoopUI.Sea, Color.white, 24, Continue, "Continue");
        DayLoopUI.Layout(_continueButton, preferredWidth: 130, preferredHeight: 56);
        _continueButton.gameObject.SetActive(false);

        Button skip = DayLoopUI.Button(bar.transform, "안내 건너뛰기", DayLoopUI.CreamDeep, DayLoopUI.WoodDark, 21, SkipGuide, "Skip");
        DayLoopUI.Layout(skip, preferredWidth: 170, preferredHeight: 56);

        _guideBar.gameObject.SetActive(false);
    }
}
