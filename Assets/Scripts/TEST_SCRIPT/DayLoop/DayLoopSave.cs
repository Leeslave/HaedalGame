using UnityEngine;

/// <summary>
/// [TEST 하루 루프] 결산 확정 체크포인트와 결산 문구. (FLOW-03, SAVE-01/02, RESULT-02/03)
///
/// 복구 정책: 파일에는 "하루 시작(DayStart)" 체크포인트만 기록한다. (기존 세이브 설계 유지)
///  - 영업 도중 종료 → 그날 시작 시점부터 다시. 영업 중 번 골드도 함께 되돌아가므로 수익을 두 번 얻지 않는다.
///  - 결산 확정 = 다음 날 시작 체크포인트 저장. 날짜 +1, 골드·평점·결산 스냅샷을 한 파일에 함께 기록한다.
///  - 결산 화면에서 종료 → 이어하기 시 저장된 결산 스냅샷을 다시 보여 준다. (매출 재지급 없음)
///  - 저장에 실패하면 날짜를 진행시키지 않고 재시도를 요구한다.
/// </summary>
public static class DayLoopSave
{
    public const string FirstDayGuideId = "DayLoopTEST.FirstDayGuide";

    /// <summary>
    /// 오늘 결산을 확정하고 다음 날 시작 체크포인트를 저장한다. 같은 날을 두 번 확정하지 않는다.
    /// 성공하면 세션이 다음 날로 넘어간다. (InGameTimeManager 등 참여자 모두 복원)
    /// </summary>
    public static bool TryCommitDay(DaySettlementRecord record, out string error)
    {
        error = null;
        if (!GameSession.IsActive)
        {
            error = "진행 중인 게임 세션이 없습니다.";
            return false;
        }

        if (GameSession.Current.lastCompletedDay >= record.day)
            return true;   // 이미 확정된 날 (재시도·연타)

        GameSaveData candidate = GameSession.CaptureSnapshot();
        candidate.day = record.day + 1;
        candidate.lastCompletedDay = record.day;
        candidate.lifetimeRevenue += record.revenue;
        candidate.lastSettlement = JsonUtility.FromJson<DaySettlementRecord>(JsonUtility.ToJson(record));
        candidate.settlementPendingReview = true;
        GameSession.StampCheckpoint(candidate, GameSaveData.ReasonDayEnded);

        if (GameSession.IsDevSession)
        {
            Debug.Log($"[DayLoop] 에디터 직접 실행 세션이라 파일 저장 없이 {candidate.day}일차로 넘어갑니다.");
        }
        else if (!SaveService.TryWrite(candidate, out error))
        {
            Debug.LogError($"[DayLoop] {record.day}일차 결산 저장 실패: {error}");
            return false;
        }

        GameSession.ApplyCheckpoint(candidate);
        Debug.Log($"[DayLoop] {record.day}일차 결산 확정 → {candidate.day}일차 시작 체크포인트 저장");
        return true;
    }

    /// <summary>플레이어가 결산 화면을 확인했다고 기록한다. 실패해도 진행에는 지장이 없다. (다음 이어하기 때 결산을 한 번 더 보여 줄 뿐)</summary>
    public static void AcknowledgeSettlement()
    {
        if (!GameSession.IsActive || !GameSession.Current.settlementPendingReview)
            return;

        GameSession.Current.settlementPendingReview = false;
        if (GameSession.IsDevSession)
            return;

        // 체크포인트 직후라 세션 내용이 파일과 같다. 확인 표시만 바꿔 다시 기록한다.
        GameSaveData copy = GameSession.Current.Clone();
        copy.saveRevision++;
        if (!SaveService.TryWrite(copy, out string error))
            Debug.LogWarning("[DayLoop] 결산 확인 표시 저장 실패 (다음 이어하기 때 결산을 다시 보여 줍니다): " + error);
        else
            GameSession.Current.saveRevision = copy.saveRevision;
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
