using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReactorDeCaos;

internal static class StationSimulation
{
    internal static bool Run(EconomyConfig config, string output)
    {
        using var voice = JsonDocument.Parse(File.ReadAllText("Docs/Cinematics/voice.json"));
        double SceneTime(string id) => voice.RootElement.GetProperty("clips").EnumerateArray()
            .Where(x => x.GetProperty("id").GetString().StartsWith(id + "_", StringComparison.Ordinal))
            .Sum(x => Math.Max(3.5, x.GetProperty("seconds").GetDouble() + 1.3));
        var reports = new List<object>(); bool allPassed = true;
        foreach (bool active in new[] { true, false })
        {
            var game = new GameEngine(GameEngine.NewCampaignState(), config);
            game.IgniteCampaign(); game.TryFinishNarrative(-1, true, _ => true);
            double time = SceneTime("intro"), remainingScene = 0, awayReward = 0;
            int scene = -1, taps = 1; bool departed = false;
            var events = new List<object>();
            for (int step = 0; step < 36000 && (game.StoryCompleted < 6 || game.StationMemories < 4); step++)
            {
                time += .1; game.Advance(.1);
                if (remainingScene > 0)
                {
                    remainingScene -= .1;
                    if (remainingScene <= 0)
                    {
                        bool saved = scene >= 10 ? game.TryFinishStationMemory(scene - 10, _ => true)
                            : game.TryFinishNarrative(scene, true, _ => true);
                        if (!saved) throw new Exception("Station simulation scene failed.");
                        events.Add(new { seconds = Math.Round(time, 1), scene, chapter = game.StoryCompleted, memories = game.StationMemories });
                    }
                    continue;
                }
                int chapter = game.StoryCompleted;
                bool guided = game.Level("coil") == 0 || game.Level("tap") == 0 || (chapter == 2 && game.Snapshot().story.overloads == 0);
                if (step % (active ? 5 : 10) == 0 && (active || guided)) { game.Tap(); taps++; }
                if (game.Overload.Ready) game.ActivateOverload();
                if (game.CampaignNeedsChoice) game.TryChooseStory(1, _ => true);
                if (game.CampaignRequirementsMet)
                {
                    game.TryBeginCampaignMission(_ => true); scene = chapter; remainingScene = SceneTime("chapter_" + chapter); continue;
                }
                if (game.PendingStationMemory >= 0)
                {
                    scene = 10 + game.PendingStationMemory; remainingScene = SceneTime("station_" + game.PendingStationMemory); continue;
                }
                if (game.CanStartExpedition && game.ExpeditionClaims == 0)
                {
                    game.TryStartExpedition(_ => true);
                    if (!departed)
                    {
                        long now = 1700000000 + (long)time;
                        game.TryCheckpointStation(now, _ => true);
                        game = new GameEngine(game.Snapshot(), config); // Simulate an actual close/reopen, no active frames during absence.
                        game.TryResumeStation(now + 240, _ => true); time += 240; departed = true;
                        awayReward = game.PendingOfflineEnergy;
                        game.TryClaimOffline(_ => true);
                        events.Add(new { seconds = Math.Round(time, 1), returnedAfterSeconds = 240, awayReward, droneReady = game.ExpeditionReady });
                    }
                }
                if (game.ExpeditionReady) game.TryClaimExpedition(_ => true);
                if (game.RestoredRooms < 3 && game.CanRestoreRoom(game.RestoredRooms))
                {
                    game.TryRestoreRoom(game.RestoredRooms, _ => true);
                    events.Add(new { seconds = Math.Round(time, 1), rooms = game.RestoredRooms, production = game.BaseProduction });
                    continue;
                }
                if (step % 10 == 0)
                {
                    string required = chapter == 0 ? game.Level("tap") == 0 ? "tap" : game.Level("coil") == 0 ? "coil" : null
                        : chapter == 2 && game.Level("plasma") == 0 ? "plasma"
                        : chapter == 3 && game.Level("quantum") == 0 ? "quantum"
                        : chapter == 4 && game.Level("auxiliary") == 0 ? "auxiliary" : null;
                    string best = required; double score = 0;
                    if (best == null)
                        foreach (var item in game.Shop)
                        {
                            if (!game.Unlocked(item.Id)) continue;
                            double value = Program.Yield(game, config, item, active ? 2 : 0, game.ComboAvailable ? game.ComboCap : 1) / game.Cost(item.Id);
                            if (value > score) { score = value; best = item.Id; }
                        }
                    if (best != null && game.CanBuy(best)) game.Buy(best);
                }
                if (!game.Snapshot().IsValid()) throw new Exception("Invalid station simulation state.");
            }
            bool finished = game.StoryCompleted == 6 && game.StationMemories == 4 && game.RestoredRooms == 3;
            bool preserved = game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next)
                && next.RestoredRooms == 3 && next.StationMemories == 4 && next.ExpeditionClaims == 1;
            bool passed = finished && preserved && departed && awayReward > 0 && time < 1800;
            allPassed &= passed;
            reports.Add(new { profile = active ? "active" : "mostly_automatic", passed, finished, preserved,
                totalWallSeconds = Math.Round(time, 1), taps, awayReward, events });
        }
        File.WriteAllText(Path.Combine(output, "station.json"), JsonSerializer.Serialize(new {
            assumptions = "Real campaign/economy; sequential room repairs, guided purchases, shipped dialogue durations. One 4-minute close/reopen after launching the drone. No Unity rendering.",
            passed = allPassed, profiles = reports
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine(allPassed ? "Two station journeys, returns, Mara endings and ascensions passed." : "Station simulation FAILED.");
        return allPassed;
    }
}
