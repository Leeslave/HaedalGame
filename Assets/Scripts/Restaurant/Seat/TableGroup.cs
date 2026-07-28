using UnityEngine;

public class TableGroup : MonoBehaviour
{
    [ReadOnly][SerializeField] private Seat[] seats;    // 현재 테이블에 묶여있는 좌석
    [ReadOnly] private int tableId;                      // 현재 지금 테이블 번호가 뭔지 알려주기 위한 변수
    public Vector3 position => transform.position;

    [Header("Sprite Size")]
    [SerializeField] private Transform visual;          // 테이블 그래픽이 붙어있는 자식 오브젝트 (비워두면 "Visual" 이름으로 자동 탐색)
    [Tooltip("테이블 스프라이트 크기 배율. 실행(Play) 시점에 한 번만 적용되므로, 값을 바꾼 뒤 다시 실행해야 반영됩니다. 좌석 위치는 PlacedTable.Initialize()에서 이 값을 반영해 TableData.chairTiles 기준으로 다시 계산됩니다.")]
    [SerializeField] private float visualScale = 1f;
    public float GetVisualScale() { return visualScale; }

    [Header("Sprite Sorting")]
    [Tooltip("이 테이블에 앉는 손님 전체에 적용할 정렬 순서 오프셋. 손님은 항상 테이블 양옆에만 앉아 좌석마다 앞/뒤 구분이 없으므로, 좌석별이 아닌 테이블 하나당 값 하나만 설정하면 됩니다.")]
    [SerializeField] private int seatedSortingOrderOffset = 0;

    void Awake()
    {
        seats = GetComponentsInChildren<Seat>(); // 테이블에 묶여있는 좌석을 플레이시 등록
        foreach (var seat in seats) { seat.SetSeatedSortingOrderOffset(seatedSortingOrderOffset); }

        if (visual == null) { visual = transform.Find("Visual"); }
        if (visual != null) { visual.localScale *= visualScale; }
    }
    public void SetTableId(int id) { tableId = id; } // 테이블 번호가 변경되면 일괄 적용
    public Seat[] GetSeats() { return seats; }

    private void OnEnable()
    {
        if (TableManager.Instance == null) return;
        TableManager.Instance.RegisterTable(this);
        TableManager.Instance.RequestRenumber();
    }

    private void OnDisable()
    {
        if (TableManager.Instance == null) return;
        TableManager.Instance.UnregisterTable(this);
        TableManager.Instance.RequestRenumber();
    }
}
