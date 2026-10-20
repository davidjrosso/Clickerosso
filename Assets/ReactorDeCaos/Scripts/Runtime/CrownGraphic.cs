using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Four distinct procedural silhouettes, with additional plates as the cycle evolves.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class CrownGraphic : MaskableGraphic
    {
        private int stage = -1, skin = -1;
        public void Configure(int newStage, int newSkin)
        {
            if (stage == newStage && skin == newSkin) return;
            stage = newStage; skin = newSkin; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * (.34f + Mathf.Max(0, stage) * .025f);
            int sides = skin == 1 ? 12 : skin == 2 ? 6 : skin == 3 ? 8 : 4 + Mathf.Max(0, stage) * 2;
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                float half = Mathf.PI / sides * (skin == 2 ? .65f : .35f);
                float tip = skin == 1 ? 1.17f : skin == 3 ? 1.12f : 1.04f;
                Vector2 left = r.center + Direction(angle - half) * radius;
                Vector2 right = r.center + Direction(angle + half) * radius;
                Vector2 outer = r.center + Direction(angle) * radius * tip;
                Vector2 inner = r.center + Direction(angle) * radius * (skin == 2 ? .78f : .92f);
                int n = mesh.currentVertCount;
                mesh.AddVert(left, color, Vector2.zero); mesh.AddVert(outer, color, Vector2.zero);
                mesh.AddVert(right, color, Vector2.zero); mesh.AddVert(inner, color, Vector2.zero);
                mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
            }
        }
        private static Vector2 Direction(float angle) { return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); }
    }
}
