using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
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
        public IEnumerator RapidTapsReuseEffectsAndRecoverAfterOverload()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            int effects = app.Hud.EffectsRoot.childCount;
            Assert.That(effects, Is.EqualTo(50));
            for (int i = 0; i < 500; i++) app.Hud.TapButton.onClick.Invoke();
            Assert.That(app.Hud.EffectsRoot.childCount, Is.EqualTo(effects));
            Assert.That(app.Hud.CoreVisual.localScale.x, Is.GreaterThan(1));
            Assert.That(app.Game.ComboMultiplier, Is.EqualTo(3));
            app.Hud.OverloadButton.onClick.Invoke();
            for (int i = 0; i < 500; i++) app.Hud.TapButton.onClick.Invoke();
            Assert.That(app.Hud.EffectsRoot.childCount, Is.EqualTo(effects));
            Assert.That(app.Game.Overload.Active, Is.True);
            app.Game.Advance(11);
            Assert.That(app.Game.Overload.Active, Is.False);
            for (int i = 0; i < 25; i++) app.Tap();
            Assert.That(app.Game.Overload.Ready, Is.True);
            yield return new WaitForSecondsRealtime(1.2f);
            for (int i = 0; i < effects; i++)
                Assert.That(app.Hud.EffectsRoot.GetChild(i).gameObject.activeSelf, Is.False);
            Assert.That(app.Game.Snapshot().IsValid(), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HudReflectsAffordabilityNextEffectsComboAndProduction()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app.Hud.ShopButtons[0].interactable, Is.False);
            Assert.That(app.Hud.OverloadButton.interactable, Is.False);
            Assert.That(Label(app, "Description").text, Does.Contain("1 a 2 E / toque"));
            for (int i = 0; i < 25; i++) app.Tap();
            Assert.That(app.Hud.ShopButtons[0].interactable, Is.True);
            Assert.That(app.Hud.OverloadButton.interactable, Is.True);
            app.Hud.ShopButtons[0].onClick.Invoke();
            Assert.That(Label(app, "Description").text, Does.Contain("2 a 3 E / toque"));
            Assert.That(Label(app, "Description").text, Does.Contain("24 E"));
            app.Hud.ShopButtons[(int)Purchase.Coil].onClick.Invoke();
            Assert.That(Label(app, "Rate").text, Does.StartWith("1 / s"));
            Assert.That(Label(app, "Energy").text, Is.EqualTo(NumberFormat.Compact(app.Game.Energy)));
            app.Game.Advance(2.25);
            app.Hud.Refresh(app.Game, true);
            Assert.That(Label(app, "Combo").text, Does.Contain("×2 · BAJANDO"));
            app.ActivateOverload();
            Assert.That(Label(app, "Rate").text, Does.StartWith("3 / s"));
            Assert.That(Label(app, "OverloadLabel").text, Does.StartWith("SOBRECARGA ×3"));
            Assert.That(app.Hud.OverloadButton.interactable, Is.False);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static TextMeshProUGUI Label(ReactorApp app, string name)
        {
            foreach (var label in app.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == name) return label;
            Assert.Fail("Missing HUD label: " + name);
            return null;
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
