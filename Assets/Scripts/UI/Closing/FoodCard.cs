using System.Collections.Generic;
using TMPro;
using UnityEngine;

// <판매 요리> 섹션 컨트롤러. 결산 팝업을 조립할 때 content/itemPrefab을 인스펙터에서 연결해 쓴다.
public class FoodCard : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private FoodCardItem itemPrefab;
    [SerializeField] private TMP_Text totalCountText;
    [SerializeField] private TMP_Text totalRevenueText;

    public void Populate(IEnumerable<SaleEntry> sales, int totalCount, int totalRevenue)
    {
        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        if (content != null && itemPrefab != null && sales != null)
        {
            foreach (SaleEntry entry in sales)
            {
                FoodCardItem item = Instantiate(itemPrefab, content);
                Sprite icon = entry.Recipe != null ? entry.Recipe.Icon : null;
                item.Setup(icon, entry.Count, entry.Revenue);
            }
        }

        if (totalCountText != null) { totalCountText.text = "판매 개수: " + totalCount + "개"; }
        if (totalRevenueText != null) { totalRevenueText.text = "판매 수익: " + totalRevenue + "G"; }
    }
}
