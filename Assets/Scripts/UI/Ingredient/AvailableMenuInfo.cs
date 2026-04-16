using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvailableMenuInfo : MonoBehaviour
{
    [SerializeField] private Image _recipeIconImage;

    [SerializeField] private TMP_Text _requiredGoldText;
    [SerializeField] private TMP_Text _gradeText;

    private RecipeData _recipe;
    public RecipeData Recipe => _recipe;

    public void Bind(RecipeData recipe_)
    {

    }

}
