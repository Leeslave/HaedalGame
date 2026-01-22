using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    private RestaurantGameManager gm;
    [SerializeField] private CustomerAgent customerPrefeb;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int maxAliveCustomers = 5;
    [SerializeField] private bool oneByOneEnter = true;

    private float spawnTimer; // 스폰을 대기하는 타이머
    private int aliveCount;   // 지금 가게에 있는 인원

    private void Start()
    {
        gm = RestaurantGameManager.instance;
    }
    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) { return; }

        spawnTimer = 0f;
        if (aliveCount >=  maxAliveCustomers + 1) { Debug.Log("현재 고객이 가득 찼습니다. "); return; }

        // if (oneByOneEnter && aliveCount > 0)
        // {
        //     /* 관련 함수 추가 */
        //     return;
        // }

        Spawn();
    }

    private void Spawn()
    {
        var customer = Instantiate(customerPrefeb, transform.position, transform.rotation);
        aliveCount++;
    }
}
