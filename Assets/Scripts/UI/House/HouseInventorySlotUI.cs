using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 집 UI "재고 현황"의 재료 한 칸. 아이콘 / 이름 / 보유 수량.
/// </summary>
public class HouseInventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;

    public void Bind(IngredientData ingredient, int count)
    {
        if (_iconImage != null)
        {
            Sprite icon = ingredient != null ? ingredient.Icon : null;
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        if (_nameText != null)
            _nameText.text = ingredient != null ? ingredient.IngredientName : "?";

        if (_countText != null)
            _countText.text = count.ToString();
    }
}
