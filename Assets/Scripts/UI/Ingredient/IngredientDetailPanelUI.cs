using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientDetailPanelUI : MonoBehaviour
{
    [SerializeField] private Image _ingredientIconImage;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _sourceText;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private TMP_Text _detailText;

    public void Bind(RecipeDatabaseSO database, Ingredient ingredient)
    {
        database.TryGetIngredientById(ingredient.IngredientId, out IngredientData data);
        _nameText.text = data.IngredientName;
        _sourceText.text = ingredient.Source;
        _amountText.text = ingredient.Amount.ToString();
        
        //Todo detail Text

    }

}
