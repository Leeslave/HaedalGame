using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 썰기/다지기 페이즈 컨트롤러.
/// 재료를 도마(드롭존)에 드래그한 뒤 클릭 연타로 자른다.
/// 진행에 따라 절단 단계 스프라이트가 교체되고, 전부 썰어야 완료 버튼이 활성화된다.
/// </summary>
public class ChopPhaseController : DragDropPhaseController
{
    public override CookingActionType ActionType => CookingActionType.Chop;

    private readonly Dictionary<LabIngredientDragItem, int> _clickCounts =
        new Dictionary<LabIngredientDragItem, int>();

    private readonly HashSet<LabIngredientDragItem> _choppedItems =
        new HashSet<LabIngredientDragItem>();

    private readonly List<LabIngredientDragItem> _subscribedItems =
        new List<LabIngredientDragItem>();

    protected override void OnBegin()
    {
        _clickCounts.Clear();
        _choppedItems.Clear();
        _subscribedItems.Clear();

        base.OnBegin();
    }

    /// <summary>도마에 올라온 재료를 클릭 연타 대상으로 전환한다.</summary>
    protected override void OnItemPlaced(LabIngredientDragItem item)
    {
        _clickCounts[item] = 0;

        item.OnClicked += HandleItemClicked;
        _subscribedItems.Add(item);

        item.EnableClickTarget();
        UpdateLabel(item, 0);
    }

    /// <summary>전부 배치 + 전부 썰어야 목표 달성.</summary>
    protected override bool IsGoalMet()
    {
        return PlacedCount >= RequiredCount && _choppedItems.Count >= RequiredCount;
    }

    private void HandleItemClicked(LabIngredientDragItem item)
    {
        if (item == null || _choppedItems.Contains(item))
            return;

        int clickRequired = GetClickCountRequired();

        int count = _clickCounts.TryGetValue(item, out int current) ? current + 1 : 1;
        _clickCounts[item] = count;

        item.PlayClickFeedback();
        UpdateCutSprite(item, count, clickRequired);
        UpdateLabel(item, count);

        if (count >= clickRequired)
            FinishChop(item);
    }

    private void FinishChop(LabIngredientDragItem item)
    {
        _choppedItems.Add(item);
        item.DisableClickTarget();
        item.SetAlpha(0.6f); // 다 썬 재료는 반투명으로 완료 표시

        CheckGoal();
    }

    /// <summary>클릭 진행률에 맞춰 절단 단계 스프라이트를 교체한다 (마지막 단계 = 다 잘린 모습).</summary>
    private void UpdateCutSprite(LabIngredientDragItem item, int count, int clickRequired)
    {
        if (!(Phase is ChopPhaseSO chopPhase))
            return;

        if (!chopPhase.TryGetChopTarget(item.IngredientId, out ChopTarget target))
            return;

        IReadOnlyList<Sprite> stages = target.CutStageSprites;

        if (stages == null || stages.Count == 0)
            return;

        int stageIndex = Mathf.CeilToInt((float)count / clickRequired * stages.Count) - 1;

        if (stageIndex >= 0 && stageIndex < stages.Count)
            item.SetSprite(stages[stageIndex]);
    }

    private void UpdateLabel(LabIngredientDragItem item, int count)
    {
        string ingredientName = item.IngredientId.ToString();

        if (Context != null && Context.Database != null
            && Context.Database.TryGetIngredientById(item.IngredientId, out IngredientData data))
        {
            ingredientName = data.IngredientName;
        }

        item.SetLabel($"{ingredientName} ({count}/{GetClickCountRequired()})");
    }

    private int GetClickCountRequired()
    {
        return Phase is ChopPhaseSO chopPhase ? Mathf.Max(1, chopPhase.ClickCountRequired) : 5;
    }

    protected override void OnCleanup()
    {
        for (int i = 0; i < _subscribedItems.Count; i++)
        {
            if (_subscribedItems[i] != null)
                _subscribedItems[i].OnClicked -= HandleItemClicked;
        }

        _subscribedItems.Clear();
    }
}
