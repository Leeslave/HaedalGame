using System.Collections.Generic;
using UnityEngine;

public class ChefManager : MonoBehaviour
{
    public static ChefManager Instance;
    [SerializeField] private PartTimerData[] chefs;
    [SerializeField] private Transform[] chefInitposition;
    [SerializeField] private Transform kitchen;


    [SerializeField] private Transform grillArea;
    [SerializeField] private Transform mixArea;
    [SerializeField] private Transform rawArea;
    [SerializeField] private Transform dessertArea;


    public Vector2 GetKitchenPosition() { return kitchen.position; }
    public Vector2 GetInitPosition(int n) { return chefInitposition[n-1].position; }
    [ReadOnly][SerializeField] private List<ServerAgent> activeServers;

    public Transform GetCookingToolTransform(CookingType type)
    {
        switch(type)
        {
            case CookingType.Grill:
                return grillArea;
            case CookingType.Mix:
                return mixArea;
            case CookingType.raw:
                return rawArea;
            case CookingType.dessert:
                return dessertArea;
        }
        return null;
    }

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
