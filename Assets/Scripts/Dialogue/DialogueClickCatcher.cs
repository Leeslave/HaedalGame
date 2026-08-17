using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 대사창 뒤에 깔아두고 화면 아무 곳이나 눌렀는지 받아내는 판.
/// 선택지 버튼이나 건너뛰기 버튼은 이 판 위에 있어서 클릭을 먼저 가져가므로,
/// 여기로 오는 클릭은 "그냥 다음으로 넘겨달라"는 뜻만 남는다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DialogueClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public event Action OnClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button == PointerEventData.InputButton.Left)
            OnClicked?.Invoke();
    }
}
