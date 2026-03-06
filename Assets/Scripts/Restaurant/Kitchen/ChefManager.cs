using System.Collections.Generic;
using UnityEngine;

public class ChefManager : MonoBehaviour
{
    public static ChefManager Instance;
    [SerializeField] private PartTimerData[] chefs;
    [SerializeField] private Transform[] chefInitposition;
    [SerializeField] private Transform kitchen;
    public Vector2 GetKitchenPosition() { return kitchen.position; }
    public Vector2 GetInitPosition(int n) { return chefInitposition[n-1].position; }
    [ReadOnly][SerializeField] private List<ServerAgent> activeServers;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //InitializeServers();
    }

    private void InitializeServers()
    {
        
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
