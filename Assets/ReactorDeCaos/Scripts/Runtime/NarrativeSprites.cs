using System;
using UnityEngine;

namespace ReactorDeCaos
{
    public sealed class NarrativeSprites : IDisposable
    {
        private readonly Sprite[] sprites = new Sprite[4];
        public Sprite Core => sprites[0];
        public Sprite Portrait(string id) => sprites[id == "nara" ? 1 : id == "mara" ? 2 : id == "memory" ? 3 : 0];
        public NarrativeSprites()
        {
            var atlas = Resources.Load<Texture2D>("Art/NarrativeAtlas");
            if (atlas == null) throw new InvalidOperationException("Missing NarrativeAtlas texture.");
            // Authored UVs include transparent margins and isolate Mara's shoulder silhouette.
            Rect[] cells = { new Rect(0, .5f, .5f, .5f), new Rect(.54f, .5f, .46f, .5f),
                new Rect(0, 0, .535f, .5f), new Rect(.55f, 0, .45f, .5f) };
            for (int i = 0; i < cells.Length; i++)
            {
                Rect uv = cells[i];
                sprites[i] = Sprite.Create(atlas, new Rect(uv.x * atlas.width, uv.y * atlas.height,
                    uv.width * atlas.width, uv.height * atlas.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                sprites[i].name = new[] { "NarrativeCore", "NaraPortrait", "MaraPortrait", "MemoryCrystal" }[i];
            }
        }
        public void Dispose()
        {
            foreach (var sprite in sprites)
            {
                if (sprite == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(sprite);
                else UnityEngine.Object.DestroyImmediate(sprite);
            }
        }
    }
}
