using System;
using TMPro;
using UnityEngine;

namespace ReactorDeCaos
{
    public static class ReactorFont
    {
        // Used by both first-run playback and the Editor's persistent font asset.
        public const string DisplaySource = "Fonts/ChakraPetch-SemiBold";
        public static TMP_FontAsset CreateRuntimeFont() => CreateRuntimeFont("Fonts/DejaVuSans", "ReactorFont Runtime");
        // Display face for figures and titles (SIL OFL, bundled). Body text keeps DejaVu Sans.
        public static TMP_FontAsset CreateRuntimeFont(string resource, string assetName)
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null
                || Shader.Find("TextMeshPro/Distance Field") == null)
                throw new InvalidOperationException(
                    "Faltan TMP Essentials. Detén Play y usa Window > Text Mesh Pro > Import TMP Essential Resources.");

            Font source = Resources.Load<Font>(resource);
            if (source == null)
                throw new InvalidOperationException(
                    "Falta Assets/ReactorDeCaos/Resources/" + resource + ".ttf. Restaura la fuente incluida en el proyecto.");

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null)
                throw new InvalidOperationException(
                    "No se pudo cargar " + resource + ". Activa Include Font Data en su Inspector y pulsa Apply.");
            try
            {
                font.name = assetName;
                string characters = "";
                for (char c = ' '; c <= '~'; c++) characters += c;
                characters += "ÁÉÍÓÚÜÑáéíóúüñ¿¡×·—";
                if (!font.TryAddCharacters(characters, out string missing))
                    throw new InvalidOperationException("No se pudieron generar caracteres de la fuente: " + missing);
                return font;
            }
            catch
            {
                Release(font);
                throw;
            }
        }

        // Only call for fonts created here, never for Resources.Load assets.
        public static void Release(TMP_FontAsset font)
        {
            if (font == null) return;
            // TMP_FontAsset.OnDestroy releases its owned material and atlas textures.
            if (Application.isPlaying) UnityEngine.Object.Destroy(font);
            else UnityEngine.Object.DestroyImmediate(font);
        }
    }
}
