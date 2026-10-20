using System;
using TMPro;
using UnityEngine;

namespace ReactorDeCaos
{
    public static class ReactorFont
    {
        // Used by both first-run playback and the Editor's persistent font asset.
        public static TMP_FontAsset CreateRuntimeFont()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null
                || Shader.Find("TextMeshPro/Distance Field") == null)
                throw new InvalidOperationException(
                    "Faltan TMP Essentials. Detén Play y usa Window > Text Mesh Pro > Import TMP Essential Resources.");

            Font source = Resources.Load<Font>("Fonts/DejaVuSans");
            if (source == null)
                throw new InvalidOperationException(
                    "Falta Assets/ReactorDeCaos/Resources/Fonts/DejaVuSans.ttf. Restaura la fuente incluida en el proyecto.");

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null)
                throw new InvalidOperationException(
                    "No se pudo cargar DejaVuSans. Activa Include Font Data en su Inspector y pulsa Apply.");
            try
            {
                font.name = "ReactorFont Runtime";
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
