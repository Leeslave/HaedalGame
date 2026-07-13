using UnityEngine;

/// <summary>
/// 튀기기 페이즈. 튀김솥 + 타이밍 게이지. (Grill과 구조 동일, 도구만 다름)
/// </summary>
[CreateAssetMenu(fileName = "FryPhaseSO", menuName = "Game Data/Lab/Phase/Fry")]
public class FryPhaseSO : TimingPhaseSO
{
    public override CookingActionType ActionType => CookingActionType.Fry;
}
