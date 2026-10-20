using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ReactorDeCaos.Tests
{
    public sealed class ReactorSmokeTests
    {
        private string path;
        private string previousPreferences;
        private bool hadPreferences;
        private readonly Dictionary<string, byte[]> previousFiles = new Dictionary<string, byte[]>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var existing = Object.FindFirstObjectByType<ReactorApp>();
            if (existing != null) Object.Destroy(existing.gameObject);
            yield return null;
            hadPreferences = PlayerPrefs.HasKey(PresentationPreferences.StorageKey);
            previousPreferences = PlayerPrefs.GetString(PresentationPreferences.StorageKey, "");
            PlayerPrefs.DeleteKey(PresentationPreferences.StorageKey);
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
            if (hadPreferences) PlayerPrefs.SetString(PresentationPreferences.StorageKey, previousPreferences);
            else PlayerPrefs.DeleteKey(PresentationPreferences.StorageKey);
            PlayerPrefs.Save();
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
        public IEnumerator ProgressionCreatesDynamicRowsAndProceduralLayers()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app.Game.Shop.Count, Is.EqualTo(7));
            Assert.That(app.Hud.ShopButtons.Length, Is.EqualTo(app.Game.Shop.Count));
            Assert.That(app.Hud.ShopButtons[5].interactable, Is.False);
            Assert.That(app.Hud.ShopButtons[5].GetComponentInChildren<TextMeshProUGUI>().text,
                Does.Contain("2.5K E · BLOQUEADO"));
            for (int i = 0; i < 2000; i++) app.Game.Tap();
            app.Hud.Refresh(app.Game, true);
            Assert.That(app.Hud.ShopButtons[5].interactable, Is.True);
            app.Hud.ShopButtons[5].onClick.Invoke();
            Assert.That(app.Game.Level("quantum"), Is.EqualTo(1));
            var row = app.Hud.ShopButtons[5].GetComponentInChildren<TextMeshProUGUI>();
            Assert.That(row.text, Does.Contain("TURBINA CUÁNTICA"));
            Assert.That(row.text, Does.Contain("60 a 123.6 E / s"));
            double energy = app.Game.Energy;
            app.GetComponent<ReactorFeedback>().Animate(app.Game.Stage, true);
            Assert.That(app.Hud.EvolutionRings[0].gameObject.activeSelf, Is.True);
            Assert.That(app.Hud.EvolutionRings[1].gameObject.activeSelf, Is.True);
            Assert.That(app.Hud.EvolutionRings[2].gameObject.activeSelf, Is.False);
            Assert.That(app.Game.Energy, Is.EqualTo(energy), "Visual evolution must not change the economy.");
            yield return null;
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
        public IEnumerator PresentationAssetsAndEffectsAreIntegratedIntoTheMainScene()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app.Hud.CoreImage.sprite, Is.Not.Null);
            Assert.That(app.Hud.CoreImage.preserveAspect, Is.True);
            Assert.That(app.Hud.CoreImage.material.shader.name, Is.EqualTo("UI/Default"));
            var mask = app.Hud.EffectsRoot.GetComponentInParent<RectMask2D>();
            Assert.That(mask, Is.Not.Null);
            Assert.That(app.Hud.CoreVisual.IsChildOf(mask.transform), Is.True);
            Assert.That(app.Hud.OverloadButton.transform.IsChildOf(mask.transform), Is.False);
            foreach (var button in app.Hud.ShopButtons)
            {
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(icon.raycastTarget, Is.False);
            }
            foreach (var graphic in app.Hud.EffectsRoot.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False);
            Assert.That(app.GetComponents<AudioSource>().Length, Is.EqualTo(4));
            double energy = app.Game.Energy;
            int originalStage = app.Game.Stage;
            for (int visualStage = 0; visualStage < 5; visualStage++)
            {
                app.GetComponent<ReactorFeedback>().Animate(visualStage, false);
                for (int ring = 0; ring < 4; ring++)
                    Assert.That(app.Hud.EvolutionRings[ring].gameObject.activeSelf, Is.EqualTo(ring < visualStage));
            }
            Assert.That(app.Game.Energy, Is.EqualTo(energy));
            Assert.That(app.Game.Stage, Is.EqualTo(originalStage));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SettingsBlockReactorInputPersistAndMuteImmediately()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            app.Tap();
            double before = app.Game.Energy;
            app.Hud.SettingsButton.onClick.Invoke();
            Assert.That(app.Hud.Menu.IsOpen, Is.True);
            Canvas.ForceUpdateCanvases();
            Vector3 center = app.Hud.TapButton.transform.TransformPoint(((RectTransform)app.Hud.TapButton.transform).rect.center);
            var eventData = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, center) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.Not.EqualTo(app.Hud.TapButton.gameObject));
            ExecuteEvents.Execute(hits[0].gameObject, eventData, ExecuteEvents.pointerClickHandler);
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            // Restore defaults independently of which settings row the raycast hit.
            app.Preferences.sound = true;
            app.Preferences.reducedMotion = false;
            app.Preferences.SaveAndNotify();
            app.Hud.Menu.Options[0].onClick.Invoke();
            app.Hud.Menu.Options[2].onClick.Invoke();
            foreach (var source in app.GetComponents<AudioSource>()) Assert.That(source.isPlaying, Is.False);
            var loaded = PresentationPreferences.Load();
            Assert.That(loaded.sound, Is.False);
            Assert.That(loaded.reducedMotion, Is.True);
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            app.Hud.Menu.SetOpen(false);
            app.Tap();
            Assert.That(app.Game.Energy, Is.GreaterThan(before));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReducedPresentationKeepsRewardsAndOverloadTransitionsIntact()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            var feedback = app.GetComponent<ReactorFeedback>();
            var control = new GameEngine(app.Game.Snapshot(), Resources.Load<EconomySettings>("Economy").values);
            app.Preferences.reducedFlashes = app.Preferences.reducedMotion = app.Preferences.limitedEffects = true;
            app.Preferences.sound = false;
            app.Preferences.SaveAndNotify();
            for (int i = 0; i < 1000; i++) { app.Tap(); control.Tap(); }
            Assert.That(app.Game.Energy, Is.EqualTo(control.Energy));
            Assert.That(app.Hud.EffectsRoot.childCount, Is.EqualTo(50));
            Assert.That(app.GetComponents<AudioSource>().Length, Is.EqualTo(4));
            app.ActivateOverload(); control.ActivateOverload();
            feedback.Animate(app.Game.Stage, true, 1);
            Assert.That(Label(app, "OverloadLabel").text, Does.Contain("SOBRECARGA ×"));
            Assert.That(Label(app, "Combo").text, Does.Contain("COMBO ×"));
            Assert.That(app.Hud.CoreFlash.color.a, Is.Zero);
            Assert.That(app.Hud.CoreVisual.anchoredPosition, Is.EqualTo(Vector2.zero));
            foreach (Transform child in app.Hud.EffectsRoot)
                if (child.name.StartsWith("Spark")) Assert.That(child.gameObject.activeSelf, Is.False);
            app.Game.Advance(11); control.Advance(11);
            feedback.Animate(app.Game.Stage, false);
            feedback.Animate(app.Game.Stage, false);
            Assert.That(feedback.OverloadEndCount, Is.EqualTo(1));
            Assert.That(app.Game.Energy, Is.EqualTo(control.Energy));
            Assert.That(app.Game.Snapshot().IsValid(), Is.True);
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
