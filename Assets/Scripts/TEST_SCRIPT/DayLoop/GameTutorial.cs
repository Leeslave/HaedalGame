using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼. 해야 할 일을 한 단계씩 짚어 준다. 짚는 곳만 눌리도록 나머지 화면은 어둡게 막는다.
/// 단계는 저장 데이터로 정하므로 중간에 껐다 켜도 이어진다. [튜토리얼 건너뛰기]를 누르면 그 안내는 다시 나오지 않는다.
///
/// 1일차 (새 게임)
///  1 섬: [관리] 열기 → 2 첫 조리도구 무료로 받기 → 3 오늘의 메뉴 정하기 → 4 관리 창 닫고 [영업 준비]로 식당 가기
///  5 식당: [운영 시작] → (영업 중에는 FirstDayGuide가 첫 손님을 따라가며 안내) → 6 결산 확인 후 섬으로
///  7 섬: 집에서 취침·저장 → 2일차가 되면 끝
///
/// 2일차부터는 기능 안내(Lesson)를 필요한 순간에 하나씩 띄운다. 안내마다 한 번만 나오고(Tutorial.{이름}.Done),
/// 하나가 진행 중이면 끝날 때까지 다른 안내가 끼어들지 않는다. 시작 조건을 먼저 채운 순서(Lessons 순서)대로 나온다.
///  재료 채우기 (2일차 영업 전. 재료가 넉넉해도 일부러 한 번 사 보게 한다. 오늘 메뉴가 있어야 한다)
///    지도 [상점] → 오늘 메뉴에 필요한 재료 담고 사기 → 마무리. 안내 전에 스스로 사면 아는 것으로 보고 끝낸다.
///    가장 적게 남은 메뉴가 RestockBelow인분 아래면 "떨어져 간다", 아니면 "미리 채워 보자"고 말한다.
///  미션 보상 (2일차~, 미션을 채우고 아직 보상을 받지 않았을 때) 섬 화면 미션 카드를 눌러 받기
///  주급 날 (5일차~, 직원이 있고 주급날이 이틀 안으로 다가왔을 때) 7일마다 잠들 때 주급이 나간다는 안내
///  평점과 폐업 (2일차~ 결산) 식당 평점이 무엇이고 폐업 기준이 얼마인지
///  자유시간 (2일차~, 영업을 마친 뒤 섬) 지도 [연구소] → 새 요리 배우는 순서 → 선착장(주민·탐험)
///  식당 키우기 (3일차~, 영업 전 섬) 관리 > 직원(고용·배치) → 관리 > 가구 배치(테이블 사서 놓기)
///  식당 증축 (8일차~, 섬) 지도 [대장간] → 식당 레벨과 조리도구 강화
/// 짚는 곳이 지도 표식인데 관리 창이 열려 있거나 식당에 있으면, 창 안에서 바로 열 수 있게 버튼으로 묻는다.
/// </summary>
public class GameTutorial : MonoBehaviour
{
    private const string DoneId = "Tutorial.FirstDay.Done";
    private const string MenuId = "Tutorial.FirstDay.Menu";
    private const int StepCount = 7;
    private const string RestockStartedId = "Tutorial.Restock.Started";
    private const int RestockBelow = 5;
    private const string NeededFilter = "오늘 메뉴에 필요";
    private const float HolePadding = 10f;
    // 관리 창 본문(왼쪽 450부터)을 짚을 때 왼쪽 띠 안에 말풍선이 들어가는 폭.
    private const float BubbleWidth = 400f;

    private enum Step
    {
        None, OpenManagement, ReceiveTool, ChooseMenu, GoRestaurant, StartOperation, Operating, Settlement, Sleep,
        RestockShop, RestockBuy, RestockDone,
        MissionClaim,
        PayrollNotice,
        RatingExplain,
        FreeLab, FreeResearch, FreeDock,
        GrowthStaff, GrowthFurniture,
        ExpandForge, ExpandLevel,
    }

    private enum Lesson { None, Restock, Mission, Payroll, Rating, FreeTime, Growth, Expansion }

    /// <summary>아직 시작하지 않은 안내끼리는 이 순서로 먼저 띄운다.</summary>
    private static readonly Lesson[] Lessons = { Lesson.Restock, Lesson.Mission, Lesson.Payroll, Lesson.Rating, Lesson.FreeTime, Lesson.Growth, Lesson.Expansion };

    /// <summary>한 번의 안내: 무엇을 짚고 무엇을 말할지.</summary>
    private struct Hint
    {
        public string key, title, body, action;
        public Action run;
        public Func<bool> actionEnabled;
        public RectTransform target;
        public bool soft; // 화면을 막지 않고 표시만 한다(결산처럼 읽을 거리가 있는 화면).
    }

