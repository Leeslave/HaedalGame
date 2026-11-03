using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 둥근 사각형의 모서리 반지름을 크기에 맞춘다.
/// 9-slice는 칸이 모서리보다 작으면 그 축만 줄여서 모서리가 납작한 타원이 된다(얇은 게이지 끝이 뾰족해 보임).
/// 반지름을 짧은 변의 절반 이하로 줄여 위아래·좌우 모두 같은 원호가 되게 한다.
/// </summary>
[RequireComponent(typeof(Image))]
public class RoundedCorner : UIBehaviour
{
    public float radius = 18f;
    private Image _image;

    protected override void OnEnable() { base.OnEnable(); Apply(); }
    protected override void OnRectTransformDimensionsChange() { Apply(); }

    private void Apply()
    {
        if (_image == null) _image = GetComponent<Image>();
        Sprite sprite = _image != null ? _image.sprite : null;
        if (sprite == null || sprite != DayLoopUI.Rounded || _image.type != Image.Type.Sliced) return;
        Vector2 size = ((RectTransform)transform).rect.size;
        float r = Mathf.Min(radius, Mathf.Min(size.x, size.y) * .5f);
        if (r < .5f) return;
        // 캔버스 기준 모서리 = 텍스처 모서리 * 100 / (PPU * 배수)
        float multiplier = sprite.border.x * 100f / (sprite.pixelsPerUnit * r);
        if (Mathf.Abs(_image.pixelsPerUnitMultiplier - multiplier) > .001f) _image.pixelsPerUnitMultiplier = multiplier;
    }
}
