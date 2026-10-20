using System;

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
        public double coilProduction = 1;
        public double plasmaProduction = 12;
        public double plasmaUnlock = 1000;
        public double finalMilestone = 10000;
        public PriceCurve tapPrice = new PriceCurve(15, 1.55);
        public PriceCurve comboPrice = new PriceCurve(75, 1.7);
        public PriceCurve overloadPrice = new PriceCurve(100, 1.7);
        public PriceCurve coilPrice = new PriceCurve(25, 1.18);
        public PriceCurve plasmaPrice = new PriceCurve(500, 1.22);

        public EconomyConfig Copy()
        {
            var copy = (EconomyConfig)MemberwiseClone();
            copy.tapPrice = tapPrice?.Copy();
            copy.comboPrice = comboPrice?.Copy();
            copy.overloadPrice = overloadPrice?.Copy();
            copy.coilPrice = coilPrice?.Copy();
            copy.plasmaPrice = plasmaPrice?.Copy();
            return copy;
        }

        internal static void Positive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1e12)
                throw new ArgumentOutOfRangeException(nameof(value), "Economy values must be finite and positive.");
        }

        public void Validate()
        {
            foreach (double value in new[] { baseTapPower, tapPowerPerLevel, comboWindow, comboDecayInterval,
                comboStepMultiplier, comboBaseCap, comboCapPerLevel, overloadChargeRequired, chargePerTap,
                overloadMultiplier, overloadSeconds, overloadSecondsPerLevel, coilProduction,
                plasmaProduction, plasmaUnlock, finalMilestone })
                Positive(value);
            if (overloadMultiplier < 1 || chargePerTap > overloadChargeRequired
                || finalMilestone <= plasmaUnlock || comboBaseCap < 1 || comboTapsPerStep < 1
                || comboTapsPerStep > 10000
                || Math.Ceiling((ComboCap(MaxLevel) - 1) / comboStepMultiplier) * comboTapsPerStep > 1000000)
                throw new ArgumentException("Invalid overload or milestone configuration.");
            foreach (PriceCurve curve in new[] { tapPrice, comboPrice, overloadPrice, coilPrice, plasmaPrice })
            {
                if (curve == null) throw new ArgumentException("Missing price curve.");
                curve.Validate();
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
