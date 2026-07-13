using System.Collections.Generic;
using UnityEngine;


// 구현한 모든 음식을 저장하는 SO
[CreateAssetMenu(fileName = "FoodDatabase", menuName = "Game Data/Food/Food Database")]
public class FoodDatabase : ScriptableObject
{
    [SerializeField] private FoodData[] allFoods;    // 모든 음식 SO
    private Dictionary<int, FoodData> foodById;      // id로 Food 찾기 위한 딕셔너리
    public IReadOnlyList<FoodData> GetAllFoods()     // 모든 음식 리스트 Getter
    {
        return allFoods;
    }

    public void Initialize()                         // all Foods 가 비어있을 경우 채우는 함수
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

    public FoodData GetFoodById(int id)              // Food 찾기 함수
    {
        Initialize();
        foodById.TryGetValue(id, out var food);
        return food;
    }
}