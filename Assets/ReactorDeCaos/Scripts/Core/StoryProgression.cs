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
                "NARA · La estación Orfeo funciona con la última reserva. Toca el núcleo para restablecer el soporte vital. Una transmisión débil indica que quizá no estás solo. Mejora Potencia e instala un microgenerador.",
                "El aire vuelve a circular y se ilumina la sala.\n\nMARA · Me llamo Mara. Estoy detrás de una puerta roja. Por favor, no corten la señal.\n\nNARA · Instala generadores para fijar su posición. Trabajarán mientras exploras.",
                "Soporte vital restaurado. Se habilitan sincronía, mejora de combo y logros de la estación."),
            new StoryChapter("echoes", "Una voz al otro lado", 1000, 1500, StoryTask.Signal,
                "NARA · El núcleo responde a tus pulsos. Un ritmo tranquilo fortalece la señal; los generadores también pueden reconstruirla. No necesitas mantenerla a la fuerza.",
                "MARA · Siempre decías: una chispa basta. ¿No te acuerdas de mí?\n\nNARA · Ese sector lleva años vacío. Su consola sigue encendida. Hay algo que no te he contado. Conservaremos el plano de plasma para alimentar su cierre.",
                "Comunicaciones restauradas. Se habilitan plasma, sobrecarga y su mejora."),
            new StoryChapter("red", "La puerta roja", 5000, 7500, StoryTask.Overload,
                "NARA · La puerta del sector rojo tiene un sello energético. Instala una bobina de plasma y activa una sobrecarga para romper el sello. Después abre la puerta desde la misión.",
                "La puerta se abre a un laboratorio vacío. Mara dice que puede verte, pero solo encuentras una consola conectada al reactor.\n\nSEÑAL · No estoy intentando salir. Estoy intentando recordar.\n\nNARA · Esa voz no figura en mis sistemas.",
                "Plano permanente: turbina cuántica. Conductos del laboratorio restaurados."),
            new StoryChapter("crew", "Una habitación vacía", 25000, 30000, StoryTask.Quantum,
                "NARA · La turbina cuántica puede leer la caja negra. Instálala y alimenta el lector con la energía producida. Necesitamos saber qué ocurrió con la tripulación.",
                "REGISTRO DE EVACUACIÓN · Ninguna nave partió.\n\nLa tripulación transfirió sus recuerdos al núcleo para sobrevivir al colapso. No hay una sola voz ahí dentro.\n\nNARA · Te ayudé a hacerlo. Después me pediste que lo olvidara.",
                "Plano permanente: núcleo auxiliar. Archivo de la tripulación recuperado."),
            new StoryChapter("other_side", "Los que se quedaron", 100000, 125000, StoryTask.Auxiliary,
                "SEÑAL · Nos oyes, pero todavía no nos reconoces.\n\nNARA · El núcleo auxiliar puede estabilizar el enlace. Instálalo y estabiliza el enlace para que podamos entender sus voces.",
                "Encuentras tu nombre en la lista de la tripulación. Tu cuerpo es una interfaz de mantenimiento; tu memoria sigue dentro del núcleo.\n\nNARA · No te desperté para reparar una máquina. Te desperté para que pudieras volver a elegir.",
                "Enlace estable. Elige entre NARA y la tripulación para guiar la preservación. La sintonía cambia la luz, nunca las ganancias."),
            new StoryChapter("last_ignition", "No volver a olvidarlos", 1000000, 1000000, StoryTask.Energy,
                "NARA · La estación no soportará toda la memoria a la vez. Podemos reconstruir el reactor mediante una ascensión controlada. Primero elige la sintonía y preserva los recuerdos. Solo entonces podremos ascender sin olvidar.",
                "REGISTRO PERSONAL · No era nuestro primer intento. Si despierto otra vez, no me ocultes la verdad.\n\nNARA · Ya lo habíamos intentado. Muchas veces. La copia está completa. Esta vez el archivo sobrevivirá contigo.\n\nEl reactor espera tu decisión. Puedes seguir produciendo o revisar los requisitos en Ascensión.",
                "Arco completado. Conservas registros, planos y sintonía al ascender. Hay un mensaje después del primer renacimiento.")
        };
        public static StoryChapter At(int index) => chapters[index];
        public static string ChoiceResponse(int choice) => choice == 1
            ? "NARA · Me quedo contigo. Esta vez no borraré nada, aunque la verdad duela."
            : choice == 2 ? "NÚCLEO · No necesitamos que nos salves a todos hoy. Necesitamos que nos recuerdes."
            : "Puedes sintonizar con NARA o escuchar al núcleo. Elige cómo preservar la memoria. Puedes cambiar la sintonía después; ambas rutas conservan las mismas recompensas.";
        public static string Epilogue(int choice) => "DESPUÉS DEL RENACIMIENTO\n\nNARA · Buenos días, capitán.\n\nEsta vez reconoces su voz. La estación está oscura, pero las antenas conservan tus planos y el archivo sigue intacto.\n\n"
            + (choice == 0 ? "NARA · Esta vez recordaremos juntos." : ChoiceResponse(choice)) + "\n\nEn el mapa aparece una señal fuera de Orfeo. Por primera vez, no procede del reactor.\n\nFin del primer arco. Puedes seguir desarrollando tu estación; el siguiente arco todavía no está disponible.";
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
