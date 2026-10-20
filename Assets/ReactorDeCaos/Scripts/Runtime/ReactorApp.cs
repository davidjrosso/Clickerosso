using System;
using System.IO;
using UnityEngine;

namespace ReactorDeCaos
{
    public sealed class ReactorApp : MonoBehaviour
    {
        public GameEngine Game { get; private set; }
        public ReactorHud Hud { get; private set; }
        private SaveStore saves;
        private ReactorFeedback feedback;
        private float saveTimer;
        private bool paused;
        private bool skipNextFrame;
        private int previousStage;
        private bool previousReady;
        private bool initialized;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            EconomySettings settings = Resources.Load<EconomySettings>("Economy");
            saves = new SaveStore(Path.Combine(Application.persistentDataPath, "reactor-save.json"), new UnityJsonCodec());
            GameState state = saves.Load();
            Game = new GameEngine(state, settings != null ? settings.values : null);
            previousStage = Game.Stage;
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
            if (skipNextFrame) { skipNextFrame = false; return; }
            // Time spent suspended never becomes offline production.
            float seconds = Mathf.Min(Time.unscaledDeltaTime, 86400f);
            Game.Advance(seconds);
            saveTimer += seconds;
            if (saveTimer >= 10) Save();
            if (Game.Stage != previousStage)
            {
                previousStage = Game.Stage;
                Hud.ShowNotice(Game.Stage == 1 ? "HITO: matriz de plasma desbloqueada" : "HITO: reactor de singularidad");
                feedback.Celebrate();
                Save();
            }
            if (Game.Overload.Ready && !previousReady) Hud.ShowNotice("Sobrecarga lista. Actívala cuando quieras.");
            previousReady = Game.Overload.Ready;
            Hud.Refresh(Game);
            feedback.Animate(Game.Stage, Game.Overload.Active);
        }

        public void Tap()
        {
            if (!initialized || paused) return;
            double reward = Game.Tap();
            feedback.Tap(reward, Game.Overload.Active);
            Hud.Refresh(Game, true);
        }

        public void Buy(Purchase item)
        {
            if (!initialized || paused || !Game.Buy(item)) return;
            feedback.Purchase();
            Hud.ShowNotice("Mejora instalada.");
            Hud.Refresh(Game, true);
            Save();
        }

        public void ActivateOverload()
        {
            if (!initialized || paused || !Game.ActivateOverload()) return;
            feedback.Celebrate();
            Hud.ShowNotice("SOBRECARGA · producción y toques ×3");
            Hud.Refresh(Game, true);
        }

        private void Save()
        {
            if (!initialized) return;
            saveTimer = 0;
            if (saves != null && Game != null && !saves.Save(Game.Snapshot()))
            {
                Hud.ShowNotice("No se pudo guardar. Comprueba el espacio disponible.");
                Debug.LogWarning("Reactor save failed: " + saves.LastIssue);
            }
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
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
