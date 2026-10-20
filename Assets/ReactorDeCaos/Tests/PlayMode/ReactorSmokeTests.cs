using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ReactorDeCaos.Tests
{
    public sealed class ReactorSmokeTests
    {
        private string path;
        private readonly Dictionary<string, byte[]> previousFiles = new Dictionary<string, byte[]>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var existing = Object.FindFirstObjectByType<ReactorApp>();
            if (existing != null) Object.Destroy(existing.gameObject);
            yield return null;
            path = Path.Combine(Application.persistentDataPath, "reactor-save.json");
            previousFiles.Clear();
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
            {
                string file = path + suffix;
                if (File.Exists(file)) { previousFiles[file] = File.ReadAllBytes(file); File.Delete(file); }
            }
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            if (app != null) Object.Destroy(app.gameObject);
            yield return null;
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
            {
                string file = path + suffix;
                if (File.Exists(file)) File.Delete(file);
            }
            foreach (var saved in previousFiles) File.WriteAllBytes(saved.Key, saved.Value);
        }

        [UnityTest]
        public IEnumerator MainSceneSupportsTapPurchaseAndOverload()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app, Is.Not.Null);
            Assert.That(app.Hud.Font, Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Art/ReactorBackdrop"), Is.Not.Null);
            for (int i = 0; i < 25; i++) app.Hud.TapButton.onClick.Invoke();
            Assert.That(app.Game.Energy, Is.GreaterThanOrEqualTo(25));
            app.Hud.ShopButtons[(int)Purchase.Coil].onClick.Invoke();
            Assert.That(app.Game.Level(Purchase.Coil), Is.EqualTo(1));
            app.Hud.OverloadButton.onClick.Invoke();
            Assert.That(app.Game.Overload.Active, Is.True);
            double before = app.Game.Energy;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.Game.Energy, Is.GreaterThan(before));
            Assert.That(File.Exists(path), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ShopReceivesRaycastWithoutTappingReactor()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            var button = app.Hud.ShopButtons[0];
            Canvas.ForceUpdateCanvases();
            Vector3 center = button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
            var eventData = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, center) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
            double before = app.Game.Energy;
            ExecuteEvents.Execute(hits[0].gameObject, eventData, ExecuteEvents.pointerClickHandler);
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PauseStopsInputAndProductionAndResumeRestoresBoth()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            for (int i = 0; i < 25; i++) app.Tap();
            app.Buy(Purchase.Coil);
            app.SendMessage("OnApplicationPause", true);
            double before = app.Game.Energy;
            app.Tap();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            Assert.That(File.Exists(path), Is.True);
            app.SendMessage("OnApplicationPause", false);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.Game.Energy, Is.GreaterThan(before));
            Assert.That(app.Game.Energy - before, Is.LessThan(.4));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
