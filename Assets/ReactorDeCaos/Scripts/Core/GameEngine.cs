using System;
using System.Collections.Generic;

namespace ReactorDeCaos
{
    public sealed partial class GameEngine
    {
        private readonly GameState state;
        private readonly EconomyConfig config;
        private readonly HashSet<string> completed;
        private readonly Queue<MilestoneDefinition> rewards = new Queue<MilestoneDefinition>();
        private readonly Dictionary<string, UpgradeDefinition> upgrades = new Dictionary<string, UpgradeDefinition>();
        private readonly Dictionary<string, GeneratorDefinition> generators = new Dictionary<string, GeneratorDefinition>();
        public IReadOnlyList<ShopEntry> Shop { get; }
        public ComboSystem Combo { get; }
        public OverloadSystem Overload { get; }
        public double Energy { get { return state.energy; } }
        public double Lifetime { get { return state.lifetimeEnergy; } }
        public int Stage { get; private set; }
        public double NextMilestone { get { return NextGoal()?.target ?? Lifetime; } }
        public int CompletedMilestoneCount { get { return completed.Count; } }
        public double BaseProduction
        {
            get
            {
                double total = 0;
                foreach (var generator in config.generators) total += generator.Production(Level(generator.id));
                return total * PermanentMultiplier * BranchMultiplier(Specialization.Automation);
            }
        }
        public double Production { get { return BaseProduction * (Overload.Active ? OverloadMultiplier : 1); } }
        public double TapPower { get { return TapPowerAt(state.tapLevel); } }
        public double ComboMultiplier { get { return Combo.Multiplier(state.comboLevel); } }
        public double ComboCap { get { return config.ComboCap(state.comboLevel); } }
        public double OverloadMultiplier { get { return config.overloadMultiplier * BranchMultiplier(Specialization.Overload); } }
        public double OverloadDuration { get { return config.overloadSeconds + state.overloadLevel * config.overloadSecondsPerLevel; } }
        public double CurrentTapGain { get { return TapPower * ComboMultiplier * (Overload.Active ? OverloadMultiplier : 1); } }
        public bool EnergyLimitReached { get { return Energy >= EconomyConfig.MaxEnergy && state.energyRemainder >= 0; } }

        public GameEngine(GameState initial = null, EconomyConfig settings = null)
        {
            config = settings == null ? new EconomyConfig() : settings.Copy();
            config.Validate();
            Combo = new ComboSystem(config);
            Overload = new OverloadSystem(config.overloadChargeRequired);
            state = initial == null ? new GameState() : initial.Copy();
            if (!state.IsValid()) throw new ArgumentException("Invalid initial state.");
            if (state.version == 1) { state.version = 2; state.rebirth = new RebirthProgress(); }
            completed = new HashSet<string>(state.completedMilestones);
            var knownMilestones = new HashSet<string>(completed);
            foreach (var goal in config.milestones) knownMilestones.Add(goal.id);
            var knownGenerators = new HashSet<string>();
            foreach (var entry in state.generators) knownGenerators.Add(entry.id);
            foreach (var entry in config.generators)
                if (entry.id != "coil" && entry.id != "plasma") knownGenerators.Add(entry.id);
            if (knownMilestones.Count > 128 || knownGenerators.Count > 32)
                throw new ArgumentException("Catalog plus retained save IDs exceed capacity; migrate retired IDs first.");
            var shop = new List<ShopEntry>();
            foreach (var upgrade in config.upgrades)
            {
                string id = EconomyConfig.ItemId(upgrade.type);
                upgrades.Add(id, upgrade);
                string unit = upgrade.type == Purchase.TapPower ? "E / toque"
                    : upgrade.type == Purchase.ComboCapacity ? "combo máximo" : "s de sobrecarga";
                shop.Add(new ShopEntry(id, upgrade.name, upgrade.description, unit, false));
            }
            foreach (var generator in config.generators)
            {
                generators.Add(generator.id, generator);
                shop.Add(new ShopEntry(generator.id, generator.name, generator.description, "E / s", true));
            }
            Shop = shop.AsReadOnly();
            // Catch up legacy saves without replaying celebrations at every load.
            EvaluateMilestones(false);
        }

        public GameState Snapshot() { return state.Copy(); }

        public double Tap()
        {
            double multiplier = Combo.Tap(state.comboLevel);
            double amount = TapPower * multiplier * (Overload.Active ? OverloadMultiplier : 1);
            Overload.Tap(config.chargePerTap);
            return Credit(amount);
        }

