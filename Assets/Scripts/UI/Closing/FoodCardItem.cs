using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <판매 요리> 리스트의 한 행(아이콘 + 수량 + 매출). ClosingMenuListItem 프리팹에 부착된다.
public class FoodCardItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text goldText;

    public void Setup(Sprite iconSprite, int count, int revenue)
    {
        if (icon != null) { icon.sprite = iconSprite; }
        if (countText != null) { countText.text = "x " + count; }
        if (goldText != null) { goldText.text = revenue + "G"; }
    }
}
