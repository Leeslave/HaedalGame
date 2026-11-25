using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 손님이 결제할 때 35% 확률로 쓰레기를 남긴다. 영업이 끝날 때 하나라도 남아 있으면 그날 평점이 0.2 깎인다.
/// QA #3: 예전에는 갈색 작은 사각형이 손님 꼬리 자리에 생겨 보이지 않았다. 지금은 외곽선이 있는 흰 종이 뭉치로
/// 손님 발밑 아래쪽에 크게 그리고, 살짝 커졌다 작아지며 눈에 띄게 한다. 그날 첫 쓰레기가 생기면 알림을 띄운다.
/// </summary>
public class RestaurantLitter : MonoBehaviour
{
    public const float RatingPenalty = .2f;
    private static readonly Vector3 Offset = new Vector3(.45f, -.75f, 0);
    private const float ClickRadius = .6f;
    private static readonly List<GameObject> Pieces = new List<GameObject>();
    private static int toastDay = -1;
    public static int Count => Pieces.Count;
    private void OnEnable() { Pieces.Clear(); CustomerAgent.OnAnyPaid += OnPaid; }
    private void OnDisable() { CustomerAgent.OnAnyPaid -= OnPaid; Pieces.Clear(); }
    private void OnPaid(CustomerAgent customer, RecipeData recipe, int amount)
    {
        if (Random.value > .35f) return;
        var item = new GameObject("눌러서 쓰레기 치우기");
        item.transform.SetParent(transform);
        item.transform.position = customer.transform.position + Offset;
        item.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));
        Layer(item.transform, "Outline", DayLoopUI.Circle, DayLoopUI.Kit.Line, new Vector2(.62f, .5f), 20);
        Layer(item.transform, "Paper", DayLoopUI.Circle, Color.white, new Vector2(.5f, .38f), 21);
        Layer(item.transform, "Crease", DayLoopUI.Rounded, DayLoopUI.Kit.InkSoft, new Vector2(.3f, .045f), 22, -25f);
        Layer(item.transform, "Crease2", DayLoopUI.Rounded, DayLoopUI.Kit.InkSoft, new Vector2(.2f, .04f), 22, 35f);
        Pieces.Add(item);
        NotifyFirstOfDay();
    }
    private static void Layer(Transform parent, string name, Sprite sprite, Color color, Vector2 size, int order, float angle = 0)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localRotation = Quaternion.Euler(0, 0, angle);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = color;
        renderer.sortingLayerID = DayLoopUI.TopSortingLayerId; renderer.sortingOrder = order;
        var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
        child.transform.localScale = new Vector3(size.x / Mathf.Max(.001f, bounds.x), size.y / Mathf.Max(.001f, bounds.y), 1);
    }
    private static void NotifyFirstOfDay()
    {
        int day = GameSession.IsActive ? GameSession.Current.day : 0;
        if (toastDay == day || DayCycleController.Instance == null) return;
        toastDay = day;
        DayCycleController.Instance.Toast("손님이 쓰레기를 남겼어요! 눌러서 치우지 않으면 영업이 끝날 때 평점이 0.2 깎여요.");
    }
    private void Update()
    {
        float pulse = 1 + Mathf.Sin(Time.unscaledTime * 4f) * .08f;
        foreach (var piece in Pieces) if (piece != null) piece.transform.localScale = new Vector3(pulse, pulse, 1);
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || Camera.main == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Vector2 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        for (int i = Pieces.Count - 1; i >= 0; i--)
            if (Vector2.Distance(position, Pieces[i].transform.position) < ClickRadius)
            { Destroy(Pieces[i]); Pieces.RemoveAt(i); break; }
    }
}
