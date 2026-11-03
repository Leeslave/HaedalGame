using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 바다 위 식당 꾸밈. (캐주얼 시안)
///  - 경계 밧줄: 식탁 구역과 웨이팅석 사이의 보이지 않던 장애물 칸(계단 모양) 위에 나무 기둥 + 초록 밧줄 + 부표를 놓는다.
///    손님은 원래 그 칸을 지나가지 못하므로 밧줄을 넘어가는 것처럼 보이지 않는다. 왼쪽 끝 틈이 입구다.
///  - 웨이팅석: 임시 도형이던 대기 좌석을 물 위 돌 의자 그림으로 바꾼다. (좌석 Transform·칸 위치는 건드리지 않는다)
///  - 웨이팅 팻말: 웨이팅석 아래에 "웨이팅 n" / "마감" 알약
/// 표시만 담당하며 길찾기·좌석 배정에는 관여하지 않는다. 좌표는 Restaurant_TEST 배치 기준이다.
/// </summary>
public class RestaurantSeaDecor : MonoBehaviour
{
    // Restaurant_TEST 장애물 칸을 따라가는 밧줄 경로 (월드 좌표). 기둥은 이 점들 사이에 고르게 선다.
    private static readonly Vector2[] RopePath = { new Vector2(-11.0f, 46.4f), new Vector2(-0.5f, 44.4f), new Vector2(-0.5f, 41.7f) };
    private const float PostSpacing = 1.75f;
    private const float PostHeight = 0.8f;      // 시안보다 작게
    private const float RopeAttach = 0.77f;     // 기둥 아래에서 밧줄 묶인 곳까지 (그림 기준 23% 아래)
    private const float RopeSag = 0.14f;
    private const float SeatWidth = 0.9f;
    private const int DecorOrder = 1;
    private const int SignOrder = 3990;

    private static readonly Color RopeGreen = new Color32(0x7F, 0xD1, 0xA0, 0xFF);
    private static readonly Color RopeLine = new Color32(0x3B, 0x31, 0x29, 0xFF);
    private static readonly Color Peach = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    private static readonly Color Butter = new Color32(0xFF, 0xD4, 0x5E, 0xFF);
    private static readonly Color Sky = new Color32(0x7C, 0xC8, 0xEE, 0xFF);

    private DayCycleController _day;
    private Transform _root;
    private Material _lineMaterial;
    private Transform _sign;
    private SpriteRenderer _signBack, _signLine, _badge;
    private TextMeshPro _signText, _badgeText;
    private int _shownCount = -1;
    private bool _shownClosing;

    public void Init(DayCycleController day)
    {
        _day = day;
        _root = new GameObject("[DayLoop] SeaDecor").transform;
        _root.SetParent(transform, false);
        _lineMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };

        List<Vector3> seats = SkinWaitingSeats();
        if (GameObject.Find("Entrance") != null) BuildRope();
        if (seats.Count > 0) BuildSign(seats);