    private Scene _scene;
    private Canvas _canvas;
    private RectTransform _root, _frame, _bubble;
    private readonly Image[] _dims = new Image[4];
    private TMP_Text _bubbleTitle, _bubbleBody, _bubbleChip;
    private Button _bubbleAction, _skip;
    private Lesson _lesson;                                           // 진행 중인 안내
    private readonly HashSet<Step> _read = new HashSet<Step>();       // [다음]으로 넘긴 설명 단계 (이 씬에서만)
    private Func<bool> _actionEnabled;
    private string _shownKey;
    private Step _lastStep = Step.None;
    private Hint _hint;
    private float _nextLookup;
    private DayCycleController _day;
    private Button _watchedStart;
    private float _startCheckAt;
    private bool _startRefused;

    // ───── 설치 ─────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameScenes.Island && scene.name != GameScenes.Restaurant) return;
        if (FindObjectsByType<GameTutorial>(FindObjectsSortMode.None).Any(t => t.gameObject.scene == scene)) return;
        var go = new GameObject("[Tutorial] Game");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<GameTutorial>();
    }

    private void Start()
    {
        _scene = gameObject.scene;
        _canvas = DayLoopUI.CreateCanvas("[Tutorial] Overlay", 400, _scene);
        _canvas.transform.SetParent(transform, false);
        _root = (RectTransform)_canvas.transform;
        Build();
        _root.gameObject.SetActive(false);
    }

    // ───── 단계 ─────

    private static bool Running => GameSession.IsActive && GameSession.Current.day == 1 && !GameSession.Current.HasTutorialCompleted(DoneId);

    private Step Current()
    {
        if (Running) return FirstDayStep();
        if (!GameSession.IsActive || GameSession.Current.day < 2) return Step.None;
        if (_lesson != Lesson.None && !IsDone(_lesson))
        {
            Step step = StepOf(_lesson);
            if (step != Step.None) return step;
        }
        foreach (Lesson lesson in Lessons)
        {
            if (lesson == _lesson || IsDone(lesson)) continue;
            Step step = StepOf(lesson);
            if (step == Step.None) continue;
            _lesson = lesson;
            return step;
        }
        return Step.None;
    }

    private static string DoneKey(Lesson lesson) => $"Tutorial.{lesson}.Done";
    private static bool IsDone(Lesson lesson) => GameSession.Current.HasTutorialCompleted(DoneKey(lesson));

    private void Finish(Lesson lesson)
    {
        GameSession.TryMarkTutorialCompleted(DoneKey(lesson));
        if (_lesson == lesson) _lesson = Lesson.None;
    }

    private Step StepOf(Lesson lesson) => lesson switch
    {
        Lesson.Restock => RestockStep(),
        Lesson.Mission => MissionStep(),
        Lesson.Payroll => PayrollStep(),
        Lesson.Rating => RatingStep(),
        Lesson.FreeTime => FreeTimeStep(),
        Lesson.Growth => GrowthStep(),
        Lesson.Expansion => ExpansionStep(),
        _ => Step.None,
    };

    private static Lesson LessonOf(Step step) => step switch
    {
        Step.RestockShop or Step.RestockBuy or Step.RestockDone => Lesson.Restock,
        Step.MissionClaim => Lesson.Mission,
        Step.PayrollNotice => Lesson.Payroll,
        Step.RatingExplain => Lesson.Rating,
        Step.FreeLab or Step.FreeResearch or Step.FreeDock => Lesson.FreeTime,
        Step.GrowthStaff or Step.GrowthFurniture => Lesson.Growth,
        Step.ExpandForge or Step.ExpandLevel => Lesson.Expansion,
        _ => Lesson.None,
    };

    private bool OnIsland => _scene.name == GameScenes.Island;

    /// <summary>식당 씬에서 지금 하루 루프 단계가 phase인지.</summary>
    private bool InRestaurantPhase(DayLoopPhase phase)
    {
        if (_scene.name != GameScenes.Restaurant) return false;
        if (_day == null) _day = FindFirstObjectByType<DayCycleController>();
        return _day != null && _day.Phase == phase;
    }

    private Step FirstDayStep()
    {
        GameSaveData s = GameSession.Current;
        if (_scene.name == GameScenes.Restaurant)
        {
            if (_day == null) _day = FindFirstObjectByType<DayCycleController>();
            if (!s.progression.firstToolChosen) return Step.ReceiveTool;
            if (_day == null) return Step.None;
            switch (_day.Phase)
            {
                case DayLoopPhase.Preparation: return GameSession.OperationCompletedToday ? Step.None : Step.StartOperation;
                case DayLoopPhase.Settlement: return Step.Settlement;
                default: return Step.Operating;
            }
        }
        if (GameSession.OperationCompletedToday) return Step.Sleep;
        if (!s.progression.firstToolChosen) return ManagementOpen || FirstToolPicker.IsOpen ? Step.ReceiveTool : Step.OpenManagement;
        if (!s.HasTutorialCompleted(MenuId)) return Step.ChooseMenu;
        return Step.GoRestaurant;
    }

    private Step RestockStep()
    {
        GameSaveData s = GameSession.Current;
        if (GameSession.OperationCompletedToday) return Step.None;
        if (!OnIsland && !InRestaurantPhase(DayLoopPhase.Preparation)) return Step.None;
        bool started = s.HasTutorialCompleted(RestockStartedId);
        if (BoughtToday(s))
        {
            if (started) return Step.RestockDone;
            Finish(Lesson.Restock);   // 안내가 뜨기 전에 스스로 샀다
            return Step.None;
        }
        if (!started)
        {
            if (LowestMenu(out _) == null) return Step.None;   // 오늘 메뉴가 없으면 상점 필터가 비어 있다
            GameSession.TryMarkTutorialCompleted(RestockStartedId);
        }
        return RestaurantManagementUI.OpenTab == "상점" ? Step.RestockBuy : Step.RestockShop;
    }

    private Step MissionStep()
    {
        if (!OnIsland || ManagementOpen) return Step.None;
        GameSaveData s = GameSession.Current;
        RestaurantManagementUI.Mission(s, out _, out int target);
        bool complete = RestaurantManagementUI.MissionProgress(s) >= target;
        if (_lesson == Lesson.Mission && !complete) { Finish(Lesson.Mission); return Step.None; }   // 보상을 받았다
        return complete ? Step.MissionClaim : Step.None;
    }

    private Step PayrollStep()
    {
        GameSaveData s = GameSession.Current;
        if (!OnIsland || ManagementOpen || s.day < 5 || s.employees.Count == 0) return Step.None;
        return DaysToWage(s) <= 2 ? Step.PayrollNotice : Step.None;
    }

    private Step RatingStep()
    {
        // 폐업 카드가 떠 있으면 결산 카드는 가려져 있다.
        if (!InRestaurantPhase(DayLoopPhase.Settlement) || FindCanvas("[DayLoop] Closure") != null) return Step.None;
        return Step.RatingExplain;
    }

    private Step FreeTimeStep()
    {
        if (!OnIsland || !GameSession.OperationCompletedToday) return Step.None;
        if (!_read.Contains(Step.FreeResearch)) return RestaurantManagementUI.OpenTab == "연구" ? Step.FreeResearch : Step.FreeLab;
        return Step.FreeDock;
    }

    private Step GrowthStep()
    {
        if (!OnIsland || GameSession.OperationCompletedToday || GameSession.Current.day < 3) return Step.None;
        return _read.Contains(Step.GrowthStaff) ? Step.GrowthFurniture : Step.GrowthStaff;
    }

    private Step ExpansionStep()
    {
        if (!OnIsland || GameSession.Current.day < 8) return Step.None;
        return RestaurantManagementUI.OpenTab == "대장간" ? Step.ExpandLevel : Step.ExpandForge;
    }

    private static int DaysToWage(GameSaveData s) => (7 - s.day % 7) % 7;

    /// <summary>말풍선 머리의 단계 표시. (번호, 전체 단계 수)</summary>
    private static (int number, int total) Position(Step step) => step switch
    {
        Step.OpenManagement => (1, StepCount),
        Step.ReceiveTool => (2, StepCount),
        Step.ChooseMenu => (3, StepCount),
        Step.GoRestaurant => (4, StepCount),
        Step.StartOperation => (5, StepCount),
        Step.Settlement => (6, StepCount),
        Step.Sleep => (7, StepCount),
        Step.RestockShop => (1, 3),
        Step.RestockBuy => (2, 3),
        Step.RestockDone => (3, 3),
        Step.FreeLab => (1, 3),
        Step.FreeResearch => (2, 3),
        Step.FreeDock => (3, 3),
        Step.GrowthStaff => (1, 2),
        Step.GrowthFurniture => (2, 2),
        Step.ExpandForge => (1, 2),
        Step.ExpandLevel => (2, 2),
        _ => (1, 1),
    };

    /// <summary>지금 단계에서 짚을 곳과 말. 짚을 곳이 아직 없으면 null을 돌려 안내를 숨긴다.</summary>
    private Hint? Describe(Step step)
    {
        switch (step)
        {
            case Step.OpenManagement:
                return Target("관리 열기", "식당 운영에 필요한 일은 관리 창에서 해요.\n왼쪽 아래 [관리]를 눌러 볼까요?",
                    Find("[DayLoop] IslandHUD", "관리"));

            case Step.ReceiveTool:
                if (FirstToolPicker.IsOpen)
                    return Target("첫 조리도구 받기", "처음 도구 하나는 무료예요. 마음에 드는 도구를 고르고 아래 [무료로 받기]를 눌러요.\n그 도구로 만드는 기본 메뉴가 함께 생겨요.",
                        Find("[FirstTool] Picker", "Card"), "picker");
                return Ask("첫 조리도구 받기", "아직 첫 조리도구를 받지 않았어요. 도구가 있어야 요리를 팔 수 있어요.", "도구 고르기",
                    () => FirstToolPicker.Show(_scene, OpenMenu));

            case Step.ChooseMenu:
                if (Find("Restaurant management", "Slots") != null)
                {
                    string menu = FirstMenuName();
                    string body = menu != null
                        ? $"'{menu}'{Particle(menu)} 오늘의 메뉴에 올라갔어요.\n메뉴 카드를 누르고 오른쪽 아래 버튼으로 등록하거나 뺄 수 있어요."
                        : "메뉴 카드를 누르고 오른쪽 아래 [오늘 메뉴에 등록]을 눌러요.";
                    var hint = Target("오늘의 메뉴 정하기", body, Find("Restaurant management", "ContentFrame"), menu ?? "empty");
                    if (hint == null) return null;
                    var value = hint.Value;
                    value.action = "메뉴 정했어요";
                    value.run = () => GameSession.TryMarkTutorialCompleted(MenuId);
                    value.actionEnabled = () => GameSession.Current.menuSlots.Any(id => id >= 0);
                    return value;
                }
                return Ask("오늘의 메뉴 정하기", "오늘 팔 메뉴를 정해요.", "메뉴 열기", OpenMenu);

            case Step.GoRestaurant:
                if (ManagementOpen)
                    return Target("식당으로 가기", "준비가 끝났어요. 관리 창을 닫아요.", Find("Restaurant management", "Close"), "close");
                return Target("식당으로 가기", "[영업 준비]를 눌러 식당으로 가요.", Find("[DayLoop] IslandHUD", "영업 준비"), "go");

            case Step.StartOperation:
                if (ManagementOpen)
                    return Target("영업 시작", "관리 창을 닫고 영업을 시작해요.", Find("Restaurant management", "Close"), "close");
                RectTransform start = Find("[DayLoop] Guide", "운영 시작");
                WatchStart(start);
                if (_startRefused)
                {
                    // 영업을 시작하지 못했으면 화면을 풀어 준비 카드의 안내대로 고칠 수 있게 한다.
                    var refused = Target("영업 시작", "아직 영업을 시작할 수 없어요. 왼쪽 준비 카드의 안내를 확인하고 다시 [운영 시작]을 눌러요.", start, "refused");
                    if (refused == null) return null;
                    var open = refused.Value; open.soft = true;
                    return open;
                }
                return Target("영업 시작", "식당에 왔어요! [운영 시작]을 누르면 손님이 들어와요.\n주문부터 계산까지는 직원이 알아서 해요.", start);

            case Step.Settlement:
                var settle = Target("오늘의 결산", "첫 영업 끝! 번 돈과 손님 평가를 확인하고 섬으로 돌아가요.",
                    Find("[DayLoop] Settlement", "섬으로 돌아가기"));
                if (settle == null) return null;
                var soft = settle.Value; soft.soft = true;
                return soft;

            case Step.Sleep:
                if (FindCanvas("[DayLoop] Settlement") != null) return null; // 다시 보여 주는 결산은 그대로 둔다.
                RectTransform sleep = FindButton("Design / 09 House", "취침");
                if (sleep != null)
                    return Target("하루 마무리", "[취침 · 저장]을 누르면 첫날이 끝나고 2일차 아침으로 저장돼요.", sleep, "house");
                return Target("하루 마무리", "영업을 마치면 자유시간이에요.\n집에서 잠들면 하루가 저장돼요. [집]을 눌러요.", Find("[DayLoop] IslandHUD", "Map 집"), "map");

            case Step.RestockShop:
                RecipeData low = LowestMenu(out int left);
                string need = low == null ? "재료는 상점에서 사요.\n재료가 없으면 그 메뉴는 품절돼요."
                    : left == 0 ? $"'{low.RecipeName}' 재료가 다 떨어졌어요.\n재료가 없으면 그 메뉴는 품절돼요."
                    : left < RestockBelow ? $"'{low.RecipeName}' 재료가 {left}인분 남았어요.\n재료가 없으면 그 메뉴는 품절돼요."
                    : $"재료는 상점에서 사요. 지금 '{low.RecipeName}'{Particle(low.RecipeName)} {left}인분 남았는데,\n떨어지면 그 메뉴는 품절되니 미리 조금 채워 봐요.";
                return MapOrAsk("재료 채우기", need, "상점", "상점 열기", "상점");

            case Step.RestockBuy:
                return Target("재료 사기", "오늘 메뉴에 필요한 재료만 모아 뒀어요.\n재료 카드를 누를 때마다 1개씩 장바구니에 담겨요. 하나만 담아도 돼요. 다 담았으면 오른쪽 아래 [사기]를 눌러요.",
                    Find("Restaurant management", "ContentFrame"), "shop");

            case Step.RestockDone:
                return Ask("재료 채우기 끝", "재료를 샀어요! 산 재료는 바로 창고에 들어가요.\n남은 재고는 관리 > 재료에서, 메뉴별 남은 인분은 영업 화면의 메뉴 그림에서 볼 수 있어요.",
                    "좋아요", () => Finish(Lesson.Restock));

            case Step.MissionClaim:
                int reward = 200 + GameSession.Current.progression.missionIndex * 100;
                return Target("미션 보상", $"미션을 다 채웠어요! 미션 카드를 누르면 {DayLoopUI.Gold(reward)}를 받고 다음 미션이 열려요.\n미션은 날짜와 상관없이 채울 때까지 이어져요.",
                    Find("[DayLoop] IslandHUD", "Mission"));

            case Step.PayrollNotice:
            {
                GameSaveData s = GameSession.Current;
                int days = DaysToWage(s);
                string when = days == 0 ? "오늘 밤" : $"{days}일 뒤({s.day + days}일차) 밤";
                return Acknowledge(Target("주급 날", $"{when} 잠들 때 직원 주급 {DayLoopUI.Gold(s.employees.Sum(e => e.wage))}가 나가요. 주급은 7일마다 나가요.\n골드가 모자라면 잠들기 전에 직원을 정리해야 해요. 남은 날과 금액은 [집]에서 볼 수 있어요.",
                    Find("[DayLoop] IslandHUD", "Map 집"), "map"), "알겠어요", () => Finish(Lesson.Payroll));
            }

            case Step.RatingExplain:
            {
                float threshold = RatingWarningUI.Threshold(GameSession.Current.restaurantLevel);
                var rating = Acknowledge(Target("평점과 폐업", $"식당 평점은 최근 7일 영업 평점의 평균이에요. 평점이 {DayLoopUI.Rating(threshold)}보다 낮아지면 식당이 문을 닫아요.\n좋은 등급 재료로 만든 음식과 손님이 좋아하는 메뉴가 평점을 올리고, 식당에 쓰레기가 남으면 그날 평점이 깎여요.",
                    Find("[DayLoop] Settlement", "Stat 식당 평점")), "알겠어요", () => Finish(Lesson.Rating));
                if (rating == null) return null;
                var calm = rating.Value; calm.soft = true;   // 결산은 읽을 거리가 많아 막지 않는다
                return calm;
            }

            case Step.FreeLab:
                return MapOrAsk("자유시간", "영업을 마치면 잠들기 전까지 자유시간이에요.\n연구소에서 새 요리를 배울 수 있어요.", "연구소", "연구소 열기", "연구");

            case Step.FreeResearch:
                return Explain("새 요리 배우기", "새 요리는 설계도 → 재료 예약 → 며칠 뒤 실습 순서로 배워요. 실습 평균이 4점 이상이면 메뉴에 올릴 수 있어요.\n설계도는 여기서 사거나, 주민의 음식 부탁을 들어주면 받아요.",
                    "다음", () => _read.Add(Step.FreeResearch));

            case Step.FreeDock:
                if (ManagementOpen)
                    return Target("선착장", "관리 창을 닫고 선착장도 둘러봐요.", Find("Restaurant management", "Close"), "close");
                return Acknowledge(Target("선착장", "선착장에서는 주민을 만나 음식 부탁을 받거나, 직원을 탐험에 보내 재료를 얻어 와요.\n잠들기 전에 한번 들러 봐요.",
                    Find("[DayLoop] IslandHUD", "Map 선착장 · 탐험"), "map"), "알겠어요", () => Finish(Lesson.FreeTime));

            case Step.GrowthStaff:
                if (RestaurantManagementUI.OpenTab == "직원")
                    return Explain("직원 고용", "[빈 자리] 카드를 누르면 스카우트로 후보를 만나 고용할 수 있어요.\n직원마다 주급이 있어요. 직원 카드를 누르면 홀이나 주방에 배치해요.",
                        "다음", () => { _read.Add(Step.GrowthStaff); RestaurantManagementUI.OpenSection("가구"); });
                return Ask("식당 키우기", "손님이 늘면 일손과 자리가 모자라요.\n관리 창에서 직원을 고용하고 테이블을 더 놓을 수 있어요.", "직원 보기",
                    () => RestaurantManagementUI.OpenSection("직원"));

            case Step.GrowthFurniture:
                if (RestaurantManagementUI.OpenTab == "가구")
                    return Explain("테이블 늘리기", $"테이블을 사서 [놓으러 가기]를 누르면 식당에 놓을 수 있어요.\n좌석은 식당 레벨마다 한도가 있어요. 지금은 최대 {RestaurantRules.SeatLimit(GameSession.Current.restaurantLevel)}석이에요.",
                        "알겠어요", () => Finish(Lesson.Growth));
                return Ask("테이블 늘리기", "자리가 늘면 한 번에 더 많은 손님을 받을 수 있어요.", "가구 보기", () => RestaurantManagementUI.OpenSection("가구"));

            case Step.ExpandForge:
                return MapOrAsk("식당 증축", "식당이 자리를 잡았어요!\n대장간에서 식당 레벨을 올릴 수 있어요.", "대장간", "대장간 열기", "대장간");

            case Step.ExpandLevel:
                return Explain("식당 레벨", "레벨을 올리면 좌석과 메뉴 칸이 늘어요. 골드와 평점 조건을 채워야 하고, 공사하는 동안은 휴업해요.\n조리도구는 관리 > 조리도구에서 강화하면 더 많은 재료를 쓸 수 있어요.",
                    "알겠어요", () => Finish(Lesson.Expansion));
        }
        return null;
    }

    /// <summary>
    /// 지도 표식을 짚는다. 관리 창이 열려 있으면 표식이 창에 가리고, 식당에는 지도가 없으므로
    /// 그때는 해당 관리 탭을 바로 여는 버튼으로 묻는다.
    /// </summary>
    private Hint? MapOrAsk(string title, string body, string marker, string action, string section)
    {
        if (ManagementOpen || !OnIsland)
            return Ask(title, body, action, () => RestaurantManagementUI.OpenSection(section));
        return Target(title, $"{body} 지도에서 [{marker}]{ObjectParticle(marker)} 눌러요.", Find("[DayLoop] IslandHUD", "Map " + marker), "map");
    }

    /// <summary>열린 관리 창 본문을 짚고 설명한다. 버튼을 눌러야 넘어간다.</summary>
    private Hint? Explain(string title, string body, string action, Action run)
        => Acknowledge(Target(title, body, Find("Restaurant management", "ContentFrame"), "content"), action, run);

    private static Hint? Acknowledge(Hint? hint, string action, Action run)
    {
        if (hint == null) return null;
        var value = hint.Value;
        value.action = action; value.run = run;
        return value;
    }

    private static Hint? Target(string title, string body, RectTransform target, string variant = "")
    {
        if (target == null) return null;
        return new Hint { key = title + variant, title = title, body = body, target = target };
    }

    private static Hint Ask(string title, string body, string action, Action run)
        => new Hint { key = title + "?", title = title, body = body, action = action, run = run };

    private static void OpenMenu() => RestaurantManagementUI.OpenSection("메뉴");

    /// <summary>오늘 메뉴 중 재료가 가장 적게 남은 메뉴. 메뉴가 없으면 null.</summary>
    private static RecipeData LowestMenu(out int portions)
    {
        portions = 0;
        RecipeData lowest = null;
        var inventory = IngredientInventoryService.Instance;
        if (inventory == null) return null;
        foreach (int id in GameSession.Current.menuSlots)
        {
            if (id < 0 || !GameDatabase.TryGetRecipe(id, out RecipeData recipe)) continue;
            int count = inventory.CraftableCount(recipe);
            if (lowest != null && count >= portions) continue;
            lowest = recipe; portions = count;
        }
        return lowest;
    }

    private static bool BoughtToday(GameSaveData s)
        => s.progression.regularStockDay == s.day && s.progression.dailyStock.Any(x => x.currentStock < x.maxStock);

    /// <summary>[운영 시작]을 누른 뒤 잠시 지나도 준비 단계면 시작이 막힌 것으로 본다.</summary>
    private void WatchStart(RectTransform start)
    {
        Button button = start != null ? start.GetComponent<Button>() : null;
        if (button == null || button == _watchedStart) return;
        _watchedStart = button;
        button.onClick.AddListener(() => _startCheckAt = Time.unscaledTime + 1f);
    }

    private void Skip()
    {
        Lesson lesson = LessonOf(_lastStep);
        if (lesson == Lesson.None) GameSession.TryMarkTutorialCompleted(DoneId);
        else Finish(lesson);
        _root.gameObject.SetActive(false);
    }

    // ───── 매 프레임 ─────

    private void Update()
    {
        if (_root == null) return;
        if (Time.unscaledTime >= _nextLookup)
        {
            _nextLookup = Time.unscaledTime + .2f;
            Step step = Current();
            // 관리 창을 처음 열면 바로 도구 선택 창을 띄운다.
            if (step == Step.ReceiveTool && _lastStep == Step.OpenManagement && !FirstToolPicker.IsOpen)
                FirstToolPicker.Show(_scene, OpenMenu);
            // 상점을 열면 오늘 메뉴에 필요한 재료만 먼저 보여 준다.
            if (step == Step.RestockBuy && _lastStep != Step.RestockBuy)
                RestaurantManagementUI.ShowFilter(NeededFilter);
            if (step != Step.StartOperation) { _startRefused = false; _startCheckAt = 0f; }
            else if (_startCheckAt > 0f && Time.unscaledTime >= _startCheckAt) { _startRefused = true; _startCheckAt = 0f; }
            _lastStep = step;
            Hint? hint = step == Step.None || step == Step.Operating ? null : Describe(step);
            _root.gameObject.SetActive(hint != null);
            if (hint == null) { _shownKey = null; return; }
            _hint = hint.Value;
            if (_shownKey != _hint.key) Show(step, _hint);
        }
        if (_root.gameObject.activeSelf) Layout();
    }

    // ───── 화면 ─────

    private void Build()
    {
        for (int i = 0; i < _dims.Length; i++)
        {
            _dims[i] = DayLoopUI.Panel(_root, new Color(0.05f, 0.08f, 0.10f, 0.62f), "Dim", false);
            Corner(_dims[i].rectTransform);
        }

        var ring = DayLoopUI.Panel(_root, DayLoopUI.Kit.Coin, "Focus");
        ring.sprite = RingSprite; ring.raycastTarget = false;
        _frame = ring.rectTransform;
        Corner(_frame);

        var bubble = DayLoopUI.Card(_root, DayLoopUI.Kit.Sail, "Bubble", 4f, 6f);
        bubble.GetComponent<Outline>().effectColor = DayLoopUI.Kit.Line;
        _bubble = bubble.rectTransform;
        Corner(_bubble);
        _bubble.sizeDelta = new Vector2(BubbleWidth, 0);
        var layout = DayLoopUI.Vertical(bubble, 0, 12);
        layout.padding = new RectOffset(24, 24, 20, 14);
        bubble.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var header = DayLoopUI.Rect("Header", _bubble);
        DayLoopUI.Layout(header, preferredHeight: 40);
        DayLoopUI.Horizontal(header, 0, 10);
        _bubbleChip = DayLoopUI.Chip(header, "", DayLoopUI.Kit.Sea, Color.white, 19);
        _bubbleTitle = DayLoopUI.Text(header, "", 30, DayLoopUI.Kit.Line, TextAlignmentOptions.MidlineLeft);
        _bubbleTitle.textWrappingMode = TextWrappingModes.NoWrap;
        DayLoopUI.Layout(_bubbleTitle, flexibleWidth: 1, preferredHeight: 40);

        _bubbleBody = DayLoopUI.Text(_bubble, "", 22, DayLoopUI.Kit.Ink, TextAlignmentOptions.TopLeft);
        _bubbleBody.lineSpacing = 8f;

        _bubbleAction = DayLoopUI.KitButton(_bubble, "", DayLoopUI.Kit.Sea, Color.white, 26, () => { _hint.run?.Invoke(); _nextLookup = 0f; }, "Action");
        DayLoopUI.Layout(_bubbleAction, preferredHeight: 64);

        _skip = DayLoopUI.Button(_bubble, "튜토리얼 건너뛰기", Color.clear, DayLoopUI.Kit.InkSoft, 18, Skip, "Skip");
        _skip.GetComponentInChildren<TMP_Text>().font = DesignFonts.Body;
        DayLoopUI.Layout(_skip, preferredHeight: 34);
    }

    private void Show(Step step, Hint hint)
    {
        _shownKey = hint.key;
        var (number, total) = Position(step);
        _bubbleChip.text = total > 1 ? $"{number} / {total}" : "알아 두기";
        DayLoopUI.SetButtonLabel(_skip, LessonOf(step) == Lesson.None ? "튜토리얼 건너뛰기" : "이 안내 건너뛰기");
        var chipText = _bubbleChip;
        DayLoopUI.Layout(chipText, preferredWidth: chipText.GetPreferredValues(chipText.text).x, preferredHeight: 19 * 1.25f);
        _bubbleTitle.text = hint.title;
        _bubbleBody.text = hint.body;
        DayLoopUI.Layout(_bubbleBody, preferredHeight: Mathf.Ceil(_bubbleBody.GetPreferredValues(hint.body, BubbleWidth - 48, 0).y) + 6);
        _bubbleAction.gameObject.SetActive(hint.action != null);
        if (hint.action != null) DayLoopUI.SetButtonLabel(_bubbleAction, hint.action);
        _actionEnabled = hint.actionEnabled;
        foreach (var dim in _dims) dim.raycastTarget = !hint.soft;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
    }

    private void Layout()
    {
        Vector2 size = _root.rect.size;
        Rect hole = Rect.zero;
        bool hasHole = _hint.target != null && _hint.target.gameObject.activeInHierarchy;
        if (hasHole)
        {
            hole = ToOverlay(_hint.target);
            hole.xMin -= HolePadding; hole.yMin -= HolePadding; hole.xMax += HolePadding; hole.yMax += HolePadding;
        }
        else if (_hint.target != null)
        {
            // 짚던 버튼이 사라졌으면(창이 닫히는 중 등) 다음 확인 때 단계를 다시 고른다.
            _nextLookup = 0f;
        }

        float alpha = _hint.soft ? 0f : 1f;
        foreach (var dim in _dims) dim.color = new Color(0.05f, 0.08f, 0.10f, 0.62f * alpha);
        SetRect(_dims[0], 0, 0, size.x, hole.yMin);
        SetRect(_dims[1], 0, hole.yMax, size.x, size.y);
        SetRect(_dims[2], 0, hole.yMin, hole.xMin, hole.yMax);
        SetRect(_dims[3], hole.xMax, hole.yMin, size.x, hole.yMax);

        _frame.gameObject.SetActive(hasHole);
        if (hasHole)
        {
            float pulse = 6f + Mathf.Sin(Time.unscaledTime * 5f) * 4f;
            _frame.anchoredPosition = new Vector2(hole.xMin - pulse, hole.yMin - pulse);
            _frame.sizeDelta = new Vector2(hole.width + pulse * 2, hole.height + pulse * 2);
        }

        if (_bubbleAction.gameObject.activeSelf) _bubbleAction.interactable = _actionEnabled == null || _actionEnabled();
        PlaceBubble(size, hasHole ? hole : (Rect?)null);
    }

    /// <summary>짚은 곳 오른쪽 → 왼쪽 → 아래 → 위 순서로 들어갈 자리를 찾는다. 짚은 곳이 없으면 가운데.</summary>
    private void PlaceBubble(Vector2 size, Rect? hole)
    {
        Vector2 b = _bubble.rect.size;
        const float gap = 14f, margin = 16f;
        Vector2 position;
        if (hole == null) position = (size - b) / 2f;
        else
        {
            Rect h = hole.Value;
            float y = Mathf.Clamp(h.center.y - b.y / 2f, margin, size.y - b.y - margin);
            float x = Mathf.Clamp(h.center.x - b.x / 2f, margin, size.x - b.x - margin);
            if (h.xMax + gap + b.x <= size.x - margin) position = new Vector2(h.xMax + gap, y);
            else if (h.xMin - gap - b.x >= margin) position = new Vector2(h.xMin - gap - b.x, y);
            else if (h.yMin - gap - b.y >= margin) position = new Vector2(x, h.yMin - gap - b.y);
            else if (h.yMax + gap + b.y <= size.y - margin) position = new Vector2(x, h.yMax + gap);
            else position = new Vector2(size.x - b.x - margin, margin);
        }
        _bubble.anchoredPosition = position;
    }

    private Rect ToOverlay(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Canvas source = target.GetComponentInParent<Canvas>();
        Camera camera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            local += _root.rect.size / 2f;
            min = Vector2.Min(min, local); max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static void Corner(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
    }

    private static void SetRect(Image image, float x0, float y0, float x1, float y1)
    {
        image.rectTransform.anchoredPosition = new Vector2(x0, y0);
        image.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, x1 - x0), Mathf.Max(0, y1 - y0));
    }

    // ───── 찾기 ─────

    private bool ManagementOpen
    {
        get
        {
            Transform window = FindCanvas("Restaurant management")?.transform.Find("ManagementWindow");
            return window != null && window.gameObject.activeSelf;
        }
    }

    private Canvas FindCanvas(string name)
    {
        foreach (GameObject root in _scene.GetRootGameObjects())
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>())
                if (canvas.name == name && canvas.isActiveAndEnabled) return canvas;
        return null;
    }

    private RectTransform Find(string canvasName, string objectName)
    {
        Canvas canvas = FindCanvas(canvasName);
        if (canvas == null) return null;
        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>())
            if (rect.name == objectName) return rect;
        return null;
    }

    private RectTransform FindButton(string canvasName, string labelPrefix)
    {
        Canvas canvas = FindCanvas(canvasName);
        if (canvas == null) return null;
        foreach (Button button in canvas.GetComponentsInChildren<Button>())
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null && label.text.StartsWith(labelPrefix)) return (RectTransform)button.transform;
        }
        return null;
    }

    private static string FirstMenuName()
    {
        foreach (int id in GameSession.Current.menuSlots)
            if (id >= 0 && GameDatabase.TryGetRecipe(id, out RecipeData recipe)) return recipe.RecipeName;
        return null;
    }

    /// <summary>받침이 있으면 "을", 없으면 "를".</summary>
    private static string ObjectParticle(string word) => HasFinalConsonant(word) ? "을" : "를";

    private static bool HasFinalConsonant(string word)
    {
        char last = word[word.Length - 1];
        return last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0;
    }

    /// <summary>받침이 있으면 "이", 없으면 "가".</summary>
    private static string Particle(string word) => HasFinalConsonant(word) ? "이" : "가";

    // ───── 강조 테두리 ─────

    private static Sprite _ring;

    /// <summary>둥근 사각형 테두리만 있는 9-slice. 가운데가 비어 짚은 버튼이 그대로 보인다.</summary>
    private static Sprite RingSprite
    {
        get
        {
            if (_ring != null) return _ring;
            const int size = 128, radius = 48;
            const float stroke = 14f, soft = 2.6f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "TutorialRing", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + .5f, py = y + .5f;
                    float cx = Mathf.Clamp(px, radius, size - radius), cy = Mathf.Clamp(py, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - radius; // 바깥선까지 거리(안쪽은 음수)
                    float alpha = Mathf.Clamp01(.5f - d / soft) * Mathf.Clamp01(.5f + (d + stroke) / soft);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            texture.SetPixels32(pixels);
            texture.Apply(true);
            _ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _ring.hideFlags = HideFlags.DontSave;
            return _ring;
        }
    }
}
