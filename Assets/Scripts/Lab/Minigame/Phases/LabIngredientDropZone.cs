using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 재료를 받는 드롭 타깃(그릇/접시). 드래그 아이템이 놓이면 이벤트를 발행한다.
/// 실제 수용/채점은 페이즈 컨트롤러가 판단한다.
/// </summary>
public class LabIngredientDropZone : MonoBehaviour, IDropHandler
{
    // 배치된 아이템이 담길 컨테이너 (미지정 시 자기 자신).
    [SerializeField] private Transform _itemContainer;

    public event Action<LabIngredientDragItem> OnItemDropped;

    public Transform ItemContainer => _itemContainer != null ? _itemContainer : transform;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        LabIngredientDragItem item = eventData.pointerDrag.GetComponent<LabIngredientDragItem>();

        if (item == null || item.IsPlaced)
            return;

        OnItemDropped?.Invoke(item);
    }
}
