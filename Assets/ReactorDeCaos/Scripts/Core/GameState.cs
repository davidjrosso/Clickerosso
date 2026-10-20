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
                && lifetimeEnergy >= energy
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
