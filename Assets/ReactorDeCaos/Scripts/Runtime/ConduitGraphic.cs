using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class ConduitGraphic : MaskableGraphic
    {
        public Vector2 source, destination = new Vector2(.5f, .5f);
        private float flow;
        private int segments = 1;
        private bool flowing;
        public void Configure(float phase, int level, bool active)
        {
            int nextSegments = Mathf.Clamp(1 + level / 5, 1, 6);
            if (flow == phase && segments == nextSegments && flowing == active) return;
            flow = phase; segments = nextSegments; flowing = active;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            Vector2 a = r.min + Vector2.Scale(source, r.size), b = r.min + Vector2.Scale(destination, r.size);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Color dim = color; dim.a *= .24f;
            Quad(mesh, a, b, normal * 1.5f, dim);
            for (int i = 0; i < segments; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, .1f + i * .12f);
                Quad(mesh, point - normal * 6, point + normal * 6, direction * 1.5f, dim);
            }
            if (!flowing) return;
            for (int i = 0; i < 2; i++)
            {
                float t = Mathf.Repeat(flow + i * .5f, 1);
                Vector2 point = Vector2.Lerp(a, b, t);
                Quad(mesh, point - direction * 7, point + direction * 7, normal * 2.5f, color);
            }
        }
        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 width, Color color)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(a - width, color, Vector2.zero); mesh.AddVert(a + width, color, Vector2.zero);
            mesh.AddVert(b + width, color, Vector2.zero); mesh.AddVert(b - width, color, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
