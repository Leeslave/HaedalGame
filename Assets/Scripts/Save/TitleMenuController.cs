using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 버튼은 인스펙터에 연결하면 Awake에서 클릭 처리가 붙는다. (버튼 OnClick에 따로 연결하지 않는다)
/// [Haedal > Dev Tools > 타이틀 씬 자동 설정] 메뉴로 한 번에 연결할 수 있다.
/// 확인·오류 안내는 씬의 PopupManager(PopupCanvas 프리팹) 확인 팝업을 사용한다.
/// </summary>
public class TitleMenuController : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _quitButton;

    [Header("이어하기")]
    [Tooltip("비워도 된다. 세이브 요약(일차·저장 시각·플레이 시간)을 표시할 텍스트")]
    [SerializeField] private TMP_Text _continueSummaryText;

    [Header("인트로")]
    [Tooltip("비우면 인트로 없이 바로 첫 체크포인트를 저장한다 (테스트용)")]
    [SerializeField] private IntroController _intro;

    private bool _busy;

    private void Awake()
    {
        BindButton(_newGameButton, OnClickNewGame, nameof(_newGameButton));
        BindButton(_continueButton, OnClickContinue, nameof(_continueButton));
        BindButton(_quitButton, OnClickQuit, nameof(_quitButton));
    }

    private void OnDestroy()
    {
        if (_newGameButton != null) _newGameButton.onClick.RemoveListener(OnClickNewGame);
        if (_continueButton != null) _continueButton.onClick.RemoveListener(OnClickContinue);
        if (_quitButton != null) _quitButton.onClick.RemoveListener(OnClickQuit);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action, string fieldName)
    {
        if (button == null)
        {
            Debug.LogWarning($"[Title] {fieldName}가 연결되지 않았습니다. [Haedal > Dev Tools > 타이틀 씬 자동 설정]을 실행하세요.", this);
            return;
        }

        button.onClick.AddListener(action);
    }

    private void Start()
    {
        if (GameSession.IsActive)
            GameSession.End();

        GameFlow.SetPhase(GamePhase.Title);
        RefreshContinue();
    }

    private void RefreshContinue()
    {
        bool hasFile = SaveService.HasSaveFile();
        bool hasValidSave = SaveService.TryGetSummary(out SaveSummary summary);

        if (_continueButton != null)
            _continueButton.interactable = hasFile;   // 손상 파일도 눌러서 오류 안내를 받을 수 있게 한다.

        if (_continueSummaryText == null)
            return;

        if (hasValidSave)
        {
            TimeSpan playTime = TimeSpan.FromSeconds(summary.PlayTimeSeconds);
            _continueSummaryText.text =
                $"{summary.Day}일차 · {summary.SavedAtLocal:yyyy.MM.dd HH:mm} 저장 · 플레이 {(int)playTime.TotalHours}시간 {playTime.Minutes}분";
        }
        else
        {
            _continueSummaryText.text = hasFile ? "세이브 파일을 읽을 수 없습니다." : "";
        }
    }

    // ───── 버튼 ─────

    public void OnClickNewGame()
    {
        if (_busy)
            return;

        if (!SaveService.HasSaveFile())
        {
            StartNewGame();
            return;
        }

        ShowPopup(
            "새 게임을 시작하면 시작 연출이 끝난 뒤\n기존 진행이 새 게임으로 교체됩니다.\n시작하시겠습니까?",
            "시작", "취소",
            StartNewGame);
    }

    public void OnClickContinue()
    {
        if (_busy)
            return;

        _busy = true;
        SaveLoadStatus status = GameFlow.TryContinue(out string error);
        if (status == SaveLoadStatus.Ok)
            return; // 섬 씬으로 이동

        _busy = false;

        string message = status switch
        {
            SaveLoadStatus.NotFound => "저장된 진행이 없습니다.",
            SaveLoadStatus.UnsupportedVersion => "이 버전에서 불러올 수 없는 세이브입니다.",
            SaveLoadStatus.Corrupted => "세이브 파일이 손상되어 불러올 수 없습니다.",
            _ => "세이브 파일을 읽는 중 오류가 발생했습니다.",
        };

        Debug.LogError($"[Title] 이어하기 실패 ({status}): {error}");
        ShowPopup(message + "\n세이브 파일은 변경되지 않았습니다.", "다시 시도", "닫기", OnClickContinue, RefreshContinue);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ───── 새로하기 → 인트로 → 첫 체크포인트 ─────

    private void StartNewGame()
    {
        if (_busy)
            return;

        _busy = true;
        GameFlow.BeginNewGame();

        if (_intro != null)
            _intro.Play(SaveIntroCheckpoint);
        else
            SaveIntroCheckpoint();
    }

    private void SaveIntroCheckpoint()
    {
        if (GameFlow.TrySaveIntroCheckpoint(out string error))
        {
            GameFlow.EnterDayStart();
            return;
        }

        // 인트로를 다시 재생하지 않고 저장만 다시 시도한다. 저장 성공 전에는 진행하지 않는다.
        Debug.LogError($"[Title] 첫 체크포인트 저장 실패: {error}");
        ShowPopup(
            "진행 상황을 저장하지 못했습니다.\n시작 연출은 완료되었지만 아직 파일에 저장되지 않았습니다.",
            "다시 시도", "",
            SaveIntroCheckpoint,
            null,
            error);
    }

    private static void ShowPopup(string content, string confirmText, string denyText, Action onConfirm, Action onDeny = null, string sub = "")
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogError("[Title] 씬에 PopupManager(PopupCanvas 프리팹)가 없어 안내 팝업을 띄울 수 없습니다: " + content);
            return;
        }

        PopupManager.Instance.ShowConfirmPopup(content, confirmText, denyText, onConfirm, onDeny, sub);
    }
}
