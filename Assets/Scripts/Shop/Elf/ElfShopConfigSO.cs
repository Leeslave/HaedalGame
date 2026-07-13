using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ElfShopItemEntry
{
    [Tooltip("재료 또는 레시피 ID")]
    public int ItemId;

    [Tooltip("Ingredient = 재료, Recipe = 레시피/도안")]
    public ElfShopItemType ItemType;
}

[CreateAssetMenu(fileName = "ElfShopConfig", menuName = "Game Data/Shop/Elf Shop Config")]
public class ElfShopConfigSO : ScriptableObject
{
    [Header("붉은 엘프 - 판매 아이템 목록")]
    [SerializeField] private List<ElfShopItemEntry> _redElfItems = new List<ElfShopItemEntry>();

    [Header("푸른 엘프 - 판매 아이템 목록")]
    [SerializeField] private List<ElfShopItemEntry> _blueElfItems = new List<ElfShopItemEntry>();

    [Header("노란 엘프 - 판매 아이템 목록 (Lv.3 이상 해금)")]
    [SerializeField] private List<ElfShopItemEntry> _yellowElfItems = new List<ElfShopItemEntry>();

    public IReadOnlyList<ElfShopItemEntry> RedElfItems   => _redElfItems;
    public IReadOnlyList<ElfShopItemEntry> BlueElfItems  => _blueElfItems;
    public IReadOnlyList<ElfShopItemEntry> YellowElfItems => _yellowElfItems;

    public IReadOnlyList<ElfShopItemEntry> GetItems(ElfType elfType) => elfType switch
    {
        ElfType.Red    => _redElfItems,
        ElfType.Blue   => _blueElfItems,
        ElfType.Yellow => _yellowElfItems,
        _              => _redElfItems,
    };
}
