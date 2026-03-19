using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    public GameObject DimPanel;

    private Stack<UIPopup> _popUpStack = new Stack<UIPopup>();

    private void Awake()
    {
        Instance = this;
    }
}
