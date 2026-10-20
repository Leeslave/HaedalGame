using System;
using UnityEngine;

/// <summary>모집·능력치 규칙. 500G 원문의 70:30:10은 가중치로 정규화한다.</summary>
public static class StaffRecruitmentRules
{
    private static readonly int[] Applicants = { 2, 3, 3, 4 };
    private static readonly int[,] Weights = { {75,20,5}, {70,30,10}, {65,25,10}, {60,30,10} };
    private static readonly int[] Minimum = {1,5,5,10,15,20};
    private static readonly int[] Maximum = {20,30,50,70,100,150};
    private static readonly int[] Totals = {40,60,100,140,200,300};
    public static int ApplicantCount(int tier, int unpaidCount, bool penalty) => Mathf.Max(0,
        Applicants[Mathf.Clamp(tier, 0, 3)] - (penalty ? Mathf.Min(3, (Mathf.Max(1, unpaidCount) + 1) / 2) : 0));
    public static int Total(int grade) => Totals[Mathf.Clamp(grade, 0, 5)];
    public static int Min(int grade) => Minimum[Mathf.Clamp(grade, 0, 5)];
    public static int Max(int grade) => Maximum[Mathf.Clamp(grade, 0, 5)];
    public static int RollGrade(int tier, System.Random random)
    {
        tier = Mathf.Clamp(tier, 0, 3);
        int roll = random.Next(Weights[tier,0] + Weights[tier,1] + Weights[tier,2]);
        for (int i = 0; i < 3; i++) { if (roll < Weights[tier,i]) return tier + i; roll -= Weights[tier,i]; }
        return tier;
    }
    public static PartTimerStatus RollStats(int grade, System.Random random)
    {
        grade = Mathf.Clamp(grade, 0, 5);
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int cooking = RollStat(grade, random), serving = RollStat(grade, random), hp = RollStat(grade, random);
            int handy = Total(grade) - cooking - serving - hp;
            if (handy >= Min(grade) && handy <= Max(grade))
                return new PartTimerStatus { cooking = cooking, serving = serving, hp = hp, handy = handy };
        }
        int m = Total(grade) / 4;
        return new PartTimerStatus { cooking = m, serving = m, hp = m, handy = Total(grade) - 3*m };
    }
    private static int RollStat(int grade, System.Random random)
    {
        float m = Total(grade) / 4f;
        int roll = random.Next(100), band = roll < 60 ? 0 : roll < 85 ? 1 : roll < 95 ? 2 : 3;
        var candidates = new System.Collections.Generic.List<int>();
        for (int value = Min(grade); value <= Max(grade); value++)
        {
            float distance = Mathf.Abs(value / m - 1);
            int actualBand = distance <= .10001f ? 0 : distance <= .30001f ? 1 : distance <= .50001f ? 2 : 3;
            if (actualBand == band) candidates.Add(value);
        }
        return candidates.Count == 0 ? Mathf.RoundToInt(m) : candidates[random.Next(candidates.Count)];
    }
}
