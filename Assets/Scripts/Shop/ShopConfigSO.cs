using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Game Data/Shop Config")]
public class ShopConfigSO : ScriptableObject
{
    [Header("일반 상점 - 판매할 레시피/도안 ID 목록")]
    [SerializeField] private List<int> _shopRecipeIds = new List<int>();

    public IReadOnlyList<int> ShopRecipeIds => _shopRecipeIds;
}
