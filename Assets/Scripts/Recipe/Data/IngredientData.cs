using System;
using UnityEngine;

[Serializable]
public class IngredientData
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private string _ingredientName;
    [SerializeField] private int _recipeCode;

    public int IngredientId => _ingredientId;
    public string IngredientName => _ingredientName;
    public int RecipeCode => _recipeCode;

    public IngredientData(int ingredientId, string ingredientName, int recipeCode)
    {
        _ingredientId = ingredientId;
        _ingredientName = ingredientName;
        _recipeCode = recipeCode;
    }   
}
