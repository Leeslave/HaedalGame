using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 위에 마우스를 올리면(터치는 누르면) 짧은 안내를 띄운다. 꺼진 버튼에서도 동작한다.
/// 안내 글이 null이나 빈 글이면 아무것도 띄우지 않는다.
/// </summary>
public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private Func<string> text;
    private GameObject tip;

    public static HoverTip Attach(GameObject target, Func<string> text)
    {
        var hover = target.GetComponent<HoverTip>();
        if (hover == null) hover = target.AddComponent<HoverTip>();
        hover.text = text;
        return hover;
    }

    /// <summary>둥근 (i) 아이콘. 올리면(누르면) text의 안내를 띄운다. 자리는 부르는 쪽에서 정한다.</summary>
    public static RectTransform InfoIcon(Transform parent, Func<string> text)
    {
        var circle = DayLoopUI.Panel(parent, DayLoopUI.Kit.Sea, "InfoIcon", false);
        circle.sprite = DayLoopUI.Circle; circle.type = Image.Type.Simple;
        circle.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var mark = DayLoopUI.Text(circle.transform, "i", 22, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        mark.raycastTarget = false;
        DayLoopUI.Stretch(mark.rectTransform);
        Attach(circle.gameObject, text);
        return circle.rectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData) => Show();
    public void OnPointerDown(PointerEventData eventData) => Show();
    public void OnPointerExit(PointerEventData eventData) => Hide();
    private void OnDisable() => Hide();

    private void Show()
    {
        Hide();
        string value = text?.Invoke();
        if (string.IsNullOrEmpty(value)) return;
        var panel = DayLoopUI.Panel(transform, new Color(58f / 255, 39f / 255, 24f / 255, .94f), "[HoverTip]");
        panel.raycastTarget = false;
        tip = panel.gameObject;
        tip.AddComponent<LayoutElement>().ignoreLayout = true;
        // 버튼을 흐리게 보여 주는 CanvasGroup의 투명도를 따라가지 않고, 다른 UI 위에 그린다.
        tip.AddComponent<CanvasGroup>().ignoreParentGroups = true;
        var canvas = tip.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 500;
        var label = DayLoopUI.Text(panel.transform, value, 21, Color.white, TextAlignmentOptions.Center);
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.lineSpacing = 12f;
        var size = label.GetPreferredValues(value);
        DayLoopUI.Stretch(label.rectTransform);
        DayLoopUI.Place(panel.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 0), new Vector2(0, 8), new Vector2(size.x + 40, size.y + 28));
    }

    private void Hide()
    {
        if (tip == null) return;
        Destroy(tip);
        tip = null;
    }
}
