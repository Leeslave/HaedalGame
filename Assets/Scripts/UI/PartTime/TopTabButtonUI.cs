using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TopTabType
{
    Server,
    Kitchen
}

[System.Serializable]
public class TopTabButtonUI : MonoBehaviour
{
    public Image background;
    public TMP_Text label;
}
