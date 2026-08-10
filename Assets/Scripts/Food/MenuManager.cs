using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    [Header("DB")]
    [SerializeField] private RecipeDatabaseSO recipeDatabase;

    [Header("디버그")]
    [SerializeField] private List<RecipeData> unlockedFoods = new List<RecipeData>();
    [SerializeField] private List<RecipeData> dailyFoods = new List<RecipeData>();

    private HashSet<int> unlockedFoodIdSet = new HashSet<int>();

    public IReadOnlyList<RecipeData> UnlockedFoods => unlockedFoods;
    public IReadOnlyList<RecipeData> DailyFoods => dailyFoods;

    private void Awake()
    {
        Instance = this;
        if (recipeDatabase == null)
        {
            Debug.LogError("MenuManager에 RecipeDatabaseSO가 연결되지 않았습니다.");
            return;
        }

        recipeDatabase.BuildCache();
        LoadMenuData();
    }

    // -------------------------
    // 해금 관련
    // -------------------------

    public bool HasUnlocked(RecipeData foodData)
    {
        if (foodData == null)
            return false;

        return unlockedFoodIdSet.Contains(foodData.RecipeId);
    }

    public bool UnlockMenu(RecipeData foodData)
    {
        if (foodData == null)
            return false;

        if (unlockedFoodIdSet.Contains(foodData.RecipeId))
            return false;

        unlockedFoodIdSet.Add(foodData.RecipeId);
        unlockedFoods.Add(foodData);

        return true;
    }

    public bool UnlockMenuById(int foodId)
    {
        if (!recipeDatabase.TryGetRecipe(foodId, out RecipeData foodData))
        {
            Debug.LogWarning($"UnlockMenuById 실패: ID {foodId}에 해당하는 RecipeData가 없습니다.");
            return false;
        }

        return UnlockMenu(foodData);
    }

    // -------------------------
    // 오늘의 메뉴 선정
    // -------------------------

    public void SetDailyFoods(int count)
    {
        dailyFoods.Clear();

        if (unlockedFoods.Count == 0)
        {
            Debug.LogWarning("해금된 메뉴가 없어서 오늘의 메뉴를 선정할 수 없습니다.");
            return;
        }

        List<RecipeData> candidates = new List<RecipeData>(unlockedFoods);
        Shuffle(candidates);

        int finalCount = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < finalCount; i++)
        {
            dailyFoods.Add(candidates[i]);
        }
    }

    public void SetDailyFoodsFromIds(List<int> ids)
    {
        dailyFoods.Clear();

        if (ids == null)
            return;

        foreach (int id in ids)
        {
            if (!recipeDatabase.TryGetRecipe(id, out RecipeData food))
            {
                Debug.LogWarning($"SetDailyFoodsFromIds: ID {id}에 해당하는 RecipeData가 없습니다.");
                continue;
            }

            // 해금된 음식만 오늘의 메뉴로 허용하고 싶으면 이 체크 유지
            if (!unlockedFoodIdSet.Contains(id))
            {
                Debug.LogWarning($"SetDailyFoodsFromIds: ID {id}는 아직 해금되지 않은 메뉴입니다.");
                continue;
            }

            dailyFoods.Add(food);
        }
    }

    public bool IsDailyMenu(RecipeData foodData)
    {
        if (foodData == null)
            return false;

        return dailyFoods.Contains(foodData);
    }

    // -------------------------
    // 저장 / 로드
    // -------------------------

    public void SaveMenuData()
    {
        MenuSaveData saveData = new MenuSaveData();

        foreach (var food in unlockedFoods)
        {
            if (food == null)
                continue;

            saveData.unlockedFoodIds.Add(food.RecipeId);
        }

        foreach (var food in dailyFoods)
        {
            if (food == null)
                continue;

            saveData.dailyFoodIds.Add(food.RecipeId);
        }

        FoodSaveLoadManager.SaveMenu(saveData);
    }

    public void LoadMenuData()
    {
        unlockedFoods.Clear();          // 탐험 후, 새로운 음식이 해금 되었다면 덮어쓰기 해야함
        unlockedFoodIdSet.Clear();      // 언락된 음식들을 빠르게 찾기 위해 만든 리스트도 덮어쓰기 해야함.
        dailyFoods.Clear();             // 매일마다 오늘의 메뉴가 다르니 덮어쓰기 해야함.

        MenuSaveData saveData = FoodSaveLoadManager.LoadMenu();

        if (saveData == null || saveData.unlockedFoodIds == null || saveData.unlockedFoodIds.Count == 0)
        {
            Debug.Log("메뉴 세이브 데이터가 없어서 기본 해금 메뉴로 시작합니다.");
            AddDefaultUnlockedRecipesIfMissing();
            return;
        }

        if (saveData.unlockedFoodIds != null)
        {
            foreach (int id in saveData.unlockedFoodIds)
            {
                if (!recipeDatabase.TryGetRecipe(id, out RecipeData food))
                {
                    Debug.LogWarning($"LoadMenuData: 해금 메뉴 ID {id}를 찾을 수 없습니다.");
                    continue;
                }

                if (unlockedFoodIdSet.Add(id))
                {
                    unlockedFoods.Add(food);
                }
            }
        }

        // Setting 씬에서 저장한 시점 이후 새로 추가된 기본 해금 레시피가 있다면 함께 채워준다.
        AddDefaultUnlockedRecipesIfMissing();

        if (saveData.dailyFoodIds != null)
        {
            foreach (int id in saveData.dailyFoodIds)
            {
                if (!recipeDatabase.TryGetRecipe(id, out RecipeData food))
                {
                    Debug.LogWarning($"LoadMenuData: 오늘의 메뉴 ID {id}를 찾을 수 없습니다.");
                    continue;
                }

                if (!unlockedFoodIdSet.Contains(id))
                {
                    Debug.LogWarning($"LoadMenuData: 오늘의 메뉴 ID {id}가 해금 목록에 없어서 제외합니다.");
                    continue;
                }

                dailyFoods.Add(food);
            }
        }
    }

    private void AddDefaultUnlockedRecipesIfMissing()
    {
        if (recipeDatabase == null)
            return;

        foreach (var food in recipeDatabase.Recipes)
        {
            if (food == null)
                continue;

            if (!food.DefaultUnlock)
                continue;

            UnlockMenu(food);
        }
    }

    // -------------------------
    // 유틸
    // -------------------------

    private void Shuffle(List<RecipeData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}