using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class ComboSegmentsGraphic : MaskableGraphic
    {
        private int lit;
        public void SetCombo(float fraction)
        {
            int count = Mathf.Clamp(Mathf.FloorToInt(fraction * 12 + .0001f), 0, 12);
            if (count == lit) return;
            lit = count; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * .5f;
            for (int i = 0; i < 12; i++)
            {
                float a = (i + .10f) / 12 * Mathf.PI * 2, b = (i + .80f) / 12 * Mathf.PI * 2;
                Color c = color; c.a *= i < lit ? 1 : .12f;
                Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int n = mesh.currentVertCount;
                mesh.AddVert(r.center + da * (radius - 5), c, Vector2.zero);
                mesh.AddVert(r.center + da * radius, c, Vector2.zero);
                mesh.AddVert(r.center + db * radius, c, Vector2.zero);
                mesh.AddVert(r.center + db * (radius - 5), c, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
