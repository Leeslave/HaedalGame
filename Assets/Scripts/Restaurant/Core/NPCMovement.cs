using UnityEngine;
using UnityEngine.UI;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private Sprite[] upSide;
    [SerializeField] private Sprite[] leftSide;
    [SerializeField] private Sprite[] downSide;
    [SerializeField] private Sprite[] rightSide;

    [Header("Idle Sprite (자리 도착 후 정지 상태)")]
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleRight;

    private Sprite[] curDir;
    private Vector2 lastDir = Vector2.down;

    private SpriteRenderer spriteRenderer;
    private float stepInterval = 0.3f;
    private float stepTimer = 0;
    private int frameIndex = 0; // 0또는 1
    private bool isMoving = false;

    private int baseSortingOrder;

    [Header("Sprite Size")]
    [Tooltip("손님 스프라이트 크기 배율. 실행(Play) 시점에 한 번만 적용되므로, 값을 바꾼 뒤 다시 실행해야 반영됩니다.")]
    [SerializeField] private float spriteScale = 1f;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseSortingOrder = spriteRenderer.sortingOrder;
        spriteRenderer.transform.localScale *= spriteScale;
    }

    // 좌석에 앉았을 때 Seat 인스펙터에 설정된 오프셋을 반영 (테이블 스프라이트와의 겹침 조절용)
    public void ApplySeatSortingOrder(int offset)
    {
        spriteRenderer.sortingOrder = baseSortingOrder + offset;
    }

    // 좌석에서 일어날 때 원래 정렬 순서로 복원
    public void ResetSortingOrder()
    {
        spriteRenderer.sortingOrder = baseSortingOrder;
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
            // 멈추면 마지막으로 걷던 방향 기준 정지 스프라이트로 자동 전환 (서버/셰프처럼 별도의 도착 방향이 없는 NPC용 기본 동작)
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

    public void ArriveDirection(Vector2 dir)
    {
        SetDirection(dir);
        spriteRenderer.sprite = GetIdleSprite(dir);
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