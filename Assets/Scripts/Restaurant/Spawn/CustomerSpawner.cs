using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    private RestaurantGameManager gm;
    [SerializeField] private CustomerAgent customerPrefeb;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 10f;
    private float spawnTimer; // 스폰을 대기하는 타이머
    
    [Header("Seat")]
    [SerializeField] private int maxAliveCustomers = 15; 
    private int maxSeatCount;   // 가게 내부 총 좌석 수
    private int seatedCount;    // 현재 앉아있는 좌석 수

    private int maxWaitCount;   // 가게 웨이팅 좌석 수

    private int waitingCount;   // 현재 웨이팅 좌석 수


    
    private int aliveCount;   // 지금 가게에 있는 인원 = seatedCount + waitingCount
    
    // ============================================
    /* Getter and Setter */
    public void setMaxSeat(int upgrade) { maxSeatCount = upgrade; }

    public void setMaxWaitng(int upgrade) { maxWaitCount = upgrade; }

    // ============================================
    /* Count Update */
    public bool CanSpawnCustomer() { return aliveCount < maxAliveCustomers; } // 만약 가게 내 총 인원 (좌석 + 웨이팅)의 수가 max가 아니라면 다음 손님을 받을 수 있음

    public bool TryEnterWaiting()
    {
        if (waitingCount >= maxWaitCount) { return false; } // 만약 웨이팅 좌석까지 꽉찼다면 다음 손님이 스폰되지 않음
        waitingCount++;
        aliveCount = waitingCount + seatedCount; // 현재 고객 수 갱신
        return true;
    }

    public void LeaveWaiting() // 손님이 웨이팅 구역에서 좌석으로 이동하였을 때
    { 
        waitingCount = Mathf.Max(0, waitingCount - 1); 
        aliveCount = waitingCount + seatedCount; // 현재 고객 수 갱신
    } 

    public void SitDown() // 손님이 무사히 자리에 앉았다면
    { 
        seatedCount++; 
        aliveCount = waitingCount + seatedCount; // 현재 고객 수 갱신
    }    

    public void StandUp() // 손님이 식사를 마치고 일어났을 경우
    { 
        seatedCount = Mathf.Max(0, seatedCount - 1); 
        aliveCount = waitingCount + seatedCount; // 현재 고객 수 갱신    
    } 


    // ============================================
    /* LifeCycle */
    private void Start()
    {
        gm = RestaurantGameManager.instance;
        seatedCount = 0;
        waitingCount = 0;
    }
    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) { return; }

        Spawn();
        spawnTimer = 0f;
    }

    // ============================================
    /* Event Logic */

    private void Spawn()
    {
        var customer = Instantiate(customerPrefeb, transform.position, transform.rotation);
        aliveCount++;

        customer.onExited += HandleCustomerExit;
        customer.SpawnCustomer();
    }

    private void HandleCustomerExit(CustomerAgent agent)
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}
