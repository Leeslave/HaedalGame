using System;
using System.Collections.Generic;
using UnityEngine;

public class MenuRecipeService : MonoBehaviour
{
    [SerializeField] private int _slotCount = 4;
    [SerializeField] private IngredientInventoryService _inventoryService;

    private RecipeData[] _recipes;

    public event Action OnChanged;

    public int SlotCount => _recipes != null ? _recipes.Length : 0;

    private void Awake()
    {
        _recipes = new RecipeData[_slotCount];
    }

    public RecipeData GetRecipe(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
            return null;

        return _recipes[slotIndex];
    }

    public MenuRecipeSetResult SetRecipe(int slotIndex, RecipeData recipe)
    {
        if (!IsValidIndex(slotIndex))
            return MenuRecipeSetResult.InvalidSlot;

        if (recipe == null)
            return MenuRecipeSetResult.InvalidRecipe;

        if (_inventoryService == null)
            return MenuRecipeSetResult.NoInventoryService;

        RecipeData currentRecipe = _recipes[slotIndex];

        // 같은 슬롯에 같은 레시피
        if (currentRecipe != null && currentRecipe.RecipeId == recipe.RecipeId)
            return MenuRecipeSetResult.SameRecipeAlreadyAssigned;

        // 다른 슬롯에 동일 레시피 존재
        if (ContainsRecipeInOtherSlot(slotIndex, recipe.RecipeId))
            return MenuRecipeSetResult.DuplicateRecipeInOtherSlot;

        // 현재 슬롯에 있던 메뉴 재료를 "가용량 계산상" 먼저 반환한다고 가정하고 체크
        if (!CanAssignRecipe(slotIndex, recipe))
            return MenuRecipeSetResult.NotEnoughIngredients;

        // 실제 반영
        if (currentRecipe != null)
            RefundRecipeIngredients(currentRecipe);

        bool consumeSuccess = ConsumeRecipeIngredients(recipe);

        if (!consumeSuccess)
        {
            // 여기 오면 이론상 거의 없지만, 안전하게 롤백
            if (currentRecipe != null)
                ConsumeRecipeIngredients(currentRecipe);

            return MenuRecipeSetResult.NotEnoughIngredients;
        }

        _recipes[slotIndex] = recipe;

        _inventoryService.NotifyChanged();
        OnChanged?.Invoke();

        return MenuRecipeSetResult.Success;
    }

    public void ClearRecipe(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
            return;

        RecipeData currentRecipe = _recipes[slotIndex];
        if (currentRecipe == null)
            return;

        RefundRecipeIngredients(currentRecipe);
        _recipes[slotIndex] = null;

        _inventoryService.NotifyChanged();
        OnChanged?.Invoke();
    }

    private bool CanAssignRecipe(int slotIndex, RecipeData newRecipe)
    {
        if (_inventoryService == null || newRecipe == null)
            return false;

        Dictionary<int, int> virtualCounts = new Dictionary<int, int>();

        // 현재 인벤토리 복사
        for (int i = 0; i < newRecipe.Requirements.Count; i++)
        {
            int ingredientId = newRecipe.Requirements[i].IngredientId;

            if (!virtualCounts.ContainsKey(ingredientId))
                virtualCounts.Add(ingredientId, _inventoryService.GetCount(ingredientId));
        }

        RecipeData currentRecipe = _recipes[slotIndex];

        // 교체 시 현재 슬롯 메뉴 재료 반환량 반영
        if (currentRecipe != null)
        {
            for (int i = 0; i < currentRecipe.Requirements.Count; i++)
            {
                RecipeIngredientRequirement req = currentRecipe.Requirements[i];

                if (virtualCounts.ContainsKey(req.IngredientId))
                    virtualCounts[req.IngredientId] += req.Amount;
                else
                    virtualCounts.Add(req.IngredientId, _inventoryService.GetCount(req.IngredientId) + req.Amount);
            }
        }

        // 새 레시피 필요량 검사
        for (int i = 0; i < newRecipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement req = newRecipe.Requirements[i];

            int available = virtualCounts.TryGetValue(req.IngredientId, out int count)
                ? count
                : _inventoryService.GetCount(req.IngredientId);

            if (available < req.Amount)
                return false;
        }

        return true;
    }

    private bool ConsumeRecipeIngredients(RecipeData recipe)
    {
        if (_inventoryService == null || recipe == null)
            return false;

        // 먼저 전체 가능 여부 다시 확인
        for (int i = 0; i < recipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement req = recipe.Requirements[i];

            if (!_inventoryService.HasEnough(req.IngredientId, req.Amount))
                return false;
        }

        for (int i = 0; i < recipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement req = recipe.Requirements[i];
            _inventoryService.ConsumeSilently(req.IngredientId, req.Amount);
        }

        return true;
    }

    private void RefundRecipeIngredients(RecipeData recipe)
    {
        if (_inventoryService == null || recipe == null)
            return;

        for (int i = 0; i < recipe.Requirements.Count; i++)
        {
            RecipeIngredientRequirement req = recipe.Requirements[i];
            _inventoryService.AddSilently(req.IngredientId, req.Amount);
        }
    }

    private bool ContainsRecipeInOtherSlot(int targetSlotIndex, int recipeId)
    {
        if (_recipes == null)
            return false;

        for (int i = 0; i < _recipes.Length; i++)
        {
            if (i == targetSlotIndex)
                continue;

            RecipeData recipe = _recipes[i];

            if (recipe == null)
                continue;

            if (recipe.RecipeId == recipeId)
                return true;
        }

        return false;
    }

    private bool IsValidIndex(int slotIndex)
    {
        return _recipes != null && slotIndex >= 0 && slotIndex < _recipes.Length;
    }

    public void ClearAllRecipes()
    {
        if (_recipes == null)
            return;

        bool changed = false;

        for (int i = 0; i < _recipes.Length; i++)
        {
            RecipeData currentRecipe = _recipes[i];
            if (currentRecipe == null)
                continue;

            RefundRecipeIngredients(currentRecipe);
            _recipes[i] = null;
            changed = true;
        }

        if (changed)
        {
            if (_inventoryService != null)
                _inventoryService.NotifyChanged();

            OnChanged?.Invoke();
        }
    }
}

public enum MenuRecipeSetResult
{
    Success = 0,
    InvalidSlot = 1,
    InvalidRecipe = 2,
    NoInventoryService = 3,
    SameRecipeAlreadyAssigned = 4,
    DuplicateRecipeInOtherSlot = 5,
    NotEnoughIngredients = 6
}