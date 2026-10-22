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
            Assert.That(Label(app, "PurchasePrice").text, Does.Contain("24 E"));
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
            Assert.That(app.Hud.ShopButtons[5].GetComponentInChildren<TextMeshProUGUI>(true).text,
                Does.Contain("2.5K E · BLOQUEADO"));
            for (int i = 0; i < 2000; i++) app.Game.Tap();
            app.Hud.Refresh(app.Game, true);
            Assert.That(app.Hud.ShopButtons[5].interactable, Is.True);
            app.Hud.ShopButtons[5].onClick.Invoke();
            Assert.That(app.Game.Level("quantum"), Is.EqualTo(1));
            var row = app.Hud.ShopButtons[5].transform.Find("Description").GetComponent<TextMeshProUGUI>();
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
            app.Hud.SelectTab(1);
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
            foreach (var graphic in app.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
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
            Assert.That(app.GetComponents<AudioSource>().Length, Is.EqualTo(7));
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
            foreach (var source in app.GetComponents<AudioSource>())
                if (source != app.GetComponent<ReactorFeedback>().MusicVoice) Assert.That(source.isPlaying, Is.False);
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
            Assert.That(app.GetComponents<AudioSource>().Length, Is.EqualTo(7));
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
        public IEnumerator ReactorModulesOpenDetailsAndInstallWithoutLeakingTaps()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app.Hud.Assembly.ModuleCount, Is.EqualTo(app.Game.Shop.Count));
            for (int i = 0; i < 25; i++) app.Tap();
            var module = app.Hud.Assembly.ButtonFor("coil");
            Canvas.ForceUpdateCanvases();
            Vector3 center = module.transform.TransformPoint(((RectTransform)module.transform).rect.center);
            var pointer = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, center) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject, Is.EqualTo(module.gameObject));
            double before = app.Game.Energy;
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(app.Hud.ModulePanel.IsOpen, Is.True);
            Assert.That(app.Hud.ModulePanel.SelectedId, Is.EqualTo("coil"));
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            double cost = app.Game.Cost("coil");
            app.Hud.ModulePanel.BuyButton.onClick.Invoke();
            Assert.That(app.Game.Energy, Is.EqualTo(before - cost));
            Assert.That(app.Game.Level("coil"), Is.EqualTo(1));
            app.Hud.Back();
            Assert.That(app.Hud.ModulePanel.IsOpen, Is.False);
            yield return null;
            Assert.That(module.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("NV. 1"));
            int objects = app.GetComponentsInChildren<Transform>(true).Length;
            app.Preferences.reducedFlashes = app.Preferences.reducedMotion = app.Preferences.limitedEffects = true;
            app.Preferences.SaveAndNotify();
            for (int i = 0; i < 1000; i++) app.Tap();
            yield return null;
            Assert.That(app.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RebirthRequiresReviewPersistsAndRefreshesTheExistingScene()
        {
            var oldApp = Object.FindFirstObjectByType<ReactorApp>();
            Object.Destroy(oldApp.gameObject);
            yield return null;
            var disk = new SaveStore(path, new UnityJsonCodec());
            Assert.That(disk.Save(new GameState { energy = 1000000, lifetimeEnergy = 1000000, coils = 5 }), Is.True);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var app = Object.FindFirstObjectByType<ReactorApp>();
            var old = app.Game;
            app.Hud.ProgressionButton.onClick.Invoke();
            app.Hud.Progression.ArmButton.onClick.Invoke();
            Assert.That(app.Hud.Progression.IsConfirming, Is.True);
            Assert.That(app.Game, Is.SameAs(old));
            app.Hud.Back();
            Assert.That(app.Hud.Progression.IsConfirming, Is.False);
            Assert.That(app.Game.RebirthCount, Is.Zero);
            app.Hud.Progression.ArmButton.onClick.Invoke();
            app.Hud.Progression.ConfirmButton.onClick.Invoke();
            Assert.That(app.Game, Is.Not.SameAs(old));
            Assert.That(app.Game.RebirthCount, Is.EqualTo(1));
            Assert.That(app.Game.Essence, Is.EqualTo(3));
            Assert.That(app.Game.Energy, Is.Zero);
            Assert.That(app.Game.Level("coil"), Is.EqualTo(1));
            Assert.That(app.Hud.Progression.IsOpen, Is.False);
            Assert.That(new GameEngine(disk.Load()).RebirthCount, Is.EqualTo(1));
            app.Hud.Progression.ConfirmButton.onClick.Invoke();
            Assert.That(app.Game.RebirthCount, Is.EqualTo(1), "Repeated confirmation must not award essence again.");
            app.Hud.Progression.Open(1);
            double power = app.Game.TapPower;
            app.Hud.Progression.MasteryButtons[0].onClick.Invoke();
            Assert.That(app.Game.TapPower, Is.GreaterThan(power));
            Assert.That(app.Game.Essence, Is.EqualTo(2));
            Assert.That(new GameEngine(disk.Load()).MasteryLevel(Specialization.Tap), Is.EqualTo(1));
            app.Hud.Progression.MasteryButtons[0].onClick.Invoke();
            Assert.That(app.Game.MasteryLevel(Specialization.Tap), Is.EqualTo(1), "Duplicate same-frame mastery purchase ignored.");
            double gain = app.Game.CurrentTapGain;
            app.SelectSkin(0);
            Assert.That(app.Game.CurrentTapGain, Is.EqualTo(gain));
            Assert.That(app.Hud.Skin, Is.Zero);
            app.Hud.ClosePanels();
            yield return null;
            foreach (var graphic in app.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
            Assert.That(app.Hud.EffectsRoot.childCount, Is.EqualTo(50));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NavigationClaimsAndAscensionRequirementsWorkInMain()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            Assert.That(app.Hud.SelectedTab, Is.Zero);
            Assert.That(app.Hud.Assembly.ButtonFor("coil").image.color.a, Is.Zero,
                "Modules must not introduce opaque rectangular backgrounds.");
            Assert.That(app.Hud.Assembly.ButtonFor("coil").GetComponentInChildren<SocketGraphic>(), Is.Not.Null);
            for (int i = 0; i < 100; i++) app.Tap();
            int objects = app.GetComponentsInChildren<Transform>(true).Length;
            app.Hud.NavigationButtons[2].onClick.Invoke();
            Assert.That(app.Hud.Achievements.IsOpen, Is.True);
            Assert.That(app.Hud.TapButton.gameObject.activeInHierarchy, Is.False);
            var claim = app.Hud.Achievements.ClaimButtons[0];
            Assert.That(claim.interactable, Is.True);
            Canvas.ForceUpdateCanvases();
            Vector3 center = claim.transform.TransformPoint(((RectTransform)claim.transform).rect.center);
            var pointer = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, center) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(claim.gameObject));
            double before = app.Game.Energy;
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(app.Game.Energy, Is.EqualTo(before + 25));
            Assert.That(claim.interactable, Is.False);
            claim.onClick.Invoke();
            Assert.That(app.Game.Energy, Is.EqualTo(before + 25));
            var disk = new SaveStore(path, new UnityJsonCodec());
            Assert.That(new GameEngine(disk.Load()).AchievementClaimed("first_spark"), Is.True);
            app.Hud.NavigationButtons[3].onClick.Invoke();
            Assert.That(app.Hud.Achievements.IsOpen, Is.False);
            Assert.That(app.Hud.Progression.IsOpen, Is.True);
            Assert.That(app.Hud.Progression.ArmButton.interactable, Is.False);
            Assert.That(Label(app, "CyclePreview").text, Does.Contain("REQUISITO"));
            app.Hud.SettingsButton.onClick.Invoke();
            Assert.That(app.Hud.Menu.IsOpen, Is.True);
            app.Hud.Back();
            Assert.That(app.Hud.Menu.IsOpen, Is.False);
            Assert.That(app.Hud.SelectedTab, Is.EqualTo(3));
            for (int i = 0; i < 20; i++) app.Hud.SelectTab(i % 4);
            app.Hud.SelectTab(0);
            yield return null;
            Assert.That(app.Hud.TapButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(app.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            foreach (var graphic in app.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MusicAndEffectsAreIndependentAndLegacyMuteMigrates()
        {
            PlayerPrefs.SetString(PresentationPreferences.StorageKey, "{\"version\":1,\"sound\":false}");
            var legacy = PresentationPreferences.Load();
            Assert.That(legacy.sound, Is.False); Assert.That(legacy.music, Is.False);
            Assert.That(legacy.effectsVolume, Is.EqualTo(.75f));
            legacy.music = true; legacy.musicVolume = .2f; legacy.effectsVolume = .6f; legacy.SaveAndNotify();
            var loaded = PresentationPreferences.Load();
            Assert.That(loaded.music, Is.True); Assert.That(loaded.sound, Is.False);
            Assert.That(loaded.musicVolume, Is.EqualTo(.2f)); Assert.That(loaded.effectsVolume, Is.EqualTo(.6f));
            var app = Object.FindFirstObjectByType<ReactorApp>();
            var audio = app.GetComponent<ReactorFeedback>();
            app.Preferences.sound = false; app.Preferences.music = true;
            app.Preferences.SaveAndNotify();
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(audio.MusicVoice.isPlaying, Is.True);
            app.Tap();
            foreach (var voice in app.GetComponents<AudioSource>())
                if (voice != audio.MusicVoice) Assert.That(voice.isPlaying, Is.False);
            app.Preferences.music = false; app.Preferences.sound = true; app.Preferences.SaveAndNotify();
            audio.Purchase();
            Assert.That(audio.MusicVoice.isPlaying, Is.False);
            bool effectPlaying = false;
            foreach (var voice in app.GetComponents<AudioSource>()) if (voice != audio.MusicVoice && voice.isPlaying) effectPlaying = true;
            Assert.That(effectPlaying, Is.True);
            app.Hud.Menu.EffectsVolume.value = .25f; app.Hud.Menu.MusicVolume.value = .45f;
            app.Hud.Menu.SetOpen(false);
            Assert.That(PresentationPreferences.Load().effectsVolume, Is.EqualTo(.25f));
            Assert.That(PresentationPreferences.Load().musicVolume, Is.EqualTo(.45f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ComboAndGeneratorCuesObserveGameplayAndKeepVoiceCountBounded()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            var audio = app.GetComponent<ReactorFeedback>();
            for (int i = 0; i < 1000; i++) app.Tap();
            Assert.That(audio.ComboRiseCount, Is.GreaterThan(0));
            yield return null;
            Assert.That(audio.GeneratorUnlockCount, Is.EqualTo(1), "Plasma unlock gets its own cue.");
            int unlocks = audio.GeneratorUnlockCount;
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(audio.GeneratorUnlockCount, Is.EqualTo(unlocks));
            Assert.That(app.GetComponents<AudioSource>().Length, Is.EqualTo(7));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DuplicatePurchaseCallbackInOneFrameCannotBuyTwice()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            for (int i = 0; i < 100; i++) app.Tap();
            double before = app.Game.Energy, price = app.Game.Cost("tap");
            app.Hud.SelectTab(1);
            app.Hud.ShopButtons[0].onClick.Invoke(); app.Hud.ShopButtons[0].onClick.Invoke();
            Assert.That(app.Game.Level("tap"), Is.EqualTo(1));
            Assert.That(app.Game.Energy, Is.EqualTo(before - price));
            for (int i = 0; i < 10; i++) yield return null;
            Assert.That(app.Game.Level("tap"), Is.EqualTo(1), "Animation never buys a module.");
            app.Hud.ShopButtons[0].onClick.Invoke();
            Assert.That(app.Game.Level("tap"), Is.EqualTo(2), "The next intentional tap remains available.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FocusAndPauseMustBothRecoverBeforeGameplayAndMusicResume()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            for (int i = 0; i < 25; i++) app.Tap(); app.Buy("coil");
            var music = app.GetComponent<ReactorFeedback>().MusicVoice;
            yield return new WaitForSecondsRealtime(.1f);
            app.SendMessage("OnApplicationFocus", false);
            app.SendMessage("OnApplicationPause", true);
            double before = app.Game.Energy;
            int sample = music.timeSamples;
            app.SendMessage("OnApplicationFocus", true);
            app.Tap(); app.ClaimAchievement("first_machine");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(app.Game.Energy, Is.EqualTo(before));
            Assert.That(app.Game.AchievementClaimed("first_machine"), Is.False);
            Assert.That(music.isPlaying, Is.False);
            Assert.That(music.timeSamples, Is.EqualTo(sample));
            app.SendMessage("OnApplicationPause", false);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(app.Game.Energy, Is.GreaterThan(before));
            Assert.That(app.Game.Energy - before, Is.LessThan(.3));
            Assert.That(music.isPlaying, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AscensionCanBeScrolledToItsActionWithoutCoveringNavigation()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            app.Hud.SelectTab(3);
            var scroll = app.Hud.Progression.ArmButton.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 0;
            yield return null; Canvas.ForceUpdateCanvases();
            var button = app.Hud.Progression.ArmButton;
            Vector3 center = button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, center), Is.True);
            var pointer = new PointerEventData(EventSystem.current) { position = center };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
            for (int tab = 0; tab < 5; tab++)
            {
                app.Hud.SelectTab(tab); Canvas.ForceUpdateCanvases();
                Assert.That(app.Hud.NavigationButtons[tab].gameObject.activeInHierarchy, Is.True);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator VisibleEconomicLabelsRemainReadableAcrossPanelsAndStates()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            foreach (bool charged in new[] { false, true })
            {
                if (charged) { for (int i = 0; i < 25; i++) app.Tap(); app.ActivateOverload(); }
                for (int tab = 0; tab < 5; tab++)
                {
                    app.Hud.SelectTab(tab); app.Hud.Refresh(app.Game, true);
                    yield return null; Canvas.ForceUpdateCanvases();
                    foreach (string name in new[] { "Energy", "Rate", "Stage", "Combo", "OverloadLabel", "PermanentSummary", "CyclePreview" })
                    {
                        var label = Label(app, name);
                        if (!label.gameObject.activeInHierarchy) continue;
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False, name + " in tab " + tab);
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator StoryArchiveRunsProductionHasReachableActionsAndKeepsSpoilersLocked()
        {
            var app = Object.FindFirstObjectByType<ReactorApp>();
            app.Hud.StoryButton.onClick.Invoke();
            Assert.That(app.Hud.Story.IsOpen, Is.True);
            Assert.That(app.Hud.SelectedTab, Is.EqualTo(4));
            Assert.That(app.Hud.Story.NextButton.interactable, Is.False);
            Assert.That(Label(app, "StoryNarrative").text, Does.Not.Contain(StoryCatalog.At(0).Resolution));
            Assert.That(Resources.Load<Texture2D>("Art/StorySignal"), Is.Not.Null);
            app.Hud.Story.ActionButton.onClick.Invoke();
            Assert.That(app.Hud.SelectedTab, Is.Zero);
            for (int i = 0; i < 100; i++) app.Tap();
            app.Buy("coil");
            app.Hud.SelectTab(4);
            Canvas.ForceUpdateCanvases();
            var action = app.Hud.Story.ActionButton;
            Vector3 center = action.transform.TransformPoint(((RectTransform)action.transform).rect.center);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = center }, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(action.gameObject));
            double before = app.Game.Energy;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(app.Game.Energy, Is.GreaterThan(before), "Reading does not pause generators.");
            app.Hud.Story.ShowChapter(0);
            action.onClick.Invoke();
            Assert.That(app.Game.StoryRead, Is.EqualTo(1));
            Assert.That(new GameEngine(new SaveStore(path, new UnityJsonCodec()).Load()).StoryRead, Is.EqualTo(1));
            app.Hud.Back();
            Assert.That(app.Hud.SelectedTab, Is.Zero);
            Assert.That(app.Hud.TapButton.gameObject.activeInHierarchy, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CompletedStoryCanBeReadChosenAndPreservedThroughAscensionInMain()
        {
            Object.Destroy(Object.FindFirstObjectByType<ReactorApp>().gameObject);
            yield return null;
            var disk = new SaveStore(path, new UnityJsonCodec());
            Assert.That(disk.Save(new GameState { energy = 1000000, lifetimeEnergy = 1000000 }), Is.True);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            var app = Object.FindFirstObjectByType<ReactorApp>();
            app.Hud.SelectTab(4);
            for (int chapter = 0; chapter < StoryCatalog.Count; chapter++)
            {
                Assert.That(app.Hud.Story.Chapter, Is.EqualTo(chapter));
                Canvas.ForceUpdateCanvases();
                var narrative = Label(app, "StoryNarrative"); narrative.ForceMeshUpdate();
                Assert.That(narrative.isTextOverflowing, Is.False, "Chapter " + chapter);
                var scroll = narrative.GetComponentInParent<ScrollRect>();
                scroll.verticalNormalizedPosition = 0;
                yield return null;
                if (chapter == 4)
                {
                    app.Hud.Story.CoreButton.onClick.Invoke();
                    Assert.That(app.Game.StoryChoice, Is.EqualTo(2));
                }
                app.Hud.Story.ActionButton.onClick.Invoke();
                app.Hud.Story.ActionButton.onClick.Invoke();
                Assert.That(app.Game.StoryRead, Is.EqualTo(chapter + 1), "Duplicate same-frame action must not skip a record.");
            }
            Assert.That(app.Game.StoryRead, Is.EqualTo(6));
            Assert.That(app.Rebirth(Specialization.Automation, app.Game.RebirthReward, app.Game.RebirthCount), Is.True);
            app.Hud.SelectTab(4);
            yield return null;
            Assert.That(app.Hud.Story.Chapter, Is.EqualTo(6));
            Assert.That(Label(app, "StoryNarrative").text, Does.Contain("capitán"));
            app.Hud.Story.ActionButton.onClick.Invoke();
            var saved = new GameEngine(disk.Load());
            Assert.That(saved.StoryEpilogueRead, Is.True);
            Assert.That(saved.StoryChoice, Is.EqualTo(2));
            Assert.That(saved.Unlocked("auxiliary"), Is.True);
            Assert.That(app.Hud.EffectsRoot.childCount, Is.EqualTo(50));
            foreach (var graphic in app.GetComponentsInChildren<StationRestorationGraphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null);
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
