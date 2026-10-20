using System;

namespace ReactorDeCaos
{
    public sealed class ComboSystem
    {
        private double remaining;
        public int Count { get; private set; }

        public void Advance(double seconds)
        {
            remaining = Math.Max(0, remaining - seconds);
            if (remaining == 0) Count = 0;
        }

        public double Tap(double window, int capacityLevel)
        {
            Count = Math.Min(10000, Count + 1);
            remaining = window;
            return Multiplier(capacityLevel);
        }

        public double Multiplier(int capacityLevel)
        {
            return Math.Min(3 + capacityLevel * .25, 1 + (Count / 5) * .25);
        }
    }
}
