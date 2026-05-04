using UnityEngine;
using UnityEngine.UI;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private Sprite[] upSide;
    [SerializeField] private Sprite[] leftSide;
    [SerializeField] private Sprite[] downSide;
    [SerializeField] private Sprite[] rightSide;

    private Sprite[] curDir;

    private SpriteRenderer spriteRenderer;
    private float stepInterval = 0.3f;
    private float stepTimer = 0;
    private int frameIndex = 0; // 0또는 1
    private bool isMoving = false;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (curDir == null) { return; }
        if (!isMoving) { return; }
        stepTimer += Time.deltaTime;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0;
            frameIndex = (frameIndex + 1) % 2;
            spriteRenderer.sprite = curDir[frameIndex];
        }
    }

    public void SetMoving(bool _isMoving) 
    { 
        isMoving = _isMoving;
        if (_isMoving)
        {
            stepTimer = 0;
            frameIndex = 0;
        }
    }

    public void SetDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            if (dir.y > 0) { curDir = upSide; }
            else { curDir = downSide; }
        }
        else
        {
            if (dir.x > 0) { curDir = rightSide; } 
            else { curDir = leftSide; }
        }
    }

    public void ArriveDirection(Vector2 dir)
    {
        SetDirection(dir);
        spriteRenderer.sprite = curDir[0];
    }
}