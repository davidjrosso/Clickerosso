using System;

namespace ReactorDeCaos
{
    public sealed class GameEngine
    {
        private readonly GameState state;
        private readonly EconomyConfig config;
        public ComboSystem Combo { get; }
        public OverloadSystem Overload { get; }
        public double Energy { get { return state.energy; } }
        public double Lifetime { get { return state.lifetimeEnergy; } }
        public int Stage { get { return Lifetime >= config.finalMilestone ? 2 : Lifetime >= config.plasmaUnlock ? 1 : 0; } }
        public double NextMilestone { get { return Stage == 0 ? config.plasmaUnlock : config.finalMilestone; } }
        public double BaseProduction { get { return state.coils * config.coilProduction + state.plasma * config.plasmaProduction; } }
        public double Production { get { return BaseProduction * (Overload.Active ? config.overloadMultiplier : 1); } }
        public double TapPower { get { return config.baseTapPower + state.tapLevel * config.tapPowerPerLevel; } }
        public double ComboMultiplier { get { return Combo.Multiplier(state.comboLevel); } }
        public double ComboCap { get { return config.ComboCap(state.comboLevel); } }
        public double OverloadMultiplier { get { return config.overloadMultiplier; } }
        public double OverloadDuration { get { return config.overloadSeconds + state.overloadLevel * config.overloadSecondsPerLevel; } }
        public double CurrentTapGain { get { return TapPower * ComboMultiplier * (Overload.Active ? OverloadMultiplier : 1); } }
        public bool EnergyLimitReached { get { return Energy >= EconomyConfig.MaxEnergy && state.energyRemainder >= 0; } }

        public GameEngine(GameState initial = null, EconomyConfig settings = null)
        {
            config = settings == null ? new EconomyConfig() : settings.Copy();
            config.Validate();
            Combo = new ComboSystem(config);
            Overload = new OverloadSystem(config.overloadChargeRequired);
            state = initial == null ? new GameState() : initial.Copy();
            if (!state.IsValid()) throw new ArgumentException("Invalid initial state.");
        }

        public GameState Snapshot() { return state.Copy(); }

        public double Tap()
        {
            double multiplier = Combo.Tap(state.comboLevel);
            double amount = TapPower * multiplier * (Overload.Active ? config.overloadMultiplier : 1);
            Overload.Tap(config.chargePerTap);
            return Credit(amount);
        }

        public bool ActivateOverload() { return Overload.Activate(OverloadDuration); }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 86400)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Combo.Advance(seconds);
            double boosted = Overload.Advance(seconds);
            Credit(BaseProduction * (seconds + boosted * (config.overloadMultiplier - 1)));
        }

        public int Level(Purchase item)
        {
            switch (item)
            {
                case Purchase.TapPower: return state.tapLevel;
                case Purchase.ComboCapacity: return state.comboLevel;
                case Purchase.OverloadDuration: return state.overloadLevel;
                case Purchase.Coil: return state.coils;
                case Purchase.Plasma: return state.plasma;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public double Cost(Purchase item)
        {
            return Math.Ceiling(config.BaseCost(item) * Math.Pow(config.CostGrowth(item), Level(item)));
        }

        public bool Unlocked(Purchase item) { return item != Purchase.Plasma || Lifetime >= config.plasmaUnlock; }
        public bool CanBuy(Purchase item)
        {
            double cost = Cost(item);
            return Unlocked(item) && Level(item) < EconomyConfig.MaxLevel
                && (Energy > cost || (Energy == cost && state.energyRemainder >= 0));
        }

        public bool Buy(Purchase item)
        {
            if (!CanBuy(item)) return false;
            CompensatedSum.Add(ref state.energy, ref state.energyRemainder, -Cost(item));
            switch (item)
            {
                case Purchase.TapPower: state.tapLevel++; break;
                case Purchase.ComboCapacity: state.comboLevel++; break;
                case Purchase.OverloadDuration: state.overloadLevel++; break;
                case Purchase.Coil: state.coils++; break;
                case Purchase.Plasma: state.plasma++; break;
            }
            return true;
        }

        private double Credit(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0)
                throw new ArithmeticException("Invalid energy reward.");
            double accepted = Math.Min(amount, Math.Max(0, (EconomyConfig.MaxEnergy - Energy) - state.energyRemainder));
            CompensatedSum.Add(ref state.energy, ref state.energyRemainder, accepted);
            double historical = Math.Min(accepted,
                Math.Max(0, (EconomyConfig.MaxEnergy - Lifetime) - state.lifetimeRemainder));
            CompensatedSum.Add(ref state.lifetimeEnergy, ref state.lifetimeRemainder, historical);
            return accepted;
        }

        // UI reads authoritative effects; no formulas are duplicated in presentation.
        public double EffectAtLevel(Purchase item, int level)
        {
            if (level < 0 || level > EconomyConfig.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            switch (item)
            {
                case Purchase.TapPower: return config.baseTapPower + level * config.tapPowerPerLevel;
                case Purchase.ComboCapacity: return config.ComboCap(level);
                case Purchase.OverloadDuration: return config.overloadSeconds + level * config.overloadSecondsPerLevel;
                case Purchase.Coil: return level * config.coilProduction;
                case Purchase.Plasma: return level * config.plasmaProduction;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
    }
}
