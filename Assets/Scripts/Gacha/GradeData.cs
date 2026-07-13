using UnityEngine;

[CreateAssetMenu(fileName = "GradeData", menuName = "Game Data/Gacha/Grade")]
public class GradeData : ScriptableObject
{
    public string GradeName; 
    public int MinStat;      
    public int MaxStat;      
    public int TotalStat;    
    public int WeeklyWage;   
}
