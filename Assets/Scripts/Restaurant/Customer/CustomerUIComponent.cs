using UnityEngine;
using UnityEngine.UI;

public class CustomerUIComponent : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image content;

    [SerializeField] private Sprite feelingHappy;
    [SerializeField] private Sprite feelingSoSo;
    [SerializeField] private Sprite feelingWarning;
    [SerializeField] private Sprite feelingBad;
    [SerializeField] private Sprite loading;

    private int waitingCount;
    private bool isWaiting;

    void Start()
    {
        panel.SetActive(false);
        content.sprite = null;
        isWaiting = false;
        waitingCount = 0;
    }

    public void ShowBubble(int state, Sprite foodSprite = null)
    {
        if (state == 0)
        {
            content.sprite = foodSprite;    
        }
        else if (state == 1)
        {
            content.sprite = feelingHappy;
            isWaiting = true;
        }
        else if (state == 2)
        {
            content.sprite = loading;
        }
        panel.SetActive(true);
        return;
    }

    public void CloseBubble()
    {
        content.sprite = null;
        panel.SetActive(false);
    }

    public void ChangeEmotion()
    {
        if (!isWaiting) { return; }
        if (waitingCount == 0)
        {
            content.sprite = feelingSoSo;
            waitingCount++;
            return;
        }
        else if (waitingCount == 1)
        {
            content.sprite = feelingWarning;
            waitingCount++;
            return;
        }
        else if (waitingCount == 2)
        {
            content.sprite = feelingBad;
            waitingCount++;
            return;
        }
        return;
    }
}