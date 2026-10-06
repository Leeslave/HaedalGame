using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class StaffRuntimeBinding
{
    public static List<T> Configure<T>(GameObject parent, PartTimerRole role) where T : PartTimerAgent
    {
        var agents = parent.GetComponentsInChildren<T>(true).ToList();
        if (!GameSession.IsActive || agents.Count == 0) return agents;
        var staff = GameSession.Current.employees.Where(e => e.role == role.ToString())
            .OrderBy(e => e.slotIndex).Take(RestaurantRules.StaffLimit(GameSession.Current.restaurantLevel, role)).ToList();
        while (agents.Count < staff.Count) agents.Add(Object.Instantiate(agents[0], parent.transform));
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].gameObject.SetActive(i < staff.Count);
            if (i < staff.Count) agents[i].BindEmployee(staff[i]);
        }
        return agents.Take(staff.Count).ToList();
    }
}

public class StaffWakeControl : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || Camera.main == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Vector2 world = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (var agent in FindObjectsByType<PartTimerAgent>(FindObjectsSortMode.None))
            if (agent.IsSleeping && Vector2.Distance(world, agent.transform.position) < .9f) { agent.WakeUp(); break; }
    }
}
