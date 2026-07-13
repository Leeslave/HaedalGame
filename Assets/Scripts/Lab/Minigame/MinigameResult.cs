using System.Collections.Generic;

/// <summary>
/// 타이밍 게이지 5단계 판정 (결과 화면 표기용).
/// </summary>
public enum TimingJudgement
{
    None,           // 타이밍 게이지 없는 페이즈
    Undercooked,    // 설익음 (1점)
    SlightlyUnder,  // 약간 익음 (3점)
    Perfect,        // 익음 (5점)
    SlightlyOver,   // 살짝 오버쿡 (3점)
    Overcooked      // 오버쿡 (1점)
}

/// <summary>
/// 페이즈 1개의 채점 결과. 결과 화면 체크리스트(기획서 5.1)의 한 줄에 대응한다.
/// </summary>
public class PhaseScoreResult
{
    public string PhaseName;
    public float Score;                    // 0~5
    public bool Passed;                    // 매니저가 성공 임계값 기준으로 채운다
    public int WrongIngredientAttempts;    // 잘못된 재료 시도 횟수
    public List<TimingJudgement> TimingResults = new List<TimingJudgement>(); // 타이밍 페이즈만

    public PhaseScoreResult() { }

    public PhaseScoreResult(string phaseName, float score, int wrongIngredientAttempts = 0)
    {
        PhaseName = phaseName;
        Score = score;
        WrongIngredientAttempts = wrongIngredientAttempts;
    }
}

/// <summary>
/// 미니게임 전체 결과. 모든 페이즈 점수의 단순 평균으로 성공 여부를 판정한다(기획서 4.11).
/// </summary>
public class MinigameResult
{
    public RecipeData Recipe;
    public List<PhaseScoreResult> PhaseResults = new List<PhaseScoreResult>();
    public float AverageScore;
    public bool Success;
}
