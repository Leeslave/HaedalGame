using System;
using UnityEngine;

public enum IslandType
{
    All,
    StartIsland,      // 시작 섬 (기본 재료, 어패류)
    FairyIsland,      // 요정 섬 (버섯, 채소, 과일)
    DolphinIsland,    // 돌고래 섬 (해산물, 해조류)
}


[Serializable]
public class IngredientData
{
    [SerializeField] private int _ingredientId;
    [SerializeField] private string _ingredientName;
    [SerializeField] private int _recipeCode;
    [SerializeField] private int _price;
    [SerializeField] private IslandType _belongIsland;
    [SerializeField] private Sprite _icon;

    public int IngredientId => _ingredientId;
    public string IngredientName => _ingredientName;
    public int RecipeCode => _recipeCode;
    public Sprite Icon => _icon;

    public int Price => _price;

    public IslandType BelongIsland => _belongIsland;

    public IngredientData(int ingredientId, string ingredientName, int recipeCode, Sprite icon)
    {
        _ingredientId = ingredientId;
        _ingredientName = ingredientName;
        _recipeCode = recipeCode;
        _icon = icon;
    }
}