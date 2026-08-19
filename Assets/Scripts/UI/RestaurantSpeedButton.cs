using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 운영 화면 상단 UI의 2배속 토글 버튼. 클릭할 때마다 RestaurantSpeedController의 배속을 켜고 끈다.
public class RestaurantSpeedButton : MonoBehaviour
{
    [SerializeField] private Button speedButton;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f);

    private void Awake()
    {
        if (speedButton != null) { speedButton.onClick.AddListener(HandleClick); }
    }

    private void OnEnable()
    {
        RestaurantSpeedController.OnFastForwardChanged += RefreshVisual;
        RefreshVisual(RestaurantSpeedController.IsFastForward);
    }

    private void OnDisable()
    {
        RestaurantSpeedController.OnFastForwardChanged -= RefreshVisual;
    }

    private void HandleClick()
    {
        RestaurantSpeedController.ToggleFastForward();
    }

    private void RefreshVisual(bool isFastForward)
    {
        if (label != null) { label.text = isFastForward ? "x2" : "x1"; }
        if (icon != null) { icon.color = isFastForward ? activeColor : normalColor; }
    }
}
