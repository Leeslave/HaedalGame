using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RestaurantLitter : MonoBehaviour
{
    private static readonly List<GameObject> Pieces = new List<GameObject>();
    public static int Count => Pieces.Count;
    private void OnEnable() { Pieces.Clear(); CustomerAgent.OnAnyPaid += OnPaid; }
    private void OnDisable() { CustomerAgent.OnAnyPaid -= OnPaid; Pieces.Clear(); }
    private void OnPaid(CustomerAgent customer, RecipeData recipe, int amount)
    {
        if (Random.value > .35f) return;
        var item = new GameObject("눌러서 쓰레기 치우기");
        item.transform.SetParent(transform);
        item.transform.position = customer.transform.position + new Vector3(.35f, -.3f, 0);
        item.transform.localScale = new Vector3(.28f, .22f, 1);
        item.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        var sprite = item.AddComponent<SpriteRenderer>();
        sprite.sprite = DayLoopUI.Rounded; sprite.color = DayLoopUI.Wood;
        sprite.sortingLayerID = DayLoopUI.TopSortingLayerId; sprite.sortingOrder = 20;
        Pieces.Add(item);
    }
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || Camera.main == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Vector2 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        for (int i = Pieces.Count - 1; i >= 0; i--)
            if (Vector2.Distance(position, Pieces[i].transform.position) < .45f)
            { Destroy(Pieces[i]); Pieces.RemoveAt(i); break; }
    }
}
