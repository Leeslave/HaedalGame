using UnityEngine;

/// <summary>
/// 섞기/밑간 페이즈. 재료를 그릇에 자유 순서로 드래그앤드롭한다.
/// </summary>
[CreateAssetMenu(fileName = "MixPhaseSO", menuName = "Game Data/Lab/Phase/Mix")]
public class MixPhaseSO : PhaseSO
{
    public override CookingActionType ActionType => CookingActionType.Mix;
}
