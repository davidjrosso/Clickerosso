using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReactorDeCaos;

internal static class Program
{
    private static bool allBudgetsPassed = true;
    // This is a purchasing heuristic, not an alternative economy implementation.
    // Income, prices, effects, unlocks and state transitions all use the real GameEngine.
    internal static double Yield(GameEngine game, EconomyConfig config, ShopEntry item, double taps, double combo)
    {
        int level = game.Level(item.Id);
        if (level >= EconomyConfig.MaxLevel) return 0;
        double gain = game.EffectAtLevel(item.Id, level + 1) - game.EffectAtLevel(item.Id, level);
        double chargeSeconds = taps > 0 ? Math.Ceiling(config.overloadChargeRequired / config.chargePerTap) / taps : double.PositiveInfinity;
        double multiplier = 1 + (game.OverloadMultiplier - 1) * game.OverloadDuration / (game.OverloadDuration + chargeSeconds);
        if (item.IsGenerator) return gain * multiplier * (1 + (game.Specialization == Specialization.Tap
            ? taps * combo * game.TapProductionFraction * game.BranchMultiplier(Specialization.Tap) : 0));
        if (item.Id == "tap") return gain * taps * combo * multiplier;
        if (item.Id == "combo") return combo >= game.ComboCap ? gain * taps * game.TapPower * multiplier : 0;
        double nextDuration = game.EffectAtLevel(item.Id, level + 1);
        double nextMultiplier = 1 + (game.OverloadMultiplier - 1) * nextDuration / (nextDuration + chargeSeconds);
        return (nextMultiplier - multiplier) * (game.BaseProduction + taps * game.TapPower * combo);
    }

