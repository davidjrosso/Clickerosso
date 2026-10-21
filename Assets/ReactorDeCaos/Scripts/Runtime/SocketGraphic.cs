using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Physical inset mounts, not opaque rectangular cards. All geometry is bounded.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class SocketGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * .5f;
            Vector2 center = r.center;
            Disc(mesh, center + Vector2.down * radius * .08f, radius, new Color(.005f, .012f, .025f, .65f));
            Disc(mesh, center, radius * .94f, new Color(.15f, .22f, .28f));
            Disc(mesh, center, radius * .88f, new Color(.035f, .065f, .095f));
            Disc(mesh, center, radius * .73f, new Color(.02f, .035f, .055f));
            for (int i = 0; i < 4; i++)
            {
                float angle = (45 + i * 90) * Mathf.Deg2Rad;
                Vector2 bolt = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * .80f;
                Disc(mesh, bolt, radius * .035f, new Color(.30f, .38f, .43f), 8);
            }
        }
        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color color, int segments = 64)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
