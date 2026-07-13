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

    // 소금/오일/후추 등 기본 양념. true면 레시피 재료 UI에 표시하지 않는다 (조리/차감에는 그대로 사용).
    [SerializeField] private bool _isBasicSeasoning;

    public int IngredientId => _ingredientId;
    public string IngredientName => _ingredientName;
    public int RecipeCode => _recipeCode;
    public Sprite Icon => _icon;

    public int Price => _price;

    public IslandType BelongIsland => _belongIsland;
    public bool DefaultUnlock => _defaultUnlock;
    public bool IsBasicSeasoning => _isBasicSeasoning;

    public IngredientData(int ingredientId, string ingredientName, int recipeCode, Sprite icon, bool isBasicSeasoning = false)
    {
        _ingredientId = ingredientId;
        _ingredientName = ingredientName;
        _recipeCode = recipeCode;
        _icon = icon;
        _isBasicSeasoning = isBasicSeasoning;
    }
}