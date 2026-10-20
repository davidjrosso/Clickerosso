using System;
using System.IO;
using UnityEngine;

namespace ReactorDeCaos
{
    public sealed class ReactorApp : MonoBehaviour
    {
        public PresentationPreferences Preferences { get; private set; }
        public GameEngine Game { get; private set; }
        public ReactorHud Hud { get; private set; }
        private SaveStore saves;
        private ReactorFeedback feedback;
        private float saveTimer;
        private bool paused;
        private bool skipNextFrame;
        private int previousMilestones;
        private float nextMilestoneNotice;
        private bool previousReady;
        private bool initialized;
        private double previousCombo = 1;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            EconomySettings settings = Resources.Load<EconomySettings>("Economy");
            saves = new SaveStore(Path.Combine(Application.persistentDataPath, "reactor-save.json"), new UnityJsonCodec());
            GameState state = saves.Load();
            Game = new GameEngine(state, settings != null ? settings.values : null);
            previousMilestones = Game.CompletedMilestoneCount;
            Preferences = PresentationPreferences.Load();
            Hud = gameObject.AddComponent<ReactorHud>();
            try
            {
                Hud.Build(this);
                feedback = gameObject.AddComponent<ReactorFeedback>();
                feedback.Initialize(Hud);
                initialized = true;
            }
            catch (Exception error)
            {
                enabled = false;
                Debug.LogException(error, this);
                return;
            }
            if (saves.RecoveredBackup) Hud.ShowNotice("Progreso recuperado desde la copia de seguridad.");
            else if (saves.LastIssue != null) Hud.ShowNotice("No se pudo leer el guardado. Se inició una partida nueva.");
            else Hud.ShowNotice("Toca el núcleo. Alimenta el caos.");
        }

        private void Update()
        {
            if (!initialized || paused) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Hud.Back();
            if (skipNextFrame) { skipNextFrame = false; return; }
            // Time spent suspended never becomes offline production.
            float seconds = Mathf.Min(Time.unscaledDeltaTime, 86400f);
            Game.Advance(seconds);
            if (Game.ComboMultiplier < previousCombo && Game.ComboMultiplier <= 1)
                Hud.ShowNotice("Combo descansado. Retómalo a tu ritmo.");
            previousCombo = Game.ComboMultiplier;
            saveTimer += seconds;
            if (saveTimer >= 10) Save();
            if (Game.CompletedMilestoneCount != previousMilestones)
            {
                previousMilestones = Game.CompletedMilestoneCount;
                Save(); // Persist claims before any animation; animation is never the source of rewards.
            }
            if (Game.Overload.Ready && !previousReady) Hud.ShowNotice("Sobrecarga lista. Actívala cuando quieras.");
            previousReady = Game.Overload.Ready;
            if (Time.unscaledTime >= nextMilestoneNotice && Game.TryTakeMilestone(out var reward))
            {
                Hud.ShowNotice("HITO · " + reward.title + " · " + reward.rewardDescription);
                Hud.ShowReward(reward);
                feedback.Celebrate();
                nextMilestoneNotice = Time.unscaledTime + 3.5f;
            }
            Hud.Refresh(Game);
            feedback.Animate(Game.Stage, Game.Overload.Active, ComboIntensity);
            Hud.Assembly.Animate(Game);
        }

        public void Tap()
        {
            if (!initialized || paused) return;
            double reward = Game.Tap();
            if (Game.ComboMultiplier > previousCombo && Game.ComboMultiplier >= Game.ComboCap)
                Hud.ShowNotice("Combo máximo ×" + NumberFormat.Stat(Game.ComboCap));
            previousCombo = Game.ComboMultiplier;
            feedback.Tap(reward, Game.Overload.Active, ComboIntensity);
            Hud.Assembly.Tap();
            Hud.Refresh(Game, true);
        }

        public void Buy(Purchase item) { Buy(EconomyConfig.ItemId(item)); }

        public void Buy(string item)
        {
            if (!initialized || paused || !Game.Buy(item)) return;
            feedback.Purchase();
            Hud.Assembly.Install(item);
            Hud.ShowNotice("Mejora instalada.");
            Hud.InvalidateEconomy();
            Hud.Refresh(Game, true);
            Save();
        }

        public void ActivateOverload()
        {
            if (!initialized || paused || !Game.ActivateOverload()) return;
            feedback.StartOverload();
            Hud.ShowNotice("SOBRECARGA · producción y toques ×" + NumberFormat.Stat(Game.OverloadMultiplier));
            Hud.Refresh(Game, true);
        }

        public void BuyMastery(Specialization branch)
        {
            if (!initialized || paused || !Game.BuyMastery(branch)) return;
            feedback.Purchase(); Hud.InvalidateEconomy(); Hud.Refresh(Game, true);
            Hud.Progression.ShowMessage(Save() ? "Maestría permanente instalada." : "No se pudo guardar. Se reintentará; conserva la sesión abierta.");
        }

        public void SelectSkin(int skin)
        {
            if (!initialized || paused || !Game.SelectSkin(skin)) return;
            Hud.Refresh(Game, true); Hud.Assembly.Animate(Game);
            Hud.Progression.ShowMessage(Save() ? "Skin equipada. La economía no cambia." : "No se pudo guardar la selección.");
        }

        public bool Rebirth(Specialization branch, int quotedReward, int quotedCycle)
        {
            if (!initialized || paused) return false;
            if (Game.RebirthCount != quotedCycle || Game.RebirthReward != quotedReward || !Game.CanRebirth)
            {
                Hud.Progression.ShowMessage("La recompensa ha cambiado. Revisa el colapso de nuevo.");
                return false;
            }
            if (!RebirthTransaction.TryCommit(Game, saves, branch, quotedReward, out var next))
            {
                Hud.Progression.ShowMessage("No se pudo guardar el colapso. Tu reactor se conserva.");
                return false;
            }
            Game = next;
            previousMilestones = Game.CompletedMilestoneCount; previousCombo = 1; previousReady = false;
            saveTimer = 0; nextMilestoneNotice = Time.unscaledTime + 3.5f;
            Hud.ResetCycle(); Hud.Refresh(Game, true);
            Hud.ShowReward(new MilestoneDefinition { title = "RENACIMIENTO " + Game.RebirthCount,
                rewardDescription = "+" + quotedReward + " esencia · " + RebirthCatalog.SkinName(Game.Skin) });
            feedback.Reborn(); Hud.Assembly.Animate(Game);
            return true;
        }

        // Visual normalization only: the engine remains the sole source of rewards.
        private float ComboIntensity => Game.ComboCap <= 1 ? 0
            : Mathf.Clamp01((float)((Game.ComboMultiplier - 1) / (Game.ComboCap - 1)));

        private bool Save()
        {
            if (!initialized) return false;
            saveTimer = 0;
            if (saves != null && Game != null && !saves.Save(Game.Snapshot()))
            {
                Hud.ShowNotice("No se pudo guardar. Comprueba el espacio disponible.");
                Debug.LogWarning("Reactor save failed: " + saves.LastIssue);
                return false;
            }
            return true;
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (feedback != null) feedback.SetSuspended(value);
            if (value) Save();
            else skipNextFrame = true;
        }

        private void OnApplicationQuit() { Save(); }

        private void OnDestroy()
        {
            // Also persists progress when stopping Play Mode.
            Save();
        }
    }
}
