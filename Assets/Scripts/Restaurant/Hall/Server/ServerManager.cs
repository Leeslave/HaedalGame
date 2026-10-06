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
            return serverInitposition.Length > 0 ? serverInitposition[index % serverInitposition.Length].position : transform.position;
        }
        return serverInitposition[index].position;
    }

    [ReadOnly][SerializeField] private List<ServerAgent> activeServers;


    void Awake()
    {
        Instance = this;
    }

    void Start() { StaffRuntimeBinding.Configure<ServerAgent>(parent, PartTimerRole.Serving); }

    public void InitializeAgents()
    {
        activeServers = StaffRuntimeBinding.Configure<ServerAgent>(parent, PartTimerRole.Serving);
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
