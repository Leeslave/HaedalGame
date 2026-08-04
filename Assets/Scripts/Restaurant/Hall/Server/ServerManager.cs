using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;

    //[SerializeField] private ServerAgent serverPrefab;
    [SerializeField] private PartTimerData[] servers;
    [SerializeField] private Transform[] serverInitposition;
    [SerializeField] private Transform kitchen;
    [SerializeField] private GameObject parent;
    public Vector2 GetKitchenPosition() { return kitchen.position; }

    // index는 0-based. 씬에 배치된 알바 순서를 그대로 사용하므로 인스펙터에서 별도로 맞춰줄 필요가 없다.
    public Vector2 GetInitPosition(int index)
    {
        if (index < 0 || index >= serverInitposition.Length)
        {
            Debug.LogError($"[ServerManager] 서빙 알바 인덱스 {index}에 해당하는 초기 위치가 없습니다. serverInitposition 배열 크기를 확인하세요.");
            return transform.position;
        }
        return serverInitposition[index].position;
    }

    [ReadOnly][SerializeField] private List<ServerAgent> activeServers;


    void Awake()
    {
        Instance = this;
    }

    void Start() { }

    public void InitializeAgents()
    {
        activeServers = new List<ServerAgent>(parent.GetComponentsInChildren<ServerAgent>());
        for (int i = 0; i < activeServers.Count; i++)
            activeServers[i].Initialize(i);
    }

    public void HireServer(PartTimerData target)
    {
        
    }

    public void FireServer(PartTimerData target)
    {
        
    }

    public void UpgradeServer(ServerAgent target)
    {
        
    }

    public void ArrangeServers()
    {
        // 레스토랑 플레이 씬에서 서버들 배치
    }




}
