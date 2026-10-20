using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ResearchGesture : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Action success, failure;
    private Vector2 direction, start, previous;
    private float distance, needed;
    private bool scrub;
    public void Scrub(float length, Action complete) { scrub = true; needed = length; success = complete; }
    public void Stroke(Vector2 dir, float length, Action complete, Action fail)
    { direction = dir; needed = length; success = complete; failure = fail; }
    private Vector2 Local(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var point);
        return point;
    }
    public void OnBeginDrag(PointerEventData e) { start = previous = Local(e); distance = 0; }
    public void OnDrag(PointerEventData e)
    {
        Vector2 point = Local(e);
        if (scrub && ((RectTransform)transform).rect.Contains(point)) distance += Vector2.Distance(previous, point);
        previous = point;
        if (scrub && distance >= needed) { distance = 0; success?.Invoke(); }
    }
    public void OnEndDrag(PointerEventData e)
    {
        if (scrub) return;
        Vector2 delta = Local(e) - start;
        if (Vector2.Dot(delta, direction) >= needed && Vector2.Dot(delta.normalized, direction) >= .8f) success?.Invoke();
        else failure?.Invoke();
    }
}
