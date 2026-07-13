using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그앤드롭형 페이즈 공통 로직 (Mix=그릇, Assembly=접시).
/// 필요 재료를 재료 바에 생성 → 드롭존에 자유 순서로 배치 → 전부 배치 후 완료 버튼으로 종료.
/// 오답 재료 개념이 없으므로 전부 배치하면 baseScore로 완료한다.
/// </summary>
public abstract class DragDropPhaseController : MinigamePhaseController
{
    [Header("UI")]
    [SerializeField] private Transform _ingredientBarRoot;        // 드래그 아이템이 생성될 부모
    [SerializeField] private LabIngredientDragItem _dragItemPrefab;
    [SerializeField] private LabIngredientDropZone _dropZone;     // 그릇/접시

    // 모든 재료 배치 후 눌러야 페이즈가 끝난다. 미할당 시 전부 배치되면 자동 완료(폴백).
    [SerializeField] private Button _completeButton;

    private readonly List<LabIngredientDragItem> _spawnedItems = new List<LabIngredientDragItem>();
    private int _requiredCount;
    private int _placedCount;

    protected int RequiredCount => _requiredCount;
    protected int PlacedCount => _placedCount;
    protected IReadOnlyList<LabIngredientDragItem> SpawnedItems => _spawnedItems;

    protected override void OnBegin()
    {
        ClearItems();
        _placedCount = 0;

        if (_dropZone != null)
            _dropZone.OnItemDropped += HandleItemDropped;

        if (_completeButton != null)
        {
            _completeButton.onClick.AddListener(OnClickComplete);
            _completeButton.interactable = false; // 전부 배치해야 활성화
        }

        SpawnItems();

        // 배치할 재료가 없으면 바로 완료 가능 상태로.
        CheckGoal();
    }

    /// <summary>아이템이 드롭존에 배치된 직후 호출. 서브클래스 확장 지점 (예: Chop의 클릭 활성화).</summary>
    protected virtual void OnItemPlaced(LabIngredientDragItem item) { }

    /// <summary>페이즈 목표 달성 여부. 기본은 전부 배치. 서브클래스가 조건 추가 가능 (예: 전부 썰기).</summary>
    protected virtual bool IsGoalMet()
    {
        return _placedCount >= _requiredCount;
    }

    /// <summary>목표 달성 검사. 서브클래스가 상태 변화 시(예: 재료 하나 다 썰었을 때) 호출한다.</summary>
    protected void CheckGoal()
    {
        if (IsGoalMet())
            HandleAllPlaced();
    }

    private void SpawnItems()
    {
        _requiredCount = 0;

        if (_ingredientBarRoot == null || _dragItemPrefab == null || Phase == null)
            return;

        // 중간 결과물(displayItems)이 정의돼 있으면 그것을, 없으면 원재료를 표시한다.
        if (Phase.DisplayItems != null && Phase.DisplayItems.Count > 0)
            SpawnDisplayItems();
        else
            SpawnIngredientItems();
    }

    /// <summary>중간 결과물 스폰 (예: Assembly의 "연어 스테이크"). 재료 ID가 없으므로 -1.</summary>
    private void SpawnDisplayItems()
    {
        IReadOnlyList<PhaseDisplayItem> items = Phase.DisplayItems;

        for (int i = 0; i < items.Count; i++)
        {
            PhaseDisplayItem displayItem = items[i];

            if (displayItem == null)
                continue;

            LabIngredientDragItem item = Instantiate(_dragItemPrefab, _ingredientBarRoot);
            item.Setup(-1, displayItem.ItemName, displayItem.Sprite);
            _spawnedItems.Add(item);
            _requiredCount++;
        }
    }

    /// <summary>원재료 스폰 (예: Mix의 연어·소금·후추).</summary>
    private void SpawnIngredientItems()
    {
        IReadOnlyList<int> ids = Phase.RequiredIngredientIds;

        for (int i = 0; i < ids.Count; i++)
        {
            int ingredientId = ids[i];

            Sprite icon = null;
            string ingredientName = ingredientId.ToString();

            if (Context != null && Context.Database != null
                && Context.Database.TryGetIngredientById(ingredientId, out IngredientData data))
            {
                icon = data.Icon;
                ingredientName = data.IngredientName;
            }

            LabIngredientDragItem item = Instantiate(_dragItemPrefab, _ingredientBarRoot);
            item.Setup(ingredientId, ingredientName, icon);
            _spawnedItems.Add(item);
            _requiredCount++;
        }
    }

    private void HandleItemDropped(LabIngredientDragItem item)
    {
        if (item == null || item.IsPlaced)
            return;

        item.PlaceInto(_dropZone != null ? _dropZone.ItemContainer : null);
        _placedCount++;

        RaiseIngredientPlaced(item.IngredientId, true);
        OnItemPlaced(item);

        // 전부 배치된 시점 알림 (목표 달성과 별개 — 예: Timing의 요리시작 버튼 활성화).
        if (_placedCount >= _requiredCount)
            OnAllItemsPlaced();

        CheckGoal();
    }

    /// <summary>모든 아이템이 배치된 순간 1회 호출. 목표 달성 여부와 무관한 알림 훅.</summary>
    protected virtual void OnAllItemsPlaced() { }

    /// <summary>목표 달성: 완료 버튼이 있으면 버튼 활성화로 대기, 없으면 즉시 완료.</summary>
    protected virtual void HandleAllPlaced()
    {
        if (_completeButton != null)
            _completeButton.interactable = true;
        else
            CompletePhase();
    }

    private void OnClickComplete()
    {
        if (_placedCount >= _requiredCount)
            CompletePhase();
    }

    protected void CompletePhase()
    {
        Cleanup();
        Complete(BuildResult());
    }

    /// <summary>페이즈 결과 구성. 기본은 baseScore. 서브클래스가 채점 방식 교체 가능 (예: Timing 평균).</summary>
    protected virtual PhaseScoreResult BuildResult()
    {
        float score = Phase != null ? Phase.BaseScore : 0f;

        return new PhaseScoreResult(Phase != null ? Phase.PhaseName : ActionType.ToString(), score);
    }

    public override void Abort()
    {
        Cleanup();
        ClearItems();
        base.Abort();
    }

    private void Cleanup()
    {
        if (_dropZone != null)
            _dropZone.OnItemDropped -= HandleItemDropped;

        if (_completeButton != null)
            _completeButton.onClick.RemoveListener(OnClickComplete);

        OnCleanup();
    }

    /// <summary>완료/중단 시 서브클래스 정리 지점 (이벤트 해제 등).</summary>
    protected virtual void OnCleanup() { }

    private void ClearItems()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }
}
