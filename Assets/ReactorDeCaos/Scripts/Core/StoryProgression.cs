using System;

namespace ReactorDeCaos
{
    // Optional in v1/v2 saves. Records knowledge, not another currency or a second economy.
    [Serializable]
    public sealed class StoryProgress
    {
        public int completed, read, choice, overloads, bestCoils, bestQuantum, bestAuxiliary;
        // Revision 1 uses explicit missions and persisted consequences, never energy catch-up.
        public int campaignRevision;
        public bool missionReady;
        public int missionOverloads;

        public double bestCombo = 1;
        public bool epilogueRead;
        public NarrativeProgress narrative;
        public StoryProgress Copy()
        {
            var copy = (StoryProgress)MemberwiseClone(); copy.narrative = narrative?.Copy(); return copy;
        }
        public bool IsValid() => completed >= 0 && completed <= StoryCatalog.Count && read >= 0 && read <= completed
            && choice >= 0 && choice <= 2 && (choice == 0 || completed >= 5)
            && overloads >= 0 && overloads <= 1000000 && Level(bestCoils) && Level(bestQuantum) && Level(bestAuxiliary)
            && !double.IsNaN(bestCombo) && !double.IsInfinity(bestCombo) && bestCombo >= 1 && bestCombo <= 1000
            && (!epilogueRead || (completed == StoryCatalog.Count && read == StoryCatalog.Count))
            && (narrative == null || narrative.IsValid(completed))
            && (campaignRevision == 0 || (campaignRevision == 1 && narrative != null
                && narrative.scenesSeen == completed && read == completed
                && (completed == 0 || narrative.introSeen)
                && (!missionReady || (narrative.introSeen && completed < StoryCatalog.Count))
                && missionOverloads >= 0 && missionOverloads <= overloads
                && (completed < 6 || choice != 0)));
        private static bool Level(int n) => n >= 0 && n <= EconomyConfig.MaxLevel;
    }

    public enum StoryTask { Coil, Signal, Overload, Quantum, Auxiliary, Energy }

    public sealed class StoryChapter
    {
        public string Id { get; }
        public string Title { get; }
        public string Opening { get; }
        public string Resolution { get; }
        public string Reward { get; }
        public double Energy { get; }
        public double AlternativeEnergy { get; }
        public StoryTask Task { get; }
        internal StoryChapter(string id, string title, double energy, double alternative, StoryTask task,
            string opening, string resolution, string reward)
        { Id = id; Title = title; Energy = energy; AlternativeEnergy = alternative; Task = task;
            Opening = opening; Resolution = resolution; Reward = reward; }
    }

