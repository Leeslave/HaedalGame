using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>House.dc.html을 새 Canvas에 구성한다. 관리 창의 집 탭과 취침 화면은 별도의 화면이다.</summary>
public sealed class ReferenceHouseView : MonoBehaviour
{
    private static ReferenceHouseView instance;
    private TMP_Text notice;

    public static void Open(Scene scene)
    {
        if (!GameSession.IsActive) return;
        if (instance != null) { Destroy(instance.gameObject); instance = null; }
        var canvas = DayLoopUI.CreateCanvas("Design / 09 House", 150, scene);
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        instance = canvas.gameObject.AddComponent<ReferenceHouseView>(); instance.Build();
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    private void Build()
    {
        var s = GameSession.Current;
        var background = ReferenceTitleView.Picture(transform, "IslandBackground"); DayLoopUI.Stretch(background.rectTransform);
        var shade = DayLoopUI.Panel(transform, new Color(.08f, .12f, .25f, .75f), "Night", false); DayLoopUI.Stretch(shade.rectTransform);
        var side = DayLoopUI.Panel(transform, new Color(74f / 255, 46f / 255, 27f / 255, .55f), "HouseMenu", false);
        ReferenceTitleView.Place(side.rectTransform, 0, 0, 280, 720);
        var status = Card(side.transform, 22, 22, 236, 108, "");
        Text(status, $"{s.day}일차 {(s.progression.freeTime ? "밤" : "아침")} · 집", 14, 10, 208, 32, 24, true);
        Text(status, $"{RestaurantProgress.Gold(s):N0}G", 14, 45, 208, 26, 21, true);
        Text(status, "잠들기 전에 정리할 것을 확인해요", 14, 78, 208, 18, 12);
        bool canSleep = s.progression.freeTime || RestaurantRules.IsClosedForConstruction(s);
        var sleep = Button(side.transform, $"취침 · 저장\n<size=60%>{s.day + 1}일차 아침으로 저장돼요</size>", 22, 142, 236, 72, DayLoopUI.Kit.Awning, () => {
            if (!RestaurantProgress.TrySleep(out string error)) notice.text = error;
        }); sleep.interactable = canSleep;
        RestaurantManagementUI.Mission(s, out string missionName, out int target);
        int reward = 200 + s.progression.missionIndex * 100;
        var claim = Button(side.transform, $"미션 보상 받기  {reward:N0}G", 22, 222, 236, 42, DayLoopUI.Kit.WoodLight, () => {
            if (RestaurantProgress.TryClaimMission(out string error)) Open(gameObject.scene); else notice.text = error;
        }); claim.interactable = s.progression.missionProgress >= target;
        Button(side.transform, "관리", 22, 272, 236, 42, DayLoopUI.Kit.WoodLight, () => { Destroy(gameObject); RestaurantManagementUI.OpenSection("집"); });
        Button(side.transform, "섬 둘러보기", 22, 652, 236, 42, DayLoopUI.Kit.Sand, () => Destroy(gameObject), DayLoopUI.Kit.Ink);

        var mission = Card(transform, 300, 20, 311, 148, "미션 · " + missionName);
        Text(mission, "오늘까지 대접한 손님", 20, 55, 270, 22, 14);
        Text(mission, $"{s.progression.missionProgress} / {target}", 20, 78, 270, 28, 26, true);
        Text(mission, $"보상 {reward:N0}G", 20, 116, 270, 20, 13);
        int wages = s.employees.Sum(e => e.wage), days = (7 - s.day % 7) % 7;
        var wage = Card(transform, 629, 20, 311, 148, "다음 주급");
        Text(wage, $"{wages:N0}G", 20, 55, 270, 36, 34, true, DayLoopUI.Kit.GoldInk);
        Text(wage, $"{days}일 뒤 · 7일마다", 20, 98, 270, 22, 14);
        Text(wage, RestaurantProgress.Gold(s) >= wages ? "보유 골드로 충분해요" : "주급을 준비해 주세요", 20, 120, 270, 20, 13);
        var staff = Card(transform, 300, 186, 640, 240, "직원 체력");
        Text(staff, "자고 나면 모두 회복해요", 340, 16, 280, 26, 13);
        int row = 0;
        foreach (var employee in s.employees.Take(3))
        {
            float y = 60 + row++ * 58;
            var portrait = ReferenceTitleView.Picture(staff, employee.role == "Kitchen" ? "OtterChef" : "OtterServer");
            portrait.preserveAspect = true; ReferenceTitleView.Place(portrait.rectTransform, 22, y, 40, 40);
            Text(staff, $"{employee.name}  · {(employee.role == "Kitchen" ? "주방" : "홀")} · {employee.grade}등급", 74, y + 5, 300, 30, 14);
            float ratio = employee.stamina < 0 ? 1 : Mathf.Clamp01(employee.stamina / Mathf.Max(1, employee.hp));
            var bar = DayLoopUI.Bar(staff, ratio, ratio < .8f ? DayLoopUI.Kit.Coin : DayLoopUI.Kit.Kelp);
            ReferenceTitleView.Place((RectTransform)bar.transform.parent, 390, y + 13, 226, 12);
        }
        var tomorrow = Card(transform, 300, 444, 640, 130, "내일 아침에 바뀌는 것");
        Text(tomorrow, "상점 재고                     새로 들어와요\n직원 체력                     모두 회복해요\n연구 · 식당 공사              다음 날 진행돼요", 20, 52, 600, 66, 14);
        var otter = ReferenceTitleView.Picture(transform, "OtterGuest"); ReferenceTitleView.Place(otter.rectTransform, 980, 200, 260, 260);
        notice = Text(transform, "", 300, 630, 640, 50, 16, false, Color.white);
    }

    private static RectTransform Card(Transform parent, float x, float y, float width, float height, string title)
    {
        var panel = DayLoopUI.Card(parent, DayLoopUI.Kit.Sail, title, 3, 5);
        ReferenceTitleView.Place(panel.rectTransform, x, y, width, height);
        if (!string.IsNullOrEmpty(title)) Text(panel.transform, title, 20, 16, width - 40, 30, 22, true);
        return panel.rectTransform;
    }
    private static TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, float size, bool heading = false, Color? color = null)
    {
        var text = DayLoopUI.Text(parent, value, size, color ?? DayLoopUI.Kit.Ink);
        DesignFonts.Apply(text, heading); ReferenceTitleView.Place(text.rectTransform, x, y, w, h); return text;
    }
    private static Button Button(Transform parent, string label, float x, float y, float w, float h, Color color, UnityAction action, Color? foreground = null)
    {
        var button = DayLoopUI.KitButton(parent, label, color, foreground ?? Color.white, 19, action);
        ReferenceTitleView.Place((RectTransform)button.transform, x, y, w, h); DesignArt.ButtonIcon(button, label, 1); return button;
    }
}
