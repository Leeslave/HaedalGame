using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeIngredientRequirementSlotUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Color _enoughColor = Color.white;
    [SerializeField] private Color _notEnoughColor = Color.red;

    public void Bind(IngredientData ingredient, int requiredCount, int ownedCount)
    {
        if (ingredient != null)
        {
            _iconImage.sprite = ingredient.Icon;
            _iconImage.enabled = ingredient.Icon != null;
            _nameText.text = ingredient.IngredientName;
        }
        else
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _nameText.text = "Unknown";
        }

        _countText.text = $"{requiredCount} / {ownedCount}";
        _countText.color = ownedCount >= requiredCount ? _enoughColor : _notEnoughColor;
    }
}