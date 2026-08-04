using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class IslandMenuButtonHoverSign : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Label")]
    [SerializeField] private string _labelText = "이름";
    [SerializeField] private TMP_FontAsset _labelFont;
    [SerializeField] private float _labelFontSize = 28f;
    [SerializeField] private Color _labelColor = new Color(0.35f, 0.2f, 0.1f);

    [Header("Sprites")]
    [SerializeField] private Sprite _chainSprite;
    [SerializeField] private Sprite _signCapLeftSprite;
    [SerializeField] private Sprite _signCapRightSprite;
    [SerializeField] private Sprite _signMiddleSprite;

    [Header("Layout")]
    [SerializeField] private float _signWidth = 130f;
    [SerializeField] private float _signHeight = 64f;
    [SerializeField] private float _capWidth = 30f;
    [SerializeField] private float _chainWidth = 14f;
    [SerializeField] private float _chainHeight = 54f;
    [SerializeField] private float _gapAboveButton = 6f;

    private RectTransform _hoverRoot;

    private void Awake()
    {
        BuildHoverSign();
        SetVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_hoverRoot != null)
            _hoverRoot.gameObject.SetActive(visible);
    }

    private void BuildHoverSign()
    {
        GameObject rootObject = new GameObject("HoverSign", typeof(RectTransform));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, _gapAboveButton);

        float chainOffsetX = (_signWidth * 0.5f) - (_capWidth * 0.5f);
        CreateChain(root, -chainOffsetX);
        CreateChain(root, chainOffsetX);
        CreateSign(root);

        _hoverRoot = root;
    }

    private void CreateChain(RectTransform parent, float anchoredX)
    {
        GameObject chainObject = new GameObject("Chain", typeof(RectTransform), typeof(Image));
        RectTransform rect = chainObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(_chainWidth, _chainHeight);
        rect.anchoredPosition = new Vector2(anchoredX, 0f);

        Image image = chainObject.GetComponent<Image>();
        image.sprite = _chainSprite;
        image.raycastTarget = false;
    }

    private void CreateSign(RectTransform parent)
    {
        GameObject signObject = new GameObject("Sign", typeof(RectTransform));
        RectTransform signRect = signObject.GetComponent<RectTransform>();
        signRect.SetParent(parent, false);
        signRect.anchorMin = new Vector2(0.5f, 0f);
        signRect.anchorMax = new Vector2(0.5f, 0f);
        signRect.pivot = new Vector2(0.5f, 0f);
        signRect.sizeDelta = new Vector2(_signWidth, _signHeight);
        signRect.anchoredPosition = new Vector2(0f, _chainHeight);

        CreateSignCap(signRect, _signCapLeftSprite, true);
        CreateSignCap(signRect, _signCapRightSprite, false);
        CreateSignMiddle(signRect);
        CreateLabel(signRect);
    }

    private void CreateSignCap(RectTransform parent, Sprite sprite, bool isLeft)
    {
        GameObject capObject = new GameObject(isLeft ? "CapLeft" : "CapRight", typeof(RectTransform), typeof(Image));
        RectTransform rect = capObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        float anchorX = isLeft ? 0f : 1f;
        rect.anchorMin = new Vector2(anchorX, 0f);
        rect.anchorMax = new Vector2(anchorX, 1f);
        rect.pivot = new Vector2(anchorX, 0.5f);
        rect.sizeDelta = new Vector2(_capWidth, 0f);
        rect.anchoredPosition = Vector2.zero;

        Image image = capObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private void CreateSignMiddle(RectTransform parent)
    {
        GameObject middleObject = new GameObject("Middle", typeof(RectTransform), typeof(Image));
        RectTransform rect = middleObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(_capWidth, 0f);
        rect.offsetMax = new Vector2(-_capWidth, 0f);

        Image image = middleObject.GetComponent<Image>();
        image.sprite = _signMiddleSprite;
        image.type = Image.Type.Tiled;
        image.raycastTarget = false;
    }

    private void CreateLabel(RectTransform parent)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = _labelText;
        label.color = _labelColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = _labelFontSize;

        if (_labelFont != null)
            label.font = _labelFont;
    }
}
