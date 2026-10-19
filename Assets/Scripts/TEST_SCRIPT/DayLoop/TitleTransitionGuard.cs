using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TEST 하루 루프] 타이틀 → 인트로 → 첫 화면 전환 보호. (FLOW-01)
/// 인트로가 시작되면 타이틀 버튼 입력을 막는다. (인트로 중 새로하기·이어하기가 다시 눌리지 않게)
/// 인트로가 끝나면 GameFlow가 SceneTransition으로 이동하므로, 같은 프레임에 가림막이 덮여 타이틀이 다시 비치지 않는다.
/// </summary>
public class TitleTransitionGuard : MonoBehaviour
{
    private Button[] _titleButtons;

    private void Start()
    {
        TitleMenuController title = FindFirstObjectByType<TitleMenuController>();
        _titleButtons = title != null ? FindTitleButtons() : new Button[0];
        GameFlow.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        GameFlow.OnPhaseChanged -= HandlePhaseChanged;
    }

    private static Button[] FindTitleButtons()
    {
        // 대화 시스템(인트로) 버튼은 제외하고 타이틀 메뉴 버튼만 고른다.
        System.Collections.Generic.List<Button> buttons = new System.Collections.Generic.List<Button>();
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponentInParent<DialogueView>(true) != null) continue;
            if (button.GetComponentInParent<PopupManager>(true) != null) continue;
            buttons.Add(button);
        }
        return buttons.ToArray();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool locked = phase == GamePhase.Intro;
        foreach (Button button in _titleButtons)
        {
            if (button != null)
                button.interactable = !locked;
        }
    }
}
