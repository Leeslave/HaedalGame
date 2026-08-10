using UnityEngine;

// 오늘 하루치 손님 수(대접/미대접) 집계.
public class DailyCustomerTracker : MonoBehaviour
{
    public static DailyCustomerTracker Instance { get; private set; }

    private int _servedCount = 0;
    private int _notServedCount = 0;

    public int ServedCount => _servedCount;
    public int NotServedCount => _notServedCount;
    public int TotalCount => _servedCount + _notServedCount;

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

    public void RecordExit(bool wasServed)
    {
        if (wasServed) { _servedCount += 1; }
        else { _notServedCount += 1; }
    }

    public void ResetDay()
    {
        _servedCount = 0;
        _notServedCount = 0;
    }
}
