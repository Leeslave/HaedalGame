using UnityEngine;
using System.Collections;

public class PartTimerAgent : MonoBehaviour
{
    protected EmployeeEntry employee;
    public bool IsSleeping { get; private set; }
    private float lastWork;
    private float restTimer;
    public void BindEmployee(EmployeeEntry entry)
    {
        employee = entry;
        IsSleeping = false;
        if (employee.stamina < 0) employee.stamina = employee.hp;
        partTimerName = entry.name;
        level = entry.grade;
        gameObject.name = entry.name;
        var color = Color.HSVToRGB((entry.appearanceSeed % 360) / 360f, .18f, 1);
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.color = color;
    }
    protected PartTimerStatus SavedStatus(PartTimerStatus fallback) => employee == null ? fallback : new PartTimerStatus
    { serving = 3f * (1 + employee.serving / 100f), cooking = employee.cooking, handy = employee.handy, hp = employee.hp };
    public void WakeUp() { IsSleeping = false; }
    // 화면 표시용 남은 체력 비율 (직원 정보가 없으면 1)
    public float StaminaRatio => employee == null || employee.hp <= 0 ? 1f : Mathf.Clamp01(employee.stamina / employee.hp);
    private void Update()
    {
        if (employee == null || Time.time - lastWork < 5) return;
        restTimer += Time.deltaTime;
        if (restTimer >= 5) { employee.stamina = Mathf.Min(employee.hp, employee.stamina + 1); restTimer = 0; }
    }
    protected IEnumerator Work(float duration, float staminaCost = 1)
    {
        while (duration > 0)
        {
            lastWork = Time.time;
            if (!IsSleeping) duration -= Time.deltaTime * RestaurantSpeedController.SpeedMultiplier * (employee != null && employee.stamina <= 0 ? .5f : 1);
            yield return null;
        }
        if (employee != null)
        {
            employee.stamina = Mathf.Max(0, employee.stamina - staminaCost);
            IsSleeping = Random.value < .15f * (1 - employee.stamina / Mathf.Max(1, employee.hp));
        }
    }
    [Header("공통 사항")]
    [ReadOnly][SerializeField] protected string partTimerName;
    [SerializeField] protected Vector2 initPosition;
    [ReadOnly][SerializeField] protected string level;
    [SerializeField] protected int positionNumber;
    [SerializeField] protected float arrivalThreshold;

    // arrivalThreshold가 0(또는 음수)이면 "좌표가 정확히 일치해야 도착"으로 취급되는데,
    // transform.position은 부동소수점 오차 때문에 목표 지점과 정확히 같아지지 않을 수 있어
    // 이동 루프가 끝나지 않을 위험이 있다. 최소 허용 오차로 이를 방지한다.
    private const float MinArrivalThreshold = 0.05f;

    protected PartTimerMovement nm;

    protected void Awake()
    {
        nm = GetComponent<PartTimerMovement>();
    }

    // MoveTo의 성공 여부를 호출자에게 돌려주기 위한 out 파라미터용 결과 상자.
    // IEnumerator는 return으로 값을 못 주므로 이 방식을 사용한다.
    protected class MoveResult
    {
        public bool Success;
    }

    // 알바의 유일한 이동 수단. 식당 배치도(RestaurantNav)의 길을 따라 목적지까지 헤엄친다.
    // 잠들어 있으면 깰 때까지 기다렸다가 출발한다. 이동 속도가 0 이하로 잘못 설정된 경우에만 result.Success가 false로 남는다.
    protected IEnumerator MoveTo(Vector2 destination, float speed, MoveResult result)
    {
        result.Success = false;
        while (IsSleeping) yield return null;
        if (employee != null && employee.stamina <= 0) speed *= .5f;

        if (speed <= 0f)
        {
            Debug.LogError($"[{name}] MoveTo: 이동 속도가 {speed}입니다. PartTimerStatus 설정을 확인하세요. 이동을 건너뜁니다.");
            yield break;
        }

        if (RestaurantRatingManager.Instance != null) { speed *= RestaurantRatingManager.Instance.StaffSpeedMultiplier; }
        float baseSpeed = speed;
        float threshold = arrivalThreshold > 0f ? arrivalThreshold : MinArrivalThreshold;

        nm?.SetMoving(true);
        yield return RestaurantNav.Swim(transform, RestaurantNav.Route(transform.position, destination),
            () => baseSpeed * RestaurantSpeedController.SpeedMultiplier, dir => nm?.SetDirection(dir), threshold);
        nm?.SetMoving(false);
        result.Success = true;
    }

    // 도착 여부를 신경 쓰지 않는 호출부(대기 위치 복귀 등)를 위한 편의 오버로드.
    protected IEnumerator MoveTo(Vector2 destination, float speed)
    {
        yield return MoveTo(destination, speed, new MoveResult());
    }

    // 예전 길찾기의 재시도 진입점. 지금은 이동이 막히지 않으므로 한 번에 끝나지만, 호출부 흐름은 그대로 둔다.
    protected IEnumerator MoveToWithRetry(Vector2 destination, float speed, MoveResult result)
    {
        yield return MoveTo(destination, speed, result);
    }

    // 손님을 응대할 때 설 곳 (좌석 옆 물길)
    protected static Vector2 ApproachPoint(CustomerAgent customer)
    {
        Seat seat = customer.GetCurrentSeat();
        Vector2 seatPoint = seat != null ? (Vector2)seat.GetSeatPoint().position : (Vector2)customer.transform.position;
        return RestaurantNav.ServePoint(seatPoint);
    }
}
