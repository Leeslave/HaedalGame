using System.Collections.Generic;
using UnityEngine;

public class ChefManager : MonoBehaviour
{
    public static ChefManager Instance;
    [SerializeField] private PartTimerData[] chefs;
    [SerializeField] private Transform[] chefInitposition;
    [SerializeField] private Transform kitchen;


    [SerializeField] private Transform panArea;
    [SerializeField] private Transform chopArea;
    [SerializeField] private Transform fryArea;
    [SerializeField] private Transform potArea;

    [SerializeField] private GameObject parent;


    public Vector2 GetKitchenPosition() { return kitchen.position; }
    public Vector2 GetInitPosition(int n) { return chefInitposition[n - 1].position; }
    [ReadOnly][SerializeField] private List<ChefAgent> activeChefs;

    private Dictionary<CookingType, bool> toolOccupied = new Dictionary<CookingType, bool>();

    public bool TryOccupyTool(CookingType type)
    {
        if (toolOccupied.ContainsKey(type) && toolOccupied[type])
            return false;
        toolOccupied[type] = true;
        return true;
    }

    public void ReleaseTool(CookingType type)
    {
        if (toolOccupied.ContainsKey(type))
            toolOccupied[type] = false;
    }


    public Transform GetCookingToolTransform(CookingType type)
    {
        switch (type)
        {
            case CookingType.Pan:
                return panArea;
            case CookingType.Chop:
                return chopArea;
            case CookingType.Fry:
                return fryArea;
            case CookingType.Pot:
                return potArea;
        }
        return null;
    }

    void Awake()
    {
        Instance = this;
        toolOccupied[CookingType.Pan]  = false;
        toolOccupied[CookingType.Chop] = false;
        toolOccupied[CookingType.Fry]  = false;
        toolOccupied[CookingType.Pot]  = false;
    }

    void Start() { }

    public void InitializeAgents()
    {
        activeChefs = new List<ChefAgent>(parent.GetComponentsInChildren<ChefAgent>());
        foreach (ChefAgent agent in activeChefs)
            agent.Initialize();
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
