using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ScoutManager : MonoBehaviour
{
    public static ScoutManager Instance;

    [Header("임금체불 패널티 관련 변수")]
    public bool _isPenaltyActive;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public int GetPenaltyReductionCount(int unpaidWageCount)
    {
        int penaltyReductionCount = 0;

        if (_isPenaltyActive)
        {
            if (unpaidWageCount <= 2)
                penaltyReductionCount = 1;
            else if (unpaidWageCount <= 4)
                penaltyReductionCount = 2;
            else
                penaltyReductionCount = 3;
        }
        
        return penaltyReductionCount;
    }
}
