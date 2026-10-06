using UnityEngine;
using UnityEngine.UI;

/// <summary>새 게임을 시작하기 전에도 접근할 수 있는 음량과 화면 설정.</summary>
public class TitleSettingsUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
    private void Start()
    {
        canvas = DayLoopUI.CreateCanvas("Title settings", 60, gameObject.scene);
        var open = DayLoopUI.Button(canvas.transform, "설정", DayLoopUI.Sea, Color.white, 26, () => panel.gameObject.SetActive(true));
        DayLoopUI.Place((RectTransform)open.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(160, 65));
        panel = DayLoopUI.Panel(canvas.transform, DayLoopUI.Cream).rectTransform;
        DayLoopUI.Place(panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(660, 430));
        DayLoopUI.Vertical(panel, 24, 16);
        var title = DayLoopUI.Text(panel, "환경 설정", 32, DayLoopUI.Ink);
        DayLoopUI.Layout(title, preferredHeight: 60);
        var volume = DayLoopUI.Text(panel, $"전체 음량 {AudioListener.volume:P0}", 26, DayLoopUI.Ink);
        DayLoopUI.Layout(volume, preferredHeight: 45);
        var controls = DayLoopUI.Rect("Volume controls", panel);
        DayLoopUI.Horizontal(controls, 0, 15); DayLoopUI.Layout(controls, preferredHeight: 60);
        foreach (float delta in new[] { -.1f, .1f })
        {
            var button = DayLoopUI.Button(controls, delta < 0 ? "줄이기" : "높이기", DayLoopUI.Sea, Color.white, 24, () => {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + delta);
                PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume); PlayerPrefs.Save();
                volume.text = $"전체 음량 {AudioListener.volume:P0}";
            });
            DayLoopUI.Layout(button, flexibleWidth: 1, preferredHeight: 60);
        }
        var screen = DayLoopUI.Button(panel, "전체 화면 / 창 모드 전환", DayLoopUI.Sea, Color.white, 24, () => {
            Screen.fullScreen = !Screen.fullScreen;
            PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0); PlayerPrefs.Save();
        });
        DayLoopUI.Layout(screen, preferredHeight: 60);
        var close = DayLoopUI.Button(panel, "닫기", DayLoopUI.Wood, Color.white, 24, () => panel.gameObject.SetActive(false));
        DayLoopUI.Layout(close, preferredHeight: 60);
        panel.gameObject.SetActive(false);
    }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