    private static object Run(string profile, EconomyConfig config, int seconds, string output)
    {
        var game = new GameEngine(settings: config);
        var purchases = new List<(double time, string id, bool generator, double cost)>();
        var firstGenerators = new Dictionary<string, double>();
        var unlocked = new Dictionary<string, double>();
        var milestones = new Dictionary<string, double>();
        var levels = new Dictionary<string, int>();
        var growth = new List<object>();
        var csv = new List<string> { "seconds,item,level,price,balance,base_production,lifetime" };
        int taps = 0, activations = 0;
        double boostedSeconds = 0;
        double nominalTaps = profile == "active" ? 2 : profile == "intermittent" ? 2d / 3 : 0;
        for (int frame = 1; frame <= seconds * 10; frame++)
        {
            double time = frame / 10d;
            boostedSeconds += Math.Min(.1, game.Overload.Remaining);
            game.Advance(.1);
            bool tapNow = frame % 5 == 0 && (profile == "active"
                || (profile == "intermittent" && frame % 150 < 50)
                || (profile == "passive" && taps < 25));
            if (tapNow) { game.Tap(); taps++; }
            if (game.Overload.Ready && game.ActivateOverload()) activations++;
            if (frame % 10 == 0)
            {
                double comboEstimate = profile == "active" ? game.ComboCap : 1.25;
                ShopEntry best = null;
                double score = 0;
                // Save for the best unlocked marginal return, not merely the cheapest affordable row.
                foreach (var item in game.Shop)
                {
                    if (!game.Unlocked(item.Id)) continue;
                    double next = Yield(game, config, item, nominalTaps, comboEstimate) / game.Cost(item.Id);
                    if (next > score) { score = next; best = item; }
                }
                if (best != null && game.CanBuy(best.Id))
                {
                    double price = game.Cost(best.Id);
                    if (!game.Buy(best.Id)) throw new Exception("Affordable purchase failed.");
                    purchases.Add((time, best.Id, best.IsGenerator, price));
                    if (best.IsGenerator && !firstGenerators.ContainsKey(best.Id)) firstGenerators[best.Id] = time;
                    csv.Add(string.Join(",", time, best.Id, game.Level(best.Id), price.ToString("R"),
                        game.Energy.ToString("R"), game.BaseProduction.ToString("R"), game.Lifetime.ToString("R")));
                }
                if (!game.Snapshot().IsValid()) throw new Exception("Invalid state at " + profile + ":" + time);
            }
            foreach (var item in game.Shop)
                if (item.IsGenerator && game.Unlocked(item.Id) && !unlocked.ContainsKey(item.Id)) unlocked[item.Id] = time;
            while (game.TryTakeMilestone(out var milestone)) milestones[milestone.id] = time;
            if (frame % 600 == 0) growth.Add(new { seconds = time, balance = game.Energy, lifetime = game.Lifetime, baseProduction = game.BaseProduction });
        }
        var gaps = new List<double>();
        double previous = 0;
        foreach (var purchase in purchases) { gaps.Add(purchase.time - previous); previous = purchase.time; }
        gaps.Add(seconds - previous); gaps.Sort();
        var earlyTimes = new[] { 0d }.Concat(purchases.Where(x => x.time <= 300).Select(x => x.time)).Concat(new[] { 300d }).ToArray();
        double earlyGap = earlyTimes.Zip(earlyTimes.Skip(1), (a, b) => b - a).Max();
        bool pacingPassed = purchases.Count > 0 && purchases[0].time <= 20 && earlyGap <= 60 && gaps.Max() <= 90
            && firstGenerators.TryGetValue("auxiliary", out double auxiliaryTime) && auxiliaryTime <= 420;
        allBudgetsPassed &= pacingPassed;
        foreach (var item in game.Shop) levels[item.Id] = game.Level(item.Id);
        File.WriteAllLines(Path.Combine(output, profile + "-purchases.csv"), csv);
        double? firstUpgrade = purchases.Where(x => !x.generator).Select(x => (double?)x.time).FirstOrDefault();
        return new {
            profile, seconds, taps, pacingPassed, purchases = purchases.Count,
            firstPurchaseSeconds = purchases.Count == 0 ? (double?)null : purchases[0].time,
            firstUpgradeSeconds = firstUpgrade, firstGeneratorPurchaseSeconds = firstGenerators, generatorUnlockSeconds = unlocked,
            maxPurchaseGapSeconds = gaps.Max(), p95PurchaseGapSeconds = gaps[(int)Math.Floor((gaps.Count - 1) * .95)],
            maxFirstFiveMinuteGapSeconds = earlyGap,
            activations, boostedSeconds, finalEnergy = game.Energy, lifetimeEnergy = game.Lifetime,
            baseProduction = game.BaseProduction, levels, milestones, growth
        };
    }

    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (args.Length != 2) throw new ArgumentException("Usage: Balance economy.json output-directory");
        var config = JsonSerializer.Deserialize<EconomyConfig>(File.ReadAllText(args[0]),
            new JsonSerializerOptions { IncludeFields = true });
        config.Validate();
        string output = args[1];
        Directory.CreateDirectory(output);
        var results = new List<object>();
        foreach (string profile in new[] { "active", "intermittent", "passive" })
            results.Add(Run(profile, config, 1800, output));
        string json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "results.json"), json + "\n");
        var efficiency = new List<string> { "context,style,item,price,estimated_extra_energy_per_second,payback_seconds" };
        foreach (string context in new[] { "early", "automated" })
        foreach (double taps in new[] { 0d, 2d })
        {
            GameState state = context == "early"
                ? new GameState { energy = 5000, lifetimeEnergy = 5000, tapLevel = 3, coils = 5, plasma = 2 }
                : new GameState { energy = 25000, lifetimeEnergy = 25000, tapLevel = 8, comboLevel = 3, coils = 10, plasma = 5,
                    generators = new[] { new GeneratorLevel { id = "quantum", level = 2 }, new GeneratorLevel { id = "auxiliary", level = 1 } } };
            var game = new GameEngine(state, config);
            foreach (var item in game.Shop)
            {
                if (!game.Unlocked(item.Id)) continue;
                double gain = Yield(game, config, item, taps, game.ComboCap);
                efficiency.Add(string.Join(",", context, taps == 0 ? "passive" : "active", item.Id,
                    game.Cost(item.Id), gain.ToString("R"), gain > 0 ? (game.Cost(item.Id) / gain).ToString("R") : "not-applicable"));
            }
        }
        File.WriteAllLines(Path.Combine(output, "efficiency.csv"), efficiency);
        Console.WriteLine("Three 30-minute simulations completed using the actual configured GameEngine.");
        Console.WriteLine("Results: " + Path.Combine(output, "results.json"));
        Console.WriteLine(allBudgetsPassed ? "All phase-2 pacing budgets passed."
            : "Pacing budgets FAILED. Inspect the saved results and purchase CSVs before retuning.");
        bool rebirthPassed = RebirthSimulation.Run(config, output);
        return allBudgetsPassed && rebirthPassed ? 0 : 1;
    }
}
