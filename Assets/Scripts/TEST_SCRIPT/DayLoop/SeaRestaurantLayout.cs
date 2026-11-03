using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// [TEST 하루 루프] 바다 위 식당 배치도. (캐주얼 시안 D/E)
/// 씬의 기울어진 타일맵·배경 대신, 시안 그대로 평평한 줄 배치를 실행 중에 깐다.
///  - 맨 위 가운데: 주방 뗏목 (조리대·수령대·주방 직원 자리)
///  - 가운데: 테이블 자리 두 줄 (SeaTableSlots)
///  - 그 아래: 나무 기둥 + 초록 밧줄 경계. 가운데 틈이 입구다.
///  - 맨 아래 물 위: 돌 의자 웨이팅석과 "웨이팅 n" 팻말
/// 손님·직원 이동은 이 배치도의 물길을 따른다. (IRestaurantFloor: 막히지 않으면 곧장, 막히면 물길로 돌아간다)
/// 씬 파일은 건드리지 않고, 씬의 기존 지점(입구·출구·스폰)은 이 배치도 위치로 옮긴다.
/// 좌표는 모두 식당 카메라 구역 가운데(Origin) 기준이며 1유닛 = 시안 54px 이다.
/// </summary>
public class SeaRestaurantLayout : MonoBehaviour, IRestaurantFloor
{
    public static SeaRestaurantLayout Instance { get; private set; }

    public static readonly Vector2 Origin = new Vector2(0f, (float)CameraZone.Restaurant);
    public static readonly Vector2 DesignSize = new Vector2(35.56f, 20f);   // 1920×1080 시안 한 화면
    public static Rect DesignRect => new Rect(Origin - DesignSize * 0.5f, DesignSize);

    // ───── 배치도 (Origin 기준) ─────
    private const float BackgroundCover = 1.52f;                 // 준비 중 왼쪽 띠만큼 넓게 보여도 바다가 끊기지 않게
    private const float BackgroundTop = 14.5f;
    private const float RaftWidth = 12f;
    private static readonly Vector2 RaftCenter = new Vector2(0f, 6.85f);

    private const float RopeY = -3.1f;                           // 밧줄이 묶인 높이 = 넘을 수 없는 선
    private const float GateHalf = 1.5f;                         // 가운데 입구 틈 (반폭)
    private const float PostBase = -4.25f;
    private const float PostHeight = 1.5f;
    private const float RopeAttach = 0.77f;                      // 기둥 아래에서 밧줄 묶인 곳까지 (그림 기준 23% 아래)
    private const float RopeSag = 0.22f;
    private static readonly float[] PostX = { 2f, 6.75f, 11.5f, 16.25f, 20.5f };

    // 물길: 밖(밧줄 아래) · 밧줄 안쪽 · 두 테이블 줄 사이 · 주방 앞
    private static readonly float[] Lanes = { -4.6f, -2.45f, 0.95f, 4.45f };
    // 이웃한 물길 사이를 오갈 수 있는 세로 통로 (Lanes[i] ↔ Lanes[i + 1])
    private static readonly float[][] Channels =
    {
        new[] { 0f },                          // 입구
        new[] { -10.5f, -3.5f, 3.5f, 10.5f },  // 아랫줄 테이블 사이
        new[] { -9.5f, 0f, 9.5f },             // 윗줄 테이블 사이
    };

    private static readonly Vector2 SpawnPoint = new Vector2(-19.8f, -4.6f);
    private static readonly Vector2 ExitPoint = new Vector2(19.8f, -4.6f);
    private static readonly Vector2 GatePoint = new Vector2(0f, RopeY);

