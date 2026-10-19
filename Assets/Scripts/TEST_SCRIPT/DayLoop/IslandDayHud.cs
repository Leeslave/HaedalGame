using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 섬(하루 시작) 화면 표시. (HUD-01, SAVE-02)
///  - 상단에 현재 일차와 보유 골드를 표시한다.
///  - 결산을 확정한 뒤 결산 화면을 닫기 전에 종료했다면, 이어하기 때 저장된 결산을 한 번 더 보여 준다.
///    (값은 저장된 스냅샷 그대로이며 매출을 다시 지급하지 않는다)
/// </summary>
public class IslandDayHud : MonoBehaviour
{
    private TMP_Text _label;
    private SettlementPanel _settlement;
    private float _nextRefresh;

    private void Start()
    {
        Canvas canvas = DayLoopUI.CreateCanvas("[DayLoop] IslandHUD", 30, gameObject.scene);
        canvas.transform.SetParent(transform, false);

        Image chip = DayLoopUI.Panel(canvas.transform, DayLoopUI.Cream, "DayChip");
        DayLoopUI.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(420, 64));
        DayLoopUI.AddOutline(chip, DayLoopUI.Wood, 2.5f);
        chip.raycastTarget = false;
        _label = DayLoopUI.Text(chip.transform, "", 30, DayLoopUI.WoodDark, TextAlignmentOptions.Center, FontStyles.Bold);
        DayLoopUI.Stretch(_label.rectTransform, 16, 0, 16, 0);
        Refresh();

        if (GameSession.IsActive && GameSession.Current.settlementPendingReview
            && GameSession.Current.lastSettlement != null && GameSession.Current.lastSettlement.day > 0)
        {
            _settlement = gameObject.AddComponent<SettlementPanel>();
            _settlement.Show(GameSession.Current.lastSettlement, $"{GameSession.Current.day}일차 시작", CloseReplay, null);
            _settlement.SetSaveState(SettlementPanel.SaveState.Saved);
        }
    }

    private void CloseReplay()
    {
        DayLoopSave.AcknowledgeSettlement();
        _settlement.Hide();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.5f;
        Refresh();
    }

    private void Refresh()
    {
        if (_label == null) return;
        int day = GameSession.IsActive ? GameSession.Current.day : (InGameTimeManager.Instance != null ? InGameTimeManager.Instance.CurrentDay : 1);
        int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetCurrencyById(GameSession.GoldCurrencyId) : -1;
        _label.text = gold >= 0 ? $"{day}일차  •  보유 {DayLoopUI.Gold(gold)}" : $"{day}일차";
    }
}
