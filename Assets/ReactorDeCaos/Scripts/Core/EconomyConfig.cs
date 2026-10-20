using System;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class EconomyConfig
    {
        public const int MaxLevel = 100;
        public const double MaxEnergy = 1e150;
        public double comboWindow = 1;
        public double chargePerTap = 4;
        public double overloadMultiplier = 3;
        public double overloadSeconds = 10;
        public double coilProduction = 1;
        public double plasmaProduction = 12;
        public double plasmaUnlock = 1000;
        public double finalMilestone = 10000;

        public void Validate()
        {
            foreach (double value in new[] { comboWindow, chargePerTap, overloadMultiplier,
                overloadSeconds, coilProduction, plasmaProduction, plasmaUnlock, finalMilestone })
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1e12)
                    throw new ArgumentOutOfRangeException(nameof(value), "Economy values must be finite and positive.");
            }
            if (overloadMultiplier < 1 || chargePerTap > 100 || finalMilestone <= plasmaUnlock)
                throw new ArgumentException("Invalid overload or milestone configuration.");
        }

        public double BaseCost(Purchase item)
        {
            switch (item)
            {
                case Purchase.TapPower: return 15;
                case Purchase.ComboCapacity: return 75;
                case Purchase.OverloadDuration: return 100;
                case Purchase.Coil: return 25;
                case Purchase.Plasma: return 500;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public double CostGrowth(Purchase item)
        {
            switch (item)
            {
                case Purchase.TapPower: return 1.55;
                case Purchase.ComboCapacity:
                case Purchase.OverloadDuration: return 1.7;
                case Purchase.Coil: return 1.18;
                case Purchase.Plasma: return 1.22;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
    }
}
