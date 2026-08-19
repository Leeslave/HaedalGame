using System;

// 운영 중 UI의 2배속 버튼으로 켜고 끄는 배속 상태 저장소.
// 손님/알바생 이동속도와 손님 인내심 감소 속도에만 적용된다 (조리/식사/대기 타이머 등은 영향 없음).
// MonoBehaviour/GameObject를 두지 않는 순수 static 클래스: 런타임에 GameObject를 새로 만드는 방식은
// 씬이 정리되는 타이밍(플레이 종료, 코루틴 실행 중 등)에 걸리면 "Some objects were not cleaned up
// when closing the scene" 경고와 함께 그 시점에 참조하던 코루틴(알바생 이동 등)이 예외로 죽을 수 있어
// 이 방식으로는 그 위험 자체가 없다.
public static class RestaurantSpeedController
{
    public static bool IsFastForward { get; private set; }
    public static float SpeedMultiplier => IsFastForward ? 2f : 1f;

    public static event Action<bool> OnFastForwardChanged;

    public static void ToggleFastForward()
    {
        SetFastForward(!IsFastForward);
    }

    public static void SetFastForward(bool value)
    {
        if (IsFastForward == value) { return; }
        IsFastForward = value;
        OnFastForwardChanged?.Invoke(IsFastForward);
    }
}
