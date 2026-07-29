using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeLabInventoryController : MonoBehaviour
{
    [SerializeField] private RecipeDatabaseSO _database;
    [SerializeField] private RecipeBookState _recipeBookState;
    [SerializeField] private IngredientInventoryService _inventoryService;

    [Header("List")]
    [SerializeField] private Transform _slotRoot;
    [SerializeField] private RecipeSlotUI _slotPrefab;

    [Header("Category Tabs")]
    [SerializeField] private RecipeCategoryTabUI[] _categoryTabs;

    [Header("Detail")]
    [SerializeField] private RecipeLabDetailPanelUI _detailPanel;

    [Header("Minigame Entry")]
    [SerializeField] private Button _researchButton; // 연구 시작 버튼
    [SerializeField] private MinigameManager _minigameManager;
    // 미니게임 UI 루트(캔버스). 연구 시작 시 켜야 페이즈/결과 UI가 표시된다.
    // 인벤토리 숨김·TopSection 표시·결과창은 이 캔버스 안의 MinigameResultPanelUI가 담당한다.
    [SerializeField] private GameObject _minigameRoot;


    private readonly List<RecipeSlotUI> _spawnedSlots = new List<RecipeSlotUI>();

    private RecipeCategory _currentCategory = RecipeCategory.All;
    private RecipeData _selectedRecipe;

    public RecipeData SelectedRecipe => _selectedRecipe;
    public bool HasSelectedRecipe => _selectedRecipe != null;

    private void OnEnable()
    {
        if (_recipeBookState != null)
            _recipeBookState.OnChanged += RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged += RefreshDetailOnly;

        if (_categoryTabs != null)
        {
            for (int i = 0; i < _categoryTabs.Length; i++)
            {
                if (_categoryTabs[i] != null)
                    _categoryTabs[i].Bind(OnClickCategory);
            }
        }

        if (_researchButton != null)
            _researchButton.onClick.AddListener(OnClickResearch);

        // 미니게임 동안 이 UI 루트가 꺼져 있어도(OnDisable) 목록이 최신이도록 재활성화 시 갱신.
        RefreshList();
    }

    private void OnDisable()
    {
        if (_recipeBookState != null)
            _recipeBookState.OnChanged -= RefreshList;

        if (_inventoryService != null)
            _inventoryService.OnChanged -= RefreshDetailOnly;

        if (_researchButton != null)
            _researchButton.onClick.RemoveListener(OnClickResearch);
    }

    // 종료(개방 처리) 구독은 Awake/OnDestroy에서 유지한다.
    // 미니게임 동안 인벤토리 루트가 꺼지면(OnDisable) OnEnable/OnDisable 구독으로는
    // 그 사이에 끝난 미니게임의 개방 처리를 놓치기 때문.
    private void Awake()
    {
        if (_minigameManager != null)
            _minigameManager.OnMinigameEnded += HandleMinigameEnded;
    }

    private void OnDestroy()
    {
        if (_minigameManager != null)
            _minigameManager.OnMinigameEnded -= HandleMinigameEnded;
    }

    /// <summary>연구 성공 시 레시피 개방 (기획서 1.3). 실패 시엔 재료만 소모된 상태로 종료.</summary>
    private void HandleMinigameEnded(MinigameResult result)
    {
        // 성공했을 때만 레시피를 개방한다.
        if (result != null && result.Success && result.Recipe != null && _recipeBookState != null)
            _recipeBookState.UnlockRecipe(result.Recipe.RecipeId);
        // UnlockRecipe가 OnChanged를 발행 → RefreshList로 목록/상세가 자동 갱신된다.
        // 캔버스/인벤토리 복귀는 결과창 확인 시 MinigameResultPanelUI가 처리한다.
    }

    /// <summary>널 안전하게 GameObject 활성 상태를 바꾼다.</summary>
    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    private void Start()
    {
        RefreshList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            MoveTab(-1);
        else if (Input.GetKeyDown(KeyCode.E))
            MoveTab(1);
    }

    private void MoveTab(int direction)
    {
        if (_categoryTabs == null || _categoryTabs.Length == 0)
            return;

        int count = _categoryTabs.Length;
        int currentIndex = GetCurrentTabIndex();

        for (int step = 1; step <= count; step++)
        {
            int nextIndex = ((currentIndex + direction * step) % count + count) % count;
            RecipeCategoryTabUI tab = _categoryTabs[nextIndex];

            if (tab == null)
                continue;

            OnClickCategory(tab.Category);
            return;
        }
    }

    private int GetCurrentTabIndex()
    {
        if (_categoryTabs == null)
            return 0;

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            if (_categoryTabs[i] != null && _categoryTabs[i].Category == _currentCategory)
                return i;
        }

        return 0;
    }

    private void OnClickCategory(RecipeCategory category)
    {
        _currentCategory = category;
        RefreshList();
    }

    public void RefreshList()
    {
        ClearSlots();

        if (_database == null || _slotPrefab == null || _slotRoot == null)
        {
            RefreshTabSelection();
            RefreshDetailOnly();
            return;
        }

        IReadOnlyList<RecipeData> recipes = _database.Recipes;

        bool stillHasSelectedRecipe = false;

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];

            if (!MatchesCategory(recipe))
                continue;

            if (_selectedRecipe != null && recipe.RecipeId == _selectedRecipe.RecipeId)
                stillHasSelectedRecipe = true;

            bool locked = _recipeBookState != null && !_recipeBookState.IsUnlocked(recipe);

            RecipeSlotUI slot = Instantiate(_slotPrefab, _slotRoot);
            slot.Bind(recipe, OnClickRecipe, IsSelected(recipe), locked);
            _spawnedSlots.Add(slot);
        }

        if (!stillHasSelectedRecipe)
            _selectedRecipe = null;

        RefreshTabSelection();
        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private bool MatchesCategory(RecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (_currentCategory == RecipeCategory.All)
            return true;

        return recipe.Categories != null && recipe.Categories.Contains(_currentCategory);
    }

    private void OnClickRecipe(RecipeData recipe)
    {
        if (recipe == null)
            return;

        if (_selectedRecipe != null && _selectedRecipe.RecipeId == recipe.RecipeId)
            _selectedRecipe = null;
        else
            _selectedRecipe = recipe;

        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    public void ClearSelection()
    {
        _selectedRecipe = null;
        RefreshSelectionVisual();
        RefreshDetailOnly();
    }

    private void RefreshTabSelection()
    {
        if (_categoryTabs == null)
            return;

        for (int i = 0; i < _categoryTabs.Length; i++)
        {
            RecipeCategoryTabUI tab = _categoryTabs[i];
            if (tab == null)
                continue;

            tab.SetSelected(tab.Category == _currentCategory);
        }
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            RecipeSlotUI slot = _spawnedSlots[i];
            if (slot == null)
                continue;

            slot.SetSelected(IsSelected(slot.Recipe));
        }
    }

    private bool IsSelected(RecipeData recipe)
    {
        if (_selectedRecipe == null || recipe == null)
            return false;

        return _selectedRecipe.RecipeId == recipe.RecipeId;
    }

    private void RefreshDetailOnly()
    {
        bool locked = _selectedRecipe != null
            && _recipeBookState != null
            && !_recipeBookState.IsUnlocked(_selectedRecipe);

        if (_detailPanel != null)
            _detailPanel.Bind(_selectedRecipe, locked, _database, _inventoryService);

        // 연구(개방 시도)는 "미개방" 레시피 대상 + 재료 충족 시에만 가능 (기획서 1.3).
        // TODO(M2): UnlockConditionSO 개방 조건 충족 검사 추가.
        // (_inventoryService.OnChanged 구독으로 재료 변동 시 자동 갱신됨)
        if (_researchButton != null)
            _researchButton.interactable = _selectedRecipe != null && locked && HasAllIngredients(_selectedRecipe);
    }

    private bool HasAllIngredients(RecipeData recipe)
    {
        if (recipe == null || _inventoryService == null)
            return false;

        IReadOnlyList<RecipeIngredientRequirement> reqs = recipe.Requirements;

        for (int i = 0; i < reqs.Count; i++)
        {
            if (!_inventoryService.HasEnough(reqs[i].IngredientId, reqs[i].Amount))
                return false;
        }

        return true;
    }

    private void OnClickResearch()
    {
        if (_selectedRecipe == null)
            return;

        // 이미 개방된 레시피는 다시 연구(미니게임)하지 않는다 — 신규 개방 시 1회만 (기획서 1.3).
        if (_recipeBookState != null && _recipeBookState.IsUnlocked(_selectedRecipe))
            return;

        RecipeData recipe = _selectedRecipe;

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowConfirmPopup(
                $"{recipe.RecipeName} 레시피를 개방하시겠습니까?",
                "네",
                "아니오",
                () => StartResearch(recipe),
                subContentText: "미니게임이 실행됩니다.");
        }
        else
        {
            // 팝업 매니저가 없으면 곧바로 시작(디버그/폴백).
            StartResearch(recipe);
        }
    }

    private void StartResearch(RecipeData recipe)
    {
        if (recipe == null || _minigameManager == null)
            return;

        if (!_minigameManager.Initialize(recipe.RecipeId))
            return;

        // 미니게임 UI 루트(캔버스)를 StartMinigame 전에 켠다.
        // → 캔버스 안의 MinigameResultPanelUI가 Awake로 OnMinigameStarted를 구독한 뒤
        //   시작 이벤트를 받아 인벤토리 숨김·TopSection 표시를 처리하게 된다.
        SetActiveSafe(_minigameRoot, true);

        // 재료 부족 등으로 시작에 실패하면 캔버스를 다시 끈다.
        if (!_minigameManager.StartMinigame())
            SetActiveSafe(_minigameRoot, false);
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
