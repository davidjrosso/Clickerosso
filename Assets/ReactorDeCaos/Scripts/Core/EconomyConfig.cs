using System;
using System.Collections.Generic;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class PriceCurve
    {
        public double initial;
        public double growth;
        public PriceCurve(double initial, double growth) { this.initial = initial; this.growth = growth; }
        public PriceCurve Copy() { return new PriceCurve(initial, growth); }
        public void Validate()
        {
            EconomyConfig.Positive(initial);
            if (double.IsNaN(growth) || growth < 1 || growth > 10)
                throw new ArgumentOutOfRangeException(nameof(growth), "Price growth must be between 1 and 10.");
        }
    }

    [Serializable]
    public sealed class EconomyConfig
    {
        public RebirthConfig rebirth = new RebirthConfig();
        public const int MaxLevel = 100;
        public const double MaxEnergy = 1e150;
        public double baseTapPower = 1;
        public double tapPowerPerLevel = 1;
        public double comboWindow = 1.5;
        public double comboDecayInterval = .75;
        public int comboTapsPerStep = 5;
        public double comboStepMultiplier = .25;
        public double comboBaseCap = 3;
        public double comboCapPerLevel = .25;
        public double overloadChargeRequired = 100;
        public double chargePerTap = 4;
        public double overloadMultiplier = 3;
        public double overloadSeconds = 10;
        public double overloadSecondsPerLevel = 1;
        public UpgradeDefinition[] upgrades = ProgressionDefaults.Upgrades();
        public GeneratorDefinition[] generators = ProgressionDefaults.Generators();
        public MilestoneDefinition[] milestones = ProgressionDefaults.Milestones();

        // Source-compatible aliases for phase-1 callers. Serialized data now lives in the catalog.
        public double coilProduction { get { return Generator("coil").baseProduction; } set { Generator("coil").baseProduction = value; } }
        public double plasmaProduction { get { return Generator("plasma").baseProduction; } set { Generator("plasma").baseProduction = value; } }
        public double plasmaUnlock { get { return Milestone("plasma").target; } set { Milestone("plasma").target = value; } }
        public double finalMilestone { get { return Milestone("singularity").target; } set { Milestone("singularity").target = value; } }
        public PriceCurve tapPrice { get { return Upgrade(Purchase.TapPower).price; } set { Upgrade(Purchase.TapPower).price = value; } }
        public PriceCurve comboPrice { get { return Upgrade(Purchase.ComboCapacity).price; } set { Upgrade(Purchase.ComboCapacity).price = value; } }
        public PriceCurve overloadPrice { get { return Upgrade(Purchase.OverloadDuration).price; } set { Upgrade(Purchase.OverloadDuration).price = value; } }
        public PriceCurve coilPrice { get { return Generator("coil").price; } set { Generator("coil").price = value; } }
        public PriceCurve plasmaPrice { get { return Generator("plasma").price; } set { Generator("plasma").price = value; } }

        public EconomyConfig Copy()
        {
            var copy = (EconomyConfig)MemberwiseClone();
            copy.rebirth = (rebirth ?? new RebirthConfig()).Copy();
            copy.upgrades = upgrades == null ? null : Array.ConvertAll(upgrades, x => x?.Copy());
            copy.generators = generators == null ? null : Array.ConvertAll(generators, x => x?.Copy());
            copy.milestones = milestones == null ? null : Array.ConvertAll(milestones, x => x?.Copy());
            return copy;
        }

        internal static void Positive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1e12)
                throw new ArgumentOutOfRangeException(nameof(value), "Economy values must be finite and positive.");
        }

        public void Validate()
        {
            if (rebirth == null) throw new ArgumentException("Missing rebirth configuration.");
            rebirth.Validate();
            foreach (double value in new[] { baseTapPower, tapPowerPerLevel, comboWindow, comboDecayInterval,
                comboStepMultiplier, comboBaseCap, comboCapPerLevel, overloadChargeRequired, chargePerTap,
                overloadMultiplier, overloadSeconds, overloadSecondsPerLevel })
                Positive(value);
            if (overloadMultiplier < 1 || chargePerTap > overloadChargeRequired
                || comboBaseCap < 1 || comboTapsPerStep < 1
                || comboTapsPerStep > 10000
                || Math.Ceiling((ComboCap(MaxLevel) - 1) / comboStepMultiplier) * comboTapsPerStep > 1000000)
                throw new ArgumentException("Invalid overload or milestone configuration.");
            if (upgrades == null || upgrades.Length != 3 || generators == null || generators.Length < 2
                || generators.Length > 32 || milestones == null || milestones.Length > 64)
                throw new ArgumentException("Invalid catalog size.");
            var ids = new HashSet<string>();
            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade == null || (int)upgrade.type < 0 || (int)upgrade.type > 2
                    || !ids.Add(ItemId(upgrade.type))) throw new ArgumentException("Invalid or duplicate upgrade.");
                Text(upgrade.name, 80); Text(upgrade.description, 160);
                if (upgrade.price == null) throw new ArgumentException("Missing upgrade price.");
                upgrade.price.Validate();
            }
            foreach (GeneratorDefinition generator in generators)
            {
                if (generator == null || !ValidId(generator.id) || !ids.Add(generator.id))
                    throw new ArgumentException("Invalid or duplicate generator ID.");
                Text(generator.name, 80); Text(generator.description, 160);
                Positive(generator.baseProduction);
                if (generator.price == null) throw new ArgumentException("Missing generator price.");
                generator.price.Validate();
                if (double.IsNaN(generator.productionGrowth) || generator.productionGrowth < 1 || generator.productionGrowth > 2)
                    throw new ArgumentException("Production growth must be between 1 and 2.");
            }
            // Retain these IDs so phase-1 saves and public Purchase wrappers remain valid.
            Generator("coil"); Generator("plasma");
            ids.Clear();
            foreach (MilestoneDefinition milestone in milestones)
            {
                if (milestone == null || !ValidId(milestone.id) || !ids.Add(milestone.id)
                    || milestone.reactorStage < 0 || milestone.reactorStage > 4
                    || (milestone.goal != MilestoneGoal.LifetimeEnergy && milestone.goal != MilestoneGoal.GeneratorLevel))
                    throw new ArgumentException("Invalid milestone.");
                Text(milestone.title, 80); Text(milestone.rewardDescription, 160);
                Positive(milestone.target);
                if (milestone.goal == MilestoneGoal.GeneratorLevel)
                {
                    Generator(milestone.generatorId);
                    if (milestone.target > MaxLevel || milestone.target != Math.Floor(milestone.target))
                        throw new ArgumentException("Generator milestone requires an attainable integer level.");
                }
            }
            foreach (GeneratorDefinition generator in generators)
            {
                string gate = generator.unlockMilestone;
                var visited = new HashSet<string>();
                while (!string.IsNullOrEmpty(gate))
                {
                    if (!visited.Add(gate)) throw new ArgumentException("Cyclic generator unlock.");
                    MilestoneDefinition milestone = Milestone(gate);
                    gate = milestone.goal == MilestoneGoal.GeneratorLevel
                        ? Generator(milestone.generatorId).unlockMilestone : null;
                }
            }
            if (!string.IsNullOrEmpty(Generator("coil").unlockMilestone))
                throw new ArgumentException("The starter microgenerator must remain unlocked.");
        }

        internal static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 48) return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_' && c != '-') return false;
            return true;
        }
        private static void Text(string text, int limit)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > limit)
                throw new ArgumentException("Missing or oversized catalog text.");
        }
        internal GeneratorDefinition Generator(string id)
        {
            foreach (var entry in generators) if (entry != null && entry.id == id) return entry;
            throw new ArgumentException("Unknown generator: " + id);
        }
        internal UpgradeDefinition Upgrade(Purchase type)
        {
            foreach (var entry in upgrades) if (entry != null && entry.type == type) return entry;
            throw new ArgumentException("Unknown upgrade.");
        }
        internal MilestoneDefinition Milestone(string id)
        {
            foreach (var entry in milestones) if (entry != null && entry.id == id) return entry;
            throw new ArgumentException("Unknown milestone: " + id);
        }
        public static string ItemId(Purchase item)
        {
            switch (item)
            {
                case Purchase.TapPower: return "tap";
                case Purchase.ComboCapacity: return "combo";
                case Purchase.OverloadDuration: return "overload";
                case Purchase.Coil: return "coil";
                case Purchase.Plasma: return "plasma";
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
        public double ComboCap(int level) { return comboBaseCap + level * comboCapPerLevel; }
        public double BaseCost(Purchase item) { return Price(item).initial; }
        public double CostGrowth(Purchase item) { return Price(item).growth; }
        private PriceCurve Price(Purchase item)
        {
            switch (item)
            {
                case Purchase.TapPower: return tapPrice;
                case Purchase.ComboCapacity: return comboPrice;
                case Purchase.OverloadDuration: return overloadPrice;
                case Purchase.Coil: return coilPrice;
                case Purchase.Plasma: return plasmaPrice;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
    }
}
