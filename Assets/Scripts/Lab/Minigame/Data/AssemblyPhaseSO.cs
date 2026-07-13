using UnityEngine;

/// <summary>
/// 플레이팅 페이즈. 완성된 재료를 접시에 자유 순서로 드래그앤드롭한다.
/// </summary>
[CreateAssetMenu(fileName = "AssemblyPhaseSO", menuName = "Game Data/Lab/Phase/Assembly")]
public class AssemblyPhaseSO : PhaseSO
{
    public override CookingActionType ActionType => CookingActionType.Assembly;
}