    // 주방 뗏목 위 지점 (뗏목 그림 기준으로 잡았다)
    private static readonly Vector2 PickupPoint = new Vector2(-0.4f, 3.75f);
    private static readonly Vector2 DropoffPoint = new Vector2(-0.5f, 5.45f);
    private static readonly Rect Deck = new Rect(-2.6f, 4.9f, 5.2f, 4f);
    private static readonly Vector2[] ChefIdle =
    {
        new Vector2(-1.9f, 6.25f), new Vector2(-0.6f, 6.25f), new Vector2(0.7f, 6.25f),
        new Vector2(-1.25f, 6.75f), new Vector2(0.05f, 6.75f), new Vector2(1.35f, 6.75f),
    };
    private static readonly Vector2[] ServerIdle =
    {
        new Vector2(0.9f, 3.0f), new Vector2(-1.7f, 3.0f), new Vector2(1.9f, 3.0f),
        new Vector2(-0.7f, 2.3f), new Vector2(0.6f, 2.2f),
    };

    // 웨이팅석: 한 줄 최대 11자리, 두 줄까지
    private static readonly float[] WaitingRows = { -6.1f, -7.8f };
    private const int WaitingPerRow = 11;
    private const float WaitingSpan = 17.5f;
    private const float WaitingMaxGap = 2.6f;
    private const float StoneWidth = 0.95f;
    private const float SitLift = 0.5f;                          // 돌 위에 앉은 것처럼 보이도록 좌석점을 살짝 올린다
    private const float StaffScale = 0.14f;                      // 손님(0.15)과 비슷한 크기

    private const int RaftOrder = 1;
    private const int StoneOrder = 1;
    private const int RopeOrder = 2;
    private const int SignOrder = 3990;

    private static readonly Color RopeGreen = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color RopeLine = new Color32(0x3B, 0x31, 0x29, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Sky = new Color32(0x7C, 0xC8, 0xEE, 0xFF);

    private DayCycleController _day;
    private Transform _root;
    private Material _lineMaterial;
    private SeaTableSlots _slots;
    private Transform _sign;
    private SpriteRenderer _signBack, _signLine, _badge;
    private TextMeshPro _signText, _badgeText;
    private int _shownCount = -1;
    private bool _shownClosing;

    public SeaTableSlots Slots => _slots;

    // 씬 오브젝트의 Awake 뒤, Start 앞에 불려야 한다. (테이블 불러오기·웨이팅 등록보다 먼저 배치도가 있어야 함)
    private void Awake()
    {
        Instance = this;
        RestaurantNav.Floor = this;
        _root = new GameObject("[DayLoop] Sea floor").transform;
        _root.SetParent(transform, false);
        _lineMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };

        HideLegacyScene();
        MoveScenePoints();
        BuildRaft();
        BuildRope();
        BuildWaitingSeats();
        _slots = gameObject.AddComponent<SeaTableSlots>();
    }

    public void Init(DayCycleController day)
    {
        _day = day;
        _day.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(_day.Phase);
    }

    private IEnumerator Start()
    {
        // 직원 매니저들이 Start에서 오늘 근무할 직원을 켜고 늘린 뒤에 자리로 옮긴다.
        yield return null;
        PlaceStaff<ServerAgent>(PartTimerRole.Serving);
        PlaceStaff<ChefAgent>(PartTimerRole.Kitchen);
    }

