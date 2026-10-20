using System;

namespace ReactorDeCaos
{
    public sealed class ComboSystem
    {
        private readonly EconomyConfig config;
        private double remaining, decayTime;
        public int Count { get; private set; }
        public bool Decaying { get { return Count > 0 && remaining <= 0; } }
        public double GraceRemaining { get { return remaining; } }
        internal ComboSystem(EconomyConfig config) { this.config = config; }

        public void Advance(double seconds)
        {
            if (Count == 0) return;
            double protectedTime = Math.Min(seconds, remaining);
            remaining = Math.Max(0, remaining - seconds);
            decayTime += seconds - protectedTime;
            double steps = Math.Floor((decayTime + config.comboDecayInterval * 1e-9) / config.comboDecayInterval);
            if (steps <= 0) return;
            int lost = (int)Math.Min(Count, steps * config.comboTapsPerStep);
            Count -= lost;
            decayTime = Count == 0 ? 0 : Math.Max(0, decayTime - steps * config.comboDecayInterval);
        }

        public double Tap(int capacityLevel)
        {
            int maximum = (int)Math.Ceiling((config.ComboCap(capacityLevel) - 1)
                / config.comboStepMultiplier) * config.comboTapsPerStep;
            Count = Math.Min(maximum, Count + 1);
            remaining = config.comboWindow;
            decayTime = 0;
            return Multiplier(capacityLevel);
        }

        public double Multiplier(int capacityLevel)
        {
            return Math.Min(config.ComboCap(capacityLevel),
                1 + (Count / config.comboTapsPerStep) * config.comboStepMultiplier);
        }
    }
}
