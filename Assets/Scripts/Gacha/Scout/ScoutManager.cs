using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class ScoutManager : MonoBehaviour
{
    public static ScoutManager Instance;
    public List<ScoutData> ScoutDatas;
    public List<PartTimerData> CandinateList = new List<PartTimerData>();
    public List<CondinateSlot> UiSlots;
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

    public void Scout(ScoutData targetScoutData)
    {
        //TODO GetPanaltyReductionCount 인자 주급 미지급횟수로 변수 변경
        int finallyScoutCnt = Mathf.Max(0, targetScoutData.applicantCount - GetPenaltyReductionCount(0));
        //후보 리스트 초기화
        CandinateList.Clear();

        for (int i = 0; i < finallyScoutCnt; i++)
        {
            GradeData rolledGrade = DetermineGrade(targetScoutData);

            if (rolledGrade == null) continue;
            PartTimerData newData = new PartTimerData();
            newData.status = GachaManager.Instance.GenerateRandomStatus(rolledGrade);
            newData.serverName = "신입 해달";
            newData.level = rolledGrade.name;
            CandinateList.Add(newData);
        }

        UpdateScoutUI(); 
    }

    public GradeData DetermineGrade(ScoutData targetScoutData)
    {
        float probabilty = Random.Range(0f, 100f);
        // 누적 확률
        float cumulativeProbability = 0f;
        foreach (var grade in targetScoutData.GradeDistribution)
        {
            cumulativeProbability += grade.Value;

            if (probabilty <= cumulativeProbability)
            {
                return grade.Key;
            }
        }

        Debug.Log("DeterminGrade 함수 오류");
        return null;
    }

    private void UpdateScoutUI()
    {
        for (int i = 0; i < UiSlots.Count; i++)
        {
            if (i < CandinateList.Count)
            {
                PartTimerData data = CandinateList[i];
                UiSlots[i].SetData(data.level, data.serverName, data.status);
            }
            else
            {
                UiSlots[i].SetEmpty();
            }
        }
    }
}
