using System;
using System.Collections.Generic;
using UnityEngine;

public class MenuRecipeService : MonoBehaviour, ISaveParticipant
{
    [SerializeField] private int _slotCount = 3;
    [SerializeField] private IngredientInventoryService _inventoryService;
    private RecipeData[] _recipes;
    public event Action OnChanged;
    public int SlotCount => _recipes == null ? 0 : _recipes.Length;
    private void Awake() { _recipes = new RecipeData[_slotCount]; GameSession.Register(this); }
    private void OnDestroy() => GameSession.Unregister(this);
    public void CaptureState(GameSaveData data)
    {
        data.menuSlots.Clear();
        foreach (var recipe in _recipes) data.menuSlots.Add(recipe == null ? -1 : recipe.RecipeId);
    }
    public void RestoreState(GameSaveData data)
    {
        _recipes = new RecipeData[RestaurantRules.MenuLimit(data.restaurantLevel)];
        for (int i = 0; i < _recipes.Length && i < data.menuSlots.Count; i++) GameDatabase.TryGetRecipe(data.menuSlots[i], out _recipes[i]);
        OnChanged?.Invoke();
    }
    public RecipeData GetRecipe(int slot) => slot >= 0 && slot < SlotCount ? _recipes[slot] : null;
    public MenuRecipeSetResult SetRecipe(int slot, RecipeData recipe)
    {
        if (slot < 0 || slot >= SlotCount) return MenuRecipeSetResult.InvalidSlot;
        if (recipe == null) return MenuRecipeSetResult.InvalidRecipe;
        if (GetRecipe(slot) == recipe) return MenuRecipeSetResult.SameRecipeAlreadyAssigned;
        return RestaurantProgress.TrySetMenu(slot, recipe.RecipeId, out _) ? MenuRecipeSetResult.Success : MenuRecipeSetResult.NotEnoughIngredients;
    }
    public void ClearRecipe(int slot) => RestaurantProgress.TrySetMenu(slot, -1, out _);
    public void ClearAllRecipes() => RestaurantProgress.TrySetMenus(new List<int>(), out _);
}
public enum MenuRecipeSetResult
{
    Success, InvalidSlot, InvalidRecipe, NoInventoryService, SameRecipeAlreadyAssigned, DuplicateRecipeInOtherSlot, NotEnoughIngredients
}
