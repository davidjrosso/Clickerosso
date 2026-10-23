using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Station fixtures become permanent consequences of the campaign. Bounded geometry, no spawned objects.
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
            Color dormant = new Color(.12f, .18f, .23f, .65f);
            Color live = new Color(light.r, light.g, light.b, .48f);
            // Life support vents, mounted on the room's two pillars.
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? .035f : .925f;
                for (int slat = 0; slat < 5; slat++)
                    Quad(mesh, r, x, .46f + slat * .018f, .04f, .006f, restored >= 1 ? live : dormant);
                // Communications antenna above the generator sockets.
                if (restored >= 2)
                {
                    float mast = side == 0 ? .14f : .86f;
                    Quad(mesh, r, mast - .003f, .895f, .006f, .085f, live);
                    for (int bar = 0; bar < 3; bar++)
                        Quad(mesh, r, mast - .012f - bar * .008f, .92f + bar * .018f, .024f + bar * .016f, .004f, live);
                }
            }
            // The red-sector hatch stays open after its seal is broken.
            Quad(mesh, r, .37f, .875f, .26f, .105f, new Color(.015f, .025f, .04f, .85f));
            float gap = restored >= 3 ? .09f : 0;
            Color seal = restored >= 3 ? live : new Color(.65f, .18f, .12f, .55f);
            Quad(mesh, r, .37f, .875f, .006f, .105f, seal);
            Quad(mesh, r, .624f, .875f, .006f, .105f, seal);
            Quad(mesh, r, .37f, .974f, .26f, .006f, seal);
            Quad(mesh, r, .38f, .883f, .115f - gap, .082f, dormant);
            Quad(mesh, r, .505f + gap, .883f, .115f - gap, .082f, dormant);
            // Black-box data rails and the stabilized memory link.
            if (restored >= 4)
                for (int row = 0; row < 3; row++)
                    Quad(mesh, r, .40f, .885f + row * .017f, .20f, .003f, live);
            if (restored >= 5)
                for (int node = 0; node < 5; node++)
                    Quad(mesh, r, .406f + node * .045f, .954f, .008f, .008f, live);
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