        _day.OnPhaseChanged += HandlePhaseChanged;
        HandlePhaseChanged(_day.Phase);
    }

    private void OnDestroy()
    {
        if (_day != null) _day.OnPhaseChanged -= HandlePhaseChanged;
        if (_lineMaterial != null) Destroy(_lineMaterial);
    }

    private void HandlePhaseChanged(DayLoopPhase phase)
    {
        if (_sign == null) return;
        _sign.gameObject.SetActive(phase == DayLoopPhase.Open || phase == DayLoopPhase.Closing);
        _shownCount = -1;
    }

    private void Update()
    {
        if (_sign == null || !_sign.gameObject.activeSelf) return;
        SeatManager seats = RestaurantGameManager.instance != null ? RestaurantGameManager.instance.seatManager : null;
        int count = seats != null ? seats.WaitingCount : 0;
        bool closing = _day.Phase == DayLoopPhase.Closing;
        if (count == _shownCount && closing == _shownClosing) return;
        _shownCount = count;
        _shownClosing = closing;
        _badgeText.text = closing ? "마감" : count.ToString();
        _badge.color = closing ? Peach : Sky;
        LayoutSign(closing);
    }

    // ───── 웨이팅석 ─────

    private List<Vector3> SkinWaitingSeats()
    {
        var positions = new List<Vector3>();
        Sprite stone = DesignArt.Load("StoneSeat");
        foreach (WaitingGroup group in FindObjectsByType<WaitingGroup>(FindObjectsSortMode.None))
        {
            foreach (Seat seat in group.GetSeats())
            {
                if (seat == null) continue;
                SpriteRenderer placeholder = seat.GetComponent<SpriteRenderer>();
                if (placeholder == null) placeholder = seat.GetComponentInChildren<SpriteRenderer>();
                Vector3 position = seat.GetSeatPoint() != null ? seat.GetSeatPoint().position : seat.transform.position;
                positions.Add(position);
                if (stone == null) continue;
                if (placeholder != null) placeholder.enabled = false;

                SpriteRenderer sr = Decor("StoneSeat", stone, DecorOrder);
                sr.transform.position = new Vector3(position.x, position.y - 0.15f, 0f);
                Fit(sr, SeatWidth / stone.bounds.size.x);
            }
        }
        return positions;
    }

    // ───── 경계 밧줄 ─────

    private void BuildRope()
    {
        Sprite post = DesignArt.Load("RopePost");
        var bases = new List<Vector2>();
        for (int i = 0; i < RopePath.Length - 1; i++)
        {
            Vector2 a = RopePath[i], b = RopePath[i + 1];
            int steps = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / PostSpacing));
            for (int s = 0; s < steps; s++) bases.Add(Vector2.Lerp(a, b, (float)s / steps));
        }
        bases.Add(RopePath[RopePath.Length - 1]);

        // 밧줄: 기둥 사이마다 아래로 처지는 곡선을 이어 한 줄로 그린다. 진한 외곽선 위에 초록 줄.
        var points = new List<Vector3>();
        for (int i = 0; i < bases.Count - 1; i++)
        {
            Vector3 a = bases[i] + Vector2.up * RopeAttach, b = bases[i + 1] + Vector2.up * RopeAttach;
            const int segments = 10;
            for (int s = 0; s < segments; s++)
            {
                float t = (float)s / segments;
                points.Add(Vector3.Lerp(a, b, t) + Vector3.down * (RopeSag * 4f * t * (1f - t)));
            }

            // 부표: 처진 밧줄 가운데에 코랄·버터 번갈아
            Vector3 mid = (a + b) * 0.5f + Vector3.down * (RopeSag + 0.06f);
            Buoy(mid, i % 2 == 0 ? Peach : Butter);
        }
        points.Add(bases[bases.Count - 1] + Vector2.up * RopeAttach);
        Line("RopeLine", points, 0.075f, RopeLine, DecorOrder + 1);
        Line("Rope", points, 0.04f, RopeGreen, DecorOrder + 2);

        if (post == null) return;
        foreach (Vector2 b in bases)
        {
            SpriteRenderer sr = Decor("RopePost", post, DecorOrder + 4);
            float scale = PostHeight / post.bounds.size.y;
            sr.transform.position = new Vector3(b.x, b.y + PostHeight * 0.5f, 0f);
            Fit(sr, scale);
        }
    }

    private void Buoy(Vector3 position, Color color)
    {
        SpriteRenderer edge = Decor("BuoyLine", DayLoopUI.Circle, DecorOrder + 3);
        edge.color = RopeLine;
        edge.transform.position = position;
        Fit(edge, 0.24f);

        SpriteRenderer body = Decor("Buoy", DayLoopUI.Circle, DecorOrder + 4);
        body.color = color;
        body.transform.position = position;
        Fit(body, 0.18f);

        SpriteRenderer band = Decor("BuoyBand", DayLoopUI.White, DecorOrder + 5);
        band.transform.position = position;
        band.transform.localScale = new Vector3(0.18f, 0.05f, 1f);
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

    // ───── 웨이팅 팻말 ─────

    private void BuildSign(List<Vector3> seats)
    {
        Vector3 center = Vector3.zero;
        float bottom = float.MaxValue;
        foreach (Vector3 seat in seats) { center += seat; bottom = Mathf.Min(bottom, seat.y); }
        center /= seats.Count;

        _sign = new GameObject("WaitingSign").transform;
        _sign.SetParent(_root, false);
        _sign.position = new Vector3(center.x, bottom - 0.75f, 0f);
        int layer = DayLoopUI.TopSortingLayerId;

        // 알약 끝이 반원이 되도록 높이 0.42의 절반을 모서리 반지름으로 쓴다.
        Sprite rounded = DayLoopUI.Rounded;
        Sprite pill = Sprite.Create(rounded.texture, rounded.rect, new Vector2(.5f, .5f), rounded.border.x / 0.21f, 0, SpriteMeshType.FullRect, rounded.border);
        pill.hideFlags = HideFlags.DontSave;
        Sprite smallPill = Sprite.Create(rounded.texture, rounded.rect, new Vector2(.5f, .5f), rounded.border.x / 0.15f, 0, SpriteMeshType.FullRect, rounded.border);
        smallPill.hideFlags = HideFlags.DontSave;

        _signLine = SignSprite("Line", pill, RopeLine, layer, SignOrder);
        _signBack = SignSprite("Back", pill, DayLoopUI.Kit.Sail, layer, SignOrder + 1);
        _badge = SignSprite("Badge", smallPill, Sky, layer, SignOrder + 2);

        _signText = SignText("Label", "웨이팅", DayLoopUI.Kit.Ink, layer, SignOrder + 3);
        _badgeText = SignText("Count", "0", Color.white, layer, SignOrder + 3);
        _sign.gameObject.SetActive(false);
    }

    private void LayoutSign(bool closing)
    {
        float labelWidth = _signText.GetPreferredValues("웨이팅").x;
        float badgeWidth = Mathf.Max(0.3f, _badgeText.GetPreferredValues(_badgeText.text).x + 0.16f);
        float width = 0.2f + labelWidth + 0.1f + badgeWidth + 0.08f;
        _signBack.size = new Vector2(width, 0.42f);
        _signLine.size = new Vector2(width + 0.07f, 0.49f);
        _signLine.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        _signText.transform.localPosition = new Vector3(-width * 0.5f + 0.2f + labelWidth * 0.5f, 0f, 0f);
        _badge.size = new Vector2(badgeWidth, 0.3f);
        Vector3 badgePos = new Vector3(width * 0.5f - 0.08f - badgeWidth * 0.5f, 0f, 0f);
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
        text.fontSize = 2.6f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
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
