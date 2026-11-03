using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// [TEST 하루 루프] 바다 식당의 테이블 자리(슬롯). 칸 위에 자유롭게 놓던 배치 대신, 시안의 두 줄 자리에 놓는다.
///  - 윗줄 4자리, 아랫줄 5자리. 어느 자리에나 1·2·4인 테이블을 놓을 수 있다.
///  - 배치 모드에서 왼쪽 띠로 테이블을 고르고 빈 자리(+)를 누르면 놓인다. 놓인 테이블을 누르면 편집 메뉴가 뜬다.
///  - 저장은 기존 배치 저장(PlacedTableEntry)을 그대로 쓰고, anchor를 (슬롯 번호, PlacedTable.SlotAnchorY)로 남긴다.
///    예전 칸 좌표로 저장된 테이블은 불러올 때 앞쪽 자리부터 채워 옮긴다.
/// 테이블은 PlacedTable·TableGroup·Seat로 만들어 좌석 배정·관리 화면·결산이 그대로 동작한다.
/// </summary>
public class SeaTableSlots : MonoBehaviour
{
    public static SeaTableSlots Instance { get; private set; }

    // 자리 가운데 (배치도 원점 기준). 앞쪽 자리일수록 새 게임·옛 저장을 옮겨 올 때 먼저 채운다.
    private static readonly Vector2[] Centers =
    {
        new Vector2(0f, -0.85f), new Vector2(-5.4f, 2.7f), new Vector2(5.4f, 2.7f),
        new Vector2(-7f, -0.85f), new Vector2(7f, -0.85f),
        new Vector2(-14f, -0.85f), new Vector2(14f, -0.85f),
        new Vector2(-13.6f, 2.7f), new Vector2(13.6f, 2.7f),
    };
    private static readonly Vector2 SlotSize = new Vector2(5.6f, 3.1f);
    private const float SitLift = 0.5f;
    private const float ServeGap = 0.95f;
    private const int TableOrder = 2;

    private sealed class Spec
    {
        public string Name, Art;
        public float Width;
        public Vector2 SpriteOffset;
        public Vector2[] Seats;
        public Vector2 BodyCenter, BodyHalf;          // 손님이 가로지르지 못하는 몸통 (의자 사이)
        public Vector2? Stool;                        // 그림에 의자가 없는 1인석은 돌 의자를 따로 놓는다
    }

    // 좌석 위치는 테이블 그림의 의자 자리 비율로 잡았다. (4인 600×330, 2인 560×213)
    private static readonly Dictionary<TableType, Spec> Specs = new Dictionary<TableType, Spec>
    {
        [TableType.FourSeat] = new Spec
        {
            Name = "4인 테이블", Art = "SeaTable4", Width = 5.6f,
            Seats = new[] { new Vector2(-2.28f, 0.99f), new Vector2(-2.28f, -0.89f), new Vector2(2.24f, 0.99f), new Vector2(2.24f, -0.89f) },
            BodyHalf = new Vector2(1.75f, 1.25f),
        },
        [TableType.TwoSeat] = new Spec
        {
            Name = "2인 테이블", Art = "SeaTable2", Width = 5.0f,
            Seats = new[] { new Vector2(-1.97f, 0.1f), new Vector2(1.93f, 0.1f) },
            BodyHalf = new Vector2(1.4f, 0.8f),
        },
        [TableType.OneSeat] = new Spec
        {
            Name = "1인 테이블", Art = "SeaTable1", Width = 1.9f, SpriteOffset = new Vector2(0.45f, 0f),
            Seats = new[] { new Vector2(-1.05f, 0f) }, Stool = new Vector2(-1.05f, -0.1f),
            BodyCenter = new Vector2(0.45f, 0f), BodyHalf = new Vector2(0.85f, 0.75f),
        },
    };

    private readonly PlacedTable[] _tables = new PlacedTable[Centers.Length];
    private readonly GameObject[] _markers = new GameObject[Centers.Length];
    private readonly TextMeshPro[] _markerLabels = new TextMeshPro[Centers.Length];
    private Transform _root;
    private TableType? _selected;
    private int _pendingSkin;
    private bool _placing;

