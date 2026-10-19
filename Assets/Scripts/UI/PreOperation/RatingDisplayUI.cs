using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 준비 화면 상단의 식당 평점 표시. RestaurantStatsStub은 실제 평점 시스템이
// 구현되기 전까지 쓰는 임시 소스(BlacksmithRestaurantTabUI와 동일 소스 공유).
public class RatingDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _ratingText;
    [SerializeField] private Image[] _starImages;

    private void OnEnable()
    {
        if (RestaurantStatsStub.Instance != null)
            RestaurantStatsStub.Instance.OnChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (RestaurantStatsStub.Instance != null)
            RestaurantStatsStub.Instance.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        float rating = RestaurantStatsStub.Instance != null ? RestaurantStatsStub.Instance.Rating : 0f;

        if (_ratingText != null)
            _ratingText.text = rating.ToString("0.0");

        if (_starImages == null)
            return;

        for (int i = 0; i < _starImages.Length; i++)
        {
            if (_starImages[i] == null)
                continue;

            _starImages[i].fillAmount = Mathf.Clamp01(rating - i);
        }
    }
}
