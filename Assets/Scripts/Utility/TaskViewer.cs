using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskViewer : MonoBehaviour
{
    [Header("Serving")]
    [SerializeField] private GameObject servingPanel;
    [SerializeField] private TMP_Text servingLogText;
    [SerializeField] private ScrollRect servingScrollRect;
    

    [Header("Cooking")]
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private TMP_Text cookingLogText;
    [SerializeField] private ScrollRect cookingScrollRect;

    private string servingFilePath;
    private string cookingFilePath;

    void Start()
    {
        servingFilePath = TaskLogger.Instance.servingFilePath;
        cookingFilePath = TaskLogger.Instance.cookingFilePath;

        servingPanel.SetActive(false);
        cookingPanel.SetActive(false);
    }

    public void OpenServingLog()
    {
        if (!File.Exists(servingFilePath)) { return; }
        servingLogText.text = File.ReadAllText(servingFilePath);
        servingPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        servingScrollRect.verticalNormalizedPosition = 0f;
    }

    public void OpenCookingLog()
    {
        if (!File.Exists(cookingFilePath)) { return; }
        cookingLogText.text = File.ReadAllText(cookingFilePath);
        cookingPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        cookingScrollRect.verticalNormalizedPosition = 0f;
    }

    public void CloseServingLog() { servingPanel.SetActive(false); }
    public void CloseCookingLog() { cookingPanel.SetActive(false); }



}