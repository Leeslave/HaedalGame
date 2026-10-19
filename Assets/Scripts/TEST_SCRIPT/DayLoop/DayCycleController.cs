using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DayLoopPhase
{
    Preparation,    // 메뉴·직원·배치 확인, 영업 시작 결정
    Open,           // 손님 입장과 자동 응대
    Closing,        // 신규 입장 종료, 남은 손님만 응대
    Settlement,     // 집계 고정, 결산 확정·저장
}

/// <summary>
/// [TEST 하루 루프] 식당 씬의 하루 단계 관리. (FLOW-02, FLOW-03, CLOSE-01, HUD-02)
/// Restaurant_TEST가 열릴 때 DayLoopInstaller가 자동으로 붙인다. 씬 파일은 수정하지 않는다.
///
/// 준비 → 영업 → 입장 마감 → 결산 → (다음 날) 준비
///  - 영업 시작: 영업 불가 조건(메뉴 없음·요리 불가·좌석 없음·직원 없음)만 이유와 함께 막는다.
///  - 입장 마감: 오늘 입장할 손님을 모두 내보낸 시점. 이미 들어온 손님은 끝까지 응대한다.
///  - 결산: 마지막 손님이 나간 뒤 한 번만 실행한다. 장부를 고정하고 평점을 확정한 뒤
///          다음 날 시작 체크포인트(날짜 +1)를 저장한다. 저장 실패 시 날짜를 넘기지 않고 재시도를 요구한다.
///  - 마감이 끝나지 않는 상황(도달 불가 등)에 대비해, 마감 중 진행이 오래 멈추면 남은 손님을 미대접으로 내보낸다.
/// </summary>
public class DayCycleController : MonoBehaviour
{
    private const float StallTimeout = 120f;   // 마감 중 아무 진행이 없을 때 남은 손님을 내보내기까지 (게임 시간)

    public static DayCycleController Instance { get; private set; }

    public DayLoopPhase Phase { get; private set; } = DayLoopPhase.Preparation;
    public DayLedger Ledger { get; } = new DayLedger();
    public bool IsPaused { get; private set; }
    /// <summary>안내(튜토리얼)를 읽는 동안의 정지. 플레이어 정지(IsPaused)와 별개로 관리한다.</summary>
    public bool IsGuidePaused { get; private set; }
    public CustomerSpawner Spawner => _rgm != null ? _rgm.customerSpawner : null;
    public IReadOnlyList<ServerAgent> Servers => _servers;

    public event Action<DayLoopPhase> OnPhaseChanged;
    public event Action<string> OnToast;

    private RestaurantGameManager _rgm;
    private RestaurantHud _hud;
    private SettlementPanel _settlement;
    private readonly List<ServerAgent> _servers = new List<ServerAgent>();
    private bool _settling;
    private bool _committed;
    private DaySettlementRecord _record;
    private int _openDay;

    // 준비 화면 메뉴판의 임시 문구 (씬에 들어 있는 TMP 텍스트를 실제 값으로 바꾼다)
    private const string RestaurantName = "해달식당";
    private const int MenuBoardLines = 5;
    private TMP_Text _boardDay, _boardRating, _boardName, _boardMenu;
    private float _nextBoardRefresh;

    /// <summary>현재 일차. 세션 값이 기준이다.</summary>
    public int Day
    {
        get
        {
            if (GameSession.IsActive) return GameSession.Current.day;
            return InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 1;
        }
    }

    public int CurrentGold => CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrencyById(GameSession.GoldCurrencyId) : -1;

    private void Awake()
    {
        Instance = this;
        _hud = gameObject.AddComponent<RestaurantHud>();
        _settlement = gameObject.AddComponent<SettlementPanel>();
        gameObject.AddComponent<RestaurantCameraFramer>();
    }

    private void Start()
    {
        _rgm = RestaurantGameManager.instance;
        if (_rgm == null || _rgm.customerSpawner == null)
        {
            Debug.LogError("[DayLoop] RestaurantGameManager/CustomerSpawner를 찾지 못해 하루 루프를 설치하지 않습니다.");
            enabled = false;
            return;
        }

        _rgm.OnOperationStarted += HandleOperationStarted;
        _rgm.OperationEndHandler = HandleOperationEnded;
        _rgm.customerSpawner.OnEntryClosed += HandleEntryClosed;
        PreOperationUIManager.StartBlocker = GetStartBlockReason;
        PreOperationUIManager.OnStartBlocked = HandleStartBlocked;

        // 배속은 Time.timeScale로 걸어 조리·식사·손님 생성까지 같은 비율로 빨라지게 한다.
        RestaurantSpeedController.AppliedByTimeScale = true;
        RestaurantSpeedController.OnFastForwardChanged += HandleSpeedChanged;

        // 다음 날 준비는 x1, 정지 해제 상태로 시작한다.
        RestaurantSpeedController.SetFastForward(false);
        SetPaused(false);

        TidySceneForTest();

        _hud.Init(this);
        gameObject.AddComponent<NpcStatusOverlay>().Init(this);
        gameObject.AddComponent<FirstDayGuide>().Init(this);
        gameObject.AddComponent<PlacementToolbar>();

        SetPhase(DayLoopPhase.Preparation);
    }

