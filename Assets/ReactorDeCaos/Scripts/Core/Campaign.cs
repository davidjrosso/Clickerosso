using System;

namespace ReactorDeCaos
{
    // The Unity entry point always uses this campaign revision. The legacy economy constructor
    // remains usable by headless economic tools; old saves are never imported into this slot.
    public sealed partial class GameEngine
    {
        public const string CampaignSaveFile = "reactor-campaign-1.json";
        public static GameState NewCampaignState() => new GameState {
            story = new StoryProgress { campaignRevision = 1, narrative = new NarrativeProgress() }
        };
        public bool CampaignActive => state.story.campaignRevision == 1;
        public bool CampaignMissionReady => CampaignActive && state.story.missionReady;
        public bool ComboAvailable => !CampaignActive || (NarrativeIntroSeen && StoryCompleted >= 1);
        public bool OverloadAvailable => !CampaignActive || (NarrativeIntroSeen && StoryCompleted >= 2);
        public bool CampaignAscensionAvailable => !CampaignActive || StoryCompleted == StoryCatalog.Count;
        public bool CampaignNeedsChoice => CampaignActive && StoryCompleted == 5 && StoryChoice == 0;
        public bool CampaignTabAvailable(int tab) => !CampaignActive || tab == 0 || (NarrativeIntroSeen
            && (tab == 1 || tab == 4 || (tab == 2 && StoryCompleted >= 1) || (tab == 3 && StoryCompleted >= 5)));
        public bool CampaignAchievementVisible(AchievementDefinition goal) => !CampaignActive || (CampaignTabAvailable(2)
            && (goal.goal != AchievementGoal.GeneratorLevel || Unlocked(goal.generatorId))
            && (goal.goal != AchievementGoal.Ascensions || StoryCompleted >= 5));
        public bool CampaignItemAvailable(string id)
        {
            if (!NarrativeIntroSeen) return false;
            switch (id)
            {
                case "tap": case "coil": return true;
                case "combo": return StoryCompleted >= 1;
                case "overload": case "plasma": return StoryCompleted >= 2;
                case "quantum": return StoryCompleted >= 3;
                case "auxiliary": return StoryCompleted >= 4;
                default: return HasStoryBlueprint(id);
            }
        }
        public string CampaignUnlockDescription(string id)
        {
            switch (id)
            {
                case "tap": case "coil": return "Despertar · enciende el reactor";
                case "combo": return "restaurar el soporte vital";
                case "overload": case "plasma": return "localizar la señal de Mara";
                case "quantum": return "abrir la puerta roja";
                case "auxiliary": return "leer la caja negra";
                default: return "avanzar en la campaña";
            }
        }
        public bool CampaignRequirementsMet
        {
            get
            {
                if (!CampaignActive || !NarrativeIntroSeen || StoryCompleted >= StoryCatalog.Count) return false;
                if (AllTimeEnergy < StoryCatalog.At(StoryCompleted).Energy) return false;
                switch (StoryCompleted)
                {
                    case 0: return Level("tap") >= 1 && Level("coil") >= 1;
                    case 1: return state.story.bestCombo >= 1.5 || BaseProduction >= 5;
                    case 2: return Level("plasma") >= 1 && state.story.overloads > state.story.missionOverloads;
                    case 3: return Level("quantum") >= 1;
                    case 4: return Level("auxiliary") >= 1;
                    case 5: return StoryChoice != 0;
                    default: return false;
                }
            }
        }
        public double CampaignProgress => StoryCompleted == StoryCatalog.Count ? 1
            : !NarrativeIntroSeen ? 0 : CampaignRequirementsMet ? 1
            : Math.Min(.9, AllTimeEnergy / StoryCatalog.At(StoryCompleted).Energy * .9);
        public string CampaignAction
        {
            get
            {
                switch (StoryCompleted)
                {
                    case 0: return "RESTAURAR SOPORTE VITAL";
                    case 1: return "LOCALIZAR A MARA";
                    case 2: return "ABRIR LA PUERTA ROJA";
                    case 3: return "LEER LA CAJA NEGRA";
                    case 4: return "ESTABILIZAR EL ENLACE";
                    case 5: return "PRESERVAR LOS RECUERDOS";
                    default: return RebirthCount == 0 ? "REVISAR ASCENSIÓN" : "ORFEO RECUERDA";
                }
            }
        }
        public string CampaignHint
        {
            get
            {
                if (!NarrativeIntroSeen) return "Enciende el núcleo para despertar Orfeo";
                if (CampaignMissionReady) return "Transmisión pendiente · continuar";
                if (StoryCompleted == StoryCatalog.Count) return RebirthCount == 0 ? "Tu memoria está a salvo · revisa la ascensión" : "Primer arco completo · reconstruye Orfeo";
                if (CampaignRequirementsMet) return "Todo preparado · toca para continuar";
                switch (StoryCompleted)
                {
                    case 0:
                        if (Level("tap") == 0) return "Mejoras → Potencia · primera reparación";
                        if (Level("coil") == 0) return "Mejoras → Microgenerador · alimenta el soporte vital";
                        break;
                    case 1: if (!StoryTaskDone(StoryTask.Signal)) return "Combo ×1.5 o 5 E/s · reconstruye la señal"; break;
                    case 2:
                        if (Level("plasma") == 0) return "Instala plasma para alimentar la puerta";
                        if (state.story.overloads <= state.story.missionOverloads) return "Carga y activa Sobrecarga para romper el sello";
                        break;
                    case 3: if (Level("quantum") == 0) return "Instala una turbina cuántica para leer el registro"; break;
                    case 4: if (Level("auxiliary") == 0) return "Instala un núcleo auxiliar para estabilizar las voces"; break;
                    case 5: if (StoryChoice == 0) return "Elige a quién escuchar · NARA o la tripulación"; break;
                }
                return NumberFormat.Compact(Math.Min(AllTimeEnergy, StoryCatalog.At(StoryCompleted).Energy))
                    + " / " + NumberFormat.Compact(StoryCatalog.At(StoryCompleted).Energy) + " E producidas · comprar no resta progreso";
            }
        }
        public string CampaignObjective
        {
            get
            {
                if (StoryCompleted == StoryCatalog.Count) return CampaignHint;
                string task;
                switch (StoryCompleted)
                {
                    case 0: task = "Potencia nivel 1 y microgenerador nivel 1"; break;
                    case 1: task = "Combo ×1.5 o producción de 5 E/s"; break;
                    case 2: task = "Plasma nivel 1 y una sobrecarga en esta misión"; break;
                    case 3: task = "Turbina cuántica nivel 1"; break;
                    case 4: task = "Núcleo auxiliar nivel 1"; break;
                    default: task = "Elige la sintonía para preservar la memoria"; break;
                }
                return CampaignHint + "\n\n" + task + "\n"
                    + NumberFormat.Compact(Math.Min(AllTimeEnergy, StoryCatalog.At(StoryCompleted).Energy)) + " / "
                    + NumberFormat.Compact(StoryCatalog.At(StoryCompleted).Energy) + " E producidas en total";
            }
        }
        // Manual scene advance cannot perform the first tap on the player's behalf.
        public double IgniteCampaign()
        {
            if (!CampaignActive || NarrativeIntroSeen || Lifetime > 0) return 0;
            return Credit(TapPower);
        }
        public bool TryBeginCampaignMission(Func<GameState, bool> persist)
        {
            if (!CampaignRequirementsMet || CampaignMissionReady) return false;
            return CommitStory(persist, x => x.missionReady = true);
        }
    }
}
