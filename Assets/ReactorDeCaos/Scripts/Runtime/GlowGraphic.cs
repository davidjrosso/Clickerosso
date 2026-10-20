using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Standard uGUI material: soft radial mesh, no render pipeline or bloom dependency.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class GlowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            Color edge = color; edge.a = 0;
            mesh.AddVert(rect.center, color, Vector2.zero);
            const int segments = 64;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, edge, Vector2.zero);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