    private void OnDestroy()
    {
        Ledger.Stop();
        if (_rgm != null)
        {
            _rgm.OnOperationStarted -= HandleOperationStarted;
            if (_rgm.OperationEndHandler == (Action)HandleOperationEnded) _rgm.OperationEndHandler = null;
            if (_rgm.customerSpawner != null) _rgm.customerSpawner.OnEntryClosed -= HandleEntryClosed;
        }
        if (PreOperationUIManager.StartBlocker == (Func<string>)GetStartBlockReason) PreOperationUIManager.StartBlocker = null;
        if (PreOperationUIManager.OnStartBlocked == (Action<string>)HandleStartBlocked) PreOperationUIManager.OnStartBlocked = null;
        RestaurantSpeedController.OnFastForwardChanged -= HandleSpeedChanged;
        RestaurantSpeedController.AppliedByTimeScale = false;
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    private void SetPhase(DayLoopPhase phase)
    {
        Phase = phase;
        if (phase != DayLoopPhase.Open && phase != DayLoopPhase.Closing)
        {
            IsGuidePaused = false;
            ApplyTimeScale();
        }
        if (phase == DayLoopPhase.Preparation)
            RefreshPreparationBoard();
        OnPhaseChanged?.Invoke(phase);
    }

    public void Toast(string message) => OnToast?.Invoke(message);

    // ───── 일시정지 (HUD-02) ─────

    /// <summary>게임 시간 전체(손님 생성·조리·식사·이동·인내심)를 멈춘다. UI 전환은 실제 시간으로 돈다.</summary>
    public void SetPaused(bool paused)
    {
        if (paused && Phase != DayLoopPhase.Open && Phase != DayLoopPhase.Closing)
            paused = false;

        IsPaused = paused;
        ApplyTimeScale();
    }

    /// <summary>안내를 읽는 동안 게임 시간을 멈춘다. 영업 중에만 멈출 수 있다.</summary>
    public void SetGuidePaused(bool paused)
    {
        if (paused && Phase != DayLoopPhase.Open && Phase != DayLoopPhase.Closing)
            paused = false;

        IsGuidePaused = paused;
        ApplyTimeScale();
    }

    private void HandleSpeedChanged(bool fast)
    {
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = IsPaused || IsGuidePaused ? 0f : RestaurantSpeedController.CurrentSpeed;
    }

    // ───── 준비 (GUIDE-01) ─────

    public void CountStaff(out int servers, out int chefs)
    {
        if (GameSession.IsActive)
        {
            servers = GameSession.Current.employees.FindAll(e => e.role == "Serving").Count;
            chefs = GameSession.Current.employees.FindAll(e => e.role == "Kitchen").Count;
            return;
        }
        servers = FindObjectsByType<ServerAgent>(FindObjectsSortMode.None).Length;
        chefs = FindObjectsByType<ChefAgent>(FindObjectsSortMode.None).Length;
    }

    /// <summary>실제로 영업할 수 없는 조건만 이유를 돌려준다. 영업 가능하면 null.</summary>
    public string GetStartBlockReason()
    {
        if (GameSession.IsActive && GameSession.Current.progression.constructionUntilDay > GameSession.Current.day) return "공사 중에는 영업할 수 없어요.";
        MenuManager menu = MenuManager.Instance;
        if (menu == null || menu.DailyFoods.Count == 0)
            return "오늘의 메뉴가 없어요. 섬의 메뉴판에서 메뉴를 등록해 주세요.";

        foreach (var food in menu.DailyFoods)
        {
            if (GameSession.IsActive && !CookwareRules.CanCook(GameSession.Current, food)) return "메뉴에 필요한 도구 또는 도구 강화가 부족해요.";
            if (IngredientInventoryService.Instance == null || IngredientInventoryService.Instance.CraftableCount(food) < 1) return "메뉴 재료가 부족해요.";
        }
        if (ChefManager.Instance != null)
        {
            bool anyCookable = false;
            foreach (RecipeData food in menu.DailyFoods)
            {
                if (food != null && ChefManager.Instance.GetCookingToolTransform(food.ClassId == 205 ? CookingType.Pan : (CookingType)food.ClassId) != null)
                {
                    anyCookable = true;
                    break;
                }
            }
            if (!anyCookable)
                return "오늘의 메뉴를 만들 조리도구가 주방에 없어요.";
        }

        CountStaff(out int servers, out int chefs);
        if (servers == 0) return "홀 직원이 없어요.";
        if (chefs == 0) return "주방 직원이 없어요.";

        SeatManager seats = _rgm != null ? _rgm.seatManager : null;
        if (TableManager.Instance != null && TableManager.Instance.GetPlacedTableCount() <= 0)
            return "테이블을 1개 이상 배치해 주세요.";
        if (seats != null && seats.IndoorSeatCount <= 0)
            return "손님이 앉을 수 있는 좌석이 없어요.";

        return null;
    }

    private void HandleStartBlocked(string reason)
    {
        Toast("영업을 시작할 수 없어요 — " + reason);
    }

    // ───── 영업 ─────

    private void HandleOperationStarted()
    {
        _openDay = Day;
        _servers.Clear();
        _servers.AddRange(FindObjectsByType<ServerAgent>(FindObjectsSortMode.None));

        // 기존 운영 캔버스(재화·2배속 버튼)는 이 HUD가 대신 보여 주므로 숨긴다. (중복 표시 방지)
        HideLegacyOperationCanvas();

        Ledger.Begin(CurrentGold);
        GameFlow.SetPhase(GamePhase.Operation);
        SetPhase(DayLoopPhase.Open);
        Toast($"{_openDay}일차 영업을 시작해요!");
    }

    private void HandleEntryClosed()
    {
        if (Phase != DayLoopPhase.Open) return;
        SetPhase(DayLoopPhase.Closing);
        Toast("오늘의 입장이 마감됐어요");
    }

    private void Update()
    {
        if (Phase == DayLoopPhase.Preparation && Time.unscaledTime >= _nextBoardRefresh)
        {
            _nextBoardRefresh = Time.unscaledTime + 0.5f;
            RefreshPreparationBoard();
        }

        if ((Phase != DayLoopPhase.Open && Phase != DayLoopPhase.Closing) || _settling) return;

        // 영업·마감 정지 방지: 남은 손님이 있는데 오래 아무 진행이 없으면 미대접으로 내보낸다.
        if (Spawner != null && Spawner.ActiveCustomerCount > 0 && Time.time - Ledger.LastProgressTime > StallTimeout)
        {
            CustomerAgent[] remaining = FindObjectsByType<CustomerAgent>(FindObjectsSortMode.None);
            Debug.LogWarning($"[DayLoop] 마감 중 {StallTimeout:0}초 동안 진행이 없어 남은 손님 {remaining.Length}명을 내보냅니다.");
            foreach (CustomerAgent customer in remaining)
            {
                if (customer.State == CustomerState.Exit) continue;
                if (!customer.WasServed) Ledger.MarkForcedLeave();
                customer.ForceLeave();
            }
        }
    }

    // ───── 결산 ─────

    // 마지막 손님이 나가면 RestaurantGameManager.EndOperation을 거쳐 한 번 호출된다.
    private void HandleOperationEnded()
    {
        if (_settling) return;
        _settling = true;
        StartCoroutine(SettleRoutine());
    }

    private IEnumerator SettleRoutine()
    {
        SetPaused(false);
        SetGuidePaused(false);
        RestaurantSpeedController.SetFastForward(false);
        if (OperationUIManager.Instance != null) OperationUIManager.Instance.HideUI();

        // 신규 입장이 없었던 경우(손님 0명)에도 마감 단계를 거친다.
        if (Phase == DayLoopPhase.Open) SetPhase(DayLoopPhase.Closing);
        Toast("오늘 영업이 끝났어요");
        yield return new WaitForSecondsRealtime(1.2f);

        // 집계 고정: 이후 들어오는 이벤트는 장부에 반영하지 않는다.
        Ledger.Stop();
        int day = _openDay > 0 ? _openDay : Day;
        _record = Ledger.BuildRecord(day, CurrentGold);

        RestaurantRatingManager rating = RestaurantRatingManager.Instance;
        if (rating != null)
        {
            _record.previousRating = rating.RestaurantRating;
            _record.todayRating = rating.TodayAverage;
            _record.ratingCount = rating.TodayScoreCount;
            rating.CommitTodayRating(day);   // 같은 날은 한 번만 확정된다
            _record.totalRating = rating.RestaurantRating;
            _record.seedRatingIncluded = rating.HasSeedRating;
        }

        if (_record.goldAtClose - _record.goldAtOpen != _record.netProfit)
            Debug.LogWarning($"[DayLoop] 골드 변화({_record.goldAtClose - _record.goldAtOpen})와 순이익({_record.netProfit})이 다릅니다. 집계 밖 거래가 있는지 확인하세요.");

        GameFlow.SetPhase(GamePhase.ClosingReport);
        SetPhase(DayLoopPhase.Settlement);

        _settlement.Show(_record, "섬에서 쉬기", GoToNextDay, CommitSettlement);
        CommitSettlement();

        // 당일 집계와 작업 배정만 초기화한다. (골드·가구·직원·해금은 그대로)
        _rgm.ResetDailyState();
    }

    private void CommitSettlement()
    {
        if (_committed) return;

        _settlement.SetSaveState(SettlementPanel.SaveState.Saving);
        if (DayLoopSave.TryCommitDay(_record, out string error))
        {
            _committed = true;
            _settlement.SetSaveState(SettlementPanel.SaveState.Saved);
        }
        else
        {
            _settlement.SetSaveState(SettlementPanel.SaveState.Failed, error);
        }
    }

    private void GoToNextDay()
    {
        if (!_committed) return;
        DayLoopSave.AcknowledgeSettlement();
        GameFlow.EnterDayStart();
    }

    // ───── TEST 씬 정리 ─────

    private void TidySceneForTest()
    {
        // 직원 대기 위치 표시용 흰 삼각형: 의미 없는 상시 표시라 숨긴다.
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr.gameObject.name == "Triangle")
                sr.enabled = false;
        }

        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string value = text.text != null ? text.text.Trim() : "";

