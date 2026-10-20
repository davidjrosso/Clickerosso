using System;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class RebirthTests
    {
        private static GameEngine Ready(double lifetime = 1000000)
        {
            return new GameEngine(new GameState { energy = 25000, lifetimeEnergy = lifetime,
                coils = 5, plasma = 2, tapLevel = 3, comboLevel = 2, overloadLevel = 2,
                generators = new[] { new GeneratorLevel { id = "quantum", level = 1 } } });
        }
        [Test]
        public void LegacyMigrationPreservesProgressAndDoesNotGrantPermanentBonuses()
        {
            var old = new GameState { energy = 42, lifetimeEnergy = 1000, coils = 2, tapLevel = 1 };
            var game = new GameEngine(old);
            Assert.That(game.Snapshot().version, Is.EqualTo(2));
            Assert.That(old.version, Is.EqualTo(1));
            Assert.That(game.Energy, Is.EqualTo(42));
            Assert.That(game.Lifetime, Is.EqualTo(1000));
            Assert.That(game.BaseProduction, Is.EqualTo(2));
            Assert.That(game.TapPower, Is.EqualTo(2));
            Assert.That(game.PermanentMultiplier, Is.EqualTo(1));
            Assert.That(game.Specialization, Is.EqualTo(Specialization.None));
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }
        [TestCase(999999, 0)]
        [TestCase(1000000, 3)]
        [TestCase(4000000, 6)]
        [TestCase(1e150, 100000)]
        public void RewardUsesCycleProductionAndHasAnExplicitCap(double produced, int reward)
        {
            var game = Ready(produced);
            Assert.That(game.RebirthReward, Is.EqualTo(reward));
            game.Buy("tap");
            Assert.That(game.RebirthReward, Is.EqualTo(reward));
        }
        [Test]
        public void RebirthCandidateResetsCycleButDoesNotMutateLiveEngine()
        {
            var game = Ready();
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload();
            double energy = game.Energy;
            Assert.That(game.TryPrepareRebirth(Specialization.Tap, 3, out var next), Is.True);
            Assert.That(game.Energy, Is.EqualTo(energy));
            Assert.That(game.Overload.Active, Is.True);
            Assert.That(game.Essence, Is.Zero);
            Assert.That(next.Energy, Is.Zero);
            Assert.That(next.Lifetime, Is.Zero);
            Assert.That(next.Level("coil"), Is.EqualTo(1));
            Assert.That(next.Level("tap"), Is.EqualTo(1));
            Assert.That(next.Level("plasma"), Is.Zero);
            Assert.That(next.Level("quantum"), Is.Zero);
            Assert.That(next.Level("combo"), Is.Zero);
            Assert.That(next.Level("overload"), Is.Zero);
            Assert.That(next.CompletedMilestoneCount, Is.Zero);
            Assert.That(next.ComboMultiplier, Is.EqualTo(1));
            Assert.That(next.Overload.Active, Is.False);
            Assert.That(next.Overload.ChargeFraction, Is.Zero);
            Assert.That(next.Essence, Is.EqualTo(3));
            Assert.That(next.RebirthCount, Is.EqualTo(1));
            Assert.That(next.Skin, Is.EqualTo(1));
            Assert.That(next.AllTimeEnergy, Is.EqualTo(game.Lifetime));
            Assert.That(next.CanRebirth, Is.False);
        }
        [Test]
        public void StaleOrInvalidConfirmationNeverProducesACandidate()
        {
            var game = Ready();
            Assert.That(game.TryPrepareRebirth(Specialization.None, 3, out _), Is.False);
            Assert.That(game.TryPrepareRebirth((Specialization)50, 3, out _), Is.False);
            Assert.That(game.TryPrepareRebirth(Specialization.Tap, 2, out _), Is.False);
            Assert.That(new GameEngine().TryPrepareRebirth(Specialization.Tap, 3, out _), Is.False);
            Assert.That(game.RebirthCount, Is.Zero);
        }
        [TestCase(Specialization.Tap)]
        [TestCase(Specialization.Automation)]
        [TestCase(Specialization.Overload)]
        public void MasteriesSpendExactIncreasingPricesWithoutReducingLifetimePower(Specialization branch)
        {
            Ready().TryPrepareRebirth(branch, 3, out var game);
            double passiveBonus = game.PermanentMultiplier;
            Assert.That(game.MasteryCost(branch), Is.EqualTo(1));
            Assert.That(game.BuyMastery(branch), Is.True);
            Assert.That(game.Essence, Is.EqualTo(2));
            Assert.That(game.MasteryCost(branch), Is.EqualTo(2));
            Assert.That(game.BuyMastery(branch), Is.True);
            Assert.That(game.Essence, Is.Zero);
            Assert.That(game.BuyMastery(branch), Is.False);
            Assert.That(game.PermanentMultiplier, Is.EqualTo(passiveBonus));
            Assert.That(game.BranchMultiplier(branch), Is.EqualTo(1.1 * game.SpecializationBonus(branch)).Within(1e-12));
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void SpecializationsBoostTheirOwnDomainAndOverloadIntegrationRemainsFrameIndependent()
        {
            Ready().TryPrepareRebirth(Specialization.Overload, 3, out var game);
            Ready().TryPrepareRebirth(Specialization.Tap, 3, out var taps);
            Ready().TryPrepareRebirth(Specialization.Automation, 3, out var automation);
            Assert.That(taps.TapPower, Is.EqualTo((game.TapPower + game.BaseProduction * taps.TapProductionFraction) * 1.5).Within(1e-12));
            Assert.That(automation.BaseProduction / game.BaseProduction, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(game.OverloadMultiplier, Is.EqualTo(5.4).Within(1e-12));
            for (int i = 0; i < 25; i++) game.Tap();
            var frames = new GameEngine(game.Snapshot());
            for (int i = 0; i < 25; i++) frames.Tap();
            game.ActivateOverload(); frames.ActivateOverload();
            double a = game.Energy, b = frames.Energy;
            game.Advance(12);
            for (int i = 0; i < 120; i++) frames.Advance(.1);
            Assert.That(game.Energy - a, Is.EqualTo(frames.Energy - b).Within(1e-8));
        }
        [Test]
        public void SkinsAreCosmeticAndCollectionAndMasteriesSurviveRepeatedRebirths()
        {
            var game = Ready();
            for (int cycle = 1; cycle <= 5; cycle++)
            {
                game.TryPrepareRebirth(cycle % 2 == 1 ? Specialization.Tap : Specialization.Automation, game.RebirthReward, out var next);
                Assert.That(next, Is.Not.Null);
                if (cycle == 1) next.BuyMastery(Specialization.Tap);
                Assert.That(next.MasteryLevel(Specialization.Tap), Is.EqualTo(1));
                int expected = cycle >= 5 ? 3 : cycle >= 3 ? 2 : 1;
                Assert.That(next.Skin, Is.EqualTo(expected));
                double power = next.TapPower, production = next.Production;
                Assert.That(next.SelectSkin(0), Is.True);
                Assert.That(next.TapPower, Is.EqualTo(power));
                Assert.That(next.Production, Is.EqualTo(production));
                Assert.That(next.SelectSkin(expected), Is.True);
                if (expected < 3) Assert.That(next.SelectSkin(expected + 1), Is.False);
                var state = next.Snapshot(); state.lifetimeEnergy = 1000000;
                game = new GameEngine(state);
                Assert.That(game.Snapshot().IsValid(), Is.True);
            }
            Assert.That(game.RebirthCount, Is.EqualTo(5));
            Assert.That(game.EarnedEssence, Is.EqualTo(15));
        }
        [Test]
        public void NextRewardTargetAndReadinessRecoverAfterReload()
        {
            var game = Ready();
            Assert.That(game.NextEssenceTarget, Is.EqualTo(1000000 * 16d / 9).Within(1e-8));
            var restored = new GameEngine(game.Snapshot());
            Assert.That(restored.RebirthReward, Is.EqualTo(3));
            Assert.That(restored.NextEssenceTarget, Is.EqualTo(game.NextEssenceTarget));
        }
        [Test]
        public void PermanentCurrencyAndMasteryLimitsNeverWrap()
        {
            var state = Ready().Snapshot();
            state.rebirth.rebirths = 100;
            state.rebirth.essence = state.rebirth.earnedEssence = RebirthProgress.MaxEssence - 1;
            state.rebirth.specialization = Specialization.Tap;
            var game = new GameEngine(state);
            Assert.That(game.RebirthReward, Is.EqualTo(1));
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, 1, out var next), Is.True);
            var after = next.Snapshot(); after.lifetimeEnergy = 1e150;
            var capped = new GameEngine(after);
            Assert.That(capped.RebirthReward, Is.Zero);
            Assert.That(double.IsPositiveInfinity(capped.NextEssenceTarget), Is.True);
            for (int i = 0; i < 20; i++) Assert.That(capped.BuyMastery(Specialization.Tap), Is.True);
            Assert.That(capped.BuyMastery(Specialization.Tap), Is.False);
            Assert.That(capped.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void RebirthHonorsThresholdRemainderAndArchivesSmallAmounts()
        {
            var state = Ready().Snapshot(); state.lifetimeRemainder = -1e-12;
            Assert.That(new GameEngine(state).CanRebirth, Is.False);
            state = Ready(1e16).Snapshot(); state.lifetimeRemainder = .25;
            var game = new GameEngine(state);
            Assert.That(game.TryPrepareRebirth(Specialization.Tap, game.RebirthReward, out var next), Is.True);
            Assert.That(next.Snapshot().rebirth.archivedEnergy, Is.EqualTo(1e16));
            Assert.That(next.Snapshot().rebirth.archivedRemainder, Is.EqualTo(.25));
            Assert.That(next.Snapshot().IsValid(), Is.True);
        }

        [Test]
        public void TapSpecializationLinksGeneratorsToClickPowerAndCosmeticChangesNeverChangeIt()
        {
            Ready().TryPrepareRebirth(Specialization.Tap, 3, out var initial);
            var state = initial.Snapshot(); state.energy = state.lifetimeEnergy = 1000;
            var game = new GameEngine(state);
            double before = game.TapPower, production = game.BaseProduction;
            Assert.That(game.Buy("coil"), Is.True);
            double expected = (game.BaseProduction - production) * game.TapProductionFraction * game.BranchMultiplier(Specialization.Tap);
            Assert.That(game.TapPower - before, Is.EqualTo(expected).Within(1e-12));
            Assert.That(game.EffectAtLevel("tap", game.Level("tap")), Is.EqualTo(game.TapPower));
            Assert.That(game.ProductionOf("coil"), Is.EqualTo(game.BaseProduction));
        }

        [Test]
        public void InvalidPermanentStatesAndSettingsAreRejected()
        {
            var state = Ready().Snapshot(); state.rebirth.essence = -1;
            Assert.That(state.IsValid(), Is.False);
            state = Ready().Snapshot(); state.rebirth.skin = 3;
            Assert.That(state.IsValid(), Is.False);
            state = Ready().Snapshot(); state.rebirth.archivedEnergy = double.PositiveInfinity;
            Assert.That(state.IsValid(), Is.False);
            state = Ready().Snapshot(); state.rebirth.tapMastery = 1;
            Assert.That(state.IsValid(), Is.False);
            state = Ready().Snapshot(); state.rebirth = null;
            Assert.That(state.IsValid(), Is.False);
            var config = new EconomyConfig(); config.rebirth.threshold = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEngine(settings: config));
        }
    }
}
