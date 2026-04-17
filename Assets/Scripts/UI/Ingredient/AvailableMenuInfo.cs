using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvailableMenuInfo : MonoBehaviour
{
    [SerializeField] private Image _recipeIconImage;
    [SerializeField] private Sprite _unknownSprite;

    [SerializeField] private TMP_Text _requiredGoldText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private List<AvailableMenuInfo> _availableMenuInfos;
    private RecipeData _recipe;
    public RecipeData Recipe => _recipe;
    

    public void Bind(RecipeData recipe_ , bool isUnLock)
    {

        if (isUnLock)
        {
            _recipeIconImage.sprite = recipe_.Icon;
            _requiredGoldText.text = recipe_.Price.ToString() + "G";
        }
        else
        {
            _recipeIconImage.sprite = _unknownSprite;

            int goldLength = recipe_.Price.ToString().Length;
            StringBuilder gold = new StringBuilder();
            for (int i = 0; i < goldLength; i++)
                gold.Append("?");

            _requiredGoldText.text = gold.ToString();
        }
    }

}
