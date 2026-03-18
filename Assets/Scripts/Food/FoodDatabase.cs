using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "food Database", menuName = "Game/Food Database")]
public class FoodDatabase : ScriptableObject
{
    [SerializeField] private FoodData[] allFoods;
    private Dictionary<int, FoodData> foodById; // id로 Food 찾기
    public IReadOnlyList<FoodData> GetAllFoods()
    {
        return allFoods;
    }

    public void Initialize()
    {
        if (foodById != null) { return; }

        foodById = new Dictionary<int, FoodData>();
        
        foreach (var food in allFoods)
        {
            if (food == null) { continue; }
            if (foodById.ContainsKey(food.id))
            {
                Debug.LogError($"FoodDatabase 중복 ID 발견: {food.id} / {food.foodName}");
                continue;
            }

            foodById.Add(food.id, food);
        }
    }

    public FoodData GetFoodById(int id)
    {
        Initialize();

        foodById.TryGetValue(id, out var food);
        return food;
    }
}