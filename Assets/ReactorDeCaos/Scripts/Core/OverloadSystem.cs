using System;

namespace ReactorDeCaos
{
    public sealed class OverloadSystem
    {
        public double Charge { get; private set; }
        public double Remaining { get; private set; }
        public bool Active { get { return Remaining > 0; } }
        public bool Ready { get { return !Active && Charge >= 100; } }

        public void Tap(double amount)
        {
            if (!Active) Charge = Math.Min(100, Charge + amount);
        }

        public bool Activate(double duration)
        {
            if (!Ready) return false;
            Charge = 0;
            Remaining = duration;
            return true;
        }

        // Return the part of this interval during which the multiplier applies.
        public double Advance(double seconds)
        {
            double boosted = Math.Min(seconds, Remaining);
            Remaining = Math.Max(0, Remaining - seconds);
            return boosted;
        }
    }
}
