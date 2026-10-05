using System;
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

    private int activeCustomerCount = 0;
    private bool dayEndTriggered = false;

    private Coroutine manageQueueCoroutine;

    // 오늘 스폰할 손님을 모두 큐에서 꺼냈고, 활성 손님도 0명이 되었을 때(=마지막 손님이 나갔을 때) 발생.
    public Action OnAllCustomersHandled;

    // 오늘 들어올 손님을 모두 내보내 신규 입장이 끝났을 때 한 번 발생. (남은 손님 응대는 계속된다)
    public event Action OnEntryClosed;

    private int plannedCount = 0;
    private bool entryClosed = false;

    public int PlannedCount => plannedCount;             // 오늘 입장 예정 손님 수
    public int SpawnedCount => plannedCount - spawnQueue.Count;
    public int PendingSpawnCount => spawnQueue.Count;
    public int ActiveCustomerCount => activeCustomerCount;
    public bool IsEntryClosed => entryClosed;

    void Awake()
    {
        csm = GetComponent<CustomerSpawnManager>();
    }

    public void StartGame()
    {
        dayEndTriggered = false;
        entryClosed = false;
        activeCustomerCount = 0;
        spawnQueue.Clear();

        // 테스트든 아니든 큐에 입력
        if (isTest)
        {
            CustomerSpawnForTest();
        }
        else
        {
            CustomerSpawn();
        }
        plannedCount = spawnQueue.Count;
        if (manageQueueCoroutine != null) { StopCoroutine(manageQueueCoroutine); }
        manageQueueCoroutine = StartCoroutine(ManageQueue());
        CheckEntryClosed();
        CheckDayEnd();
    }

    private void CheckEntryClosed()
    {
        if (entryClosed || spawnQueue.Count > 0) { return; }
        entryClosed = true;
        OnEntryClosed?.Invoke();
    }

    void Start()
    {
        
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
        yield return new WaitForSeconds(1f);
        while (true)
        {
            if (spawnQueue.Count > 0 && RestaurantGameManager.instance.seatManager.HasAvailableSeat())
            {
                float curPat = spawnQueue.Dequeue();
                CustomerAgent customer = csm.SpawnCustomer(curPat, customerParent);
                activeCustomerCount++;
                customer.OnExited += HandleCustomerExited;
                CheckEntryClosed();
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    private void HandleCustomerExited(CustomerAgent customer)
    {
        customer.OnExited -= HandleCustomerExited;
        activeCustomerCount--;

        if (DailyCustomerTracker.Instance != null)
        {
            DailyCustomerTracker.Instance.RecordExit(customer.WasServed);
        }

        CheckDayEnd();
    }

    private void CheckDayEnd()
    {
        if (dayEndTriggered) { return; }
        if (spawnQueue.Count > 0 || activeCustomerCount > 0) { return; }

        dayEndTriggered = true;
        OnAllCustomersHandled?.Invoke();
    }
}
