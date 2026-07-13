using UnityEngine;

/// <summary>
/// 끓이기 페이즈. 냄비 + 타이밍 게이지. (Grill과 구조 동일, 도구만 다름)
/// </summary>
[CreateAssetMenu(fileName = "BoilPhaseSO", menuName = "Game Data/Lab/Phase/Boil")]
public class BoilPhaseSO : TimingPhaseSO
{
    public override CookingActionType ActionType => CookingActionType.Boil;
}
