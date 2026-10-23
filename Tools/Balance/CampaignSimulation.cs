using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReactorDeCaos;

internal static class CampaignSimulation
{
    internal static bool Run(EconomyConfig config, string output)
    {
        var reports = new List<object>(); bool passed = true;
        // Real shipped voice durations + reading margins, not a claim of audiovisual playback.
        using var speech = JsonDocument.Parse(File.ReadAllText("Docs/Cinematics/voice.json"));
        double SceneSeconds(string id) => speech.RootElement.GetProperty("clips").EnumerateArray()
            .Where(x => x.GetProperty("id").GetString().StartsWith(id + "_", StringComparison.Ordinal))
            .Sum(x => Math.Max(3.5, x.GetProperty("seconds").GetDouble() + 1.3));
        foreach (string profile in new[] { "active", "intermittent", "mostly_automatic" })
        {
            var game = new GameEngine(GameEngine.NewCampaignState(), config);
            game.IgniteCampaign();
            if (!game.TryFinishNarrative(-1, true, _ => true)) throw new Exception("Cannot finish campaign opening.");
            double opening = SceneSeconds("intro"), wall = opening, sceneLeft = 0, firstPurchase = -1;
            double productionAtEntry = 0; int taps = 1, overloads = 0, purchases = 0;
            int savedPhase = 0;
            var missions = new List<object>();
            var csv = new List<string> { "wall_seconds,chapter,item,level,balance,base_production,lifetime" };
            var firstMachines = new Dictionary<string, double>();
            for (int frame = 1; frame <= 36000 && game.StoryCompleted < StoryCatalog.Count; frame++)
            {
                wall = opening + frame / 10d; game.Advance(.1);
                if (sceneLeft > 0)
                {
                    sceneLeft -= .1;
                    if (sceneLeft <= 0)
                    {
                        int chapter = game.StoryCompleted;
                        if (!game.TryFinishNarrative(chapter, true, _ => true)) throw new Exception("Campaign resolution failed.");
                        missions.Add(new { chapter = chapter + 1, title = StoryCatalog.At(chapter).Title,
                            wallSeconds = Math.Round(wall, 1), lifetime = game.Lifetime, production = game.BaseProduction,
                            energyDuringScene = game.Energy - productionAtEntry });
                    }
                    continue;
                }
                int phase = game.StoryCompleted;
                bool guidedTap = profile == "mostly_automatic" && (game.Level("coil") == 0 || game.Level("tap") == 0
                    || (phase == 2 && game.Snapshot().story.overloads == 0));
                bool tapNow = profile == "active" ? frame % 5 == 0
                    : profile == "intermittent" ? frame % 5 == 0 && frame % 150 < 50
                    : guidedTap && frame % 10 == 0;
                if (tapNow) { game.Tap(); taps++; }
                if (game.Overload.Ready && game.ActivateOverload()) overloads++;
                if (frame % 10 == 0)
                {
                    // Follow the actual mission before optimizing further purchases.
                    string required = phase == 0 ? game.Level("tap") == 0 ? "tap" : game.Level("coil") == 0 ? "coil" : null
                        : phase == 2 && game.Level("plasma") == 0 ? "plasma"
                        : phase == 3 && game.Level("quantum") == 0 ? "quantum"
                        : phase == 4 && game.Level("auxiliary") == 0 ? "auxiliary" : null;
                    string best = required; double score = 0;
                    if (best == null)
                    foreach (var item in game.Shop)
                    {
                        if (!game.Unlocked(item.Id)) continue;
                        double nominal = profile == "active" ? 2 : profile == "intermittent" ? 2d / 3 : 0;
                        double value = Program.Yield(game, config, item, nominal, game.ComboAvailable ? game.ComboCap : 1) / game.Cost(item.Id);
                        if (value > score) { score = value; best = item.Id; }
                    }
                    if (best != null && game.CanBuy(best))
                    {
                        game.Buy(best); purchases++; if (firstPurchase < 0) firstPurchase = wall;
                        if (game.Shop.First(x => x.Id == best).IsGenerator && !firstMachines.ContainsKey(best)) firstMachines[best] = Math.Round(wall, 1);
                        csv.Add(string.Join(",", wall, phase, best, game.Level(best), game.Energy, game.BaseProduction, game.Lifetime));
                    }
                }
                if (game.CampaignNeedsChoice) game.TryChooseStory(2, _ => true);
                if (game.CampaignRequirementsMet)
                {
                    if (!game.TryBeginCampaignMission(_ => true)) throw new Exception("Cannot start ready mission.");
                    sceneLeft = SceneSeconds("chapter_" + phase); productionAtEntry = game.Energy;
                    // Reload the real pending checkpoint: it must stay pending, without granting its unlock.
                    game = new GameEngine(game.Snapshot(), config);
                    if (!game.CampaignMissionReady || game.StoryCompleted != phase) throw new Exception("Pending mission lost on reload.");
                }
                if (!game.Snapshot().IsValid() || game.StoryCompleted < savedPhase) throw new Exception("Invalid campaign state.");
                savedPhase = game.StoryCompleted;
            }
            bool completed = game.StoryCompleted == StoryCatalog.Count && game.CanRebirth;
            bool ascended = completed && game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next)
                && next.CampaignActive && next.StoryCompleted == 6 && next.StoryChoice == 2 && next.NarrativeIntroSeen;
            bool budget = completed && ascended && firstPurchase - opening <= 30 && wall <= 1800;
            passed &= budget;
            reports.Add(new { profile, completed, ascended, budgetPassed = budget, totalWallSeconds = Math.Round(wall, 1),
                openingSeconds = Math.Round(opening, 1), firstPurchaseWallSeconds = Math.Round(firstPurchase, 1),
                taps, overloads, purchases, firstMachines, missions });
            File.WriteAllLines(Path.Combine(output, "campaign-" + profile + ".csv"), csv);
        }
        File.WriteAllText(Path.Combine(output, "campaign.json"), JsonSerializer.Serialize(new {
            assumptions = "Real economy and campaign. Shipped voice duration plus 1.3 s per line; instantaneous mission/choice input. No Unity rendering. Automatic style taps only for initial repairs and the required door overload. Intro time included.",
            passed, profiles = reports
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine(passed ? "Three complete campaign paths and first ascensions passed." : "Campaign pacing FAILED; see campaign.json.");
        return passed;
    }
}
