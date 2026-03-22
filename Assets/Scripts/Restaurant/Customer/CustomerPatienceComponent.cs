using System;
using Unity.IntegerTime;
using UnityEngine;

public class CustomerPatienceComponent : MonoBehaviour
{
    private float defaultPatience = 25f;          // 기본 인내심 수치;
    [ReadOnly][SerializeField] 
    private float curPatience;        // 현재 인내심 수치 (SerializeField로 보이게는 설정)
    [SerializeField] private float graceSec = 5f;       // 모든 동작 이후, 인내심이 바로 줄어드는 것이 아니라 약간의 텀을 주기 위함.
    private float graceTimer;                           // 인내심 감소를 측정하는 시간
    [SerializeField] private float drainPerSec = 0.1f;  // 인내심이 초당 얼마 떨어지는지.
    private float ratio;

    private bool isPatience;                            // 인내심 감소 중인지 확인 flag
    private bool isWaiting;
    private bool triggered07;
    private bool triggered04;
    private bool triggered01;

    public event Action OnPatienceExhausted;            // 만약 인내심이 바닥났을 경우 실행하는 이벤트
    public event Action OnWaitingProgress;

    /* public Method */

    public void SetDrainRate(float value)               // 감소 수치 변경 시.
    { 
        drainPerSec = value; 
    }

    public void SetPatience(float value)
    {
        curPatience = defaultPatience = value;
    }
    public void SetIsWaiting(bool value)
    {
        isWaiting = value;
    }

    public void ChangeState()
    {
        isPatience = !isPatience;
    }

    public void ResetGraceTimer()                       // 감소 방어 타임 초기화
    {
        graceTimer = graceSec;
        isPatience = true;
    }

    /* RunTime */

    void Start()
    {
        isPatience = false;
        
        triggered07 = false;
        triggered04 = false;
        triggered01 = false;

        ratio = 0.0f;
        curPatience = defaultPatience;
        graceTimer = graceSec;
    }


    private void Update()
    {
        if (isPatience)
        {
            if (graceTimer > 0)                         // 아직 인내심 감소 방어 시간이 남아있을 경우
            { 
                graceTimer -= Time.deltaTime; 
            }
            else                                        // 인내심 방어 시간이 전부 떨어졌을 경우
            {
                
                curPatience -= drainPerSec * Time.deltaTime;
                if (isWaiting) { ForWaiting(); }
                if (curPatience <= 0)
                {
                    curPatience = 0f;
                    isPatience = false;
                    OnPatienceExhausted?.Invoke();
                }
            }
        }
    }

    private void ForWaiting()
    {
        ratio = curPatience / defaultPatience;

        if (!triggered07 && ratio < 0.7f)
        {
            triggered07 = true;
            OnWaitingProgress?.Invoke();
            return;
        }

        if (!triggered04 && ratio < 0.4f)
        {
            triggered04 = true;
            OnWaitingProgress?.Invoke();
            return;
        }
        
        if (!triggered01 && ratio < 0.1f)
        {
            triggered01 = true;
            OnWaitingProgress?.Invoke();
            return;
        }

        return;
    }



}