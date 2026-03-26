using UnityEngine;

public class AspectRatioComtroller : MonoBehaviour
{
    public Camera targetCamera;
    public float targetAspect = 16f / 9f;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        lastWidth = lastHeight = 0;
        ApplyAspect();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ApplyAspect();
        }
    }

    private void ApplyAspect()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = targetCamera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            targetCamera.rect = rect;
        }

        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = targetCamera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            targetCamera.rect = rect;
        }
        
    }
}