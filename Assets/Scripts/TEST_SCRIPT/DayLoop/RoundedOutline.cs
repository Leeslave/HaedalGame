using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 둥근 사각형·원 전용 외곽선.
/// 기본 Outline은 도형을 대각선 네 방향으로 복사해 겹치므로 모서리 곡선이 울퉁불퉁하게 깨진다.
/// 여기서는 도형의 바깥 꼭짓점만 두께만큼 밀어, 같은 중심에 반지름이 두께만큼 큰 도형 하나를 뒤에 깐다.
/// 코드로 그린 도형이 아닌 그림(아이콘 등)은 기본 Outline 동작을 그대로 쓴다.
/// </summary>
public class RoundedOutline : Outline
{
    private static readonly List<UIVertex> Verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        var image = graphic as Image;
        if (image == null || !DayLoopUI.IsShapeSprite(image.sprite)) { base.ModifyMesh(vh); return; }

        Verts.Clear();
        vh.GetUIVertexStream(Verts);
        int count = Verts.Count;
        if (count == 0) return;

        Vector2 min = Verts[0].position, max = min;
        for (int i = 1; i < count; i++)
        {
            min = Vector2.Min(min, Verts[i].position);
            max = Vector2.Max(max, Verts[i].position);
        }

        float line = Mathf.Max(Mathf.Abs(effectDistance.x), Mathf.Abs(effectDistance.y));
        const float edge = 0.01f;
        for (int i = 0; i < count; i++)
        {
            UIVertex v = Verts[i];
            // 원본은 뒤로 보내 외곽선 위에 그린다.
            Verts.Add(v);
            Vector3 p = v.position;
            if (p.x <= min.x + edge) p.x -= line; else if (p.x >= max.x - edge) p.x += line;
            if (p.y <= min.y + edge) p.y -= line; else if (p.y >= max.y - edge) p.y += line;
            v.position = p;
            Color32 color = effectColor;
            if (useGraphicAlpha) color.a = (byte)(color.a * v.color.a / 255);
            v.color = color;
            Verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(Verts);
    }
}
