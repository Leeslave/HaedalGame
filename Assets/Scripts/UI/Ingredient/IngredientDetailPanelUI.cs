using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientDetailPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _sourceText;
    [SerializeField] private TMP_Text _unselectedText;

    [SerializeField] private RecipeBookState _recipeBookState;

    [SerializeField] List<AvailableMenuInfo> _availMenuInfos;

    public void Bind(RecipeDatabaseSO database, Ingredient ingredient)
    {
        if (_root != null)
            _root.SetActive(ingredient != null);

        if (ingredient == null)
        {
            _unselectedText.gameObject.SetActive(true);
            return;
        }

        _unselectedText.gameObject.SetActive(false);

        IngredientData ingredientData = null;
        if (database != null)
            database.TryGetIngredientById(ingredient.IngredientId, out ingredientData);

        if (_iconImage != null)
        {
            _iconImage.sprite = ingredientData != null ? ingredientData.Icon : null;
            _iconImage.enabled = ingredientData != null && ingredientData.Icon != null;
        }

        if (_nameText != null)
            _nameText.text = ingredientData != null ? ingredientData.IngredientName : $"Ingredient {ingredient.IngredientId}";

        if (_countText != null)
            _countText.text = ingredient.Amount.ToString();

        if (_sourceText != null)
            _sourceText.text = string.IsNullOrWhiteSpace(ingredient.Source) ? "-" : ingredient.Source;

        List<RecipeData> availableRecipes = _recipeBookState.GetRecipesWithIngredients(ingredient.IngredientId);


        for (int i = 0; i < availableRecipes.Count; i++)
        {
            RecipeData availableRecipe = availableRecipes[i];
            bool isUnLocked = _recipeBookState.IsUnlocked(availableRecipe.RecipeId);

            _availMenuInfos[i].gameObject.SetActive(true);
            _availMenuInfos[i].Bind(availableRecipe, isUnLocked);
        }


        for (int i = availableRecipes.Count; i < _availMenuInfos.Count; i++)
            _availMenuInfos[i].gameObject.SetActive(false);

    }
}