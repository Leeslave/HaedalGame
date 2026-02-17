using System.Collections.Generic;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;

    [SerializeField] private ServerAgent serverPrefab;
    [SerializeField] private ServerData[] servers;
    [ReadOnly][SerializeField] private List<ServerAgent> activeServers;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeServers();
    }

    private void InitializeServers()
    {
        foreach(ServerData d in servers)
        {
            var server = Instantiate(serverPrefab, Vector2.zero, Quaternion.identity);
            server.InitialServerSetting(d);
            activeServers.Add(server);
        }
    }

    public void HireServer(ServerData target)
    {
        
    }

    public void FireServer(ServerData target)
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
