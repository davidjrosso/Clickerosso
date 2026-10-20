using System;

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

        public GameState Copy()
        {
            return (GameState)MemberwiseClone();
        }

        public bool IsValid()
        {
            return version == 1 && ValidNumber(energy) && ValidNumber(lifetimeEnergy)
                && CompensatedSum.IsValid(energy, energyRemainder)
                && CompensatedSum.IsValid(lifetimeEnergy, lifetimeRemainder)
                && (lifetimeEnergy > energy || (lifetimeEnergy == energy && lifetimeRemainder >= energyRemainder))
                && ValidLevel(tapLevel) && ValidLevel(comboLevel)
                && ValidLevel(overloadLevel) && ValidLevel(coils) && ValidLevel(plasma);
        }

        private static bool ValidLevel(int n) { return n >= 0 && n <= EconomyConfig.MaxLevel; }
        private static bool ValidNumber(double n)
        {
            return !double.IsNaN(n) && !double.IsInfinity(n)
                && n >= 0 && n <= EconomyConfig.MaxEnergy;
        }
    }
}
