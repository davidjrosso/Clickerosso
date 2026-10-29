using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Embedded cutaway: three rooms, fixed mesh budget, no instantiated particles or images.
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class StationVistaGraphic : MaskableGraphic
    {
        private int rooms; private bool flower, drone, motion;
        public void Present(int count, bool bloom, bool travelling, bool animate)
        {
            if (rooms == count && flower == bloom && drone == travelling && motion == animate) return;
            rooms = count; flower = bloom; drone = travelling; motion = animate; SetVerticesDirty();
        }
        private void Update() { if (drone && motion) SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Quad(mesh, 0, 0, 1, 1, new Color(.012f, .022f, .045f));
            for (int i = 0; i < 32; i++) Quad(mesh, (i * .173f) % 1, .6f + (i * .231f) % .39f, .003f, .006f, new Color(.5f, .75f, .9f, .5f));
            for (int room = 0; room < 3; room++)
            {
                float x = .03f + room * .325f; bool lit = rooms > room;
                Color light = lit ? room == 0 ? new Color(.3f, 1, .65f) : room == 1 ? ReactorHud.Cyan : ReactorHud.Violet : new Color(.12f, .19f, .26f);
                Quad(mesh, x, .10f, .29f, .65f, new Color(.04f, .07f, .11f));
                Quad(mesh, x + .012f, .14f, .266f, .55f, new Color(light.r * .12f, light.g * .12f, light.b * .12f));
                Quad(mesh, x, .73f, .29f, .018f, light);
                Quad(mesh, x, .10f, .29f, .025f, light);
                for (int rib = 0; rib < 4; rib++) Quad(mesh, x + rib * .092f, .12f, .008f, .61f, new Color(.1f, .18f, .23f));
                if (!lit) continue;
                if (room == 0)
                    for (int plant = 0; plant < 4; plant++)
                    {
                        float px = x + .045f + plant * .055f;
                        Quad(mesh, px, .22f, .035f, .055f, new Color(.12f, .23f, .24f));
                        Quad(mesh, px + .015f, .27f, .005f, .16f, light);
                        Quad(mesh, px - .005f, .32f, .024f, .018f, light);
                        Quad(mesh, px + .018f, .365f, .024f, .018f, light);
                        if (flower) { Diamond(mesh, px + .018f, .45f, .027f, ReactorHud.Warm); Diamond(mesh, px + .018f, .45f, .011f, Color.white); }
                    }
                if (room == 1)
                {
                    Quad(mesh, x + .04f, .22f, .21f, .07f, light);
                    Quad(mesh, x + .07f, .29f, .15f, .22f, new Color(.15f, .42f, .45f, .7f));
                    Diamond(mesh, x + .145f, .40f, .025f, ReactorHud.Warm);
                    Quad(mesh, x + .13f, .55f, .03f, .10f, light); Quad(mesh, x + .1f, .585f, .09f, .03f, light);
                }
                if (room == 2)
                {
                    Quad(mesh, x + .135f, .2f, .015f, .25f, light);
                    Diamond(mesh, x + .145f, .5f, .085f, light);
                    Diamond(mesh, x + .145f, .5f, .06f, new Color(.035f, .06f, .13f));
                    for (int i = 0; i < 5; i++) Diamond(mesh, x + .06f + i * .04f, .58f + Mathf.Sin(i) * .06f, .007f, ReactorHud.Warm);
                }
            }
            // Power conduit visibly joins all rooms to the same station.
            Quad(mesh, .04f, .055f, .92f, .008f, rooms > 0 ? ReactorHud.Cyan : new Color(.12f, .2f, .25f));
            if (drone)
            {
                float x = motion ? .12f + Mathf.PingPong(Time.unscaledTime * .025f, .76f) : .5f;
                Diamond(mesh, x, .86f, .025f, ReactorHud.Cyan);
                Quad(mesh, x - .055f, .85f, .03f, .015f, ReactorHud.Warm);
                Quad(mesh, x + .025f, .85f, .03f, .015f, ReactorHud.Warm);
            }
        }
        private void Quad(VertexHelper mesh, float x, float y, float w, float h, Color color)
        {
            int n = mesh.currentVertCount;
            Vertex(mesh, x, y, color); Vertex(mesh, x, y + h, color);
            Vertex(mesh, x + w, y + h, color); Vertex(mesh, x + w, y, color);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
        private void Diamond(VertexHelper mesh, float x, float y, float radius, Color color)
        {
            int n = mesh.currentVertCount;
            Vertex(mesh, x - radius, y, color); Vertex(mesh, x, y + radius * 2, color);
            Vertex(mesh, x + radius, y, color); Vertex(mesh, x, y - radius * 2, color);
            mesh.AddTriangle(n, n + 1, n + 2); mesh.AddTriangle(n, n + 2, n + 3);
        }
        private void Vertex(VertexHelper mesh, float x, float y, Color color)
        {
            Rect r = rectTransform.rect;
            mesh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + y * r.height), color, Vector2.zero);
        }
    }
}