    private void OnDestroy()
    {
        if (_day != null) _day.OnPhaseChanged -= HandlePhaseChanged;
        if (_lineMaterial != null) Destroy(_lineMaterial);
        if (ReferenceEquals(RestaurantNav.Floor, this)) RestaurantNav.Floor = null;
        if (Instance == this) Instance = null;
    }

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        if (_sign == null) return;
        _sign.gameObject.SetActive(phase == DayLoopPhase.Open || phase == DayLoopPhase.Closing);
        _shownCount = -1;
    }

    private void Update()
    {
        if (_sign == null || !_sign.gameObject.activeSelf || _day == null) return;
        SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
        int count = seats != null ? seats.WaitingCount : 0;
        bool closing = _day.Phase == DayLoopPhase.Closing;
        if (count == _shownCount && closing == _shownClosing) return;
        _shownCount = count;
        _shownClosing = closing;
        _badgeText.text = closing ? "마감" : count.ToString();
        _badge.color = closing ? Peach : Sky;
        LayoutSign();
    }

    // ───── IRestaurantFloor ─────

    public Vector2 KitchenPickup => Origin + PickupPoint;
    public Vector2 KitchenDropoff => Origin + DropoffPoint;

    public Vector2 StationPoint(CookingType type)
    {
        switch (type)
        {
            case CookingType.Chop: return Origin + new Vector2(-2.0f, 5.4f);
            case CookingType.Pot: return Origin + new Vector2(-1.26f, 5.4f);
            case CookingType.Pan: return Origin + new Vector2(0.12f, 5.4f);
            case CookingType.Fry: return Origin + new Vector2(0.8f, 5.4f);
            default: return KitchenDropoff;
        }
    }

    public Vector2 IdlePoint(PartTimerRole role, int index)
    {
        Vector2[] spots = role == PartTimerRole.Kitchen ? ChefIdle : ServerIdle;
        int i = Mathf.Max(0, index);
        // 자리보다 사람이 많으면 한 바퀴 돌 때마다 살짝 비켜 선다.
        return Origin + spots[i % spots.Length] + new Vector2(0.35f, -0.2f) * (i / spots.Length);
    }

    public Vector2 ServePoint(Vector2 seatPoint)
    {
        return _slots != null ? _slots.ServePoint(seatPoint) : seatPoint + Vector2.down * 0.8f;
    }

    public List<Vector3> Route(Vector2 from, Vector2 to)
    {
        Vector2 a = from - Origin, b = to - Origin;
        if ((OnDeck(a) && OnDeck(b)) || Clear(a, b)) return new List<Vector3> { to };

        // 막혀 있으면 물길로: 지금 높이의 물길 → 통로로 물길을 옮겨 가며 → 목적지 높이의 물길 → 목적지
        var points = new List<Vector2> { a };
        int lane = LaneOf(a), goal = LaneOf(b);
        float x = a.x;
        points.Add(new Vector2(x, Lanes[lane]));
        while (lane != goal)
        {
            int next = lane + Math.Sign(goal - lane);
            float channel = BestChannel(Channels[Mathf.Min(lane, next)], x, b.x);
            points.Add(new Vector2(channel, Lanes[lane]));
            points.Add(new Vector2(channel, Lanes[next]));
            x = channel;
            lane = next;
        }
        points.Add(new Vector2(b.x, Lanes[goal]));
        points.Add(b);

        // 줄 당기기: 곧장 갈 수 있는 지점은 건너뛰어 자연스럽게 헤엄치게 한다.
        var route = new List<Vector3>();
        int i = 0;
        while (i < points.Count - 1)
        {
            int j = points.Count - 1;
            while (j > i + 1 && !Clear(points[i], points[j])) j--;
            route.Add(Origin + points[j]);
            i = j;
        }
        return route;
    }

    private static bool OnDeck(Vector2 p) => Deck.Contains(p);

    private static int LaneOf(Vector2 p)
    {
        if (OnDeck(p)) return Lanes.Length - 1;
        if (p.y < RopeY) return 0;
        int best = 1;
        for (int i = 2; i < Lanes.Length; i++)
            if (Mathf.Abs(p.y - Lanes[i]) < Mathf.Abs(p.y - Lanes[best])) best = i;
        return best;
    }

    private static float BestChannel(float[] channels, float from, float to)
    {
        return channels.OrderBy(c => Mathf.Abs(from - c) + Mathf.Abs(c - to)).First();
    }

    // 두 점 사이를 곧장 헤엄칠 수 있는지: 입구 밖에서 밧줄을 넘거나, 테이블 몸통·주방 뗏목을 가로지르면 안 된다.
    private bool Clear(Vector2 p, Vector2 q)
    {
        if ((p.y - RopeY) * (q.y - RopeY) < 0f)
        {
            float t = (RopeY - p.y) / (q.y - p.y);
            if (Mathf.Abs(p.x + t * (q.x - p.x)) > GateHalf) return false;
        }
        if (!OnDeck(p) && !OnDeck(q) && SegmentHitsRect(p, q, Deck)) return false;
        if (_slots != null)
            foreach (Rect body in _slots.Bodies())
                if (SegmentHitsRect(p, q, body)) return false;
        return true;
    }

    // 선분-사각형 교차 (Liang–Barsky)
    private static bool SegmentHitsRect(Vector2 p, Vector2 q, Rect r)
    {
        float t0 = 0f, t1 = 1f;
        Vector2 d = q - p;
        float[] pp = { -d.x, d.x, -d.y, d.y };
        float[] qq = { p.x - r.xMin, r.xMax - p.x, p.y - r.yMin, r.yMax - p.y };
        for (int i = 0; i < 4; i++)
        {
            if (Mathf.Approximately(pp[i], 0f))
            {
                if (qq[i] < 0f) return false;
                continue;
            }
            float t = qq[i] / pp[i];
            if (pp[i] < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
        }
        return t0 <= t1;
    }

    // ───── 씬 정리 ─────

    // 기울어진 타일맵·배경·임시 도형을 숨기고, 바다 배경만 시안 크기로 다시 깐다.
    private void HideLegacyScene()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "Grid")
            {
                foreach (TilemapRenderer tiles in root.GetComponentsInChildren<TilemapRenderer>(true)) tiles.enabled = false;
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.gameObject.name == "BackGround") FitBackground(sr);
                    else sr.enabled = false;
                }
            }
            else if (root.name == "Environment")
            {
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.GetComponentInParent<PartTimerAgent>(true) != null) continue;
                    if (sr.GetComponentInParent<CustomerAgent>(true) != null) continue;
                    sr.enabled = false;   // 벽·입구·출구·주방 표시 사각형, 위치 표시 삼각형, 옛 웨이팅 도형
                }
            }
        }

        // 옛 웨이팅석은 끈다. (WaitingManager에서 빠지고 이 배치도의 돌 의자가 대신 등록된다)
        foreach (WaitingGroup group in FindObjectsByType<WaitingGroup>(FindObjectsSortMode.None))
            group.gameObject.SetActive(false);
    }

    private static void FitBackground(SpriteRenderer sr)
    {
        if (sr.sprite == null) return;
        Vector2 size = sr.sprite.bounds.size;
        float scale = DesignSize.x * BackgroundCover / size.x;
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.position = new Vector3(Origin.x, Origin.y + BackgroundTop - size.y * scale * 0.5f, sr.transform.position.z);
    }

    // 입구(테이블 번호를 매기는 기준)·출구·손님 스폰 위치를 배치도로 옮긴다.
    private static void MoveScenePoints()
    {
        GameObject entrance = GameObject.Find("Entrance");
        if (entrance != null) entrance.transform.position = Origin + GatePoint;

        RestaurantGameManager gm = RestaurantGameManager.instance;
        if (gm != null && gm.exitPoint != null) gm.exitPoint.position = Origin + ExitPoint;

        CustomerSpawnManager spawner = FindFirstObjectByType<CustomerSpawnManager>();
        if (spawner != null) spawner.transform.position = Origin + SpawnPoint;
    }

    // 오늘 근무하는 직원을 대기 위치에 세운다. 씬에서 주방 직원만 크게 잡혀 있어 모든 해달이 같은 크기로 보이게 맞춘다.
    private static void PlaceStaff<T>(PartTimerRole role) where T : PartTimerAgent
    {
        List<T> agents = FindObjectsByType<T>(FindObjectsSortMode.None)
            .OrderBy(a => a.transform.GetSiblingIndex()).ToList();
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].transform.position = RestaurantNav.Floor.IdlePoint(role, i);
            agents[i].transform.localScale = new Vector3(StaffScale, StaffScale, 1f);
        }
    }

    // ───── 주방 뗏목 ─────

    private void BuildRaft()
    {
        Sprite raft = DesignArt.Load("KitchenRaft");
        if (raft == null) return;
        SpriteRenderer sr = Decor("KitchenRaft", raft, RaftOrder);
        sr.transform.position = Origin + RaftCenter;
        Fit(sr, RaftWidth / raft.bounds.size.x);
    }

    // ───── 경계 밧줄 ─────

    private void BuildRope()
    {
        Sprite post = DesignArt.Load("RopePost");
        foreach (int side in new[] { -1, 1 })
        {
            var points = new List<Vector3>();
            for (int i = 0; i < PostX.Length; i++)
            {
                Vector2 basePoint = Origin + new Vector2(side * PostX[i], PostBase);
                if (post != null)
                {
                    SpriteRenderer sr = Decor("RopePost", post, RopeOrder + 2);
                    sr.transform.position = basePoint + Vector2.up * (PostHeight * 0.5f);
                    Fit(sr, PostHeight / post.bounds.size.y);
                }
                if (i == PostX.Length - 1) { points.Add(basePoint + Vector2.up * PostHeight * RopeAttach); break; }

                // 기둥 사이마다 아래로 처지는 곡선을 이어 한 줄로 그린다. 처진 가운데에 부표를 단다.
                Vector3 a = basePoint + Vector2.up * PostHeight * RopeAttach;
                Vector3 b = Origin + new Vector2(side * PostX[i + 1], PostBase + PostHeight * RopeAttach);
                const int segments = 12;
                for (int s = 0; s < segments; s++)
                {
                    float t = (float)s / segments;
                    points.Add(Vector3.Lerp(a, b, t) + Vector3.down * (RopeSag * 4f * t * (1f - t)));
                }
                Buoy((a + b) * 0.5f + Vector3.down * (RopeSag + 0.08f), i % 2 == 0 ? Peach : Butter);
            }
            Line("RopeLine", points, 0.11f, RopeLine, RopeOrder);
            Line("Rope", points, 0.06f, RopeGreen, RopeOrder + 1);
        }
    }

    private void Buoy(Vector3 position, Color color)
    {
        SpriteRenderer edge = Decor("BuoyLine", DayLoopUI.Circle, RopeOrder + 1);
        edge.color = RopeLine;
        edge.transform.position = position;
        Fit(edge, 0.36f);

        SpriteRenderer body = Decor("Buoy", DayLoopUI.Circle, RopeOrder + 2);
        body.color = color;
        body.transform.position = position;
        Fit(body, 0.28f);

        SpriteRenderer band = Decor("BuoyBand", DayLoopUI.White, RopeOrder + 3);
        band.transform.position = position;
        band.transform.localScale = new Vector3(0.28f, 0.07f, 1f);
    }

    private void Line(string name, List<Vector3> points, float width, Color color, int order)
    {
        LineRenderer line = new GameObject(name).AddComponent<LineRenderer>();
        line.transform.SetParent(_root, false);
        line.sharedMaterial = _lineMaterial;
        line.useWorldSpace = true;
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingOrder = order;
    }

    // ───── 웨이팅석 ─────

    // 오늘 식당 레벨의 대기열 수만큼 돌 의자를 놓는다. 줄 맨 앞(0번)은 입구 바로 아래 가운데 자리다.
    private void BuildWaitingSeats()
    {
        int count = GameSession.IsActive ? RestaurantRules.WaitingLimit(GameSession.Current.restaurantLevel) : 4;
        count = Mathf.Clamp(count, 1, WaitingPerRow * WaitingRows.Length);
        int rows = Mathf.CeilToInt(count / (float)WaitingPerRow);
        int perRow = Mathf.CeilToInt(count / (float)rows);
        float gap = perRow > 1 ? Mathf.Min(WaitingMaxGap, WaitingSpan / (perRow - 1)) : 0f;

        var spots = new List<Vector2>();
        for (int r = 0, left = count; r < rows; r++)
        {
            int n = Mathf.Min(perRow, left);
            left -= n;
            var row = new List<Vector2>();
            for (int i = 0; i < n; i++) row.Add(new Vector2((i - (n - 1) * 0.5f) * gap, WaitingRows[r]));
            spots.AddRange(row.OrderBy(p => Mathf.Abs(p.x)).ThenBy(p => p.x));
        }

        // WaitingGroup은 Awake에서 자식 좌석을 모으므로, 꺼 둔 채 좌석을 다 붙인 뒤 켠다.
        var group = new GameObject("WaitingSeats");
        group.SetActive(false);
        group.transform.SetParent(_root, false);
        Sprite stone = DesignArt.Load("StoneSeat");
        for (int i = 0; i < spots.Count; i++)
        {
            Vector2 at = Origin + spots[i];
            if (stone != null)
            {
                SpriteRenderer sr = Decor("StoneSeat", stone, StoneOrder);
                sr.transform.position = at;
                Fit(sr, StoneWidth / stone.bounds.size.x);
            }
            var seat = new GameObject($"Waiting {i + 1}").AddComponent<Seat>();
            seat.transform.SetParent(group.transform, false);
            seat.transform.position = at + Vector2.up * SitLift;
            seat.SetFacingDirection(Vector2.down);
        }
        group.AddComponent<WaitingGroup>();
        group.SetActive(true);

        BuildSign(Origin + new Vector2(0f, WaitingRows[rows - 1] - 1.05f));
    }

    // ───── 웨이팅 팻말 ─────

    private void BuildSign(Vector2 position)
    {
        _sign = new GameObject("WaitingSign").transform;
        _sign.SetParent(_root, false);
        _sign.position = position;
        int layer = DayLoopUI.TopSortingLayerId;

        _signLine = SignSprite("Line", Pill(0.25f), RopeLine, layer, SignOrder);
        _signBack = SignSprite("Back", Pill(0.25f), DayLoopUI.Kit.Sail, layer, SignOrder + 1);
        _badge = SignSprite("Badge", Pill(0.18f), Sky, layer, SignOrder + 2);

        _signText = SignText("Label", "웨이팅", DayLoopUI.Kit.Ink, layer, SignOrder + 3);
        _badgeText = SignText("Count", "0", Color.white, layer, SignOrder + 3);
        _sign.gameObject.SetActive(false);
    }

    // 끝이 반원인 알약 스프라이트 (반지름 = 높이의 절반)
    private static Sprite Pill(float radius)
    {
        Sprite rounded = DayLoopUI.Rounded;
        Sprite pill = Sprite.Create(rounded.texture, rounded.rect, new Vector2(.5f, .5f), rounded.border.x / radius, 0, SpriteMeshType.FullRect, rounded.border);
        pill.hideFlags = HideFlags.DontSave;
        return pill;
    }

    private void LayoutSign()
    {
        float labelWidth = _signText.GetPreferredValues("웨이팅").x;
        float badgeWidth = Mathf.Max(0.36f, _badgeText.GetPreferredValues(_badgeText.text).x + 0.2f);
        float width = 0.26f + labelWidth + 0.12f + badgeWidth + 0.1f;
        _signBack.size = new Vector2(width, 0.5f);
        _signLine.size = new Vector2(width + 0.08f, 0.58f);
        _signLine.transform.localPosition = new Vector3(0f, -0.03f, 0f);
        _signText.transform.localPosition = new Vector3(-width * 0.5f + 0.26f + labelWidth * 0.5f, 0f, 0f);
        _badge.size = new Vector2(badgeWidth, 0.36f);
        Vector3 badgePos = new Vector3(width * 0.5f - 0.1f - badgeWidth * 0.5f, 0f, 0f);
        _badge.transform.localPosition = badgePos;
        _badgeText.transform.localPosition = badgePos;
    }

    private SpriteRenderer SignSprite(string name, Sprite sprite, Color color, int layer, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(_sign, false);
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.color = color;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        return sr;
    }

    private TextMeshPro SignText(string name, string value, Color color, int layer, int order)
    {
        TextMeshPro text = new GameObject(name).AddComponent<TextMeshPro>();
        text.transform.SetParent(_sign, false);
        text.font = DesignFonts.Heading;
        text.fontSharedMaterial = text.font.material;
        text.fontSize = 3.1f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        text.color = color;
        text.text = value;
        MeshRenderer mesh = text.GetComponent<MeshRenderer>();
        mesh.sortingLayerID = layer;
        mesh.sortingOrder = order;
        return text;
    }

    // ───── 조립 도구 ─────

    private SpriteRenderer Decor(string name, Sprite sprite, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(_root, false);
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static void Fit(SpriteRenderer sr, float scale)
    {
        // Circle처럼 1유닛 스프라이트는 scale이 곧 지름이다.
        sr.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
