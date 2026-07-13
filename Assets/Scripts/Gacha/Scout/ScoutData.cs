using UnityEngine;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "Scout", menuName = "Game Data/Gacha/Scout")]
public class ScoutData : ScriptableObject
{
    [Header("�ĺ� �� �⺻ ����")]
    public int RequiredCurrencyCount;
    public SerializedDictionary<GradeData, float> GradeDistribution;
    public int ApplicantCount;
    public Sprite ScoutIcon;
    public string ScoutName;
}