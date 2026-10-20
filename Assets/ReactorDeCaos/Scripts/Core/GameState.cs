using System;
using System.Collections.Generic;

namespace ReactorDeCaos
{
    public enum Purchase { TapPower, ComboCapacity, OverloadDuration, Coil, Plasma }

    [Serializable]
    public sealed class GameState
    {
        public int version = 1;
        public double energy;
        public double lifetimeEnergy;
        // Optional low-order fields; old version-1 saves deserialize these as zero.
        public double energyRemainder;
        public double lifetimeRemainder;
        public int tapLevel;
        public int comboLevel;
        public int overloadLevel;
        public int coils;
        public int plasma;
        // Optional additions to version 1. Legacy coil/plasma fields retain their meaning.
        public GeneratorLevel[] generators = Array.Empty<GeneratorLevel>();
        public string[] completedMilestones = Array.Empty<string>();

        public GameState Copy()
        {
            var copy = (GameState)MemberwiseClone();
            copy.generators = generators == null ? Array.Empty<GeneratorLevel>()
                : Array.ConvertAll(generators, x => x?.Copy());
            copy.completedMilestones = completedMilestones == null ? Array.Empty<string>()
                : (string[])completedMilestones.Clone();
            return copy;
        }

        public bool IsValid()
        {
            return version == 1 && ValidNumber(energy) && ValidNumber(lifetimeEnergy)
                && CompensatedSum.IsValid(energy, energyRemainder)
                && CompensatedSum.IsValid(lifetimeEnergy, lifetimeRemainder)
                && (lifetimeEnergy > energy || (lifetimeEnergy == energy && lifetimeRemainder >= energyRemainder))
                && ValidLevel(tapLevel) && ValidLevel(comboLevel)
                && ValidLevel(overloadLevel) && ValidLevel(coils) && ValidLevel(plasma)
                && ValidProgression();
        }

        private bool ValidProgression()
        {
            var ids = new HashSet<string>();
            if (generators != null)
            {
                if (generators.Length > 32) return false;
                foreach (var entry in generators)
                    if (entry == null || !EconomyConfig.ValidId(entry.id) || entry.id == "coil" || entry.id == "plasma"
                        || entry.id == "tap" || entry.id == "combo" || entry.id == "overload"
                        || !ids.Add(entry.id) || !ValidLevel(entry.level)) return false;
            }
            ids.Clear();
            if (completedMilestones != null)
            {
                if (completedMilestones.Length > 128) return false;
                foreach (string id in completedMilestones)
                    if (!EconomyConfig.ValidId(id) || !ids.Add(id)) return false;
            }
            return true;
        }

        private static bool ValidLevel(int n) { return n >= 0 && n <= EconomyConfig.MaxLevel; }
        private static bool ValidNumber(double n)
        {
            return !double.IsNaN(n) && !double.IsInfinity(n)
                && n >= 0 && n <= EconomyConfig.MaxEnergy;
        }
    }
}
