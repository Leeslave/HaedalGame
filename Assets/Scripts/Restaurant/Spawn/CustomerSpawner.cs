using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CustomerType
{
    Default, // 일반 손님
    Special  // 특별 손님
}
[System.Serializable]
public struct CustomerProperty
{
    public float patience;
}



public class CustomerSpawner : MonoBehaviour
{

    [ReadOnly][SerializeField] CustomerSpawnManager csm;

    [SerializeField] private Transform customerParent;
    [Header("Test Customer")]
    [SerializeField] private bool isTest = false;
    public CustomerProperty[] customers;

    [Header("Spawn")]
    private Queue<float> spawnQueue = new Queue<float>();
    [SerializeField] private float spawnInterval = 5f;

    void Awake()
    {
        csm = GetComponent<CustomerSpawnManager>();
    }

    void Start()
    {
        // 테스트든 아니든 큐에 입력
        if (isTest)
        {
            CustomerSpawnForTest();
        }
        else
        {
            CustomerSpawn();
        }

        StartCoroutine(ManageQueue());
    }

    private void CustomerSpawnForTest()
    {
        foreach (var dc in customers) { spawnQueue.Enqueue(dc.patience); }
    }

    private void CustomerSpawn()
    {
        // CSV Read
    }

    private IEnumerator ManageQueue()
    {
        while (true)
        {
            if (spawnQueue.Count > 0)
            {
                float curPat = spawnQueue.Dequeue();
                csm.SpawnCustomer(curPat, customerParent);
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                yield return null; // 큐가 비면 매 프레임 대기
            }
        }
    }
}
