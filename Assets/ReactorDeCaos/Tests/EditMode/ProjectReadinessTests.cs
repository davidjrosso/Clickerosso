#if UNITY_EDITOR
using NUnit.Framework;
using ReactorDeCaos.Editor;
using TMPro;
using UnityEngine;
using UnityEditor;

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
        public void EconomyAssetContainsThePhaseTwoCatalog()
        {
            var settings = Resources.Load<EconomySettings>("Economy");
            Assert.That(settings, Is.Not.Null);
            Assert.DoesNotThrow(() => settings.values.Validate());
            var game = new GameEngine(settings: settings.values);
            Assert.That(settings.values.generators.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(game.Shop.Count, Is.EqualTo(settings.values.upgrades.Length + settings.values.generators.Length));
            Assert.That(game.Cost("quantum"), Is.EqualTo(2500));
            Assert.That(game.Cost("auxiliary"), Is.EqualTo(12000));
            Assert.That(game.Unlocked("auxiliary"), Is.False);
        }

        [Test]
        public void PresentationCatalogHasValidImportedSpritesAndTransparency()
        {
            var assets = Resources.Load<PresentationAssets>("Presentation");
            Assert.That(assets, Is.Not.Null);
            Assert.That(assets.core, Is.Not.Null);
            Assert.That(assets.iconAtlas, Is.Not.Null);
            Assert.That(assets.icons.Length, Is.EqualTo(9));
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var icon in assets.icons)
            {
                Assert.That(ids.Add(icon.id), Is.True);
                Assert.That(icon.uv.width, Is.GreaterThan(0));
                Assert.That(icon.uv.height, Is.GreaterThan(0));
                Assert.That(icon.uv.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(icon.uv.yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(icon.uv.xMax, Is.LessThanOrEqualTo(1));
                Assert.That(icon.uv.yMax, Is.LessThanOrEqualTo(1));
            }
            foreach (string id in new[] { "tap", "combo", "overload", "coil", "plasma", "quantum", "auxiliary", "reward", "settings" })
                Assert.That(ids.Contains(id), Is.True, id);
            foreach (string name in new[] { "ReactorCore", "InterfaceAtlas" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/ReactorDeCaos/Resources/Art/" + name + ".png");
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.isReadable, Is.False);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            }
            var coreImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(assets.core));
            Assert.That(coreImporter.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(coreImporter.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            foreach (string name in new[] { "Tap", "Purchase", "Overload", "OverloadEnd", "OverloadHum", "Milestone", "ComboRise", "GeneratorUnlock", "Ascension", "AmbientMusic" })
                Assert.That(Resources.Load<AudioClip>("Audio/" + name), Is.Not.Null, name);
        }

        [Test]
        public void StoryIllustrationIsImportedAsAClampedNonReadableUiTexture()
        {
            var texture = Resources.Load<Texture2D>("Art/StorySignal");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width / (float)texture.height, Is.EqualTo(1.5f).Within(.01f));
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
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
