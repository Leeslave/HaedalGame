using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "Scout", menuName = "Scriptable Objects/GameData/Scout")]
public class ScoutData : ScriptableObject
{
    [Header("식별 및 기본 정보")]
    public int RequiredCurrencyCount;
    public SerializedDictionary<string, float> GradeDistribution;
    public int applicantCount;
}