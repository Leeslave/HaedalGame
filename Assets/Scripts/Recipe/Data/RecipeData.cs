using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum RecipeCategory
{
    All,
    Seaweed,
    Seafood,
    PremiumSeafood,
    Mushroom,
    Fruit,
    Grain,
    Dairy
}

[Serializable]
public class RecipeData
{
    [SerializeField] private int _recipeId;
    [SerializeField] private string _recipeName;
    [SerializeField] private int _unlockType;
    [SerializeField] private int _classId;
    [SerializeField] private float _cookTime;
    [SerializeField] private string _rawRecipeText;
    [SerializeField] private List<RecipeIngredientRequirement> _requirements = new List<RecipeIngredientRequirement>();
    [SerializeField] private Sprite _icon;

    [SerializeField] private bool _defaultUnlock;
    [SerializeField] private float _price;

    [SerializeField] private List<RecipeCategory>_categories;

    [SerializeField] private string _grade;
    [SerializeField] private string _description;
    public int RecipeId => _recipeId;
    public string RecipeName => _recipeName;
    public int UnlockType => _unlockType;
    public int ClassId => _classId;
    public float CookTime => _cookTime;
    public string RawRecipeText => _rawRecipeText;
    public IReadOnlyList<RecipeIngredientRequirement> Requirements => _requirements;
    public Sprite Icon => _icon;
    public bool DefaultUnlock => _defaultUnlock;

    public float Price => _price;
    public List<RecipeCategory>Categories => _categories;
    public string Grade => _grade;
    public string Description => _description;
    public RecipeData(
        int recipeId,
        string recipeName,
        int unlockType,
        int classId,
        float cookTime,
        string rawRecipeText,
        List<RecipeIngredientRequirement> requirements,
        Sprite icon,
        List<RecipeCategory> categories = null,
        string grade = null,
        string description = null)
    {
        _recipeId = recipeId;
        _recipeName = recipeName;
        _unlockType = unlockType;
        _classId = classId;
        _cookTime = cookTime;
        _rawRecipeText = rawRecipeText;
        _requirements = requirements ?? new List<RecipeIngredientRequirement>();
        _icon = icon;
        _categories = categories ?? new List<RecipeCategory>();
        _grade = grade ?? string.Empty;
        _description = description ?? string.Empty;
    }
}