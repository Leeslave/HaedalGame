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

    // index는 0-based. 씬에 배치된 알바 순서를 그대로 사용하므로 인스펙터에서 별도로 맞춰줄 필요가 없다.
    public Vector2 GetInitPosition(int index)
    {
        if (index < 0 || index >= chefInitposition.Length)
        {
            Debug.LogError($"[ChefManager] 주방 알바 인덱스 {index}에 해당하는 초기 위치가 없습니다. chefInitposition 배열 크기를 확인하세요.");
            return transform.position;
        }
        return chefInitposition[index].position;
    }

    [ReadOnly][SerializeField] private List<ChefAgent> activeChefs;

    private Dictionary<CookingType, bool> toolOccupied = new Dictionary<CookingType, bool>();
    private Dictionary<CookingType, Queue<ChefAgent>> toolWaiters = new Dictionary<CookingType, Queue<ChefAgent>>();

    // 도구가 비어있으면 즉시 점유하고 true를 반환한다. 사용 중이면 대기열에 등록하고 false를 반환하며,
    // 이후 해당 도구가 반납되는 즉시(폴링 없이) requester.OnToolGranted(type)이 호출된다.
    public bool RequestTool(CookingType type, ChefAgent requester)
    {
        if (!toolOccupied[type])
        {
            toolOccupied[type] = true;
            return true;
        }
        toolWaiters[type].Enqueue(requester);
        return false;
    }

    public void ReleaseTool(CookingType type)
    {
        if (toolWaiters[type].Count > 0)
        {
            ChefAgent next = toolWaiters[type].Dequeue();
            next.OnToolGranted(type); // 점유 상태를 유지한 채 다음 대기자에게 바로 넘긴다.
        }
        else
        {
            toolOccupied[type] = false;
        }
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
        toolWaiters[CookingType.Pan]  = new Queue<ChefAgent>();
        toolWaiters[CookingType.Chop] = new Queue<ChefAgent>();
        toolWaiters[CookingType.Fry]  = new Queue<ChefAgent>();
        toolWaiters[CookingType.Pot]  = new Queue<ChefAgent>();
    }

    void Start() { }

    public void InitializeAgents()
    {
        activeChefs = new List<ChefAgent>(parent.GetComponentsInChildren<ChefAgent>());
        for (int i = 0; i < activeChefs.Count; i++)
            activeChefs[i].Initialize(i);
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