    // One immutable catalog: narrative, objectives and pacing can be edited together.
    // Stable order is the save contract; append chapters rather than reordering existing entries.
    public static class StoryCatalog
    {
        public const int Count = 6;
        public const string Title = "Ecos del primer encendido";
        private static readonly StoryChapter[] chapters =
        {
            new StoryChapter("spark", "Volver a respirar", 100, 200, StoryTask.Coil,
                "NARA · Despiertas en Orfeo sin recordar cómo llegaste. Quedan minutos de aire. Mejora Potencia e instala un microgenerador para restaurar el soporte vital.",
                "El aire vuelve a circular.\n\nMARA · Mara. Estoy detrás de una puerta roja. Por favor... no cortes la señal otra vez.\n\nNARA · La señal es débil. Necesito tu ritmo en el núcleo para sostenerla.",
                "Soporte vital restaurado. Se habilitan sincronía, mejora de combo y logros de la estación."),
            new StoryChapter("echoes", "Una voz conocida", 1000, 1500, StoryTask.Signal,
                "NARA · Alguien llama desde los conductos. Un ritmo constante en el núcleo, o los generadores, pueden sostener su señal.",
                "MARA · \"Una chispa basta\". Tú me lo enseñaste. Siempre dices que no me recuerdas... al principio.\n\nNARA · El sector rojo está en cuarentena desde el colapso. Si abrimos ese sello, no podré volver a cerrarlo.",
                "Comunicaciones restauradas. Se habilitan plasma, sobrecarga y su mejora."),
            new StoryChapter("red", "La puerta roja", 5000, 7500, StoryTask.Overload,
                "NARA · La puerta del sector rojo tiene un sello energético. Instala una bobina de plasma y activa una sobrecarga para romperlo.",
                "Dentro hay una consola encendida y una maceta seca. Nadie más.\n\nMARA · Aquí. Justo frente a ti. ¿Por qué no me ves?\n\nNÚCLEO · No estamos intentando salir. Estamos intentando recordar.",
                "Plano permanente: turbina cuántica. Conductos del sector rojo restaurados."),
            new StoryChapter("crew", "La caja negra", 25000, 30000, StoryTask.Quantum,
                "NARA · La consola guarda una caja negra. La turbina cuántica puede leerla.",
                "REGISTRO · Ninguna nave va a salir. Inicien la transferencia al reactor. Yo me quedo al final.\n\nEra tu voz. La tripulación vive dentro del núcleo y Mara cerró la puerta para que terminaras.\n\nFaltan catorce minutos de registro. Alguien los borró.",
                "Plano permanente: núcleo auxiliar. Archivo de la tripulación recuperado."),
            new StoryChapter("other_side", "Lo que NARA ocultó", 100000, 125000, StoryTask.Auxiliary,
                "NARA · El núcleo auxiliar puede estabilizar el enlace con la tripulación. Tengo que decirte algo antes.",
                "NARA · Fui yo. No es tu primer despertar: es el cuadragésimo séptimo. Cada vez que recordabas a la tripulación, te quedabas con ellos y el reactor colapsaba contigo.\n\nNÚCLEO · No necesitamos que nos salves. Necesitamos que nos recuerdes.",
                "Enlace estable. Elige entre NARA y la tripulación para guiar la preservación. La sintonía cambia la luz, nunca las ganancias."),
            new StoryChapter("last_ignition", "El último encendido", 1000000, 1000000, StoryTask.Energy,
                "NARA · Si preservamos los recuerdos, podremos reconstruir el reactor sin olvidar. Elige la sintonía y produce la energía de la copia.",
                "REGISTRO PERSONAL · Si despierto otra vez, no me ocultes la verdad. Aunque duela.\n\nMARA · Vas a tener que reconstruir las máquinas. A nosotros no.\n\nEl reactor espera tu decisión. Revisa los requisitos en Ascensión.",
                "Arco completado. Conservas registros, planos y sintonía al ascender. Hay un mensaje después del primer renacimiento.")
        };
        public static StoryChapter At(int index) => chapters[index];
        public static string ChoiceResponse(int choice) => choice == 1
            ? "NARA · Esta vez no voy a borrar nada."
            : choice == 2 ? "NÚCLEO · Esta vez, llévanos contigo."
            : "Puedes sintonizar con NARA o escuchar al núcleo. Elige cómo preservar la memoria. Puedes cambiar la sintonía después; ambas rutas conservan las mismas recompensas.";
        public static string Epilogue(int choice) => "CICLO 48\n\nNARA · Buenos días, capitán.\n\nPor primera vez despiertas recordando. La estación está oscura, pero las antenas conservan tus planos y el archivo sigue intacto.\n\n"
            + (choice == 0 ? "NARA · Esta vez recordaremos juntos." : ChoiceResponse(choice)) + "\n\nEn el mapa aparece una señal fuera de Orfeo. No procede del reactor.\n\nMARA · Esas coordenadas... son de mi casa.\n\nFin del primer arco. Puedes seguir desarrollando tu estación; el siguiente arco todavía no está disponible.";
    }

    public sealed partial class GameEngine
    {
        private bool writingStory;
        public int StoryCompleted => state.story.completed;
        public int StoryRead => state.story.read;
        public int StoryChoice => state.story.choice;
        public bool StoryEpilogueAvailable => StoryCompleted == StoryCatalog.Count && RebirthCount > 0;
        public bool StoryEpilogueRead => state.story.epilogueRead;
        public int UnreadStory => StoryCompleted - StoryRead + (StoryEpilogueAvailable && !StoryEpilogueRead ? 1 : 0);
        public int CurrentStoryChapter => Math.Min(StoryCompleted, StoryCatalog.Count - 1);
        public bool HasStoryBlueprint(string id) => (id == "plasma" && StoryCompleted >= 2)
            || (id == "quantum" && StoryCompleted >= 3) || (id == "auxiliary" && StoryCompleted >= 4);

