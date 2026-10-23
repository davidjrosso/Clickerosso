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
        public bool interactive;
    }
    [Serializable]
    public sealed class NarrativeScene
    {
        public string id, title, background, mood;
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
                foreach (var beat in scene.beats)
                    if (beat == null || string.IsNullOrWhiteSpace(beat.text) || beat.text.Length > 180
                        || string.IsNullOrWhiteSpace(beat.speaker) || !EconomyConfig.ValidId(beat.voice) || !voices.Add(beat.voice)
                        || (beat.portrait != "nara" && beat.portrait != "mara" && beat.portrait != "memory" && beat.portrait != "core")
                        || (beat.interactive && scene.id != "intro")) throw new ArgumentException("Invalid narrative beat.");
            }
            foreach (string id in new[] { "intro", "ascent", "cycle", "buy", "overload", "combo" })
                if (!ids.Contains(id)) throw new ArgumentException("Missing narrative scene " + id);
            for (int i = 0; i < StoryCatalog.Count; i++)
                if (!ids.Contains("chapter_" + i)) throw new ArgumentException("Missing chapter scene.");
            for (int i = 0; i <= StoryCatalog.Count; i++)
                if (!ids.Contains("resume_" + i)) throw new ArgumentException("Missing recap.");
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
