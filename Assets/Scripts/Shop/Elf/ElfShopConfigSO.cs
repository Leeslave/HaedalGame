using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElfShopConfig", menuName = "Game Data/Elf Shop Config")]
public class ElfShopConfigSO : ScriptableObject
{
    [Header("붉은 엘프 - 일반 식재료 ID 목록")]
    [SerializeField] private List<int> _redElfIngredientIds = new List<int>();

    [Header("푸른 엘프 - 레시피/도안 ID 목록")]
    [SerializeField] private List<int> _blueElfRecipeIds = new List<int>();

    [Header("노란 엘프 - 희귀 식재료 ID 목록 (Lv.3 이상 해금)")]
    [SerializeField] private List<int> _yellowElfIngredientIds = new List<int>();

    public IReadOnlyList<int> RedElfIngredientIds => _redElfIngredientIds;
    public IReadOnlyList<int> BlueElfRecipeIds => _blueElfRecipeIds;
    public IReadOnlyList<int> YellowElfIngredientIds => _yellowElfIngredientIds;
}
