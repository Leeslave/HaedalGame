using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>시안의 지역 선택·직원 선택·출발·수령에 사용하는 저장 가능한 탐험 상태.</summary>
public static class ReferenceExpedition
{
    public static readonly string[] Names = { "앞바다", "갯벌", "산호 숲" };
    public static int Duration(int destination) => destination == 2 ? 2 : 1;
    public static bool IsAway(GameSaveData s, string employeeId) => s.progression.expedition != null
        && s.progression.expedition.destination >= 0 && s.day < s.progression.expedition.returnDay
        && s.progression.expedition.employeeIds.Contains(employeeId);
    public static bool Returned(GameSaveData s) => s.progression.expedition != null && s.progression.expedition.destination >= 0 && s.day >= s.progression.expedition.returnDay;
    public static Dictionary<int, int> Rewards(GameSaveData s)
    {
        var trip = s.progression.expedition;
        var rewards = new Dictionary<int, int>();
        if (trip == null || trip.destination < 0) return rewards;
        string[] names = trip.destination == 0 ? new[] { "고등어", "광어", "새우" } : trip.destination == 1 ? new[] { "조개", "가리비" } : new[] { "전복", "문어" };
        foreach (string name in names)
        {
            var item = GameDatabase.Recipes.Ingredients.FirstOrDefault(x => x.IngredientName == name);
            if (item != null) rewards[item.IngredientId] = 3 + (trip.departureDay + item.IngredientId + trip.employeeIds.Count) % 3;
        }
        return rewards;
    }
}

public static partial class RestaurantProgress
{
    public static bool TryDepart(int destination, IReadOnlyList<string> employees, out string error) => Change(s => {
        if (destination < 0 || destination > 2) return "탐험할 곳을 선택해 주세요.";
        if (destination == 2 && s.restaurantLevel < 3) return "산호 숲은 식당 Lv.3에 열려요.";
        var trip = s.progression.expedition ?? (s.progression.expedition = new ExpeditionState());
        if (trip.destination >= 0) return "배가 탐험 중이거나 아직 수확을 받지 않았어요.";
        int min = destination == 2 ? 2 : 1, max = destination == 0 ? 1 : 2;
        if (employees == null || employees.Count < min || employees.Count > max || employees.Distinct().Count() != employees.Count) return $"직원 {min}~{max}명을 선택해 주세요.";
        if (employees.Any(id => !s.employees.Any(e => e.instanceId == id))) return "선택한 직원이 없어요.";
        if (!s.progression.freeTime || ReferenceExpedition.Duration(destination) > 1)
        {
            var remaining = s.employees.Where(e => !employees.Contains(e.instanceId)).ToList();
            if (!remaining.Any(e => e.role == "Serving") || !remaining.Any(e => e.role == "Kitchen"))
                return "영업에 필요한 홀 직원과 주방 직원을 한 명씩 남겨 주세요.";
        }
        trip.destination = destination; trip.departureDay = s.day; trip.returnDay = s.day + ReferenceExpedition.Duration(destination);
        trip.employeeIds = employees.ToList(); return null;
    }, out error);

    public static bool TryClaimExpedition(out string error) => Change(s => {
        if (!ReferenceExpedition.Returned(s)) return "아직 탐험이 끝나지 않았어요.";
        EnsureGrades(s);
        foreach (var item in ReferenceExpedition.Rewards(s)) AddIngredient(s, item.Key, 0, item.Value);
        s.progression.expedition = new ExpeditionState(); return null;
    }, out error);

    public static int FurnitureCount(GameSaveData s, int seats) => s.progression.furniture?.FirstOrDefault(x => x.seats == seats)?.amount ?? 0;
    public static bool TryBuyFurniture(int seats, out string error) => Change(s => {
        if (seats != 1 && seats != 2 && seats != 4) return "이 테이블은 아직 구매할 수 없어요.";
        if (!Spend(s, seats * 40)) return $"{seats * 40}G가 필요해요.";
        if (s.progression.furniture == null) s.progression.furniture = new List<FurnitureStock>();
        var stock = s.progression.furniture.FirstOrDefault(x => x.seats == seats);
        if (stock == null) s.progression.furniture.Add(new FurnitureStock { seats = seats, amount = 1 }); else stock.amount++;
        return null;
    }, out error);
    public static bool TryBuyTableSkin(int skin, out string error) => Change(s => {
        if (s.progression.unlockedTableSkins == null) s.progression.unlockedTableSkins = new List<int> { 0, 1 };
        if (skin != 2 || s.progression.unlockedTableSkins.Contains(skin)) return "이미 가지고 있는 색상이에요.";
        if (!Spend(s, 120)) return "120G가 필요해요.";
        s.progression.unlockedTableSkins.Add(skin); return null;
    }, out error);
}
