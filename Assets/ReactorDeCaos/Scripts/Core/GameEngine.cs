using System;

namespace ReactorDeCaos
{
    public sealed class GameEngine
    {
        private readonly GameState state;
        private readonly EconomyConfig config;
        public ComboSystem Combo { get; } = new ComboSystem();
        public OverloadSystem Overload { get; } = new OverloadSystem();
        public double Energy { get { return state.energy; } }
        public double Lifetime { get { return state.lifetimeEnergy; } }
        public int Stage { get { return Lifetime >= config.finalMilestone ? 2 : Lifetime >= config.plasmaUnlock ? 1 : 0; } }
        public double NextMilestone { get { return Stage == 0 ? config.plasmaUnlock : config.finalMilestone; } }
        public double BaseProduction { get { return state.coils * config.coilProduction + state.plasma * config.plasmaProduction; } }
        public double Production { get { return BaseProduction * (Overload.Active ? config.overloadMultiplier : 1); } }
        public double TapPower { get { return 1 + state.tapLevel; } }
        public double ComboMultiplier { get { return Combo.Multiplier(state.comboLevel); } }
        public double OverloadDuration { get { return config.overloadSeconds + state.overloadLevel; } }

        public GameEngine(GameState initial = null, EconomyConfig settings = null)
        {
            config = settings ?? new EconomyConfig();
            config.Validate();
            state = initial == null ? new GameState() : initial.Copy();
            if (!state.IsValid()) throw new ArgumentException("Invalid initial state.");
        }

        public GameState Snapshot() { return state.Copy(); }

        public double Tap()
        {
            double multiplier = Combo.Tap(config.comboWindow, state.comboLevel);
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
            return Unlocked(item) && Level(item) < EconomyConfig.MaxLevel && Energy >= Cost(item);
        }

        public bool Buy(Purchase item)
        {
            if (!CanBuy(item)) return false;
            state.energy -= Cost(item);
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
            double previous = Energy;
            state.energy = Math.Min(EconomyConfig.MaxEnergy, state.energy + amount);
            state.lifetimeEnergy = Math.Min(EconomyConfig.MaxEnergy, state.lifetimeEnergy + amount);
            return Energy - previous;
        }
    }
}
