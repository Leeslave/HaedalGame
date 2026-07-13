using UnityEngine;

/// <summary>
/// 굽기 페이즈. 후라이팬 + 타이밍 게이지.
/// </summary>
[CreateAssetMenu(fileName = "GrillPhaseSO", menuName = "Game Data/Lab/Phase/Grill")]
public class GrillPhaseSO : TimingPhaseSO
{
    public override CookingActionType ActionType => CookingActionType.Grill;
}
