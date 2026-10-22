using System;

namespace ReactorDeCaos
{
    // Optional in v1/v2 saves. Records knowledge, not another currency or a second economy.
    [Serializable]
    public sealed class StoryProgress
    {
        public int completed, read, choice, overloads, bestCoils, bestQuantum, bestAuxiliary;
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
            && (narrative == null || narrative.IsValid(completed));
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
            new StoryChapter("spark", "La primera chispa", 100, 200, StoryTask.Coil,
                "NARA · La estación Orfeo funciona con la última reserva. Toca el núcleo para restablecer el soporte vital. Una mujer llamada Mara pide ayuda desde el sector rojo.",
                "El aire vuelve a circular y se ilumina la sala.\n\nMARA · Hay una foto pegada a mi consola. Creo que estás en ella.\n\nNARA · Instala generadores para fijar su posición. Trabajarán mientras exploras.",
                "Iluminación de emergencia restaurada. Se abre el archivo de comunicaciones."),
            new StoryChapter("echoes", "Ecos en los conductos", 1000, 1500, StoryTask.Signal,
                "NARA · El núcleo responde a tus pulsos. Un ritmo tranquilo fortalece la señal; los generadores también pueden reconstruirla. No necesitas mantenerla a la fuerza.",
                "MARA · Siempre decías: una chispa basta. ¿No te acuerdas de mí?\n\nNARA · Ese sector lleva años vacío. La transmisión tiene fecha de mañana. Conservaremos el plano de plasma para alimentar su cierre.",
                "Plano permanente: bobina de plasma. Antenas de comunicación en línea."),
            new StoryChapter("red", "Protocolo rojo", 5000, 7500, StoryTask.Overload,
                "NARA · La puerta del sector rojo tiene un sello energético. Una sobrecarga puede abrirlo. Si prefieres esperar, la reserva automática alimentará el cierre sin riesgo.",
                "La puerta se abre a un laboratorio vacío. Mara dice que puede verte, pero solo encuentras una consola conectada al reactor.\n\nSEÑAL · No estoy intentando salir. Estoy intentando recordar.\n\nNARA · Esa voz no figura en mis sistemas.",
                "Plano permanente: turbina cuántica. Conductos del laboratorio restaurados."),
            new StoryChapter("crew", "La tripulación ausente", 25000, 30000, StoryTask.Quantum,
                "NARA · La turbina cuántica puede leer la caja negra. También puedo reconstruirla acumulando energía. Necesitamos saber qué ocurrió con la tripulación.",
                "REGISTRO DE EVACUACIÓN · Ninguna nave partió.\n\nLa tripulación transfirió sus recuerdos al núcleo para sobrevivir al colapso. No hay una sola voz ahí dentro.\n\nNARA · Te ayudé a hacerlo. Después me pediste que lo olvidara.",
                "Plano permanente: núcleo auxiliar. Archivo de la tripulación recuperado."),
            new StoryChapter("other_side", "El otro lado", 100000, 125000, StoryTask.Auxiliary,
                "SEÑAL · Nos oyes, pero todavía no nos reconoces.\n\nNARA · El núcleo auxiliar puede estabilizar el enlace. Antes de continuar, tendrás que decidir a quién escuchar.",
                "Encuentras tu nombre en la lista de la tripulación. Tu cuerpo es una interfaz de mantenimiento; tu memoria sigue dentro del núcleo.\n\nNARA · No te desperté para reparar una máquina. Te desperté para que pudieras volver a elegir.",
                "Sintonía del enlace: NARA o el núcleo. Cambia el diálogo y la luz, nunca las ganancias."),
            new StoryChapter("last_ignition", "El último encendido", 1000000, 1000000, StoryTask.Energy,
                "NARA · La estación no soportará toda la memoria a la vez. Podemos reconstruir el reactor mediante una ascensión controlada. Perderemos las instalaciones del ciclo, pero conservaremos la esencia y lo aprendido.",
                "REGISTRO PERSONAL · No era nuestro primer intento. Si despierto otra vez, no me ocultes la verdad.\n\nNARA · Ya lo habíamos intentado. Muchas veces. Esta vez el archivo sobrevivirá contigo.\n\nEl reactor espera tu decisión. Puedes seguir produciendo o revisar los requisitos en Ascensión.",
                "Arco completado. Conservas registros, planos y sintonía al ascender. Hay un mensaje después del primer renacimiento.")
        };
        public static StoryChapter At(int index) => chapters[index];
        public static string ChoiceResponse(int choice) => choice == 1
            ? "NARA · Me quedo contigo. Esta vez no borraré nada, aunque la verdad duela."
            : choice == 2 ? "NÚCLEO · No necesitamos que nos salves a todos hoy. Necesitamos que nos recuerdes."
            : "Puedes sintonizar con NARA o escuchar al núcleo. La elección es opcional y puedes cambiarla; ambas rutas conservan las mismas recompensas.";
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
            var goal = StoryCatalog.At(chapter);
            double secondary = StoryTaskDone(goal.Task) ? 1 : AllTimeEnergy / goal.AlternativeEnergy;
            return Math.Min(1, Math.Min(AllTimeEnergy / goal.Energy, secondary));
        }
        public string StoryObjective(int chapter)
        {
            if (chapter < StoryCompleted) return "OBJETIVOS COMPLETADOS · recompensa permanente obtenida";
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
            if (writingStory || persist == null) return false;
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
