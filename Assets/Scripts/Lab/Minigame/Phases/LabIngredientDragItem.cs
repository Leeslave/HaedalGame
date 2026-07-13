using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 재료 아이콘. 재료 바에서 그릇/접시(LabIngredientDropZone)로 드래그한다.
/// 드롭존에 성공적으로 놓이기 전에는 원위치로 되돌아온다.
/// 배치 후에는 컨트롤러가 EnableClickTarget()을 켜면 클릭(연타) 대상이 될 수 있다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LabIngredientDragItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;

    public int IngredientId { get; private set; }
    public bool IsPlaced { get; private set; }

    /// <summary>클릭 대상 상태에서 클릭될 때 발행 (Chop 연타용).</summary>
    public event Action<LabIngredientDragItem> OnClicked;

    private bool _clickable;
    private Coroutine _punchRoutine;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;

    private Transform _originParent;
    private Vector2 _originAnchoredPos;

    private void Awake()
    {
        _rect = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(int ingredientId, string ingredientName, Sprite icon)
    {
        IngredientId = ingredientId;
        IsPlaced = false;

        if (_iconImage != null)
        {
            // 아이콘이 없어도 Image를 끄지 않는다 (꺼지면 Raycast Target도 죽어서 드래그 불가).
            // 스프라이트 없으면 흰 박스 플레이스홀더로 표시된다.
            _iconImage.sprite = icon;
            _iconImage.enabled = true;
        }

        if (_nameText != null)
            _nameText.text = ingredientName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsPlaced)
            return;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        _originParent = _rect.parent;
        _originAnchoredPos = _rect.anchoredPosition;

        // 드래그 중엔 최상단에 그리고, 레이캐스트를 꺼서 드롭존이 포인터를 받도록 한다.
        _rect.SetParent(_canvas != null ? _canvas.transform : _originParent, true);
        _rect.SetAsLastSibling();
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsPlaced)
            return;

        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        _rect.anchoredPosition += eventData.delta / Mathf.Max(scale, 0.0001f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsPlaced)
            return;

        _canvasGroup.blocksRaycasts = true;

        // 드롭존이 받지 못했으면(=아직 배치 안 됨) 원위치로 복귀.
        if (!IsPlaced)
            ReturnToOrigin();
    }

    private void ReturnToOrigin()
    {
        _rect.SetParent(_originParent, true);
        _rect.anchoredPosition = _originAnchoredPos;
    }

    /// <summary>드롭존에 성공적으로 놓였을 때 컨트롤러가 호출. 드롭한 위치를 유지한 채 드래그를 잠근다.</summary>
    public void PlaceInto(Transform container)
    {
        IsPlaced = true;
        _canvasGroup.blocksRaycasts = false;

        // worldPositionStays=true: 컨테이너 중앙으로 스냅하지 않고 드롭한 자리에 그대로 둔다.
        if (container != null)
            _rect.SetParent(container, true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_clickable)
            OnClicked?.Invoke(this);
    }

    /// <summary>배치된 아이템을 클릭(연타) 대상으로 전환한다 (Chop).</summary>
    public void EnableClickTarget()
    {
        _clickable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public void DisableClickTarget()
    {
        _clickable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    /// <summary>절단 단계 등 진행에 따른 스프라이트 교체.</summary>
    public void SetSprite(Sprite sprite)
    {
        if (_iconImage != null && sprite != null)
            _iconImage.sprite = sprite;
    }

    /// <summary>라벨 갱신 (예: "당근 (3/5)").</summary>
    public void SetLabel(string label)
    {
        if (_nameText != null)
            _nameText.text = label;
    }

    /// <summary>완료 표시 등으로 투명도 조절.</summary>
    public void SetAlpha(float alpha)
    {
        _canvasGroup.alpha = alpha;
    }

    /// <summary>클릭 피드백: 살짝 커졌다 돌아오는 스케일 펀치.</summary>
    public void PlayClickFeedback()
    {
        if (_punchRoutine != null)
            StopCoroutine(_punchRoutine);

        _punchRoutine = StartCoroutine(PunchScale());
    }

    private IEnumerator PunchScale()
    {
        const float duration = 0.1f;
        Vector3 baseScale = Vector3.one;
        Vector3 punchScale = Vector3.one * 1.15f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = t / duration;

            // 절반까지 커졌다가 다시 복귀
            transform.localScale = ratio < 0.5f
                ? Vector3.Lerp(baseScale, punchScale, ratio * 2f)
                : Vector3.Lerp(punchScale, baseScale, (ratio - 0.5f) * 2f);

            yield return null;
        }

        transform.localScale = baseScale;
        _punchRoutine = null;
    }
}
