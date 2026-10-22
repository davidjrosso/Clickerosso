using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReactorDeCaos;

internal static class RebirthSimulation
{
    public static bool Run(EconomyConfig config, string output)
    {
        var reports = new List<object>();
        bool passed = true;
        foreach (Specialization branch in new[] { Specialization.Tap, Specialization.Automation, Specialization.Overload })
        foreach (string profile in new[] { "active", "intermittent", "passive" })
        {
            bool passive = profile == "passive";
            double nominalTaps = profile == "active" ? 2 : passive ? 0 : 2d / 3;
            var game = new GameEngine(settings: config);
            var cycles = new List<object>();
            double previous = 0, firstCycle = 0, longestReturn = 0;
            int cycleTaps = 0, cyclePurchases = 0;
            for (int tick = 1; tick <= 72000 && game.RebirthCount < 6; tick++)
            {
                double time = tick * .1;
                game.Advance(.1);
                if (tick % 5 == 0 && (profile == "active" || (profile == "intermittent" && tick % 150 < 50)
                    || (passive && game.RebirthCount == 0 && cycleTaps < 25)))
                { game.Tap(); cycleTaps++; }
                if (game.Overload.Ready) game.ActivateOverload();
                if (tick % 10 == 0)
                {
                    ShopEntry best = null;
                    double score = 0;
                    foreach (var item in game.Shop)
                    {
                        if (!game.Unlocked(item.Id)) continue;
                        // Permanent blueprints expose late machines immediately after rebirth.
                        // Compare purchases reachable within one minute, not an unaffordable
                        // best-return machine that would idle a starter coil for an hour.
                        double income = game.BaseProduction + nominalTaps * game.TapPower
                            * (profile == "active" ? game.ComboCap : 1.25);
                        double wait = Math.Max(0, game.Cost(item.Id) - game.Energy);
                        if (wait > income * 60) continue;
                        double value = Program.Yield(game, config, item, nominalTaps, profile == "active" ? game.ComboCap : 1.25) / game.Cost(item.Id);
                        if (value > score) { best = item; score = value; }
                    }
                    if (best != null && game.Buy(best.Id)) cyclePurchases++;
                    if (!game.Snapshot().IsValid()) throw new Exception("Invalid rebirth simulation state.");
                }
                if (!game.CanRebirth) continue;
                double duration = time - previous;
                int reward = game.RebirthReward;
                int oldCount = game.RebirthCount;
                if (!game.TryPrepareRebirth(branch, reward, out var next)) throw new Exception("Rebirth candidate failed.");
                if (oldCount == 0) firstCycle = duration;
                else longestReturn = Math.Max(longestReturn, duration);
                cycles.Add(new { cycle = oldCount, durationSeconds = Math.Round(duration, 1), totalSeconds = time,
                    generated = game.Lifetime, reward, taps = cycleTaps, purchases = cyclePurchases,
                    skinUnlocked = RebirthCatalog.SkinName(next.Skin), permanentMultiplier = next.PermanentMultiplier });
                game = next;
                while (game.CanBuyMastery(branch)) game.BuyMastery(branch);
                previous = time; cycleTaps = cyclePurchases = 0;
            }
            // Baseline must reach the new goal; all subsequent loops should be shorter with permanent progression.
            bool budget = cycles.Count == 6 && firstCycle <= 1200 && longestReturn < firstCycle;
            passed &= budget;
            reports.Add(new { branch = branch.ToString(), profile,
                budgetPassed = budget, purchasePlanningHorizonSeconds = 60, firstRebirthSeconds = Math.Round(firstCycle, 1), longestReturnSeconds = Math.Round(longestReturn, 1),
                finalRebirths = game.RebirthCount, finalEssence = game.Essence, earnedEssence = game.EarnedEssence,
                mastery = game.MasteryLevel(branch), cycles });
        }
        File.WriteAllText(Path.Combine(output, "rebirth.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine(passed ? "Six-cycle rebirth simulations passed for all nine specialization/style combinations." : "Rebirth simulation budget FAILED.");
        return passed;
    }
}