        public bool ActivateOverload() { return Overload.Activate(OverloadDuration); }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 86400)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Combo.Advance(seconds);
            double boosted = Overload.Advance(seconds);
            Credit(BaseProduction * (seconds + boosted * (OverloadMultiplier - 1)));
        }

        public int Level(Purchase item) { return Level(EconomyConfig.ItemId(item)); }
        public double Cost(Purchase item) { return Cost(EconomyConfig.ItemId(item)); }
        public bool Unlocked(Purchase item) { return Unlocked(EconomyConfig.ItemId(item)); }
        public bool CanBuy(Purchase item) { return CanBuy(EconomyConfig.ItemId(item)); }
        public bool Buy(Purchase item) { return Buy(EconomyConfig.ItemId(item)); }

        public int Level(string id)
        {
            RequireItem(id);
            switch (id)
            {
                case "tap": return state.tapLevel;
                case "combo": return state.comboLevel;
                case "overload": return state.overloadLevel;
                case "coil": return state.coils;
                case "plasma": return state.plasma;
                default:
                    foreach (var entry in state.generators) if (entry.id == id) return entry.level;
                    return 0;
            }
        }

        public double Cost(string id)
        {
            RequireItem(id);
            PriceCurve price = upgrades.TryGetValue(id, out var upgrade) ? upgrade.price : generators[id].price;
            return Math.Ceiling(price.initial * Math.Pow(price.growth, Level(id)));
        }

        public bool Unlocked(string id)
        {
            RequireItem(id);
            return !generators.TryGetValue(id, out var generator) || string.IsNullOrEmpty(generator.unlockMilestone)
                || completed.Contains(generator.unlockMilestone);
        }

        public string UnlockDescription(string id)
        {
            RequireItem(id);
            if (!generators.TryGetValue(id, out var generator) || string.IsNullOrEmpty(generator.unlockMilestone)) return "";
            MilestoneDefinition goal = config.Milestone(generator.unlockMilestone);
            return goal.goal == MilestoneGoal.LifetimeEnergy ? NumberFormat.Compact(goal.target) + " E producidos"
                : config.Generator(goal.generatorId).name + " · nivel " + NumberFormat.Stat(goal.target);
        }

        public bool CanBuy(string id)
        {
            double cost = Cost(id);
            return Unlocked(id) && Level(id) < EconomyConfig.MaxLevel
                && (Energy > cost || (Energy == cost && state.energyRemainder >= 0));
        }

        public bool Buy(string id)
        {
            if (!CanBuy(id)) return false;
            // Allocate any new progress entry before spending, never leave a partial purchase.
            GeneratorLevel additional = null;
            if (generators.ContainsKey(id) && id != "coil" && id != "plasma")
            {
                foreach (var entry in state.generators) if (entry.id == id) additional = entry;
                if (additional == null)
                {
                    if (state.generators.Length >= 32) return false;
                    additional = new GeneratorLevel { id = id };
                    Array.Resize(ref state.generators, state.generators.Length + 1);
                    state.generators[state.generators.Length - 1] = additional;
                }
            }
            CompensatedSum.Add(ref state.energy, ref state.energyRemainder, -Cost(id));
            switch (id)
            {
                case "tap": state.tapLevel++; break;
                case "combo": state.comboLevel++; break;
                case "overload": state.overloadLevel++; break;
                case "coil": state.coils++; break;
                case "plasma": state.plasma++; break;
                default: additional.level++; break;
            }
            EvaluateMilestones(true);
            return true;
        }

        private void RequireItem(string id)
        {
            if (id == null || (!upgrades.ContainsKey(id) && !generators.ContainsKey(id)))
                throw new ArgumentException("Unknown shop item: " + id);
        }

        public bool HasMilestone(string id) { return completed.Contains(id); }

        public MilestoneDefinition NextGoal()
        {
            foreach (var goal in config.milestones)
                if (!completed.Contains(goal.id)) return goal.Copy();
            return null;
        }

        public double GoalProgress(MilestoneDefinition goal)
        {
            return goal.goal == MilestoneGoal.LifetimeEnergy ? Lifetime : Level(goal.generatorId);
        }

        public bool TryTakeMilestone(out MilestoneDefinition reward)
        {
            reward = rewards.Count == 0 ? null : rewards.Dequeue();
            return reward != null;
        }

        private void EvaluateMilestones(bool notify)
        {
            foreach (var goal in config.milestones)
            {
                if (!completed.Contains(goal.id) && GoalProgress(goal) >= goal.target)
                {
                    if (state.completedMilestones.Length >= 128)
                        throw new InvalidOperationException("Saved milestone capacity exceeded. Migrate retired IDs before adding more.");
                    completed.Add(goal.id);
                    Array.Resize(ref state.completedMilestones, state.completedMilestones.Length + 1);
                    state.completedMilestones[state.completedMilestones.Length - 1] = goal.id;
                    if (notify) rewards.Enqueue(goal.Copy());
                }
                if (completed.Contains(goal.id)) Stage = Math.Max(Stage, goal.reactorStage);
            }
        }

        private double Credit(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0)
                throw new ArithmeticException("Invalid energy reward.");
            double accepted = Math.Min(amount, Math.Max(0, (EconomyConfig.MaxEnergy - Energy) - state.energyRemainder));
            CompensatedSum.Add(ref state.energy, ref state.energyRemainder, accepted);
            double historical = Math.Min(accepted,
                Math.Max(0, (EconomyConfig.MaxEnergy - Lifetime) - state.lifetimeRemainder));
            CompensatedSum.Add(ref state.lifetimeEnergy, ref state.lifetimeRemainder, historical);
            EvaluateMilestones(true);
            return accepted;
        }

        // UI reads authoritative effects; no formulas are duplicated in presentation.
        public double EffectAtLevel(Purchase item, int level) { return EffectAtLevel(EconomyConfig.ItemId(item), level); }
        public double EffectAtLevel(string id, int level)
        {
            RequireItem(id);
            if (level < 0 || level > EconomyConfig.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            switch (id)
            {
                case "tap": return TapPowerAt(level);
                case "combo": return config.ComboCap(level);
                case "overload": return config.overloadSeconds + level * config.overloadSecondsPerLevel;
                default: return generators[id].Production(level) * PermanentMultiplier * BranchMultiplier(Specialization.Automation);
            }
        }
    }
}
