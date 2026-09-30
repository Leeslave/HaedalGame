using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연구 결과 화면 (기획서 5.1 체크리스트).
/// MinigameManager.OnMinigameEnded를 구독해 페이즈별 점수/통과/타이밍 결과와
/// 평균 점수, 성공/실패를 표시한다. 재시도 버튼 없음 — 확인으로 닫기만 한다.
/// </summary>
public class MinigameResultPanelUI : MonoBehaviour
{
    [SerializeField] private MinigameManager _manager;

    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Header")]
    [SerializeField] private TMP_Text _titleText;      // 성공! / 실패
    [SerializeField] private TMP_Text _recipeNameText; // 레시피명 (선택)

    [Header("Checklist")]
    [SerializeField] private Transform _rowRoot;
    [SerializeField] private MinigameResultRowUI _rowPrefab;

    [Header("Footer")]
    [SerializeField] private TMP_Text _averageText;    // "평균 점수: 4.9 / 5.0"
    [SerializeField] private Button _confirmButton;

    [Header("Flow")]
    // 미니게임 동안 숨길 UI (연구실 인벤토리 루트). 결과 확인 후 다시 켜진다.
    [SerializeField] private GameObject _hideDuringMinigame;

    // 미니게임 동안만 보일 UI (페이즈 타이틀 TopSection 등). 결과가 뜨면 꺼진다.
    [SerializeField] private GameObject _hideDuringResult;

    // 미니게임 UI 루트(캔버스). 결과 확인 시 함께 끈다. (연구 시작 시 켜는 건 연구실 인벤토리가 담당)
    [SerializeField] private GameObject _minigameRoot;

    private readonly List<MinigameResultRowUI> _spawnedRows = new List<MinigameResultRowUI>();

    // 구독은 Awake/OnDestroy에서 한다.
    // OnEnable 구독이면 Awake에서 _root(=자기 자신일 수 있음)를 끄는 순간 OnEnable이 안 불려 구독이 누락된다.
    private void Awake()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(Close);

        if (_manager != null)
        {
            _manager.OnMinigameStarted += HandleMinigameStarted;
            _manager.OnMinigameEnded += Show;
        }
        else
        {
            Debug.LogWarning("[MinigameResult] _manager 미할당 - 결과 패널이 뜨지 않습니다", this);
        }

        if (_root != null)
            _root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(Close);

        if (_manager != null)
        {
            _manager.OnMinigameStarted -= HandleMinigameStarted;
            _manager.OnMinigameEnded -= Show;
        }
    }

    /// <summary>미니게임 시작: 인벤토리 UI를 숨기고 페이즈 타이틀 등 미니게임 UI를 켠다.</summary>
    private void HandleMinigameStarted(RecipeData recipe)
    {
        if (_hideDuringMinigame != null)
            _hideDuringMinigame.SetActive(false);

        if (_hideDuringResult != null)
            _hideDuringResult.SetActive(true);
    }

    public void Show(MinigameResult result)
    {
        if (result == null)
            return;

        // 결과가 뜨는 동안 페이즈 타이틀 등 미니게임 UI는 숨긴다.
        if (_hideDuringResult != null)
            _hideDuringResult.SetActive(false);

        ClearRows();

        if (_titleText != null)
            _titleText.text = result.Success ? "성공!" : "실패";

        if (_recipeNameText != null)
            _recipeNameText.text = result.Recipe != null ? result.Recipe.RecipeName : string.Empty;

        if (_rowPrefab != null && _rowRoot != null)
        {
            for (int i = 0; i < result.PhaseResults.Count; i++)
            {
                MinigameResultRowUI row = Instantiate(_rowPrefab, _rowRoot);
                row.Bind(i, result.PhaseResults[i]);
                _spawnedRows.Add(row);
            }
        }

        if (_averageText != null)
            _averageText.text = $"평균 점수: {result.AverageScore:0.0} / 5.0";

        if (_root != null)
            _root.SetActive(true);
    }

    public void Close()
    {
        ClearRows();

        if (_root != null)
            _root.SetActive(false);

        // 결과 확인 후 인벤토리 UI 복귀.
        if (_hideDuringMinigame != null)
            _hideDuringMinigame.SetActive(true);

        // 미니게임 UI 루트(캔버스)를 끈다. (자기 자신이 이 캔버스의 자식이므로 마지막에 처리)
        if (_minigameRoot != null)
            _minigameRoot.SetActive(false);
    }

    private void ClearRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }

        _spawnedRows.Clear();
    }
}
