using System;
using System.Collections.Generic;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class NarrativeProgress
    {
        public bool introSeen, ascentSeen, briefingSeen;
        public int scenesSeen, communications;
        public NarrativeProgress Copy() => (NarrativeProgress)MemberwiseClone();
        public bool IsValid(int completed) => scenesSeen >= 0 && scenesSeen <= completed
            && communications >= 0 && communications <= 7 && (introSeen || (scenesSeen == 0 && !briefingSeen));
    }

    [Serializable]
    public sealed class NarrativeBeat
    {
        public string speaker, text, voice, portrait;
        // mode: "" spoken line, "fx" silent stage direction, "record" archived recording.
        // condition: "" always, "nara" / "crew" only for that attunement choice.
        public string mode, condition;
        public bool interactive;
        public bool Silent => mode == "fx";
        public bool Record => mode == "record";
        public bool Matches(int choice) => string.IsNullOrEmpty(condition)
            || (condition == "nara" && choice == 1) || (condition == "crew" && choice == 2);
    }
    [Serializable]
    public sealed class NarrativeScene
    {
        public string id, title, background, mood;
        // Key line of the scene, revealed letter by letter on the mission card while working toward it.
        public string teaser;
        public NarrativeBeat[] beats;
    }
    [Serializable]
    public sealed class NarrativeCatalog
    {
        public NarrativeScene[] scenes;
        public NarrativeScene Find(string id) => Array.Find(scenes, x => x.id == id);
        public void Validate()
        {
            if (scenes == null || scenes.Length > 32) throw new ArgumentException("Invalid narrative catalog.");
            var ids = new HashSet<string>(); var voices = new HashSet<string>();
            foreach (var scene in scenes)
            {
                if (scene == null || !EconomyConfig.ValidId(scene.id) || !ids.Add(scene.id)
                    || string.IsNullOrWhiteSpace(scene.title) || scene.title.Length > 70
                    || (scene.background != "exterior" && scene.background != "reactor" && scene.background != "signal")
                    || (scene.mood != "quiet" && scene.mood != "door" && scene.mood != "ascent")
                    || scene.beats == null || scene.beats.Length == 0 || scene.beats.Length > 12)
                    throw new ArgumentException("Invalid narrative scene.");
                if (scene.teaser != null && scene.teaser.Length > 60) throw new ArgumentException("Invalid narrative teaser.");
                bool always = false;
                foreach (var beat in scene.beats)
                {
                    string mode = beat?.mode ?? "", condition = beat?.condition ?? "";
                    if (beat == null || string.IsNullOrWhiteSpace(beat.text) || beat.text.Length > 180
                        || (mode != "" && mode != "fx" && mode != "record")
                        || (condition != "" && condition != "nara" && condition != "crew")
                        || (mode == "fx" ? (!string.IsNullOrEmpty(beat.voice) || beat.interactive)
                            : string.IsNullOrWhiteSpace(beat.speaker) || !EconomyConfig.ValidId(beat.voice) || !voices.Add(beat.voice))
                        || (beat.portrait != "nara" && beat.portrait != "mara" && beat.portrait != "memory" && beat.portrait != "core")
                        || (beat.interactive && scene.id != "intro")) throw new ArgumentException("Invalid narrative beat.");
                    always |= condition == "";
                }
                if (!always) throw new ArgumentException("Every scene needs an unconditional beat.");
            }
            for (int i = 0; i < StationCatalog.MemoryCount; i++)
                if (!ids.Contains("station_" + i)) throw new ArgumentException("Missing station memory scene.");
            foreach (string id in new[] { "intro", "ascent", "cycle", "buy", "overload", "combo" })
                if (!ids.Contains(id)) throw new ArgumentException("Missing narrative scene " + id);
            for (int i = 0; i < StoryCatalog.Count; i++)
                if (!ids.Contains("chapter_" + i)) throw new ArgumentException("Missing chapter scene.");
            for (int i = 0; i <= StoryCatalog.Count; i++)
                if (!ids.Contains("resume_" + i)) throw new ArgumentException("Missing recap.");
        }
    }

    // Pure presentation helpers shared by the HUD and tests; they never change game state.
    public static class NarrativeText
    {
        public static string Duration(double seconds)
        {
            if (seconds < 60) return Math.Max(1, Math.Ceiling(seconds)).ToString("0") + " s";
            if (seconds < 3600) return Math.Floor(seconds / 60).ToString("0") + " min " + Math.Floor(seconds % 60).ToString("00") + " s";
            return Math.Floor(seconds / 3600).ToString("0") + " h " + Math.Floor(seconds % 3600 / 60).ToString("00") + " min";
        }
        // Reveal the next scene's key line in a stable order as mission progress grows. Display only.
        public static string Corrupted(string line, double progress)
        {
            if (string.IsNullOrEmpty(line)) return "";
            int letters = 0;
            foreach (char c in line) if (char.IsLetterOrDigit(c)) letters++;
            int shown = (int)Math.Floor(Math.Max(0, Math.Min(1, progress)) * letters);
            var text = new System.Text.StringBuilder("\"");
            bool hidden = false; int index = 0;
            foreach (char c in line)
            {
                bool letter = char.IsLetterOrDigit(c);
                bool hide = letter && ((index++ * 37 + 11) % Math.Max(1, letters)) >= shown;
                if (hide && !hidden) text.Append("<mark=#2A3A5AFF><color=#00000000>");
                if (!hide && hidden) text.Append("</color></mark>");
                hidden = hide; text.Append(c);
            }
            if (hidden) text.Append("</color></mark>");
            return text.Append("\"").ToString();
        }
    }

    public sealed partial class GameEngine
    {
        public bool NarrativeIntroSeen => state.story.narrative.introSeen;
        public bool NarrativeBriefingSeen => state.story.narrative.briefingSeen;
        public bool NarrativeAscentSeen => state.story.narrative.ascentSeen;
        public int NarrativeScenesSeen => state.story.narrative.scenesSeen;
        public bool NarrativeCommunicationSeen(int kind) => kind >= 0 && kind < 3 && (state.story.narrative.communications & (1 << kind)) != 0;
        public bool CanReplayNarrative(int chapter) => chapter == -1 || (chapter >= 0 && chapter < StoryCompleted)
            || (chapter == StoryCatalog.Count && RebirthCount > 0);
        public bool TryFinishNarrative(int chapter, bool read, Func<GameState, bool> persist)
        {
            if (chapter == -2)
            {
                if (!NarrativeIntroSeen || NarrativeBriefingSeen) return false;
                return CommitStory(persist, x => x.narrative.briefingSeen = true);
            }
            if (chapter == -1)
            {
                if (NarrativeIntroSeen || (CampaignActive && Lifetime <= 0)) return false;
                return CommitStory(persist, x => { x.narrative.introSeen = true; x.narrative.briefingSeen = true; });
            }
            if (!NarrativeIntroSeen) return false;
            if (chapter == StoryCatalog.Count)
            {
                if (RebirthCount == 0 || NarrativeAscentSeen) return false;
                return CommitStory(persist, x => x.narrative.ascentSeen = true);
            }
            if (CampaignActive)
            {
                if (chapter != StoryCompleted || !CampaignMissionReady) return false;
                return CommitStory(persist, x => {
                    x.completed++; x.read = x.completed; x.narrative.scenesSeen = x.completed;
                    x.missionReady = false; x.missionOverloads = x.overloads;
                });
            }
            if (chapter < 0 || chapter != NarrativeScenesSeen || chapter >= StoryCompleted) return false;
            return CommitStory(persist, x => {
                x.narrative.scenesSeen++;
                if (read && x.read == chapter) x.read++;
            });
        }
        public bool TryMarkCommunication(int kind, Func<GameState, bool> persist)
        {
            if (kind < 0 || kind >= 3 || NarrativeCommunicationSeen(kind)) return false;
            return CommitStory(persist, x => x.narrative.communications |= 1 << kind);
        }
    }
}
