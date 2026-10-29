using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // A bounded presentation controller. All progression writes go through the engine/save store.
    public sealed class NarrativeDirector : IDisposable
    {
        private readonly ReactorApp app;
        private readonly ReactorHud hud;
        private readonly ReactorFeedback feedback;
        private readonly NarrativeCatalog catalog;
        private readonly GameObject root;
        private readonly Image portrait, veil, leftDoor, rightDoor;
        private readonly RawImage backdrop;
        private readonly AspectRatioFitter backdropFit;
        private readonly CanvasGroup actor;
        private readonly TextMeshProUGUI title, speaker, subtitle, action, status, skipLabel;
        private readonly RingGraphic orbit;
        private readonly RectTransform[] dust = new RectTransform[12];
        private readonly Texture2D exterior, reactor, signal;
        private NarrativeScene scene;
        private int beatIndex, chapter, actionFrame = -1, suppressedChapter = -99;
        private bool replay, suspended, audioPaused, communication, previousVoiceEnabled = true, pendingVoiceStart;
        private float elapsed, sceneAge, nextSceneAt;
        private readonly bool[] pendingComms = new bool[3];
        public AudioSource Voice { get; }
        public AudioSource Cue { get; }
        public Button NextButton { get; }
        public Button SkipButton { get; }
        public Button IgniteButton { get; }
        public bool IsBlocking => root.activeSelf;
        public bool AwaitingTap => IsBlocking && scene.beats[beatIndex].interactive;
        public bool IsReplay => replay;
        public bool CanIgnite => AwaitingTap && !IsPaused;
        public string SceneId => scene?.id;
        public int BeatIndex => beatIndex;
        private bool CanPresent => !hud.Menu.IsOpen && !hud.ModulePanel.IsOpen && hud.SelectedTab == 0 && !app.Game.Overload.Active;
        private bool IsPaused => suspended || hud.Menu.IsOpen || (communication && (hud.SelectedTab != 0 || hud.ModulePanel.IsOpen));
        public NarrativeDirector(ReactorApp owner, ReactorFeedback sound)
        {
            app = owner; hud = owner.Hud; feedback = sound;
            var data = Resources.Load<TextAsset>("Narrative");
            if (data == null) throw new InvalidOperationException("Missing Narrative.json.");
            catalog = JsonUtility.FromJson<NarrativeCatalog>(data.text); catalog.Validate();
            exterior = Resources.Load<Texture2D>("Art/OrfeoExterior");
            reactor = Resources.Load<Texture2D>("Art/ReactorBackdrop"); signal = Resources.Load<Texture2D>("Art/StorySignal");
            Voice = app.gameObject.AddComponent<AudioSource>(); Voice.playOnAwake = false; Voice.spatialBlend = 0; Voice.priority = 32;
            Cue = app.gameObject.AddComponent<AudioSource>(); Cue.playOnAwake = false; Cue.spatialBlend = 0;
            var surface = ReactorHud.Image("CinematicOverlay", hud.SafeRoot, new Color(.008f, .014f, .025f, 1), 0, 0, 1, 1);
            surface.raycastTarget = true; root = surface.gameObject;
            var view = ReactorHud.Rect("CinematicStage", surface.transform, 0, .43f, 1, 1);
            view.gameObject.AddComponent<RectMask2D>();
            var camera = ReactorHud.Rect("CinematicCamera", view, 0, 0, 1, 1);
            backdrop = camera.gameObject.AddComponent<RawImage>(); backdrop.raycastTarget = false;
            backdropFit = camera.gameObject.AddComponent<AspectRatioFitter>(); backdropFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            veil = ReactorHud.Image("CinematicVeil", view, new Color(0, .015f, .04f, .5f), 0, 0, 1, 1);
            for (int i = 0; i < dust.Length; i++)
            {
                var mote = ReactorHud.Image("CinematicMote" + i, view, new Color(.25f, .8f, 1, .22f), 0, 0, 0, 0);
                dust[i] = mote.rectTransform; dust[i].sizeDelta = Vector2.one * (3 + i % 3 * 2);
                dust[i].localEulerAngles = new Vector3(0, 0, 45);
            }
            leftDoor = ReactorHud.Image("SealLeft", view, new Color(.04f, .065f, .10f, .97f), 0, 0, .5f, 1);
            rightDoor = ReactorHud.Image("SealRight", view, new Color(.04f, .065f, .10f, .97f), .5f, 0, 1, 1);
            ReactorHud.Image("SealLeftLight", leftDoor.transform, ReactorHud.Warm, .98f, 0, 1, 1);
            ReactorHud.Image("SealRightLight", rightDoor.transform, ReactorHud.Warm, 0, 0, .02f, 1);
            var actorRect = ReactorHud.Rect("CinematicActor", surface.transform, .12f, .45f, .88f, .86f);
            actor = actorRect.gameObject.AddComponent<CanvasGroup>(); actor.blocksRaycasts = false;
            portrait = ReactorHud.Image("CinematicPortrait", actorRect, Color.white, 0, 0, 1, 1); portrait.preserveAspect = true;
            orbit = ReactorHud.Ring("TransmissionOrbit", actorRect, ReactorHud.Cyan); orbit.fraction = .65f; orbit.thickness = 2;
            IgniteButton = ReactorHud.Button("NarrativeIgnite", surface.transform, new Color(.02f, .06f, .09f, .10f), .23f, .52f, .77f, .84f);
            var core = ReactorHud.Image("IgnitionCore", IgniteButton.transform, Color.white, 0, 0, 1, 1);
            core.sprite = hud.NarrativeArt.Core; core.preserveAspect = true;
            IgniteButton.onClick.AddListener(() => { if (replay) Ignite(); else app.NarrativeTap(); });
            title = hud.Label("CinematicTitle", surface.transform, "", 29, ReactorHud.Cyan, .045f, .90f, .73f, .985f, TextAlignmentOptions.Left);
            var settings = ReactorHud.Button("CinematicSettings", surface.transform, new Color(.03f, .08f, .13f), .76f, .90f, .955f, .99f);
            hud.Label("CinematicSettingsLabel", settings.transform, "AJUSTES", 24, Color.white, .02f, .05f, .98f, .95f);
            settings.onClick.AddListener(() => hud.Menu.SetOpen(true));
            var dialogue = ReactorHud.Image("CinematicDialogue", surface.transform, new Color(.02f, .035f, .06f, 1), .035f, .15f, .965f, .455f);
            ReactorHud.Image("DialogueRule", dialogue.transform, ReactorHud.Cyan, 0, .985f, 1, 1);
            speaker = hud.Label("CinematicSpeaker", dialogue.transform, "", 30, ReactorHud.Cyan, .04f, .79f, .96f, .96f, TextAlignmentOptions.Left);
            subtitle = hud.Label("CinematicSubtitle", dialogue.transform, "", 34, Color.white, .04f, .17f, .96f, .78f, TextAlignmentOptions.TopLeft);
            status = hud.Label("CinematicStatus", dialogue.transform, "", 23, new Color(.62f, .73f, .82f), .04f, .025f, .96f, .155f, TextAlignmentOptions.Left);
            SkipButton = ReactorHud.Button("CinematicSkip", surface.transform, new Color(.05f, .09f, .14f), .045f, .025f, .48f, .12f);
            skipLabel = hud.Label("CinematicSkipLabel", SkipButton.transform, "OMITIR ESCENA", 27, Color.white, .025f, .05f, .975f, .95f);
            NextButton = ReactorHud.Button("CinematicNext", surface.transform, new Color(.045f, .20f, .25f), .52f, .025f, .955f, .12f);
            action = hud.Label("CinematicNextLabel", NextButton.transform, "CONTINUAR", 28, Color.white, .025f, .05f, .975f, .95f);
            SkipButton.onClick.AddListener(() => {
                if (app.Game.CampaignActive && !replay)
                {
                    hud.Preferences.autoDialogue = !hud.Preferences.autoDialogue;
                    hud.Preferences.SaveAndNotify();
                }
                else Skip();
            });
            NextButton.onClick.AddListener(Next);
            hud.Preferences.Changed += ApplyAudio; root.SetActive(false);
        }
        public void Start(bool returning)
        {
            nextSceneAt = Time.unscaledTime + 8;
            if (!app.Game.NarrativeIntroSeen) Play("intro", -1, false);
            else if (app.Game.RebirthCount > 0 && !app.Game.NarrativeAscentSeen) AfterRebirth();
            else if (app.Game.CampaignMissionReady) PresentMission();
            else if (!app.Game.NarrativeBriefingSeen) Play("resume_" + app.Game.StoryCompleted, -2, false);
            else if (returning) Communicate(catalog.Find("resume_" + app.Game.StoryCompleted));
        }
        public void PresentMission()
        {
            if (suspended || IsBlocking || !app.Game.CampaignMissionReady) return;
            hud.ClosePanels(); hud.SelectTab(0);
            Play("chapter_" + app.Game.StoryCompleted, app.Game.StoryCompleted, false);
        }
        public void ClearArrivalTransmission()
        {
            if (!communication || IsBlocking) return;
            StopSpeech(); scene = null; nextSceneAt = Time.unscaledTime + 8;
        }
        public bool PresentStationMemory(int index, bool isReplay = false)
        {
            if (index < 0 || index >= StationCatalog.MemoryCount || IsBlocking || suspended
                || (isReplay ? index >= app.Game.StationMemories : index != app.Game.PendingStationMemory)) return false;
            hud.ClosePanels(); hud.SelectTab(5);
            Play("station_" + index, -10 - index, isReplay);
            return true;
        }
        public bool Replay(int index)
        {
            if (IsBlocking || suspended || !app.Game.CanReplayNarrative(index)) return false;
            Play(index == -1 ? "intro" : index == StoryCatalog.Count ? "ascent" : "chapter_" + index, index, true);
            return true;
        }
        public void AfterRebirth()
        {
            if (!app.Game.NarrativeIntroSeen) return;
            Play(app.Game.NarrativeAscentSeen ? "cycle" : "ascent", StoryCatalog.Count, false);
        }
        private void Play(string id, int index, bool isReplay)
        {
            StopSpeech(); chapter = index; replay = isReplay; communication = false;
            scene = catalog.Find(id); beatIndex = 0; sceneAge = 0;
            root.SetActive(true); root.transform.SetAsLastSibling();
            title.text = scene.title;
            skipLabel.text = app.Game.CampaignActive && !replay ? "LECTURA AUTOMÁTICA" : "OMITIR ESCENA";
            leftDoor.gameObject.SetActive(scene.mood == "door"); rightDoor.gameObject.SetActive(scene.mood == "door");
            Cue.Stop(); Cue.loop = id == "intro";
            Cue.clip = Resources.Load<AudioClip>(id == "intro" ? "Audio/StoryAlarm" : scene.mood == "door" ? "Audio/StoryDoor" : "Audio/Ascension");
            if ((id == "intro" || scene.mood == "door") && hud.Preferences.sound && !IsPaused) Cue.Play();
            SetBeat();
        }
        private void SetBeat()
        {
            elapsed = 0;
            var beat = scene.beats[beatIndex];
            speaker.text = beat.speaker; subtitle.text = beat.text;
            portrait.sprite = hud.NarrativeArt.Portrait(scene.id == "intro" && beatIndex == 3 ? "core" : beat.portrait);
            speaker.color = beat.portrait == "mara" ? ReactorHud.Warm : beat.portrait == "memory" ? ReactorHud.Violet : ReactorHud.Cyan;
            orbit.color = speaker.color;
            portrait.gameObject.SetActive(!beat.interactive);
            orbit.gameObject.SetActive(!beat.interactive && beat.portrait != "mara");
            IgniteButton.gameObject.SetActive(beat.interactive);
            backdrop.texture = beat.interactive || (scene.id == "intro" && beatIndex >= 2) || (scene.mood == "ascent" && beatIndex >= 2) || scene.background == "reactor" ? reactor : scene.background == "signal" ? signal : exterior;
            backdropFit.aspectRatio = backdrop.texture.width / (float)backdrop.texture.height;
            NextButton.interactable = !beat.interactive;
            action.text = beatIndex == scene.beats.Length - 1
                ? (chapter <= -10 ? "VOLVER A ORFEO" : !replay && chapter == 4 ? "ELEGIR SINTONÍA" : !replay && chapter == 5 ? "REVISAR ASCENSIÓN" : hud.SelectedTab == 4 ? "VOLVER AL ARCHIVO" : "CONTINUAR MISIÓN") : "CONTINUAR";
            status.text = beat.interactive ? (replay ? "TOCA PARA REPETIR EL ENCENDIDO" : "TOCA EL NÚCLEO PARA ENCENDERLO")
                : (replay ? "REPETICIÓN" : "TRANSMISIÓN") + " · " + (beatIndex + 1) + " / " + scene.beats.Length;
            StartSpeech(beat);
            if (scene.id == "intro" && beatIndex > 2) Cue.Stop();
        }
        public void Ignite()
        {
            if (!AwaitingTap || IsPaused) return;
            Cue.Stop(); beatIndex++; SetBeat();
            Cue.loop = false; Cue.clip = Resources.Load<AudioClip>("Audio/Overload");
            if (hud.Preferences.sound) Cue.Play();
        }
        public void Next()
        {
            if (!IsBlocking || AwaitingTap || IsPaused || actionFrame == Time.frameCount) return;
            actionFrame = Time.frameCount;
            if (++beatIndex >= scene.beats.Length) Finish(false);
            else SetBeat();
        }
        public void Skip()
        {
            if (!IsBlocking || IsPaused) return;
            // First-time scenes have a required sequence; speed up lines without skipping the ignition.
            if (app.Game.CampaignActive && !replay) { Next(); return; }
            Finish(true);
        }
        private void Finish(bool skipped)
        {
            bool committed = replay || (chapter == StoryCatalog.Count && app.Game.NarrativeAscentSeen)
                || app.FinishNarrative(chapter, !skipped);
            if (!committed && app.Game.CampaignActive)
            {
                beatIndex = scene.beats.Length - 1;
                status.text = "No se pudo guardar. Libera espacio y pulsa CONTINUAR para reintentar.";
                elapsed = 0; action.text = "REINTENTAR GUARDADO";
                return;
            }
            if (!committed)
            {
                suppressedChapter = chapter;
                hud.ShowNotice("No se pudo guardar la escena. Podrás volver a verla en Historia.");
            }
            StopSpeech(); Cue.Stop(); root.SetActive(false); scene = null;
            nextSceneAt = Time.unscaledTime + 8;
            hud.Refresh(app.Game, true);
            if (committed && !replay && !skipped && chapter == 4)
            { hud.SelectTab(4); hud.Story.ShowChapter(4); hud.Story.FocusChoice(); }
            else if (committed && !replay && chapter == 5) hud.SelectTab(3);
            else if (committed && chapter <= -10) hud.SelectTab(5);
        }
        public void Notify(int kind)
        {
            if (kind >= 0 && kind < pendingComms.Length && !app.Game.NarrativeCommunicationSeen(kind)) pendingComms[kind] = true;
        }
        private void Communicate(NarrativeScene message)
        {
            scene = message; beatIndex = 0; elapsed = 0; communication = true;
            var beat = scene.beats[0]; hud.ShowTransmission(beat.speaker, beat.text, hud.NarrativeArt.Portrait(beat.portrait));
            StartSpeech(beat);
        }
        private void StartSpeech(NarrativeBeat beat)
        {
            Voice.Stop(); audioPaused = false; pendingVoiceStart = false;
            Voice.clip = Resources.Load<AudioClip>("Audio/Voice/" + beat.voice);
            ApplyAudio();
            if (hud.Preferences.voice && hud.Preferences.voiceVolume > 0 && !IsPaused && Voice.clip != null) Voice.Play();
        }
        private void StopSpeech()
        {
            Voice.Stop(); audioPaused = false; pendingVoiceStart = false; communication = false;
            hud.ClearTransmission(); feedback.DialogueActive = false; feedback.CinematicSilence = false;
        }
        public void SetSuspended(bool value) { suspended = value; ApplyAudio(); }
        private void ApplyAudio()
        {
            if (IsBlocking && app.Game.CampaignActive && !replay)
                skipLabel.text = hud.Preferences.autoDialogue ? "LECTURA: AUTO" : "LECTURA: MANUAL";
            bool enabled = hud.Preferences.voice && hud.Preferences.voiceVolume > 0;
            if (enabled && !previousVoiceEnabled) pendingVoiceStart = true;
            if (pendingVoiceStart && enabled && !IsPaused && scene != null && Voice.clip != null && elapsed < Voice.clip.length)
            { Voice.time = Mathf.Min(elapsed, Voice.clip.length - .01f); Voice.Play(); pendingVoiceStart = false; }
            previousVoiceEnabled = enabled;
            Voice.volume = hud.Preferences.voiceVolume * .62f;
            Cue.volume = hud.Preferences.effectsVolume * .25f;
            if (!hud.Preferences.voice || hud.Preferences.voiceVolume <= 0) { Voice.Stop(); audioPaused = false; }
            else if (IsPaused && Voice.isPlaying) { Voice.Pause(); audioPaused = true; }
            else if (!IsPaused && audioPaused) { Voice.UnPause(); audioPaused = false; }
            if (!hud.Preferences.sound || IsPaused) Cue.Pause();
            else if (IsBlocking && Cue.clip != null) Cue.UnPause();
            feedback.DialogueActive = Voice.isPlaying;
        }
        public void Tick(float seconds)
        {
            ApplyAudio();
            if (IsPaused) return;
            float dt = Mathf.Min(seconds, .1f);
            if (IsBlocking || communication)
            {
                elapsed += dt; sceneAge += dt;
                if (IsBlocking)
                {
                    Animate();
                    float length = Voice.clip != null ? Voice.clip.length : scene.beats[beatIndex].text.Length / 13f;
                    if (hud.Preferences.autoDialogue && !AwaitingTap && elapsed > Mathf.Max(3.5f, length + 1.3f)) Next();
                }
                else
                {
                    float length = Voice.clip != null ? Voice.clip.length : scene.beats[0].text.Length / 12f;
                    if (elapsed > Mathf.Max(5, length + 1.5f)) { StopSpeech(); scene = null; nextSceneAt = Time.unscaledTime + 3; }
                }
                return;
            }
            if (app.Game.CampaignMissionReady && !hud.Menu.IsOpen && !hud.ModulePanel.IsOpen)
            { PresentMission(); return; }
            if (app.Game.PendingStationMemory >= 0 && Time.unscaledTime >= nextSceneAt
                && !hud.Menu.IsOpen && !hud.ModulePanel.IsOpen && (hud.SelectedTab == 0 || hud.SelectedTab == 5)
                && !app.Game.Overload.Active)
            { PresentStationMemory(app.Game.PendingStationMemory); return; }
            if (!CanPresent || Time.unscaledTime < nextSceneAt) return;
            if (app.Game.RebirthCount > 0 && !app.Game.NarrativeAscentSeen && suppressedChapter != StoryCatalog.Count)
            { AfterRebirth(); return; }
            int next = app.Game.NarrativeScenesSeen;
            if (app.Game.NarrativeIntroSeen && next < app.Game.StoryCompleted && next != suppressedChapter)
            { Play("chapter_" + next, next, false); return; }
            for (int i = 0; i < pendingComms.Length; i++)
            {
                if (!pendingComms[i]) continue;
                pendingComms[i] = false;
                if (app.MarkCommunication(i)) Communicate(catalog.Find(i == 0 ? "buy" : i == 1 ? "overload" : "combo"));
                break;
            }
        }
        private void Animate()
        {
            bool motion = !hud.Preferences.reducedMotion;
            float blend = hud.Preferences.reducedFlashes ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .5f));
            actor.alpha = blend;
            actor.transform.localScale = Vector3.one * (motion ? 1 + .014f * Mathf.Sin(sceneAge * 1.2f) : 1);
            backdrop.rectTransform.localScale = Vector3.one * (motion ? 1.04f + .018f * Mathf.Sin(sceneAge * .18f) : 1);
            backdrop.rectTransform.anchoredPosition = motion ? new Vector2(Mathf.Sin(sceneAge * .13f) * 9, 0) : Vector2.zero;
            orbit.rectTransform.localEulerAngles = motion ? new Vector3(0, 0, sceneAge * 5) : Vector3.zero;
            float darkness = scene.id == "intro" && beatIndex < 3 ? .65f : .2f;
            feedback.CinematicSilence = scene.mood == "ascent" && beatIndex == 1;
            if (scene.mood == "ascent" && beatIndex < 2)
            {
                darkness = beatIndex == 1 ? .98f : Mathf.Lerp(.35f, .98f, Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 4)));
                actor.alpha = beatIndex == 1 ? 0 : blend;
            }
            veil.color = new Color(0, .015f, .04f, darkness + (1 - blend) * .15f);
            float door = motion ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(sceneAge / 3.5f)) : 1;
            leftDoor.rectTransform.anchoredPosition = Vector2.left * leftDoor.rectTransform.rect.width * door;
            rightDoor.rectTransform.anchoredPosition = Vector2.right * rightDoor.rectTransform.rect.width * door;
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i].gameObject.SetActive(motion && !hud.Preferences.limitedEffects);
                Vector2 anchor = new Vector2((i * .173f) % 1, Mathf.Repeat(i * .13f + sceneAge * (.006f + i * .0005f), 1));
                dust[i].anchorMin = dust[i].anchorMax = anchor;
            }
        }
        public void Dispose()
        {
            hud.Preferences.Changed -= ApplyAudio;
            if (Voice != null) Voice.Stop();
            if (Cue != null) Cue.Stop();
            if (hud != null) hud.ClearTransmission();
            if (feedback != null) { feedback.DialogueActive = false; feedback.CinematicSilence = false; }
        }
    }
}
