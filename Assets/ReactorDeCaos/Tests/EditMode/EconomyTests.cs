using System;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class EconomyTests
    {
        [Test]
        public void FirstTapProducesOneEnergy()
        {
            var game = new GameEngine();
            Assert.That(game.Tap(), Is.EqualTo(1));
            Assert.That(game.Energy, Is.EqualTo(1));
            Assert.That(game.Lifetime, Is.EqualTo(1));
        }

        [Test]
        public void FifthTapRaisesComboAndInactivityResetsIt()
        {
            var game = new GameEngine();
            for (int i = 0; i < 4; i++) game.Tap();
            Assert.That(game.Tap(), Is.EqualTo(1.25));
            game.Advance(.99);
            Assert.That(game.Combo.Count, Is.EqualTo(5));
            game.Advance(.02);
            Assert.That(game.Tap(), Is.EqualTo(1));
        }

        [Test]
        public void ComboHasConfiguredCap()
        {
            var game = new GameEngine();
            for (int i = 0; i < 200; i++) game.Tap();
            Assert.That(game.ComboMultiplier, Is.EqualTo(3));
        }

        [Test]
        public void ComboUpgradeRaisesCap()
        {
            var game = new GameEngine(new GameState { comboLevel = 2 });
            for (int i = 0; i < 200; i++) game.Tap();
            Assert.That(game.ComboMultiplier, Is.EqualTo(3.5));
        }

        [Test]
        public void OverloadRequiresChargeAndManualActivation()
        {
            var game = new GameEngine();
            Assert.That(game.ActivateOverload(), Is.False);
            for (int i = 0; i < 25; i++) game.Tap();
            Assert.That(game.Overload.Ready, Is.True);
            Assert.That(game.Overload.Active, Is.False);
            Assert.That(game.ActivateOverload(), Is.True);
            Assert.That(game.ActivateOverload(), Is.False);
            Assert.That(game.Tap(), Is.EqualTo(game.TapPower * game.ComboMultiplier * 3));
            Assert.That(game.Overload.Charge, Is.Zero);
        }

        [Test]
        public void PassiveProductionSplitsIntervalAtOverloadExpiry()
        {
            var game = new GameEngine(new GameState { coils = 2 });
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload();
            double before = game.Energy;
            game.Advance(12);
            Assert.That(game.Energy - before, Is.EqualTo(2 * (10 * 3 + 2)).Within(1e-9));
            Assert.That(game.Overload.Active, Is.False);
        }

        [Test]
        public void DurationUpgradeAddsOneSecondPerLevel()
        {
            var game = new GameEngine(new GameState { overloadLevel = 3 });
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload();
            Assert.That(game.Overload.Remaining, Is.EqualTo(13));
        }

        [Test]
        public void AutomationDoesNotDependOnFrameRate()
        {
            var state = new GameState { coils = 3, plasma = 2, lifetimeEnergy = 1000 };
            var coarse = new GameEngine(state);
            var fine = new GameEngine(state);
            coarse.Advance(60);
            for (int i = 0; i < 3600; i++) fine.Advance(1d / 60);
            Assert.That(fine.Energy, Is.EqualTo(coarse.Energy).Within(1e-7));
            Assert.That(coarse.Energy, Is.EqualTo(1620));
        }

        [Test]
        public void CannotBuyWithoutFundsAndFailedPurchaseDoesNotMutateState()
        {
            var game = new GameEngine();
            Assert.That(game.Buy(Purchase.Coil), Is.False);
            Assert.That(game.Level(Purchase.Coil), Is.Zero);
            Assert.That(game.Energy, Is.Zero);
        }

        [Test]
        public void ExactBalancePurchaseAndCostGrowth()
        {
            var game = new GameEngine(new GameState { energy = 25, lifetimeEnergy = 25 });
            Assert.That(game.Buy(Purchase.Coil), Is.True);
            Assert.That(game.Energy, Is.Zero);
            Assert.That(game.BaseProduction, Is.EqualTo(1));
            Assert.That(game.Cost(Purchase.Coil), Is.EqualTo(30));
            Assert.That(game.Lifetime, Is.EqualTo(25));
        }

        [Test]
        public void TapUpgradeChangesActualReward()
        {
            var game = new GameEngine(new GameState { energy = 15, lifetimeEnergy = 15 });
            Assert.That(game.Buy(Purchase.TapPower), Is.True);
            Assert.That(game.Tap(), Is.EqualTo(2));
        }

        [Test]
        public void PlasmaUnlockUsesLifetimeAndSurvivesSpending()
        {
            var game = new GameEngine(new GameState { energy = 999, lifetimeEnergy = 999 });
            Assert.That(game.Buy(Purchase.Plasma), Is.False);
            game.Tap();
            Assert.That(game.Stage, Is.EqualTo(1));
            Assert.That(game.Buy(Purchase.Plasma), Is.True);
            Assert.That(game.Unlocked(Purchase.Plasma), Is.True);
            Assert.That(game.Production, Is.EqualTo(12));
        }

        [Test]
        public void FinalMilestoneChangesStage()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 9999 });
            game.Tap();
            Assert.That(game.Stage, Is.EqualTo(2));
        }

        [Test]
        public void UpgradeLevelHasHardLimit()
        {
            var game = new GameEngine(new GameState { energy = 1e100, lifetimeEnergy = 1e100, coils = 100 });
            Assert.That(game.Buy(Purchase.Coil), Is.False);
            Assert.That(game.Level(Purchase.Coil), Is.EqualTo(100));
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(86401d)]
        public void InvalidTimeIsRejected(double seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEngine().Advance(seconds));
        }

        [Test]
        public void InvalidConfigAndStateAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEngine(settings: new EconomyConfig { comboWindow = 0 }));
            Assert.Throws<ArgumentException>(() => new GameEngine(new GameState { energy = -1 }));
            Assert.That(new GameState { energy = double.NaN }.IsValid(), Is.False);
            Assert.That(new GameState { energy = 10, lifetimeEnergy = 9 }.IsValid(), Is.False);
            Assert.That(new GameState { plasma = -1 }.IsValid(), Is.False);
        }

        [Test]
        public void CallerCannotMutateInternalState()
        {
            var state = new GameState();
            var game = new GameEngine(state);
            state.energy = 999;
            game.Snapshot().energy = 123;
            Assert.That(game.Energy, Is.Zero);
        }

        [Test]
        public void ResourceCapStaysFinite()
        {
            var game = new GameEngine(new GameState { energy = EconomyConfig.MaxEnergy, lifetimeEnergy = EconomyConfig.MaxEnergy, coils = 100 });
            game.Tap();
            game.Advance(86400);
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }

        [Test]
        public void LoadingDoesNotAwardOfflineIncomeOrRestoreTransientBoost()
        {
            var game = new GameEngine(new GameState { coils = 1 });
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload();
            var restored = new GameEngine(game.Snapshot());
            Assert.That(restored.Energy, Is.EqualTo(game.Energy));
            Assert.That(restored.Combo.Count, Is.Zero);
            Assert.That(restored.Overload.Active, Is.False);
        }

        [Test]
        public void NewPlayerCanReachFirstGeneratorAndOverload()
        {
            var game = new GameEngine();
            for (int i = 0; i < 25; i++) { game.Advance(.2); game.Tap(); }
            Assert.That(game.Buy(Purchase.Coil), Is.True);
            Assert.That(game.ActivateOverload(), Is.True);
            double before = game.Energy;
            game.Advance(5);
            Assert.That(game.Energy, Is.EqualTo(before + 15).Within(1e-9));
        }

        [TestCase(1234d, "1.2K")]
        [TestCase(1200000d, "1.2M")]
        [TestCase(0d, "0")]
        public void NumbersUseCompactStableFormatting(double number, string expected)
        {
            Assert.That(NumberFormat.Compact(number), Is.EqualTo(expected));
        }
    }
}
