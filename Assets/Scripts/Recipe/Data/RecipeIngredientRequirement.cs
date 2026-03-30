using System;
using UnityEngine;

[Serializable]
public sealed class RecipeIngredientRequirement
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private int _recipeCode;
    [SerializeField] private string _ingredientName;
    [SerializeField] private int _amount;

    public int IngredientId => _ingredientId;
    public int RecipeCode => _recipeCode;
    public string IngredientName => _ingredientName;
    public int Amount => _amount;

    public RecipeIngredientRequirement(int ingredientId, int recipeCode, string ingredientName, int amount)
    {
        _ingredientId = ingredientId;
        _recipeCode = recipeCode;
        _ingredientName = ingredientName;
        _amount = amount;
    }
}
