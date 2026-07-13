using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 결과 화면 체크리스트의 한 줄 (기획서 5.1). 페이즈명 / 점수 / 통과 / 타이밍 결과.
/// </summary>
public class MinigameResultRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _phaseNameText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _passText;      // ✓ / ✗
    [SerializeField] private TMP_Text _timingText;    // 타이밍 페이즈만, 그 외 "-"

    public void Bind(int index, PhaseScoreResult result)
    {
        if (result == null)
            return;

        if (_phaseNameText != null)
            _phaseNameText.text = $"{index + 1}. {result.PhaseName}";

        if (_scoreText != null)
            _scoreText.text = result.Score.ToString("0.0");

        if (_passText != null)
            _passText.text = result.Passed ? "✓" : "✗";

        if (_timingText != null)
            _timingText.text = BuildTimingLabel(result);
    }

    private static string BuildTimingLabel(PhaseScoreResult result)
    {
        if (result.TimingResults == null || result.TimingResults.Count == 0)
            return "-";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < result.TimingResults.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            sb.Append(ToLabel(result.TimingResults[i]));
        }

        return sb.ToString();
    }

    private static string ToLabel(TimingJudgement judgement)
    {
        switch (judgement)
        {
            case TimingJudgement.Undercooked: return "설익음";
            case TimingJudgement.SlightlyUnder: return "약간 익음";
            case TimingJudgement.Perfect: return "익음";
            case TimingJudgement.SlightlyOver: return "살짝 오버쿡";
            case TimingJudgement.Overcooked: return "오버쿡";
            default: return "-";
        }
    }
}
