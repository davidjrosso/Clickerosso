using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Six pairs of embedded light strips. Bounded geometry, no spawned effects or raycasts.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class StationRestorationGraphic : MaskableGraphic
    {
        private int restored = -1, tuning = -1;
        public void Present(int chapters, int choice)
        {
            if (restored == chapters && tuning == choice) return;
            restored = chapters; tuning = choice; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = rectTransform.rect;
            Color light = tuning == 2 ? ReactorHud.Violet : ReactorHud.Cyan;
            for (int i = 0; i < StoryCatalog.Count; i++)
            {
                float y = .22f + i * .115f;
                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? .012f : .976f;
                    Quad(mesh, r, x - .005f, y - .006f, .022f, .065f, new Color(.025f, .045f, .07f, .9f));
                    if (i < restored)
                    {
                        Quad(mesh, r, x - .006f, y - .009f, .024f, .071f, new Color(light.r, light.g, light.b, .10f));
                        Quad(mesh, r, x, y, .012f, .05f, new Color(light.r, light.g, light.b, .75f));
                    }
                    else Quad(mesh, r, x, y, .012f, .05f, new Color(.16f, .21f, .27f, .6f));
                }
            }
        }
        private static void Quad(VertexHelper mesh, Rect r, float x, float y, float w, float h, Color tint)
        {
            int n = mesh.currentVertCount;
            mesh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + y * r.height), tint, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + (y + h) * r.height), tint, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin + (x + w) * r.width, r.yMin + (y + h) * r.height), tint, Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin + (x + w) * r.width, r.yMin + y * r.height), tint, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
