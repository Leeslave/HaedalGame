/// <summary>
/// 섞기/밑간 페이즈 컨트롤러. 재료를 그릇에 자유 순서로 드래그앤드롭한다.
/// 로직은 DragDropPhaseController 공통 구현을 사용한다.
/// </summary>
public class MixPhaseController : DragDropPhaseController
{
    public override CookingActionType ActionType => CookingActionType.Mix;
}
