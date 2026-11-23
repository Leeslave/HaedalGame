using UnityEngine;
using UnityEngine.UI;

/// <summary>그림을 가공하지 않고 그리는 테두리 말풍선. 좌표는 컷의 왼쪽 위 기준이다.</summary>
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class ComicSpeechBubble : MaskableGraphic
{
    public Rect body;
    public Vector2 tailTip;
    public bool tailFromLeft;
    private const float Stroke = 3.5f;
    private static readonly Color32 Ink = new Color32(0x2A, 0x1E, 0x14, 0xFF);
    protected ComicSpeechBubble() { useLegacyMeshGeneration = false; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 center = new Vector2(body.center.x, -body.center.y);
        Vector2 radius = body.size * .5f;
        Vector2 tip = new Vector2(tailTip.x, -tailTip.y);
        float angle = tailFromLeft ? -2.6f : Mathf.Atan2((tip.y - center.y) / radius.y, (tip.x - center.x) / radius.x);
        Vector2 a = Edge(center, radius, angle - .15f);
        Vector2 b = Edge(center, radius, angle + .15f);
        Triangle(mesh, a, b, tip, Ink);
        Ellipse(mesh, center, radius, Ink);
        Vector2 inner = radius - Vector2.one * Stroke;
        Ellipse(mesh, center, inner, Color.white);
        // 안쪽 꼬리를 마지막에 겹쳐 타원 테두리가 꼬리의 시작 부분을 가로지르지 않게 한다.
        Triangle(mesh, Edge(center, inner, angle - .105f), Edge(center, inner, angle + .105f),
            Vector2.MoveTowards(tip, center, Stroke * 1.8f), Color.white);
    }

    private static Vector2 Edge(Vector2 center, Vector2 radius, float angle) =>
        center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);

    private static void Ellipse(VertexHelper mesh, Vector2 center, Vector2 radius, Color32 color)
    {
        const int segments = 96;
        int start = mesh.currentVertCount;
        mesh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i <= segments; i++) mesh.AddVert(Edge(center, radius, i * Mathf.PI * 2 / segments), color, Vector2.zero);
        for (int i = 0; i < segments; i++) mesh.AddTriangle(start, start + i + 2, start + i + 1);
    }

    private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero);
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (cross > 0) mesh.AddTriangle(start, start + 2, start + 1);
        else mesh.AddTriangle(start, start + 1, start + 2);
    }
}
