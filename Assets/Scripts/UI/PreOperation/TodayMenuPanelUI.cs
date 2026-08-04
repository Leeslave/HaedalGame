using System.Collections.Generic;
using UnityEngine;

// 메뉴판: 해금된 레시피 중 오늘 판매할 메뉴를 등록/해제한다.
// MenuManager.DailyFoods가 실제 손님 주문 생성(CustomerOrderComponent)에 쓰이는 정본이다.
public class TodayMenuPanelUI : MonoBehaviour
{
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private TodayMenuCandidateSlotUI _slotPrefab;

    private readonly List<TodayMenuCandidateSlotUI> _spawnedSlots = new List<TodayMenuCandidateSlotUI>();

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        ClearSlots();

        if (MenuManager.Instance == null || _slotPrefab == null || _slotRoot == null)
            return;

        IReadOnlyList<RecipeData> unlockedFoods = MenuManager.Instance.UnlockedFoods;

        for (int i = 0; i < unlockedFoods.Count; i++)
        {
            RecipeData recipe = unlockedFoods[i];

            TodayMenuCandidateSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(recipe, MenuManager.Instance.IsDailyMenu(recipe), ToggleRecipe);
            _spawnedSlots.Add(slot);
        }
    }

    private void ToggleRecipe(RecipeData recipe)
    {
        if (MenuManager.Instance == null || recipe == null)
            return;

        List<int> ids = new List<int>();
        foreach (RecipeData daily in MenuManager.Instance.DailyFoods)
        {
            if (daily != null)
                ids.Add(daily.RecipeId);
        }

        if (!ids.Remove(recipe.RecipeId))
            ids.Add(recipe.RecipeId);

        MenuManager.Instance.SetDailyFoodsFromIds(ids);
        Refresh();
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