    public TableType? Selected => _selected;

    private void Awake()
    {
        Instance = this;
        _root = new GameObject("[DayLoop] Sea tables").transform;
        _root.SetParent(transform, false);
        BuildMarkers();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static Vector2 Origin => SeaRestaurantLayout.Origin;

    private bool Occupied(int slot) => _tables[slot] != null && PlacedTable.Active.Contains(_tables[slot]);

    // ───── 배치도에 알려 주는 정보 ─────

    public IEnumerable<Rect> Bodies()
    {
        for (int i = 0; i < Centers.Length; i++)
        {
            if (!Occupied(i)) continue;
            Spec spec = Specs[_tables[i].tableData.tableType];
            Vector2 center = Centers[i] + spec.BodyCenter;
            yield return new Rect(center - spec.BodyHalf, spec.BodyHalf * 2f);
        }
    }

    // 좌석 바깥쪽 옆 물길에서 응대한다.
    public Vector2 ServePoint(Vector2 seatWorld)
    {
        Vector2 seat = seatWorld - Origin;
        int best = -1;
        for (int i = 0; i < Centers.Length; i++)
            if (Occupied(i) && (best < 0 || (Centers[i] - seat).sqrMagnitude < (Centers[best] - seat).sqrMagnitude)) best = i;
        if (best < 0) return seatWorld + Vector2.down * 0.8f;
        float side = seat.x >= Centers[best].x ? 1f : -1f;
        return seatWorld + new Vector2(side * ServeGap, -0.2f);
    }

    // ───── 불러오기 ─────

    public void Load(List<PlacedTableEntry> entries)
    {
        foreach (PlacedTable existing in new List<PlacedTable>(PlacedTable.Active)) existing.RemoveTable(false);
        System.Array.Clear(_tables, 0, _tables.Length);

        var pending = new List<PlacedTableEntry>();
        foreach (PlacedTableEntry entry in new List<PlacedTableEntry>(entries))
        {
            if (!TryParse(entry.tableType, out TableType type))
            {
                Debug.LogWarning($"[SeaTableSlots] 알 수 없는 테이블 종류 '{entry.tableType}'를 건너뜁니다.");
                continue;
            }
            int slot = entry.anchorX;
            if (entry.anchorY == PlacedTable.SlotAnchorY && slot >= 0 && slot < Centers.Length && !Occupied(slot)) Build(type, slot, entry.skin);
            else pending.Add(entry);
        }

        // 예전 칸 좌표로 저장된 테이블(또는 겹친 자리)은 빈 자리를 앞에서부터 채운다. 자리가 모자라면 창고로 돌려보낸다.
        foreach (PlacedTableEntry entry in pending)
        {
            TryParse(entry.tableType, out TableType type);
            int slot = FirstFreeSlot();
            if (slot >= 0) { Build(type, slot, entry.skin); continue; }
            ReturnToStorage(Specs[type].Seats.Length);
            Debug.LogWarning($"[SeaTableSlots] 빈 자리가 없어 {entry.tableType} 테이블을 창고로 돌려보냈습니다.");
        }

        if (pending.Count > 0 && TableSaveLoadManager.Instance != null) TableSaveLoadManager.Instance.SavePlacement();
    }

    private static bool TryParse(string value, out TableType type)
    {
        return System.Enum.TryParse(value, out type) && Specs.ContainsKey(type);
    }

    private int FirstFreeSlot()
    {
        for (int i = 0; i < Centers.Length; i++) if (!Occupied(i)) return i;
        return -1;
    }

    private static void ReturnToStorage(int seats)
    {
        if (!GameSession.IsActive) return;
        var furniture = GameSession.Current.progression.furniture ?? (GameSession.Current.progression.furniture = new List<FurnitureStock>());
        FurnitureStock stock = furniture.Find(x => x.seats == seats);
        if (stock == null) furniture.Add(new FurnitureStock { seats = seats, amount = 1 });
        else stock.amount++;
    }

    // ───── 테이블 만들기 ─────

    private PlacedTable Build(TableType type, int slot, int skin)
    {
        Spec spec = Specs[type];
        Vector2 center = Origin + Centers[slot];

        // TableGroup은 Awake에서 자식 좌석을 모으므로, 꺼 둔 채 다 붙인 뒤 켠다.
        var go = new GameObject($"Table {slot + 1} ({spec.Name})");
        go.SetActive(false);
        go.transform.SetParent(_root, false);
        go.transform.position = center;

        Sprite art = DesignArt.Load(spec.Art);
        Vector2 artSize = new Vector2(spec.Width, spec.Width * 0.5f);
        if (art != null)
        {
            SpriteRenderer visual = Child<SpriteRenderer>(go.transform, "Visual", spec.SpriteOffset);
            visual.sprite = art;
            visual.sortingOrder = TableOrder;
            float scale = spec.Width / art.bounds.size.x;
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            artSize = art.bounds.size * scale;
        }
        if (spec.Stool.HasValue)
        {
            Sprite stone = DesignArt.Load("StoneSeat");
            if (stone != null)
            {
                SpriteRenderer stool = Child<SpriteRenderer>(go.transform, "Stool", spec.Stool.Value);
                stool.sprite = stone;
                stool.sortingOrder = TableOrder;
                float scale = 0.85f / stone.bounds.size.x;
                stool.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        for (int i = 0; i < spec.Seats.Length; i++)
        {
            Seat seat = Child<Seat>(go.transform, $"Seat {i + 1}", spec.Seats[i] + Vector2.up * SitLift);
            seat.SetFacingDirection(spec.Seats[i].x < spec.BodyCenter.x ? Vector2.right : Vector2.left);
        }

        // 테이블을 누르면 편집 메뉴 (영업 중에는 꺼서 손님 누르기를 가리지 않는다)
        BoxCollider2D hit = go.AddComponent<BoxCollider2D>();
        hit.offset = spec.SpriteOffset;
        hit.size = artSize;
        hit.enabled = RestaurantProgress.CanManage;

        var data = ScriptableObject.CreateInstance<TableData>();
        data.hideFlags = HideFlags.DontSave;
        data.tableName = spec.Name;
        data.tableType = type;
        data.chairTiles = new Vector2Int[spec.Seats.Length];
        data.tableTiles = new Vector2Int[0];

        PlacedTable placed = go.AddComponent<PlacedTable>();   // TableGroup도 함께 붙는다
        go.SetActive(true);
        placed.InitializeSlot(data, slot);
        placed.SetSkin(skin);
        _tables[slot] = placed;
        return placed;
    }

    private static T Child<T>(Transform parent, string name, Vector2 localPosition) where T : Component
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child.AddComponent<T>();
    }

    // ───── 배치 모드 ─────

    // 놓을 테이블을 고른다. (null이면 고르기 취소)
    public void Select(TableType? type)
    {
        _selected = type;
        if (type == null) _pendingSkin = 0;
        RefreshMarkers();
    }

    // 놓인 테이블을 집어 든다: 창고로 돌려놓고 같은 종류를 골라 둔다. 빈 자리를 누르면 그 자리로 옮겨진다.
    public void PickUp(PlacedTable table)
    {
        if (table == null || !RestaurantProgress.CanManage) return;
        TableSaveLoadManager.Instance?.RememberEdit();
        TableType type = table.tableData.tableType;
        int skin = table.Skin;
        table.RemoveTable();
        TableSaveLoadManager.Instance?.SavePlacement();
        Select(type);
        _pendingSkin = skin;
        DayCycleController.Instance?.Toast("옮길 빈 자리를 눌러 주세요.");
    }

    private void Place(int slot)
    {
        if (!RestaurantProgress.CanManage || Occupied(slot)) return;
        if (_selected == null)
        {
            DayCycleController.Instance?.Toast("왼쪽에서 놓을 테이블을 먼저 골라 주세요.");
            return;
        }

        TableType type = _selected.Value;
        int seats = Specs[type].Seats.Length;
        if (GameSession.IsActive)
        {
            if (RestaurantProgress.FurnitureCount(GameSession.Current, seats) <= 0)
            {
                DayCycleController.Instance?.Toast("창고에 테이블이 없어요. 관리 > 가구에서 구매해 주세요.");
                Select(null);
                return;
            }
            int used = PlacedTable.Active.Sum(t => t.tableData.chairTiles.Length);
            if (used + seats > RestaurantRules.SeatLimit(GameSession.Current.restaurantLevel))
            {
                DayCycleController.Instance?.Toast("현재 식당의 좌석 한도를 초과했어요.");
                return;
            }
            TableSaveLoadManager.Instance?.RememberEdit();
            GameSession.Current.progression.furniture.Find(x => x.seats == seats).amount--;
        }

        Build(type, slot, _pendingSkin);
        _pendingSkin = 0;
        TableSaveLoadManager.Instance?.SavePlacement();
        if (GameSession.IsActive && RestaurantProgress.FurnitureCount(GameSession.Current, seats) <= 0) Select(null);
        else RefreshMarkers();
    }

    private void Update()
    {
        bool canManage = RestaurantProgress.CanManage;
        foreach (PlacedTable table in _tables)
        {
            if (table == null) continue;
            BoxCollider2D hit = table.GetComponent<BoxCollider2D>();
            if (hit != null && hit.enabled != canManage) hit.enabled = canManage;
        }

        TablePlacementManager placement = TablePlacementManager.Instance;
        bool placing = placement != null && placement.PlacementUI != null && placement.PlacementUI.activeInHierarchy;
        if (placing != _placing)
        {
            _placing = placing;
            if (!placing) _selected = null;
            RefreshMarkers();
        }
        else if (placing)
        {
            RefreshMarkers();
        }
        if (!placing) return;

        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        for (int i = 0; i < Centers.Length; i++)
        {
            if (Occupied(i)) continue;
            if (new Rect(Origin + Centers[i] - SlotSize * 0.5f, SlotSize).Contains(world)) { Place(i); break; }
        }
    }

    // ───── 빈 자리 표시 ─────

    private void BuildMarkers()
    {
        Sprite rounded = DayLoopUI.Rounded;
        Sprite box = Sprite.Create(rounded.texture, rounded.rect, new Vector2(.5f, .5f), rounded.border.x / 0.45f, 0, SpriteMeshType.FullRect, rounded.border);
        box.hideFlags = HideFlags.DontSave;

        for (int i = 0; i < Centers.Length; i++)
        {
            var marker = new GameObject($"Slot {i + 1}");
            marker.transform.SetParent(_root, false);
            marker.transform.position = Origin + Centers[i];

            SpriteRenderer back = marker.AddComponent<SpriteRenderer>();
            back.sprite = box;
            back.drawMode = SpriteDrawMode.Sliced;
            back.size = SlotSize - new Vector2(0.2f, 0.2f);
            back.sortingOrder = TableOrder;

            TextMeshPro label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(marker.transform, false);
            label.font = DesignFonts.Heading;
            label.fontSharedMaterial = label.font.material;
            label.fontSize = 5f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = SlotSize;
            label.GetComponent<MeshRenderer>().sortingOrder = TableOrder + 1;

            _markers[i] = marker;
            _markerLabels[i] = label;
            marker.SetActive(false);
        }
    }

    private void RefreshMarkers()
    {
        for (int i = 0; i < _markers.Length; i++)
        {
            bool show = _placing && !Occupied(i);
            if (_markers[i].activeSelf != show) _markers[i].SetActive(show);
            if (!show) continue;

            bool ready = _selected != null;
            _markers[i].GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, ready ? 0.55f : 0.28f);
            string text = ready ? "여기에 놓기" : "+";
            if (_markerLabels[i].text != text)
            {
                _markerLabels[i].text = text;
                _markerLabels[i].fontSize = ready ? 3.4f : 6f;
            }
            _markerLabels[i].color = ready ? DayLoopUI.Kit.Ink : new Color(1f, 1f, 1f, 0.9f);
        }
    }
}
