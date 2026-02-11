using UnityEngine;

public class TableGroup : MonoBehaviour
{
    [ReadOnly][SerializeField] private Seat[] seats;    // 현재 테이블에 묶여있는 좌석 
    [ReadOnly] private int tableId;                      // 현재 지금 테이블 번호가 뭔지 알려주기 위한 변수
    public Vector3 position => transform.position;


    void Awake() { seats = GetComponentsInChildren<Seat>(); } // 테이블에 묶여있는 좌석을 플레이시 등록
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
