using UnityEngine;

/// <summary>
/// 볶기 페이즈. 웍 + 타이밍 게이지. (Grill과 구조 동일, 도구만 다름)
/// </summary>
[CreateAssetMenu(fileName = "StirFryPhaseSO", menuName = "Game Data/Lab/Phase/StirFry")]
public class StirFryPhaseSO : TimingPhaseSO
{
    public override CookingActionType ActionType => CookingActionType.StirFry;
}
