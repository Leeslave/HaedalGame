using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingWarningUI : MonoBehaviour
{
    private Canvas canvas;
    private Image banner;
    private TMP_Text label;
    private static Thresholds thresholds;
    // 단계별 폐업 기준 수치는 기획에 없어 기본 1.0을 사용한다. 에셋에서 레벨별 조정 가능.
    public static float Threshold(int level)
    {
        if (thresholds == null)
        {
            var config = Resources.Load<TextAsset>("Data/ClosureThresholds");
            thresholds = config != null ? JsonUtility.FromJson<Thresholds>(config.text) : new Thresholds();
        }
        if (thresholds.levels != null && thresholds.levels.Length > 0)
        {
            float value = thresholds.levels[Mathf.Clamp(level - 1, 0, thresholds.levels.Length - 1)];
            if (!float.IsNaN(value) && !float.IsInfinity(value)) return Mathf.Clamp(value, 0, 5);
        }
        return 1;
    }
    [System.Serializable] private class Thresholds { public float[] levels; }
    private void Start()
    {
        canvas = DayLoopUI.CreateCanvas("Rating warning", 145, gameObject.scene);
        banner = DayLoopUI.Panel(canvas.transform, DayLoopUI.Warn);
        banner.raycastTarget = false;
        DayLoopUI.Place(banner.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -110), new Vector2(780, 50));
        label = DayLoopUI.Text(banner.transform, "", 25, Color.white, TextAlignmentOptions.Center);
        label.raycastTarget = false; DayLoopUI.Stretch(label.rectTransform);
    }
    private void Update()
    {
        if (banner == null) return;
        var s = GameSession.Current;
        float rating = !GameSession.IsActive || s.dailyRatingHistory.Count == 0 ? 5 : (float)s.dailyRatingHistory.Average(x => x.value);
        if (GameSession.IsActive && RestaurantRatingManager.Instance != null) rating = RestaurantRatingManager.Instance.RestaurantRating;
        bool warning = GameSession.IsActive && rating <= Threshold(s.restaurantLevel) + .3f;
        banner.gameObject.SetActive(warning);
        if (!warning) return;
        banner.color = Color.Lerp(DayLoopUI.Warn, DayLoopUI.Coral, (Mathf.Sin(Time.unscaledTime * 5) + 1) * .5f);
        label.text = $"폐업 위기 · 평점 {rating:0.0} / 기준 {Threshold(s.restaurantLevel):0.0} · 음식 품질을 높여 주세요";
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
