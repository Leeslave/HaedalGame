using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeLabIngredientSlotUI : MonoBehaviour
{
    [Header("Icon")]
    [SerializeField] private Image _iconImage;

    [Header("Count")]
    // "필요 / 보유" 형식 (예: 20 / 15)
    [SerializeField] private TMP_Text _countText;

    [Header("Satisfied")]
    [SerializeField] private GameObject _checkImage;

    public void Bind(IngredientData ingredient, int requiredCount, int ownedCount)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = ingredient != null ? ingredient.Icon : null;
            _iconImage.enabled = ingredient != null && ingredient.Icon != null;
        }

        if (_countText != null)
            _countText.text = $"{requiredCount} / {ownedCount}";

        // 보유 수량이 필요 수량 이상이면 충족 → 체크 이미지 표시.
        bool satisfied = ownedCount >= requiredCount;

        if (_checkImage != null)
            _checkImage.SetActive(satisfied);
    }
}
