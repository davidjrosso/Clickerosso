#if UNITY_EDITOR
using NUnit.Framework;
using ReactorDeCaos.Editor;
using TMPro;
using UnityEngine;

namespace ReactorDeCaos.Tests
{
    public sealed class ProjectReadinessTests
    {
        [Test]
        public void PreparedProjectContainsUsableTmpSettingsAndShader()
        {
            Assert.That(ProjectSetup.HasTmpResources(), Is.True,
                "Run Reactor de Caos > Prepare Project and wait for completion.");
            Assert.That(Resources.Load<TMP_Settings>("TMP Settings"), Is.Not.Null);
            Assert.That(Shader.Find("TextMeshPro/Distance Field"), Is.Not.Null);
        }

        [Test]
        public void ReactorFontHasMaterialAtlasAndSpanishGlyphs()
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/ReactorFont");
            Assert.That(font, Is.Not.Null);
            Assert.That(font.material, Is.Not.Null);
            Assert.That(font.material.shader, Is.Not.Null);
            Assert.That(font.atlasTextures.Length, Is.GreaterThan(0));
            Assert.That(font.atlasTextures[0], Is.Not.Null);
            Assert.That(font.atlasTextures[0].width, Is.GreaterThan(0));
            foreach (char character in "REACTOR0123456789ÁÉÍÓÚÑáéíóúñ×")
                Assert.That(font.HasCharacter(character), Is.True, "Missing glyph: " + character);
        }
    }
}
#endif
