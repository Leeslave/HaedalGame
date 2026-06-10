using System;
using UnityEngine;

public enum IslandType
{
    All,
    StartIsland,      // ���� �� (�⺻ ���, ���з�)
    FairyIsland,      // ���� �� (����, ä��, ����)
    DolphinIsland,    // ������ �� (�ػ깰, ������)
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
    [SerializeField] private bool _defaultUnlock;

    public int IngredientId => _ingredientId;
    public string IngredientName => _ingredientName;
    public int RecipeCode => _recipeCode;
    public Sprite Icon => _icon;

    public int Price => _price;

    public IslandType BelongIsland => _belongIsland;
    public bool DefaultUnlock => _defaultUnlock;

    public IngredientData(int ingredientId, string ingredientName, int recipeCode, Sprite icon)
    {
        _ingredientId = ingredientId;
        _ingredientName = ingredientName;
        _recipeCode = recipeCode;
        _icon = icon;
    }
}