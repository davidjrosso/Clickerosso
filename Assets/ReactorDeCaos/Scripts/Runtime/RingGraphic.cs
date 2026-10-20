using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // MaskableGraphic needs this renderer before OnEnable registers with a RectMask2D.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class RingGraphic : MaskableGraphic
    {
        public float thickness = 3;
        public float fraction = 1;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            float inner = Mathf.Max(0, radius - thickness);
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2 * fraction;
                float b = (i + 1) / (float)segments * Mathf.PI * 2 * fraction;
                Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = mesh.currentVertCount;
                mesh.AddVert(rect.center + da * inner, color, Vector2.zero);
                mesh.AddVert(rect.center + da * radius, color, Vector2.zero);
                mesh.AddVert(rect.center + db * radius, color, Vector2.zero);
                mesh.AddVert(rect.center + db * inner, color, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2);
                mesh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
