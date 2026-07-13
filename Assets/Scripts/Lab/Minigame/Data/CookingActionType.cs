using System;

/// <summary>
/// 조리 액션 종류. 페이즈의 인터랙션 타입이자 조리 도구가 지원하는 액션 목록에 사용된다.
/// </summary>
[Serializable]
public enum CookingActionType
{
    Chop,       // 썰기/다지기 (드래그 + 클릭 연타)
    Mix,        // 섞기/밑간 (드래그앤드롭)
    Grill,      // 굽기 (타이밍 게이지)
    Boil,       // 끓이기 (타이밍 게이지)
    StirFry,    // 볶기 (타이밍 게이지)
    Fry,        // 튀기기 (타이밍 게이지)
    Assembly    // 플레이팅 (드래그앤드롭)
}
