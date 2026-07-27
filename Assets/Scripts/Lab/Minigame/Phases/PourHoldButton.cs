using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 꾹 누르고 있는 동안 IsHeld = true. 계량 페이즈에서 "따르기" 입력으로 쓴다.
/// 마우스/터치 모두 지원. 비활성화되면 눌림 상태를 해제한다.
/// </summary>
public class PourHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsHeld = false;
    }

    private void OnDisable()
    {
        IsHeld = false;
    }
}
