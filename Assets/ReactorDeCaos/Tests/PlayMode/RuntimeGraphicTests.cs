using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ReactorDeCaos.Tests
{
    public sealed class RuntimeGraphicTests
    {
        [UnityTest]
        public IEnumerator CustomGraphicsCreateRenderersBeforeClippingAndPoolReuse()
        {
            var canvas = new GameObject("GraphicsTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                RectTransform mask = ReactorHud.Rect("Mask", canvas.transform, 0, 0, 1, 1);
                mask.gameObject.AddComponent<RectMask2D>();
                // Match runtime construction: initially only a RectTransform, under an active mask.
                var ring = ReactorHud.Rect("Ring", mask, .1f, .1f, .9f, .9f).gameObject.AddComponent<RingGraphic>();
                var glow = ReactorHud.Rect("Glow", mask, .1f, .1f, .9f, .9f).gameObject.AddComponent<GlowGraphic>();
                var socket = ReactorHud.Rect("Socket", mask, .1f, .1f, .9f, .9f).gameObject.AddComponent<SocketGraphic>();
                foreach (var graphic in new MaskableGraphic[] { ring, glow, socket })
                {
                    Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
                    // The reported exception occurred during this immediate deactivation.
                    graphic.gameObject.SetActive(false);
                }
                Canvas.ForceUpdateCanvases();
                yield return null;
                for (int reuse = 0; reuse < 3; reuse++)
                {
                    socket.gameObject.SetActive(true);
                    ring.gameObject.SetActive(true);
                    glow.gameObject.SetActive(true);
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Assert.That(socket.canvasRenderer.hasRectClipping, Is.True);
                    Assert.That(ring.canvasRenderer.hasRectClipping, Is.True);
                    Assert.That(glow.canvasRenderer.hasRectClipping, Is.True);
                    socket.gameObject.SetActive(false);
                    ring.gameObject.SetActive(false);
                    glow.gameObject.SetActive(false);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(ring.canvasRenderer.hasRectClipping, Is.False);
                    Assert.That(glow.canvasRenderer.hasRectClipping, Is.False);
                }
            }
            finally { Object.Destroy(canvas); }
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
