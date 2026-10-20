using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReactorDeCaos.Tests
{
    public sealed class RuntimeFontTests
    {
        private TMP_FontAsset font;
        private GameObject canvas;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (canvas != null) Object.Destroy(canvas);
            yield return null;
            ReactorFont.Release(font);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BundledSourceFontCanRenderWithoutPreparedReactorFont()
        {
            // Directly exercises the missing-asset fallback without deleting user assets.
            font = ReactorFont.CreateRuntimeFont();
            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            foreach (char character in "ENERGÍA×30123456789ñ")
                Assert.That(font.HasCharacter(character), Is.True, "Missing glyph: " + character);

            canvas = new GameObject("RuntimeFontTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(canvas.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 36;
            label.rectTransform.sizeDelta = new Vector2(600, 100);
            label.text = "ENERGÍA ×3 +123";
            yield return null;
            label.ForceMeshUpdate();
            Assert.That(label.textInfo.characterCount, Is.EqualTo(label.text.Length));
            Assert.That(label.textInfo.meshInfo[0].vertexCount, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
