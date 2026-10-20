using System;

namespace ReactorDeCaos
{
    public sealed class OverloadSystem
    {
        public double Charge { get; private set; }
        public double Remaining { get; private set; }
        public double ActiveDuration { get; private set; }
        public double RequiredCharge { get; }
        public double ChargeFraction { get { return Charge / RequiredCharge; } }
        internal OverloadSystem(double requiredCharge) { RequiredCharge = requiredCharge; }
        public bool Active { get { return Remaining > 0; } }
        public bool Ready { get { return !Active && Charge >= RequiredCharge; } }

        public void Tap(double amount)
        {
            if (!Active) Charge = Math.Min(RequiredCharge, Charge + amount);
        }

        public bool Activate(double duration)
        {
            if (!Ready) return false;
            Charge = 0;
            Remaining = duration;
            ActiveDuration = duration;
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
