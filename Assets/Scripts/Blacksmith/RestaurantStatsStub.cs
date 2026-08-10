using System;
using UnityEngine;

/// <summary>
/// [임시 스텁] 식당 레벨 강화 조건에 쓰이는 전체 평점 제공자.
/// 평점 시스템이 구현되면 이 값을 실제 시스템에서 읽도록 교체한다.
/// 그 전까지는 인스펙터 값으로 테스트한다.
/// </summary>
public class RestaurantStatsStub : MonoBehaviour
{
    public static RestaurantStatsStub Instance { get; private set; }

    [Header("스텁 값 (실제 시스템 연결 전 테스트용)")]
    [SerializeField] private float _rating = 3.0f;
    [SerializeField] private int _salesAmount = 0;    // 판매 금액
    [SerializeField] private int _seatCount = 0;      // 현재 좌석 개수

    public event Action OnChanged;

    public float Rating => _rating;
    public int SalesAmount => _salesAmount;
    public int SeatCount => _seatCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetRating(float value)
    {
        _rating = Mathf.Max(0f, value);
        OnChanged?.Invoke();
    }

    public void SetSalesAmount(int value)
    {
        _salesAmount = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }

    public void SetSeatCount(int value)
    {
        _seatCount = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }
}
