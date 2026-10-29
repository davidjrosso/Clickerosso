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
        public NarrativeDirector Narrative { get; private set; }
        private bool InputBlocked => !initialized || paused || arrivalPending || (Narrative?.IsBlocking ?? false);
        private SaveStore saves;
        private EconomyConfig economySettings;
        private bool arrivalPending, showArrival;
        private long suspensionUtc;
        private float arrivalRetry;
        private int stationActionFrame = -1;
        private static long UtcNow => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private ReactorFeedback feedback;
        private float saveTimer;
        private bool paused, applicationPaused;
        private bool hasFocus = true;
        private bool[] knownUnlocks;
        private readonly System.Collections.Generic.Dictionary<string, int> purchaseFrames = new System.Collections.Generic.Dictionary<string, int>();
        private bool skipNextFrame;
        private int previousMilestones, savedStoryChapters;
        private float nextMilestoneNotice;
        private bool previousReady;
        private bool initialized;
        private double previousCombo = 1;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            EconomySettings settings = Resources.Load<EconomySettings>("Economy");
            saves = new SaveStore(Path.Combine(Application.persistentDataPath, GameEngine.CampaignSaveFile), new UnityJsonCodec());
            GameState state = saves.Load();
            bool recoveredBackup = saves.RecoveredBackup, returning = saves.LoadedExisting;
            string loadIssue = saves.LastIssue;
            if (state.story == null || state.story.campaignRevision != 1) state = GameEngine.NewCampaignState();
            economySettings = settings != null ? settings.values : null;
            Game = new GameEngine(state, economySettings);
            arrivalPending = !Game.TryResumeStation(UtcNow, Persist);
            showArrival = (Game.PendingOfflineEnergy > 0 && Game.PendingOfflineSeconds >= 60) || Game.ExpeditionReady;
            previousMilestones = Game.CompletedMilestoneCount;
            savedStoryChapters = Game.StoryCompleted;
            knownUnlocks = new bool[Game.Shop.Count];
            RememberUnlocks();
            Preferences = PresentationPreferences.Load();
            Hud = gameObject.AddComponent<ReactorHud>();
            try
            {
                Hud.Build(this);
                feedback = gameObject.AddComponent<ReactorFeedback>();
                feedback.Initialize(Hud);
                initialized = true;
                Narrative = new NarrativeDirector(this, feedback);
                Narrative.Start(returning);
            }
            catch (Exception error)
            {
                enabled = false;
                Debug.LogException(error, this);
                return;
            }
            if (recoveredBackup) Hud.ShowNotice("Progreso recuperado desde la copia de seguridad.");
            else if (loadIssue != null) Hud.ShowNotice("No se pudo leer el guardado. Se inició una partida nueva.");
            else Hud.ShowNotice("ORFEO · Restaura la estación. Encuentra a Mara.");
        }

        private void Update()
        {
            if (!initialized || paused) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Hud.Back();
            if (skipNextFrame) { skipNextFrame = false; return; }
            if (arrivalPending)
            {
                arrivalRetry -= Time.unscaledDeltaTime;
                if (arrivalRetry <= 0)
                {
                    arrivalRetry = 2;
                    arrivalPending = !Game.TryResumeStation(UtcNow, Persist, suspensionUtc);
                    if (!arrivalPending) { suspensionUtc = 0; showArrival = (Game.PendingOfflineEnergy > 0 && Game.PendingOfflineSeconds >= 60) || Game.ExpeditionReady; }
                    else Hud.ShowNotice("No se pudo guardar el regreso. Libera espacio; se reintentará.");
                }
                return;
            }
            // Absence is settled once, separately from the live frame clock.
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
            for (int i = 0; i < Game.Shop.Count; i++)
            {
                bool unlocked = Game.Unlocked(Game.Shop[i].Id);
                if (Game.Shop[i].IsGenerator && unlocked && !knownUnlocks[i]) feedback.GeneratorUnlocked();
                knownUnlocks[i] = unlocked;
            }
            if (Game.StoryCompleted != savedStoryChapters && Time.unscaledTime >= nextMilestoneNotice && Save())
            {
                savedStoryChapters = Game.StoryCompleted;
                Hud.ShowNotice("MISIÓN COMPLETADA · " + Game.CampaignHint);
                feedback.Celebrate();
                nextMilestoneNotice = Time.unscaledTime + 3.5f;
            }
            Narrative.Tick(seconds);
            if (showArrival && !Narrative.IsBlocking && !Hud.Menu.IsOpen && Game.CampaignTabAvailable(5))
            { showArrival = false; Narrative.ClearArrivalTransmission(); Hud.SelectTab(5); Hud.Station.ShowMessage(Game.ExpeditionReady ? "Bienvenido a Orfeo. El dron regresó; su recompensa te espera." : "Bienvenido a Orfeo. Tus generadores siguieron trabajando."); }
            Hud.Refresh(Game);
            feedback.Animate(Game.Stage, Game.Overload.Active, ComboIntensity);
            Hud.Assembly.Animate(Game);
        }

        public void Tap()
        {
            if (InputBlocked) return;
            TapCore();
        }
        public void NarrativeTap()
        {
            if (!initialized || paused || arrivalPending || Narrative == null || !Narrative.CanIgnite || Narrative.IsReplay) return;
            // A saved, interrupted opening may already contain its first tap.
            // Replaying that ignition step must not grant a second scripted reward.
            if (Game.CampaignActive)
            {
                double reward = Game.IgniteCampaign();
                if (reward > 0) feedback.Tap(reward, false, 0);
                Save();
            }
            else if (Game.AllTimeEnergy == 0) TapCore();
            Narrative.Ignite();
        }
        private void TapCore()
        {
            double reward = Game.Tap();
            if (Game.ComboMultiplier > previousCombo && Game.ComboMultiplier >= Game.ComboCap)
                Hud.ShowNotice("Combo máximo ×" + NumberFormat.Stat(Game.ComboCap));
            if (Game.ComboMultiplier > previousCombo) feedback.ComboRise(Game.ComboMultiplier);
            if (Game.ComboMultiplier >= 1.5) Narrative?.Notify(2);
            previousCombo = Game.ComboMultiplier;
            feedback.Tap(reward, Game.Overload.Active, ComboIntensity);
            Hud.Assembly.Tap();
            Hud.Refresh(Game, true);
        }

        public void CampaignAction()
        {
            if (InputBlocked) return;
            if (Game.CampaignNeedsChoice)
            { Hud.SelectTab(4); Hud.Story.ShowChapter(4); Hud.Story.FocusChoice(); return; }
            if (Game.CampaignMissionReady) { Narrative.PresentMission(); return; }
            if (Game.StoryCompleted == StoryCatalog.Count) { Hud.SelectTab(3); return; }
            if (Game.CampaignRequirementsMet)
            {
                if (!Game.TryBeginCampaignMission(Persist))
                { Hud.ShowNotice("No se pudo guardar la misión. Inténtalo de nuevo."); return; }
                saveTimer = 0;
                Narrative.PresentMission();
            }
            else { Hud.SelectTab(4); Hud.Story.ShowChapter(Game.CurrentStoryChapter); }
        }

        public void Buy(Purchase item) { Buy(EconomyConfig.ItemId(item)); }

        public void Buy(string item)
        {
            if (InputBlocked || (purchaseFrames.TryGetValue(item, out int frame) && frame == Time.frameCount) || !Game.Buy(item)) return;
            purchaseFrames[item] = Time.frameCount;
            feedback.Purchase();
            if (item == "coil") Narrative?.Notify(0);
            Hud.Assembly.Install(item);
            Hud.ShowNotice("Mejora instalada.");
            Hud.InvalidateEconomy();
            Hud.Refresh(Game, true);
            Save();
        }

        public void ActivateOverload()
        {
            if (InputBlocked || !Game.ActivateOverload()) return;
            feedback.StartOverload();
            Narrative?.Notify(1);
            Hud.ShowNotice("SOBRECARGA · producción y toques ×" + NumberFormat.Stat(Game.OverloadMultiplier));
            Hud.Refresh(Game, true);
        }

        public void ClaimAchievement(string id)
        {
            if (InputBlocked || !Game.CampaignTabAvailable(2)) return;
            if (!Game.TryClaimAchievement(id, Persist))
            {
                Hud.Achievements.ShowMessage("No se pudo reclamar. Revisa el progreso o el espacio de guardado.");
                return;
            }
            feedback.Celebrate(); Hud.InvalidateEconomy(); Hud.Refresh(Game, true);
            Hud.Achievements.ShowMessage("Recompensa de energía recibida y guardada.");
            saveTimer = 0;
        }

        public bool ReadStory(int chapter)
        {
            if (InputBlocked) return false;
            bool saved = chapter == StoryCatalog.Count ? Game.TryReadEpilogue(Persist) : Game.TryReadStory(chapter, Persist);
            if (!saved) { Hud.Story.ShowMessage("No se pudo archivar. Revisa el espacio de guardado e inténtalo de nuevo."); return false; }
            saveTimer = 0; Hud.Refresh(Game, true);
            Hud.Story.ShowMessage("Registro archivado. Se conservará al ascender.");
            return true;
        }

        public void ChooseStory(int choice)
        {
            if (InputBlocked) return;
            if (!Game.TryChooseStory(choice, Persist))
            { Hud.Story.ShowMessage("No se pudo guardar la sintonía. Conservas la anterior."); return; }
            saveTimer = 0; Hud.Refresh(Game, true);
            Hud.Story.ShowMessage("Sintonía guardada. Puedes cambiarla; tus ganancias no cambian.");
        }

        public void BuyMastery(Specialization branch)
        {
            string key = "mastery:" + (int)branch;
            if (InputBlocked || (purchaseFrames.TryGetValue(key, out int frame) && frame == Time.frameCount) || !Game.BuyMastery(branch)) return;
            purchaseFrames[key] = Time.frameCount;
            feedback.Purchase(); Hud.InvalidateEconomy(); Hud.Refresh(Game, true);
            Hud.Progression.ShowMessage(Save() ? "Maestría permanente instalada." : "No se pudo guardar. Se reintentará; conserva la sesión abierta.");
        }

        public void SelectSkin(int skin)
        {
            if (InputBlocked || !Game.SelectSkin(skin)) return;
            Hud.Refresh(Game, true); Hud.Assembly.Animate(Game);
            Hud.Progression.ShowMessage(Save() ? "Skin equipada. La economía no cambia." : "No se pudo guardar la selección.");
        }

        public bool Rebirth(Specialization branch, int quotedReward, int quotedCycle)
        {
            if (InputBlocked) return false;
            if (Game.RebirthCount != quotedCycle || Game.RebirthReward != quotedReward || !Game.CanRebirth)
            {
                Hud.Progression.ShowMessage("La recompensa ha cambiado. Revisa el colapso de nuevo.");
                return false;
            }
            if (!RebirthTransaction.TryCommit(Game, Persist, branch, quotedReward, out var next))
            {
                Hud.Progression.ShowMessage("No se pudo guardar el colapso. Tu reactor se conserva.");
                return false;
            }
            Game = next;
            RememberUnlocks(); purchaseFrames.Clear();
            savedStoryChapters = Game.StoryCompleted;
            previousMilestones = Game.CompletedMilestoneCount; previousCombo = 1; previousReady = false;
            saveTimer = 0; nextMilestoneNotice = Time.unscaledTime + 3.5f;
            Hud.ResetCycle(); Hud.Refresh(Game, true);
            Hud.ShowReward(new MilestoneDefinition { title = "ASCENSIÓN " + Game.RebirthCount,
                rewardDescription = "+" + quotedReward + " esencia · " + RebirthCatalog.SkinName(Game.Skin) });
            feedback.Reborn(); Hud.Assembly.Animate(Game);
            Narrative.AfterRebirth();
            return true;
        }

        public bool FinishNarrative(int chapter, bool read)
        {
            if (!initialized || paused || arrivalPending) return false;
            bool result = chapter <= -10 && chapter > -14
                ? Game.TryFinishStationMemory(-10 - chapter, Persist)
                : Game.TryFinishNarrative(chapter, read, Persist);
            if (result) saveTimer = 0;
            return result;
        }
        public bool MarkCommunication(int kind)
        {
            if (!initialized || paused || arrivalPending) return false;
            return Game.TryMarkCommunication(kind, Persist);
        }

        public void RestoreRoom(int index)
        {
            if (InputBlocked || stationActionFrame == Time.frameCount) return;
            if (!Game.TryRestoreRoom(index, Persist)) { Hud.Station.ShowMessage("Revisa los requisitos, la energía y el espacio de guardado."); return; }
            StationChanged("Sala restaurada · beneficio permanente activado");
            Narrative.PresentStationMemory(Game.PendingStationMemory);
        }
        public void StartExpedition()
        {
            if (InputBlocked || stationActionFrame == Time.frameCount) return;
            if (!Game.TryStartExpedition(Persist)) { Hud.Station.ShowMessage("No se pudo enviar el dron. Revisa el objetivo y el guardado."); return; }
            StationChanged("Dron enviado. Puedes seguir jugando o salir; el resultado no caduca.");
        }
        public void ClaimExpedition()
        {
            if (InputBlocked || stationActionFrame == Time.frameCount) return;
            if (!Game.TryClaimExpedition(Persist)) { Hud.Station.ShowMessage("No se pudo recoger. Revisa el tiempo, el límite de energía o el guardado."); return; }
            StationChanged("Dron recibido · recompensa guardada");
            if (Game.PendingStationMemory >= 0) Narrative.PresentStationMemory(Game.PendingStationMemory);
        }
        public void ClaimOffline()
        {
            if (InputBlocked || stationActionFrame == Time.frameCount) return;
            if (!Game.TryClaimOffline(Persist)) { Hud.Station.ShowMessage("No se pudo reclamar. Tu reserva sigue esperando."); return; }
            StationChanged("Energía recibida y guardada");
        }
        private void StationChanged(string message)
        {
            stationActionFrame = Time.frameCount;
            saveTimer = 0; feedback.Celebrate(); Hud.InvalidateEconomy(); Hud.Refresh(Game, true);
            Hud.Station.ShowMessage(message);
        }
        private bool Persist(GameState candidate)
        {
            candidate.station = candidate.station ?? new StationProgress();
            long stamp = paused && suspensionUtc > 0 ? suspensionUtc : UtcNow;
            candidate.station.lastUtcSeconds = Math.Max(candidate.station.lastUtcSeconds, stamp);
            candidate.station.offlineRate = Math.Min(EconomyConfig.MaxEnergy, new GameEngine(candidate, economySettings).BaseProduction);
            return saves.Save(candidate);
        }

        // Visual normalization only: the engine remains the sole source of rewards.
        private float ComboIntensity => Game.ComboCap <= 1 ? 0
            : Mathf.Clamp01((float)((Game.ComboMultiplier - 1) / (Game.ComboCap - 1)));

        private bool Save()
        {
            if (!initialized) return false;
            saveTimer = 0;
            if (arrivalPending) return false;
            if (saves != null && Game != null && !Game.TryCheckpointStation(paused && suspensionUtc > 0 ? suspensionUtc : UtcNow, Persist))
            {
                Hud.ShowNotice("No se pudo guardar. Comprueba el espacio disponible.");
                Debug.LogWarning("Reactor save failed: " + saves.LastIssue);
                return false;
            }
            return true;
        }

        private void RememberUnlocks()
        {
            for (int i = 0; i < knownUnlocks.Length; i++) knownUnlocks[i] = Game.Unlocked(Game.Shop[i].Id);
        }
        private void OnApplicationPause(bool value) { applicationPaused = value; ApplyActivity(); }
        private void OnApplicationFocus(bool value) { hasFocus = value; ApplyActivity(); }
        private void ApplyActivity()
        {
            bool inactive = applicationPaused || !hasFocus;
            if (paused == inactive) return;
            paused = inactive;
            if (feedback != null) feedback.SetSuspended(inactive);
            Narrative?.SetSuspended(inactive);
            if (inactive)
            {
                if (!arrivalPending) suspensionUtc = UtcNow;
                Save(); Preferences?.Save();
            }
            else
            {
                if (initialized)
                {
                    long absence = suspensionUtc > 0 ? Math.Max(0, UtcNow - suspensionUtc) : 0;
                    arrivalPending = !Game.TryResumeStation(UtcNow, Persist, suspensionUtc);
                    if (!arrivalPending) { suspensionUtc = 0; showArrival = (Game.PendingOfflineEnergy > 0 && absence >= 60) || Game.ExpeditionReady; }
                }
                skipNextFrame = true;
            }
        }

        private void OnApplicationQuit() { Save(); Preferences?.Save(); }

        private void OnDestroy()
        {
            Narrative?.Dispose();
            // Also persists progress when stopping Play Mode.
            Save();
            Preferences?.Save();
        }
    }
}
