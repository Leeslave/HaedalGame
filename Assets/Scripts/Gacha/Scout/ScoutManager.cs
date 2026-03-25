using System.Collections.Generic;
using UnityEngine;

public class ScoutManager : MonoBehaviour
{
    public static ScoutManager Instance;

    public List<ScoutData> ScoutDatas;
    public List<PartTimerData> CandinateLists = new();
    public List<CondinateSlot> UiSlots;

    public GameObject SelectedSection;

    [SerializeField] private List<GameObject> _sections;
    private int _selectedSectionIndex;

    [Header("임금체불 패널티 관련 변수")]
    public bool _isPenaltyActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    public void Initialize()
    {
        _sections[_selectedSectionIndex].SetActive(false);
        _selectedSectionIndex = 0;
        SelectedSection = _sections[_selectedSectionIndex];
        SelectedSection.SetActive(true);

        for (int i = 0; i < UiSlots.Count; i++)
            UiSlots[i].SetEmpty();
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
        int finallyScoutCnt = Mathf.Max(0, targetScoutData.ApplicantCount - GetPenaltyReductionCount(0));

        CandinateLists.Clear();

        for (int i = 0; i < finallyScoutCnt; i++)
        {
            GradeData rolledGrade = DetermineGrade(targetScoutData);
            if (rolledGrade == null)
                continue;

            PartTimerData newData = new PartTimerData();
            newData.status = GachaManager.Instance.GenerateRandomStatus(rolledGrade);
            newData.serverName = "신입 해달";
            newData.level = rolledGrade.name;

            CandinateLists.Add(newData);
        }

        UpdateScoutUI();
    }

    public GradeData DetermineGrade(ScoutData targetScoutData)
    {
        float probabilty = Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var grade in targetScoutData.GradeDistribution)
        {
            cumulativeProbability += grade.Value;

            if (probabilty <= cumulativeProbability)
                return grade.Key;
        }

        Debug.LogWarning("DetermineGrade 함수 오류");
        return null;
    }

    private void UpdateScoutUI()
    {
        for (int i = 0; i < UiSlots.Count; i++)
        {
            if (i < CandinateLists.Count)
                UiSlots[i].BindMystery(CandinateLists[i], i);
            else
                UiSlots[i].SetEmpty();
        }
    }

    public void RequestHire(CondinateSlot slot)
    {
        if (slot == null || slot.Data == null)
            return;

        PopupManager.Instance.ShowConfirmPopup(
            $"{slot.Data.serverName}을(를) 고용하시겠습니까?\n",
            "네",
            "아니오",
            () => HireCandidate(slot),
            null,
            "(고용 시 첫 주급이 바로 지불됩니다.)"
        );
    }

    private void HireCandidate(CondinateSlot slot)
    {
        if (slot == null || slot.Data == null)
            return;

        PartTimerData hiredData = slot.Data;

        Debug.Log($"{hiredData.serverName} 고용 완료");
        bool success = PartTimerAssignmentManager.Instance.RegisterHiredPartTimer(slot.Data);
        slot.MarkHired();
        // TODO:
        Initialize();
    }

    public void ChangeSection()
    {
        _sections[_selectedSectionIndex].SetActive(false);

        _selectedSectionIndex = (_selectedSectionIndex + 1) % _sections.Count;

        GameObject nextSection = _sections[_selectedSectionIndex];
        nextSection.SetActive(true);

        SelectedSection = nextSection;
    }
}