using System.Collections;
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
            StartCoroutine(UnActiveEmoji());
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
        }
        else if (waitingCount == 1)
        {
            content.sprite = feelingWarning;
        }
        else if (waitingCount == 2)
        {
            content.sprite = feelingBad;
            
        }
        panel.SetActive(true);
        waitingCount++;
        StartCoroutine(UnActiveEmoji());
        return;
    }

    private IEnumerator UnActiveEmoji()
    {
        yield return new WaitForSeconds(3f);
        CloseBubble();
    }

}