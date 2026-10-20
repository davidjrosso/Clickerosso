using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class AtlasIcon
    {
        public string id;
        public Rect uv;
    }

    [CreateAssetMenu(menuName = "Reactor de Caos/Presentation Assets")]
    public sealed class PresentationAssets : ScriptableObject
    {
        public Sprite core;
        public Texture2D iconAtlas;
        public AtlasIcon[] icons;
    }

    // One atlas, sliced once per HUD. No pixel copies or sprites allocated per tap.
    public sealed class PresentationSprites : IDisposable
    {
        private readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        public Sprite Core { get; }

        public PresentationSprites(PresentationAssets assets)
        {
            if (assets == null || assets.core == null || assets.iconAtlas == null || assets.icons == null)
                throw new InvalidOperationException("Missing presentation assets. Reimport Resources/Presentation.asset.");
            Core = assets.core;
            var ids = new HashSet<string>();
            foreach (var entry in assets.icons)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || !ids.Add(entry.id)
                    || entry.uv.width <= 0 || entry.uv.height <= 0 || entry.uv.xMin < 0 || entry.uv.yMin < 0
                    || entry.uv.xMax > 1.00001f || entry.uv.yMax > 1.00001f)
                    throw new InvalidOperationException("Invalid presentation atlas entry.");
            }
            try
            {
                foreach (var entry in assets.icons)
                {
                    Texture2D atlas = assets.iconAtlas;
                    var rect = new Rect(entry.uv.x * atlas.width, entry.uv.y * atlas.height,
                        entry.uv.width * atlas.width, entry.uv.height * atlas.height);
                    Sprite sprite = Sprite.Create(atlas, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    sprite.name = entry.id;
                    icons.Add(entry.id, sprite);
                }
            }
            catch { Dispose(); throw; }
        }

        public Sprite Icon(string id)
        {
            if (icons.TryGetValue(id, out var sprite)) return sprite;
            return icons.TryGetValue("coil", out sprite) ? sprite : Core;
        }

        public void Dispose()
        {
            foreach (var sprite in icons.Values)
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            icons.Clear();
        }
    }
}
