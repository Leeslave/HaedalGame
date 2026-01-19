public enum CustomerState
{
    Enter,              // 식당 입장
    Seating,            // 좌석 배정 중
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