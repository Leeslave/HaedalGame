using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ManagementDialog : MonoBehaviour
{
    public static void Show(Transform parent, string message, Action confirm)
    {
        var shade = DayLoopUI.Panel(parent, DayLoopUI.Dim).rectTransform;
        DayLoopUI.Stretch(shade);
        var panel = DayLoopUI.Panel(shade, DayLoopUI.Cream).rectTransform;
        DayLoopUI.Place(panel, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(900, 350));
        var text = DayLoopUI.Text(panel, message, 28, DayLoopUI.Ink, TextAlignmentOptions.Center);
        DayLoopUI.Place(text.rectTransform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 50), new Vector2(820, 180));
        var cancel = DayLoopUI.Button(panel, "계속 연구", DayLoopUI.Sea, Color.white, 26, () => Destroy(shade.gameObject));
        DayLoopUI.Place((RectTransform)cancel.transform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(-200, -110), new Vector2(300, 60));
        var ok = DayLoopUI.Button(panel, "도안 파손·취소", DayLoopUI.Warn, Color.white, 26, () => { Destroy(shade.gameObject); confirm(); });
        DayLoopUI.Place((RectTransform)ok.transform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(200, -110), new Vector2(300, 60));
    }
    public static void RefundEffect(Transform parent)
    {
        var root = DayLoopUI.Rect("Research refund", parent);
        DayLoopUI.Stretch(root);
        root.gameObject.AddComponent<ManagementDialog>().StartCoroutine(Animate(root));
    }
    private static IEnumerator Animate(RectTransform root)
    {
        var pieces = new RectTransform[12];
        for (int i = 0; i < pieces.Length; i++)
        {
            var image = DayLoopUI.Panel(root, i < 6 ? DayLoopUI.Wood : DayLoopUI.Good);
            image.raycastTarget = false;
            pieces[i] = image.rectTransform;
            DayLoopUI.Place(pieces[i], Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(20, 20));
        }
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].anchoredPosition = i < 6 ? new Vector2((i - 2.5f) * t * 100, -t*t*250) : Vector2.Lerp(Vector2.zero, new Vector2(700, -400), t*t) + new Vector2((i-9)*20, 0);
                pieces[i].localScale = Vector3.one * (1-t);
            }
            yield return null;
        }
        Destroy(root.gameObject);
    }
}
