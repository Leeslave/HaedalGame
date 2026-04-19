using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SnapScrollRect : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _viewport;

    [Header("Item Size")]
    [SerializeField] private float _itemHeight = 200f;
    [SerializeField] private float _spacing = 16f;

    [Header("Option")]
    [SerializeField] private float _snapDuration = 0.15f;
    [SerializeField] private bool _useWheel = true;
    [SerializeField] private bool _useKeyboard = true;
    [SerializeField] private int _maxValidItemCount = 4;


    private Coroutine _snapCoroutine;
    private bool _isDragging;

    private float StepSize => _itemHeight + _spacing;

    public int ValidItemCount = 0;

    private void Awake()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();

        if (_content == null)
            _content = _scrollRect.content;

        if (_viewport == null)
            _viewport = _scrollRect.viewport;
    }

    private void Update()
    {
        if (_scrollRect == null || _content == null || ValidItemCount < _maxValidItemCount)
            return;

        if (!_isDragging)
        {
            if (_useWheel)
                HandleWheel();

            if (_useKeyboard)
                HandleKeyboard();
        }
    }

    private void HandleWheel()
    {
        float wheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(wheel) < 0.01f)
            return;

        if (wheel < 0)
            MoveNext();
        else
            MovePrev();
    }

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            MoveNext();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            MovePrev();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        if (_snapCoroutine != null)
        {
            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        SnapToNearest();
    }

    public void MoveNext()
    {
        int index = GetNearestIndex();
        index = Mathf.Clamp(index + 1, 0, GetMaxIndex());
        SnapToIndex(index);
    }

    public void MovePrev()
    {
        int index = GetNearestIndex();
        index = Mathf.Clamp(index - 1, 0, GetMaxIndex());
        SnapToIndex(index);
    }

    public void SnapToNearest()
    {
        SnapToIndex(GetNearestIndex());
    }

    public void SnapToIndex(int index)
    {
        index = Mathf.Clamp(index, 0, GetMaxIndex());

        float targetY = index * StepSize;
        targetY = ClampY(targetY);

        if (_snapCoroutine != null)
            StopCoroutine(_snapCoroutine);

        _snapCoroutine = StartCoroutine(CoSnap(targetY));
    }

    private IEnumerator CoSnap(float targetY)
    {
        _scrollRect.velocity = Vector2.zero;

        Vector2 start = _content.anchoredPosition;
        Vector2 end = new Vector2(start.x, targetY);

        float elapsed = 0f;

        while (elapsed < _snapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _snapDuration);
            t = 1f - Mathf.Pow(1f - t, 3f); // 부드러운 ease-out

            _content.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        _content.anchoredPosition = end;
        _snapCoroutine = null;
    }

    private int GetNearestIndex()
    {
        float y = _content.anchoredPosition.y;
        return Mathf.Clamp(Mathf.RoundToInt(y / StepSize), 0, GetMaxIndex());
    }

    private int GetMaxIndex()
    {
        return Mathf.Max(0, _content.childCount - 1);
    }

    private float ClampY(float y)
    {
        float contentHeight = _content.rect.height;
        float viewportHeight = _viewport.rect.height;

        float maxY = Mathf.Max(0, contentHeight - viewportHeight);
        return Mathf.Clamp(y, 0, maxY);
    }
}