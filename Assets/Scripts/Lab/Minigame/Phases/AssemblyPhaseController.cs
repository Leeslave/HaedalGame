/// <summary>
/// 플레이팅 페이즈 컨트롤러. 완성된 재료를 접시에 자유 순서로 드래그앤드롭한다.
/// 로직은 DragDropPhaseController 공통 구현을 사용한다.
/// </summary>
public class AssemblyPhaseController : DragDropPhaseController
{
    public override CookingActionType ActionType => CookingActionType.Assembly;
}
