using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용] 대장간 강화 조건을 한 번에 충족시키는 디버그 헬퍼.
/// 빈 오브젝트에 붙이고 F2 (또는 컴포넌트 우클릭 메뉴) → 골드/식당레벨/사용횟수/대장간레벨 지급.
/// 출시 전 씬에서 제거.
/// </summary>
public class BlacksmithTestHelper : MonoBehaviour
{
    [Header("지급 대상")]
    [SerializeField] private Currency _goldCurrency;
    [SerializeField] private List<CookwareUpgradeSO> _upgrades = new List<CookwareUpgradeSO>();

    [Header("지급량")]
    [SerializeField] private int _goldAmount = 100000;
    [SerializeField] private int _restaurantLevel = 10;
    [SerializeField] private int _blacksmithLevel = 10;
    [SerializeField] private int _useCountAmount = 100;

    [SerializeField] private KeyCode _fulfillKey = KeyCode.F2;

    private void Update()
    {
        if (Input.GetKeyDown(_fulfillKey))
            FulfillAllConditions();
    }

    /// <summary>골드 지급 + 식당/대장간 레벨 상승 + 모든 도구 사용횟수 지급.</summary>
    [ContextMenu("Fulfill All Conditions")]
    public void FulfillAllConditions()
    {
        // 골드
        if (CurrencyManager.Instance != null && _goldCurrency != null)
        {
            CurrencyManager.Instance.ProcessTransaction(
                new CurrencyTransaction(_goldCurrency, _goldAmount, TransactionSource.TestGet));
        }

        // 식당 레벨
        if (RestaurantLevelManager.Instance != null)
            RestaurantLevelManager.Instance.SetLevel(_restaurantLevel);

        // 대장간 레벨
        if (BlacksmithLevelManager.Instance != null)
            BlacksmithLevelManager.Instance.SetLevel(_blacksmithLevel);

        // 도구 사용횟수
        if (CookwareLevelState.Instance != null)
        {
            for (int i = 0; i < _upgrades.Count; i++)
            {
                if (_upgrades[i] != null)
                    CookwareLevelState.Instance.AddUseCount(_upgrades[i], _useCountAmount);
            }
        }

        Debug.Log($"[BlacksmithTest] 조건 충족 지급 완료: 골드+{_goldAmount}, 식당 Lv.{_restaurantLevel}, " +
                  $"대장간 Lv.{_blacksmithLevel}, 사용횟수+{_useCountAmount} × {_upgrades.Count}개 도구");
    }

    /// <summary>도구 레벨/사용횟수 + 대장간/식당 레벨을 초기 상태로 되돌린다.</summary>
    [ContextMenu("Reset Blacksmith State")]
    public void ResetBlacksmithState()
    {
        if (CookwareLevelState.Instance != null)
            CookwareLevelState.Instance.ResetAll(_upgrades);

        if (BlacksmithLevelManager.Instance != null)
            BlacksmithLevelManager.Instance.SetLevel(1);

        if (RestaurantLevelManager.Instance != null)
            RestaurantLevelManager.Instance.SetLevel(1);

        Debug.Log("[BlacksmithTest] 대장간 상태 초기화 완료 (도구 Lv.1, 사용횟수 0, 대장간/식당 Lv.1)");
    }
}
