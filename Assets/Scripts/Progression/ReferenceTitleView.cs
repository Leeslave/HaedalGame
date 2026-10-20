using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Main.dc.html의 1280×720 좌표를 그대로 사용한 새 타이틀. 씬의 기존 메뉴/로고를 참조하지 않는다.</summary>
public sealed class ReferenceTitleView : MonoBehaviour
{
    private Canvas canvas;
    public Button Continue { get; private set; }
    public TMP_Text Summary { get; private set; }
    public TMP_Text SaveInfo { get; private set; }

    public static ReferenceTitleView Create(Scene scene, UnityAction resume, UnityAction start, UnityAction settings, UnityAction quit)
    {
        var canvas = DayLoopUI.CreateCanvas("Design / 01 Title", 100, scene);
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        var view = canvas.gameObject.AddComponent<ReferenceTitleView>();
        view.canvas = canvas;
        view.Build(resume, start, settings, quit);
        return view;
    }

    private void Build(UnityAction resume, UnityAction start, UnityAction settings, UnityAction quit)
    {
        var background = Picture(transform, "TitleBackground");
        DayLoopUI.Stretch(background.rectTransform);
        var side = DayLoopUI.Panel(transform, new Color(74f / 255, 46f / 255, 27f / 255, .55f), "TitleMenu", false);
        side.rectTransform.anchorMin = Vector2.zero; side.rectTransform.anchorMax = new Vector2(0, 1);
        side.rectTransform.pivot = new Vector2(0, .5f); side.rectTransform.offsetMin = Vector2.zero; side.rectTransform.offsetMax = new Vector2(280, 0);
        var logo = Picture(side.transform, "Logo");
        Place(logo.rectTransform, 22, 22, 236, 108.03f);
        Continue = MenuButton(side.transform, "이어하기", 156.03f, 72, DayLoopUI.Kit.Awning, Color.white, resume, "Play");
        var continueLabel = Continue.GetComponentInChildren<TMP_Text>();
        continueLabel.fontSize = 24;
        Place(continueLabel.rectTransform, 54, 12, 164, 29);
        Summary = DayLoopUI.Text(Continue.transform, "저장된 게임이 없어요", 13, new Color32(255, 233, 223, 255), name: "SaveSummary");
        Place(Summary.rectTransform, 54, 43, 164, 18);
        MenuButton(side.transform, "새로하기", 236.03f, 42, DayLoopUI.Kit.WoodLight, Color.white, start, "New");
        MenuButton(side.transform, "설정", 286.03f, 42, DayLoopUI.Kit.WoodLight, Color.white, settings, "Settings");
        SaveInfo = DayLoopUI.Text(side.transform, "", 12, new Color32(255, 243, 227, 255), name: "LastSave");
        SaveInfo.lineSpacing = 2;
        DayLoopUI.Place(SaveInfo.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24, 80), new Vector2(232, 36));
        var exit = MenuButton(side.transform, "게임 종료", 652, 42, DayLoopUI.Kit.Sand, DayLoopUI.Kit.Line, quit, "Exit");
        // 하단 항목은 화면 높이가 달라져도 바닥에서 같은 거리다.
        DayLoopUI.Place((RectTransform)exit.transform, Vector2.zero, Vector2.zero, new Vector2(22, 26), new Vector2(236, 42));
        var otter = Picture(transform, "OtterServer");
        DayLoopUI.Place(otter.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, -6), new Vector2(400 * 440f / 430f, 400));
    }

    private static Button MenuButton(Transform parent, string label, float y, float height, Color background, Color foreground, UnityAction action, string icon)
    {
        var button = DayLoopUI.KitButton(parent, label, background, foreground, 19, action, label);
        var outline = button.GetComponent<Outline>(); outline.effectDistance = new Vector2(3, -3);
        foreach (var shadow in button.GetComponents<Shadow>())
            if (!(shadow is Outline)) shadow.effectDistance = new Vector2(0, -4);
        Place((RectTransform)button.transform, 22, y, 236, height);
        var text = button.GetComponentInChildren<TMP_Text>();
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Place(text.rectTransform, 48, 0, 168, height);
        var graphic = Picture(button.transform, "Icon" + icon);
        graphic.color = foreground;
        Place(graphic.rectTransform, 16, (height - 22) / 2, 22, 22);
        return button;
    }

    public void RefreshSave()
    {
        Continue.interactable = SaveService.HasSaveFile();
        if (SaveService.TryGetSummary(out var summary))
        {
            Summary.text = $"{summary.Day}일차 아침 · Lv.{summary.RestaurantLevel} · {summary.Gold:N0}G";
            var time = TimeSpan.FromSeconds(summary.PlayTimeSeconds);
            SaveInfo.text = $"마지막 저장 {summary.SavedAtLocal:yyyy.MM.dd HH:mm}\n플레이 {(int)time.TotalHours}시간 {time.Minutes}분";
        }
        else { Summary.text = SaveService.HasSaveFile() ? "저장 파일을 확인해 주세요" : "저장된 게임이 없어요"; SaveInfo.text = ""; }
    }

    public void SetVisible(bool value) => canvas.gameObject.SetActive(value);
    public static Image Picture(Transform parent, string asset)
    {
        var image = DayLoopUI.Panel(parent, Color.white, asset, false);
        image.sprite = DesignArt.Load(asset); image.preserveAspect = false; image.raycastTarget = false;
        return image;
    }
    public static void Place(RectTransform rect, float x, float y, float width, float height)
        => DayLoopUI.Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
}
