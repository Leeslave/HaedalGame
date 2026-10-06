using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 결산 확정 체크포인트와 결산 문구. (FLOW-03, SAVE-01/02, RESULT-02/03)
///
/// 복구 정책: 파일에는 "하루 시작(DayStart)" 체크포인트만 기록한다. (기존 세이브 설계 유지)
///  - 영업 도중 종료 → 그날 시작 시점부터 다시. 영업 중 번 골드도 함께 되돌아가므로 수익을 두 번 얻지 않는다.
///  - 결산 확정은 메모리에서 당일 자유시간으로 전환한다.
///  - 파일 저장과 다음 날 전환은 RestaurantProgress.TrySleep에서 함께 수행한다.
/// </summary>
public static class DayLoopSave
{
    public const string FirstDayGuideId = "DayLoopTEST.FirstDayGuide";

    /// <summary>
    /// 오늘 결산을 메모리에 한 번 확정한다. 날짜와 마지막 저장 파일은 유지한다.
    /// </summary>
    public static bool TryCommitDay(DaySettlementRecord record, out string error)
    {
        error = null;
        if (!GameSession.IsActive)
        {
            error = "진행 중인 게임 세션이 없습니다.";
            return false;
        }
        if (record == null || record.day != GameSession.Current.day) { error = "결산 일차가 현재 일차와 다릅니다."; return false; }

        if (GameSession.Current.lastCompletedDay >= record.day)
            return true;   // 이미 확정된 날 (재시도·연타)

        GameSaveData candidate = GameSession.CaptureSnapshot();
        candidate.lastCompletedDay = record.day;
        candidate.lifetimeRevenue += record.revenue;
        candidate.lastSettlement = JsonUtility.FromJson<DaySettlementRecord>(JsonUtility.ToJson(record));
        candidate.settlementPendingReview = true;
        candidate.progression.freeTime = true;
        GameSession.ApplyRuntime(candidate);
        return true;
    }

    /// <summary>플레이어가 결산 화면을 확인했다고 기록한다. 실패해도 진행에는 지장이 없다. (다음 이어하기 때 결산을 한 번 더 보여 줄 뿐)</summary>
    public static void AcknowledgeSettlement()
    {
        if (!GameSession.IsActive || !GameSession.Current.settlementPendingReview)
            return;

        GameSession.Current.settlementPendingReview = false;
    }

    // ───── 결산 문구 ─────

    /// <summary>평점 변화 설명. 시작 평판과 평가 0건을 구분해 설명한다.</summary>
    public static string RatingNote(DaySettlementRecord r)
    {
        if (r.ratingCount <= 0)
            return "오늘은 평가가 없어 식당 평점이 그대로예요.";

        string note = $"식당 평점은 최근 7일 영업 평점의 평균이에요. 오늘 평점 {DayLoopUI.Rating(r.todayRating)} ({r.ratingCount}건)이 반영됐어요.";
        if (r.seedRatingIncluded)
            note += " 처음 7일 동안은 시작 평판 4.0이 평균에 함께 들어가요.";
        return note;
    }

    /// <summary>
    /// 실제로 측정한 값만으로 다음 날 준비 한 가지를 제안한다. 측정값이 없으면 진단하지 않는다.
    /// 아직 없는 기능(업그레이드 구매 등)을 권하지 않는다.
    /// </summary>
    public static string Suggestion(DaySettlementRecord r)
    {
        if (r.visitedCount == 0)
            return "오늘은 손님이 없었어요.";

        float seatWait = r.seatWaitSamples > 0 ? r.seatWaitSum / r.seatWaitSamples : -1f;
        float foodWait = r.foodWaitSamples > 0 ? r.foodWaitSum / r.foodWaitSamples : -1f;
        float orderTake = r.orderTakeSamples > 0 ? r.orderTakeSum / r.orderTakeSamples : -1f;

        if (r.notServedCount > 0)
        {
            if (seatWait >= 15f)
                return $"기다리다 돌아간 손님이 {r.notServedCount}명 있어요. 자리 대기가 평균 {seatWait:0}초였어요. 테이블 배치를 확인해 보세요.";
            if (foodWait >= 25f)
                return $"기다리다 돌아간 손님이 {r.notServedCount}명 있어요. 음식이 도착하기까지 평균 {foodWait:0}초 걸렸어요. 주방·홀 직원 배치를 확인해 보세요.";
            return $"대접하지 못한 손님이 {r.notServedCount}명 있어요. 오늘의 메뉴와 직원 배치를 확인해 보세요.";
        }

        if (seatWait >= 15f)
            return $"자리 대기가 평균 {seatWait:0}초였어요. 테이블 배치를 확인해 보세요.";
        if (foodWait >= 25f)
            return $"음식이 도착하기까지 평균 {foodWait:0}초 걸렸어요. 주방·홀 직원 배치를 확인해 보세요.";
        if (orderTake >= 10f)
            return $"주문을 받기까지 평균 {orderTake:0}초 걸렸어요. 홀 직원 배치를 확인해 보세요.";

        return r.day == 1
            ? "첫 영업에서 모든 손님을 대접했어요! 내일은 섬의 메뉴판에서 메뉴 구성을 바꿔 보세요."
            : "모든 손님을 대접했어요. 메뉴 구성을 바꿔 매출을 늘려 보세요.";
    }

    public static string Headline(DaySettlementRecord r)
    {
        if (r.visitedCount == 0) return "손님이 없는 하루였어요";
        if (r.notServedCount == 0) return r.day == 1 ? "첫 영업 성공!" : "모든 손님 대접 완료!";
        return $"{r.servedCount}명을 대접했어요";
    }
}
