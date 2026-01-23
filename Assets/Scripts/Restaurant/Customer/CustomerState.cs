public enum CustomerState
{
    None,               // 아무 상태도 아님
    Enter,              // 식당 입장
    Seating,            // 좌석 배정 중
    WaitingRoom,        // 웨이팅 석으로 이동
    WaitingForOrder,    // 주문 받기 대기
    WaitingForFood,     // 음식 받기 대기
    Eating,             // 식사
    Paying,             // 계산
    Exit                // 퇴장
}

public enum RatingFlag
{
    None,       // 0
    Low,        // 1
    Normal,     // 2
    High,       // 3
    Perfect     // 4
}

public enum CustomerType
{
    Default, // 일반 손님
    Special  // 특별 손님
}