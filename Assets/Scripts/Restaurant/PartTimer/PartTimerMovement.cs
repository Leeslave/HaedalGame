using UnityEngine;

// 알바(Server/Chef) 전용 이동 스프라이트 처리. CustomerAgent가 쓰는 NPCMovement와는
// 별개 컴포넌트로 분리 - 좌석(Seat) 관련 기능이 필요 없고, PartTimerAgent의 이동 코루틴과
// 섞여서 꼬이지 않도록 이동(위치 갱신)은 그대로 PartTimerAgent에 두고 이 스크립트는
// 방향에 따른 스프라이트 교체만 담당한다.
public class PartTimerMovement : MonoBehaviour
{
    [SerializeField] private Sprite[] upSide;
    [SerializeField] private Sprite[] leftSide;
    [SerializeField] private Sprite[] downSide;
    [SerializeField] private Sprite[] rightSide;

    [Header("Idle Sprite (정지 상태)")]
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleRight;

    private Sprite[] curDir;
    private Vector2 lastDir = Vector2.down;

    private SpriteRenderer spriteRenderer;
    private float stepInterval = 0.3f;
    private float stepTimer = 0;
    private int frameIndex = 0;
    private bool isMoving = false;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (curDir == null) { return; }
        if (!isMoving) { return; }
        stepTimer += Time.deltaTime * RestaurantSpeedController.SpeedMultiplier;
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
        else if (curDir != null)
        {
            spriteRenderer.sprite = GetIdleSprite(lastDir);
        }
    }

    public void SetDirection(Vector2 dir)
    {
        lastDir = dir;
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

    private Sprite GetIdleSprite(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            if (dir.y > 0) { return idleUp; }
            else { return idleDown; }
        }
        else
        {
            if (dir.x > 0) { return idleRight; }
            else { return idleLeft; }
        }
    }
}
