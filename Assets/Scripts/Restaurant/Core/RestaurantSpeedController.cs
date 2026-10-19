using System;

// 운영 중 UI의 배속 버튼으로 바꾸는 배속 상태 저장소. (x1 / x2 / x3)
// 기본: 손님/알바생 이동속도와 손님 인내심 감소 속도에만 적용된다 (조리/식사/대기 타이머 등은 영향 없음).
// 하루 루프(DayCycleController)처럼 배속을 Time.timeScale로 거는 쪽이 있으면 AppliedByTimeScale을 켜서
// 조리·식사·손님 생성까지 모두 같은 비율로 빨라지게 하고, 이동·인내심 배율은 1로 둬 이중 적용을 막는다.
// MonoBehaviour/GameObject를 두지 않는 순수 static 클래스: 런타임에 GameObject를 새로 만드는 방식은
// 씬이 정리되는 타이밍(플레이 종료, 코루틴 실행 중 등)에 걸리면 "Some objects were not cleaned up
// when closing the scene" 경고와 함께 그 시점에 참조하던 코루틴(알바생 이동 등)이 예외로 죽을 수 있어
// 이 방식으로는 그 위험 자체가 없다.
public static class RestaurantSpeedController
{
    private static readonly float[] Speeds = { 1f, 2f, 3f };

    public static int SpeedIndex { get; private set; }
    public static float CurrentSpeed => Speeds[SpeedIndex];
    public static bool IsFastForward => SpeedIndex > 0;
    public static bool AppliedByTimeScale { get; set; }
    public static float SpeedMultiplier => AppliedByTimeScale ? 1f : CurrentSpeed;

    public static event Action<bool> OnFastForwardChanged;

    // 기존 2배속 토글 버튼용: x1 <-> x2
    public static void ToggleFastForward()
    {
        SetFastForward(!IsFastForward);
    }

    // x1 -> x2 -> x3 -> x1
    public static void CycleSpeed()
    {
        SetSpeedIndex((SpeedIndex + 1) % Speeds.Length);
    }

    public static void SetFastForward(bool value)
    {
        SetSpeedIndex(value ? 1 : 0);
    }

    private static void SetSpeedIndex(int index)
    {
        if (index < 0 || index >= Speeds.Length || SpeedIndex == index) { return; }
        SpeedIndex = index;
        OnFastForwardChanged?.Invoke(IsFastForward);
    }
}
