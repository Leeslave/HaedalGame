using UnityEngine;

[CreateAssetMenu(fileName = "GradeData", menuName = "Scriptable Objects/GameData/Grade")]
public class GradeData : ScriptableObject
{
    public string GradeName; 
    public int MinStat;      
    public int MaxStat;      
    public int TotalStat;    
    public int WeeklyWage;   
}
