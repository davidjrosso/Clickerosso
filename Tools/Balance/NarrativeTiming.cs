using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// Scene length as the player experiences it: shipped voice duration (+1.3 s, minimum 3.5 s) per visible
// line, or the director's subtitle estimate (text / 13 chars per second) while a clip awaits generation.
internal static class NarrativeTiming
{
    private static Dictionary<string, double> clips;
    private static JsonDocument catalog;
    public static int PendingClips { get; private set; }
    public static double SceneSeconds(string id, int choice)
    {
        if (clips == null)
        {
            using var voice = JsonDocument.Parse(File.ReadAllText("Docs/Cinematics/voice.json"));
            clips = voice.RootElement.GetProperty("clips").EnumerateArray()
                .ToDictionary(x => x.GetProperty("id").GetString(), x => x.GetProperty("seconds").GetDouble());
            catalog = JsonDocument.Parse(File.ReadAllText("Assets/ReactorDeCaos/Resources/Narrative.json"));
        }
        var scene = catalog.RootElement.GetProperty("scenes").EnumerateArray().First(x => x.GetProperty("id").GetString() == id);
        double total = 0;
        foreach (var beat in scene.GetProperty("beats").EnumerateArray())
        {
            string condition = beat.TryGetProperty("condition", out var c) ? c.GetString() : "";
            if ((condition == "nara" && choice != 1) || (condition == "crew" && choice != 2)) continue;
            string voice = beat.TryGetProperty("voice", out var v) ? v.GetString() : "";
            double length = beat.GetProperty("text").GetString().Length / 13d;
            if (!string.IsNullOrEmpty(voice) && clips.TryGetValue(voice, out double seconds)) length = seconds;
            else if (!string.IsNullOrEmpty(voice)) PendingClips++;
            total += Math.Max(3.5, length + 1.3);
        }
        return total;
    }
}
