using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 집 UI "재고 현황" 패널.
/// 보유 중인 재료를 아이콘 + 수량으로 나열하고, 상단에 "보유 종류 / 최대 종류"를 표시한다.
/// </summary>
public class HouseInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private IngredientInventoryService _inventoryService; // 비우면 싱글톤 사용

    [Header("List")]
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private HouseInventorySlotUI _slotPrefab;

    [Header("Header")]
    [SerializeField] private TMP_Text _capacityText; // "18 / 20"
    [SerializeField] private int _maxKinds = 20;     // 보관 가능한 재료 종류 수

    [Header("표시 옵션")]
    [Tooltip("소금·오일 등 기본 양념은 재고 목록에서 숨긴다")]
    [SerializeField] private bool _hideBasicSeasoning = true;

    [Tooltip("최대 표시 개수 (0이면 제한 없음)")]
    [SerializeField] private int _maxDisplayCount = 0;

    private readonly List<HouseInventorySlotUI> _spawnedSlots = new List<HouseInventorySlotUI>();

    public void Refresh()
    {
        ClearSlots();

        IngredientInventoryService inventory = _inventoryService != null
            ? _inventoryService
            : IngredientInventoryService.Instance;

        if (inventory == null)
            return;

        Dictionary<int, int> counts = inventory.GetIngrdients();
        int kinds = 0;

        foreach (KeyValuePair<int, int> pair in counts)
        {
            if (pair.Value <= 0)
                continue;

            IngredientData ingredient = null;
            if (_database != null)
                _database.TryGetIngredientById(pair.Key, out ingredient);

            if (_hideBasicSeasoning && ingredient != null && ingredient.IsBasicSeasoning)
                continue;

            kinds++;

            if (_maxDisplayCount > 0 && _spawnedSlots.Count >= _maxDisplayCount)
                continue;

            if (_slotPrefab == null || _slotRoot == null)
                continue;

            HouseInventorySlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(ingredient, pair.Value);
            _spawnedSlots.Add(slot);
        }

        if (_capacityText != null)
            _capacityText.text = $"{kinds} / {_maxKinds}";
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
    }
}