            // 한글 글리프가 없는 기본 폰트(LiberationSans)를 쓰는 텍스트는 글자가 깨지므로 한글 폰트로 바꾼다.
            // (자리 배치에서 테이블을 눌렀을 때 뜨는 이동·삭제·취소 메뉴 등)
            if (text.font != null && text.font.name.StartsWith("LiberationSans") && ContainsHangul(value) && DayLoopUI.Font != null)
                text.font = DayLoopUI.Font;

            // 준비 화면 메뉴판의 임시 문구(날짜·평점·식당 이름·오늘의 메뉴)는 실제 값으로 바꾼다.
            bool onBoard = text.transform.root.name == "PreOperation";
            if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{4}/\d{1,2}/\d{1,2}$"))
                _boardDay = text;
            else if (onBoard && value.StartsWith("평점"))
                _boardRating = text;
            else if (onBoard && value.EndsWith("레스토랑"))
                _boardName = text;
            else if (onBoard && value.StartsWith("오늘의 메뉴"))
                _boardMenu = text;

            // 개발용 작업 목록 버튼은 개발 빌드에서만 보이고, 상단 HUD와 겹치지 않게 왼쪽 아래로 옮긴다.
            if (value == "주방 Task" || value == "서빙 Task")
            {
                Button button = text.GetComponentInParent<Button>();
                if (button == null) continue;
                if (!Debug.isDebugBuild)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                RectTransform rt = (RectTransform)button.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, value == "주방 Task" ? 12f : 48f);
            }
        }

        // 재화 테스트 UI는 개발 빌드에서만 보인다.
        if (!Debug.isDebugBuild)
        {
            foreach (TestCurrencyUI test in FindObjectsByType<TestCurrencyUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                test.gameObject.SetActive(false);
        }
    }

    private static bool ContainsHangul(string value)
    {
        foreach (char c in value)
        {
            if (c >= '\uAC00' && c <= '\uD7A3') return true;
        }
        return false;
    }

    private void RefreshPreparationBoard()
    {
        if (_boardDay != null)
            _boardDay.text = $"{Day}일차";

        if (_boardName != null)
            _boardName.text = RestaurantName;

        if (_boardRating != null)
        {
            RestaurantRatingManager rating = RestaurantRatingManager.Instance;
            float value = rating != null ? rating.RestaurantRating : 0f;
            _boardRating.text = value > 0f ? $"평점 : {DayLoopUI.Rating(value)}/5.0" : "평점 : -/5.0";
        }

        if (_boardMenu != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("오늘의 메뉴\n\n");
            MenuManager menu = MenuManager.Instance;
            int count = menu != null ? menu.DailyFoods.Count : 0;
            if (count == 0)
            {
                sb.Append("등록된 메뉴 없음");
            }
            else
            {
                int shown = count > MenuBoardLines ? MenuBoardLines - 1 : count;
                for (int i = 0; i < shown; i++)
                {
                    RecipeData food = menu.DailyFoods[i];
                    if (food != null) sb.Append(food.RecipeName).Append('\n');
                }
                if (shown < count) sb.Append("외 ").Append(count - shown).Append("개");
            }
            _boardMenu.text = sb.ToString().TrimEnd('\n');
        }
    }

    private static void HideLegacyOperationCanvas()
    {
        if (OperationUIManager.Instance == null) return;
        foreach (RestaurantSpeedButton speed in FindObjectsByType<RestaurantSpeedButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            HideCanvasOf(speed.transform);
    }

    private static void HideCanvasOf(Transform child)
    {
        Canvas canvas = child.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        CanvasGroup group = canvas.GetComponent<CanvasGroup>();
        if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
