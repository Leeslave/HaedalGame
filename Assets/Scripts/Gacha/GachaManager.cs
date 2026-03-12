using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance;
    public List<GradeData> GradeDatas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public PartTimerStatus GenerateRandomStatus(GradeData gradeData)
    {
        int totalStat = gradeData.TotalStat;
        int M = totalStat / 4; 
        int minStat = gradeData.MinStat; int maxStat = gradeData.MaxStat; 
        bool isValid = false;
        int failSafeCount = 0;

        PartTimerStatus status = new PartTimerStatus();

        // 능력치 범위 벗어나면 100번까지 다시 굴리기
        while (!isValid && failSafeCount < 100)
        {
            failSafeCount++;

            int tempServing = GetRandomStatValue(M, minStat, maxStat);
            int tempCooking = GetRandomStatValue(M, minStat, maxStat);
            int tempHp = GetRandomStatValue(M, minStat, maxStat);

            int tempHandy = totalStat - (tempServing + tempCooking + tempHp);

            if (tempHandy >= minStat && tempHandy <= maxStat)
            {
                status.serving = tempServing;
                status.cooking = tempCooking;
                status.hp = tempHp;
                status.handy = tempHandy;
                isValid = true;
            }
        }

        // 100번 연속 실패할 경우
        if (!isValid)
        {
            Debug.LogWarning("스탯 분배 재시도 초과");
            status.serving = totalStat / 4;
            status.cooking = totalStat / 4;
            status.hp = totalStat / 4;
            status.handy = totalStat - (status.serving + status.cooking + status.hp);
        }

        return status;
    }

    private int GetRandomStatValue(float M, int minStat, int maxStat)
    {
        int result = 0;
        float rand = Random.Range(0f, 100f);

        if (rand < 60f) // 60% 확률
        {
            result = Mathf.RoundToInt(Random.Range(M * 0.9f, M * 1.1f));
        }
        else if (rand < 85f) // 25% 확률
        {
            if (Random.value < 0.5f) result = Mathf.RoundToInt(Random.Range(M * 0.7f, M * 0.9f));
            else result = Mathf.RoundToInt(Random.Range(M * 1.1f, M * 1.3f));
        }
        else if (rand < 95f) // 10% 확률
        {
            if (Random.value < 0.5f) result = Mathf.RoundToInt(Random.Range(M * 0.5f, M * 0.7f));
            else result = Mathf.RoundToInt(Random.Range(M * 1.3f, M * 1.5f));
        }
        else // 5% 확률
        {
            if (Random.value < 0.5f) result = Mathf.RoundToInt(Random.Range(minStat, M * 0.5f));
            else result = Mathf.RoundToInt(Random.Range(M * 1.5f, maxStat));
        }

        return Mathf.Clamp(result, minStat, maxStat);
    }
}