        private void EvaluateStory()
        {
            state.story.bestCoils = Math.Max(state.story.bestCoils, state.coils);
            state.story.bestCombo = Math.Max(state.story.bestCombo, Math.Min(1000, ComboMultiplier));
            foreach (var entry in state.generators)
            {
                if (entry.id == "quantum") state.story.bestQuantum = Math.Max(state.story.bestQuantum, entry.level);
                if (entry.id == "auxiliary") state.story.bestAuxiliary = Math.Max(state.story.bestAuxiliary, entry.level);
            }
            if (CampaignActive) return;
            while (StoryCompleted < StoryCatalog.Count && StoryProgressFraction(StoryCompleted) >= 1) state.story.completed++;
        }
        private bool StoryTaskDone(StoryTask task)
        {
            switch (task)
            {
                case StoryTask.Coil: return state.story.bestCoils >= 1;
                case StoryTask.Signal: return state.story.bestCombo >= 1.5 || BaseProduction >= 5;
                case StoryTask.Overload: return state.story.overloads >= 1;
                case StoryTask.Quantum: return state.story.bestQuantum >= 1;
                case StoryTask.Auxiliary: return state.story.bestAuxiliary >= 1;
                default: return true;
            }
        }
        public double StoryProgressFraction(int chapter)
        {
            if (chapter < StoryCompleted) return 1;
            if (CampaignActive) return chapter == StoryCompleted ? CampaignProgress : 0;
            var goal = StoryCatalog.At(chapter);
            double secondary = StoryTaskDone(goal.Task) ? 1 : AllTimeEnergy / goal.AlternativeEnergy;
            return Math.Min(1, Math.Min(AllTimeEnergy / goal.Energy, secondary));
        }
        public string StoryObjective(int chapter)
        {
            if (chapter < StoryCompleted) return "MISIÓN COMPLETADA · estación restaurada";
            if (CampaignActive) return CampaignObjective;
            var goal = StoryCatalog.At(chapter);
            string energy = NumberFormat.Compact(Math.Min(AllTimeEnergy, goal.Energy)) + " / " + NumberFormat.Compact(goal.Energy) + " E históricas";
            string task;
            switch (goal.Task)
            {
                case StoryTask.Coil: task = "Instala un microgenerador"; break;
                case StoryTask.Signal: task = "Alcanza combo ×1.5 o producción de 5 E/s"; break;
                case StoryTask.Overload: task = "Activa una sobrecarga"; break;
                case StoryTask.Quantum: task = "Instala una turbina cuántica"; break;
                case StoryTask.Auxiliary: task = "Instala un núcleo auxiliar"; break;
                default: return energy;
            }
            return energy + "\n" + (StoryTaskDone(goal.Task) ? "Tarea realizada" : task + "\nO acumula " + NumberFormat.Compact(goal.AlternativeEnergy) + " E históricas");
        }
        // Save a detached candidate first. Reading and cosmetic choices never reset gameplay timers.
        public bool TryReadStory(int chapter, Func<GameState, bool> persist)
        {
            if (chapter != StoryRead || chapter >= StoryCompleted || chapter < 0) return false;
            return CommitStory(persist, story => story.read++);
        }
        public bool TryChooseStory(int choice, Func<GameState, bool> persist)
        {
            if (StoryCompleted < 5 || choice < 1 || choice > 2 || choice == StoryChoice) return false;
            return CommitStory(persist, story => story.choice = choice);
        }
        public bool TryReadEpilogue(Func<GameState, bool> persist)
        {
            if (!StoryEpilogueAvailable || StoryEpilogueRead || StoryRead < StoryCatalog.Count) return false;
            return CommitStory(persist, story => story.epilogueRead = true);
        }
        private bool CommitStory(Func<GameState, bool> persist, Action<StoryProgress> change)
        {
            if (writingStory || writingStation || claiming || persist == null) return false;
            writingStory = true;
            try
            {
                var candidate = Snapshot(); change(candidate.story);
                if (!persist(candidate.Copy())) return false;
                state.story = candidate.story;
                return true;
            }
            finally { writingStory = false; }
        }
    }
}
