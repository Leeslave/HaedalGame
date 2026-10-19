using System.Collections.Generic;
using UnityEngine;

// Setting 씬 전용: 메뉴 해금 여부를 GUI로 편집하고 "적용" 버튼을 누르면 세이브 데이터에 반영한다.
// 실제 게임의 MenuManager.LoadMenuData()가 이 세이브를 읽어 UnlockedFoods를 구성한다.
public class MenuUnlockSettingController : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _recipeDatabase;
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private MenuUnlockSlotUI _slotPrefab;

    private readonly HashSet<int> _pendingUnlockedIds = new HashSet<int>();
    private readonly HashSet<int> _pendingDailyIds = new HashSet<int>();
    private readonly List<MenuUnlockSlotUI> _spawnedSlots = new List<MenuUnlockSlotUI>();

    private void Awake()
    {
        if (_recipeDatabase != null)
            _recipeDatabase.BuildCache();
    }

    private void Start()
    {
        LoadCurrentState();
        Refresh();
    }

    private void LoadCurrentState()
    {
        _pendingUnlockedIds.Clear();
        _pendingDailyIds.Clear();

        MenuSaveData saveData = FoodSaveLoadManager.LoadMenu();

        if (saveData != null && saveData.unlockedFoodIds != null && saveData.unlockedFoodIds.Count > 0)
        {
            _pendingUnlockedIds.UnionWith(saveData.unlockedFoodIds);

            if (saveData.dailyFoodIds != null)
                _pendingDailyIds.UnionWith(saveData.dailyFoodIds);

            return;
        }

        if (_recipeDatabase == null)
            return;

        foreach (RecipeData recipe in _recipeDatabase.Recipes)
        {
            if (recipe != null && recipe.DefaultUnlock)
                _pendingUnlockedIds.Add(recipe.RecipeId);
        }
    }

    private void Refresh()
    {
        ClearSlots();

        if (_recipeDatabase == null || _slotPrefab == null || _slotRoot == null)
            return;

        foreach (RecipeData recipe in _recipeDatabase.Recipes)
        {
            if (recipe == null)
                continue;

            bool isUnlocked = _pendingUnlockedIds.Contains(recipe.RecipeId);
            bool isDaily = _pendingDailyIds.Contains(recipe.RecipeId);

            MenuUnlockSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(recipe, isUnlocked, isDaily, ToggleUnlock, ToggleDailyMenu);
            _spawnedSlots.Add(slot);
        }
    }

    private void ToggleUnlock(RecipeData recipe)
    {
        if (recipe == null)
            return;

        // 해금 취소 → 오늘의 메뉴에서도 함께 제외 (잠긴 메뉴는 오늘의 메뉴가 될 수 없음)
        if (_pendingUnlockedIds.Contains(recipe.RecipeId))
        {
            _pendingUnlockedIds.Remove(recipe.RecipeId);
            _pendingDailyIds.Remove(recipe.RecipeId);
            Refresh();
            return;
        }

        _pendingUnlockedIds.Add(recipe.RecipeId);
        Refresh();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowConfirmPopup(
                $"'{recipe.RecipeName}'\n오늘의 메뉴에도 등록하시겠습니까?",
                "등록",
                "안 함",
                () => SetDailyMenu(recipe.RecipeId, true));
        }
    }

    private void ToggleDailyMenu(RecipeData recipe)
    {
        if (recipe == null || !_pendingUnlockedIds.Contains(recipe.RecipeId))
            return;

        SetDailyMenu(recipe.RecipeId, !_pendingDailyIds.Contains(recipe.RecipeId));
    }

    private void SetDailyMenu(int recipeId, bool isDaily)
    {
        if (isDaily)
            _pendingDailyIds.Add(recipeId);
        else
            _pendingDailyIds.Remove(recipeId);

        Refresh();
    }

    public void UnlockAll()
    {
        if (_recipeDatabase == null)
            return;

        foreach (RecipeData recipe in _recipeDatabase.Recipes)
        {
            if (recipe != null)
                _pendingUnlockedIds.Add(recipe.RecipeId);
        }

        Refresh();
    }

    public void LockAll()
    {
        _pendingUnlockedIds.Clear();
        _pendingDailyIds.Clear();
        Refresh();
    }

    // "적용" 버튼 OnClick에 연결
    public void ApplyAndSave()
    {
        MenuSaveData saveData = new MenuSaveData();
        saveData.unlockedFoodIds.AddRange(_pendingUnlockedIds);
        saveData.dailyFoodIds.AddRange(_pendingDailyIds);

        FoodSaveLoadManager.SaveMenu(saveData);
        Debug.Log($"메뉴 설정을 저장했습니다. 해금 {saveData.unlockedFoodIds.Count}개 / 오늘의 메뉴 {saveData.dailyFoodIds.Count}개.");
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
    }
}
