using System;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class GeneratorDefinition
    {
        public string id, name, description, unlockMilestone;
        public PriceCurve price;
        public double baseProduction, productionGrowth = 1;

        public GeneratorDefinition Copy()
        {
            var copy = (GeneratorDefinition)MemberwiseClone();
            copy.price = price?.Copy();
            return copy;
        }

        // Each level adds baseProduction * productionGrowth^(level-1).
        public double Production(int level)
        {
            return productionGrowth == 1 ? baseProduction * level
                : baseProduction * (Math.Pow(productionGrowth, level) - 1) / (productionGrowth - 1);
        }
    }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        public Purchase type;
        public string name, description;
        public PriceCurve price;
        public UpgradeDefinition Copy()
        {
            var copy = (UpgradeDefinition)MemberwiseClone();
            copy.price = price?.Copy();
            return copy;
        }
    }

    public enum MilestoneGoal { LifetimeEnergy, GeneratorLevel }

    [Serializable]
    public sealed class MilestoneDefinition
    {
        public string id, title, rewardDescription, generatorId;
        public MilestoneGoal goal;
        public double target;
        public int reactorStage;
        public MilestoneDefinition Copy() { return (MilestoneDefinition)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class GeneratorLevel
    {
        public string id;
        public int level;
        public GeneratorLevel Copy() { return (GeneratorLevel)MemberwiseClone(); }
    }

    public sealed class ShopEntry
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Unit { get; }
        public bool IsGenerator { get; }
        internal ShopEntry(string id, string name, string description, string unit, bool generator)
        { Id = id; Name = name; Description = description; Unit = unit; IsGenerator = generator; }
    }

    internal static class ProgressionDefaults
    {
        public static UpgradeDefinition[] Upgrades()
        {
            return new[] {
                new UpgradeDefinition { type = Purchase.TapPower, name = "POTENCIA",
                    description = "Más energía en cada toque", price = new PriceCurve(15, 1.55) },
                new UpgradeDefinition { type = Purchase.ComboCapacity, name = "COMBO",
                    description = "Amplía el máximo al encadenar toques", price = new PriceCurve(75, 1.7) },
                new UpgradeDefinition { type = Purchase.OverloadDuration, name = "SOBRECARGA",
                    description = "Prolonga el impulso de toques y generadores", price = new PriceCurve(100, 1.7) }
            };
        }

        public static GeneratorDefinition[] Generators()
        {
            return new[] {
                Generator("coil", "MICROGENERADOR", "Producción estable desde el inicio", 25, 1.18, 1, 1, ""),
                Generator("plasma", "BOBINA DE PLASMA", "Concentra el flujo del reactor", 500, 1.16, 12, 1, "plasma"),
                Generator("quantum", "TURBINA CUÁNTICA", "Acelera la producción sostenida", 2500, 1.14, 60, 1.06, "quantum"),
                Generator("auxiliary", "NÚCLEO AUXILIAR", "Una segunda fuente de energía", 12000, 1.14, 350, 1.08, "auxiliary")
            };
        }

        private static GeneratorDefinition Generator(string id, string name, string description,
            double price, double growth, double production, double productionGrowth, string gate)
        {
            return new GeneratorDefinition { id = id, name = name, description = description,
                price = new PriceCurve(price, growth), baseProduction = production,
                productionGrowth = productionGrowth, unlockMilestone = gate };
        }

        public static MilestoneDefinition[] Milestones()
        {
            return new[] {
                Energy("ignition", "Primera ignición", 100, 0, "Pulso de ignición"),
                new MilestoneDefinition { id = "micro_array", title = "Red de microgeneradores",
                    goal = MilestoneGoal.GeneratorLevel, generatorId = "coil", target = 5,
                    reactorStage = 1, rewardDescription = "Primer anillo orbital" },
                Energy("plasma", "Confinamiento de plasma", 1000, 1, "Bobina de plasma disponible"),
                Energy("quantum", "Flujo cuántico", 5000, 2, "Turbina cuántica y segundo anillo"),
                Energy("singularity", "Resonancia estable", 10000, 2, "Pulso de resonancia"),
                Energy("auxiliary", "Doble núcleo", 25000, 3, "Núcleo auxiliar y tercera capa"),
                Energy("mastery", "Reactor de caos", 100000, 4, "Corona del reactor"),
                Energy("star", "Estrella confinada", 1000000, 4, "Pulso estelar"),
                Energy("tide", "Marea de energía", 10000000, 4, "Pulso de marea"),
                Energy("horizon", "Horizonte de caos", 100000000, 4, "Pulso del horizonte"),
                Energy("resonance", "Resonancia absoluta", 1000000000, 4, "Pulso de resonancia absoluta"),
                Energy("cosmos", "Energía del cosmos", 10000000000, 4, "Pulso cósmico")
            };
        }

        private static MilestoneDefinition Energy(string id, string title, double target, int stage, string reward)
        {
            return new MilestoneDefinition { id = id, title = title, goal = MilestoneGoal.LifetimeEnergy,
                target = target, reactorStage = stage, rewardDescription = reward };
        }
    }
}
