using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void CatalogKeepsLegacyPurchasesAndAddsTwoGeneratorClasses()
        {
            var game = new GameEngine();
            Assert.That(game.Shop.Count, Is.EqualTo(7));
            Assert.That(game.Shop[3].Id, Is.EqualTo("coil"));
            Assert.That(game.Shop[4].Id, Is.EqualTo("plasma"));
            Assert.That(game.Unlocked("coil"), Is.True);
            Assert.That(game.CanBuy("quantum"), Is.False);
            Assert.That(game.Level(Purchase.Coil), Is.EqualTo(game.Level("coil")));
        }

        [Test]
        public void GeneratorUnlockUsesHistoricalEnergyAndSurvivesSpending()
        {
            var game = new GameEngine(new GameState { energy = 4999, lifetimeEnergy = 4999 });
            Assert.That(game.Unlocked("quantum"), Is.False);
            Assert.That(game.Buy("quantum"), Is.False);
            Assert.That(game.Energy, Is.EqualTo(4999));
            game.Tap();
            Assert.That(game.Unlocked("quantum"), Is.True);
            Assert.That(game.Buy("quantum"), Is.True);
            Assert.That(game.Energy, Is.EqualTo(2500));
            Assert.That(game.BaseProduction, Is.EqualTo(60));
            Assert.That(game.Cost("quantum"), Is.EqualTo(2850));
            Assert.That(game.Unlocked("quantum"), Is.True);
        }

        [Test]
        public void GeneratorGrowthMatchesActualIncomeAndNextLevelPreview()
        {
            var game = new GameEngine(new GameState { energy = 100000, lifetimeEnergy = 100000 });
            game.Buy("quantum");
            Assert.That(game.EffectAtLevel("quantum", 2), Is.EqualTo(123.6).Within(1e-9));
            game.Buy("quantum");
            game.Buy("auxiliary");
            double before = game.Energy;
            game.Advance(60);
            Assert.That(game.Energy - before, Is.EqualTo((123.6 + 350) * 60).Within(1e-6));
        }

        [Test]
        public void AllGeneratorsAndOverloadRemainIndependentOfFrameRate()
        {
            var initial = new GameState { energy = 100000, lifetimeEnergy = 100000 };
            var game = new GameEngine(initial);
            foreach (var entry in game.Shop) if (entry.IsGenerator) game.Buy(entry.Id);
            var coarse = new GameEngine(game.Snapshot());
            var fine = new GameEngine(game.Snapshot());
            for (int i = 0; i < 25; i++) { coarse.Tap(); fine.Tap(); }
            coarse.ActivateOverload(); fine.ActivateOverload();
            coarse.Advance(60);
            for (int i = 0; i < 3600; i++) fine.Advance(1d / 60);
            Assert.That(fine.Energy, Is.EqualTo(coarse.Energy).Within(1e-6));
            Assert.That(fine.Snapshot().IsValid(), Is.True);
        }

        [Test]
        public void ConfiguredGeneratorAppearsWithoutAddingPurchaseEnumOrLogic()
        {
            var settings = new EconomyConfig();
            var list = new List<GeneratorDefinition>(settings.generators) {
                new GeneratorDefinition { id = "test_cell", name = "CELDA", description = "Celda de prueba",
                    baseProduction = 7, productionGrowth = 1.1, price = new PriceCurve(10, 2) }
            };
            settings.generators = list.ToArray();
            var game = new GameEngine(new GameState { energy = 10, lifetimeEnergy = 10 }, settings);
            Assert.That(game.Shop[7].Id, Is.EqualTo("test_cell"));
            Assert.That(game.Buy("test_cell"), Is.True);
            game.Advance(3);
            Assert.That(game.Energy, Is.EqualTo(21));
            Assert.That(game.Cost("test_cell"), Is.EqualTo(20));
        }

        [Test]
        public void ReorderingCatalogDoesNotChangeSavedOwnership()
        {
            var game = new GameEngine(new GameState { energy = 25000, lifetimeEnergy = 25000 });
            game.Buy("auxiliary"); game.Buy("quantum");
            var settings = new EconomyConfig();
            Array.Reverse(settings.generators);
            var restored = new GameEngine(game.Snapshot(), settings);
            Assert.That(restored.Level("quantum"), Is.EqualTo(1));
            Assert.That(restored.Level("auxiliary"), Is.EqualTo(1));
            Assert.That(restored.BaseProduction, Is.EqualTo(410));
            var snapshot = restored.Snapshot();
            snapshot.generators[0].level = 99;
            Assert.That(restored.Level("auxiliary"), Is.EqualTo(1));
        }

        [Test]
        public void LevelMilestoneIsClaimedOnceAndDoesNotChangeEconomy()
        {
            var game = new GameEngine(new GameState { energy = 500, lifetimeEnergy = 500 });
            for (int i = 0; i < 5; i++) Assert.That(game.Buy("coil"), Is.True);
            Assert.That(game.HasMilestone("micro_array"), Is.True);
            Assert.That(game.Stage, Is.EqualTo(1));
            Assert.That(game.BaseProduction, Is.EqualTo(5));
            Assert.That(game.TryTakeMilestone(out var reward), Is.True);
            Assert.That(reward.id, Is.EqualTo("micro_array"));
            game.Advance(1);
            Assert.That(game.TryTakeMilestone(out _), Is.False);
            var restored = new GameEngine(game.Snapshot());
            Assert.That(restored.Stage, Is.EqualTo(1));
            Assert.That(restored.TryTakeMilestone(out _), Is.False);
        }

        [Test]
        public void LargeTimeStepClaimsEveryCrossedMilestoneOnce()
        {
            var game = new GameEngine(new GameState { coils = 100 });
            game.Advance(1000);
            var ids = new HashSet<string>();
            while (game.TryTakeMilestone(out var reward)) Assert.That(ids.Add(reward.id), Is.True);
            Assert.That(ids.Count, Is.EqualTo(6)); // level milestone was silently reconciled at load.
            Assert.That(game.Stage, Is.EqualTo(4));
            game.Advance(1000);
            Assert.That(game.TryTakeMilestone(out _), Is.False);
        }

        [Test]
        public void LegacyProgressReconcilesWithoutCelebrationsOrLostLevels()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 10000, coils = 8, plasma = 3 });
            Assert.That(game.BaseProduction, Is.EqualTo(44));
            Assert.That(game.HasMilestone("plasma"), Is.True);
            Assert.That(game.HasMilestone("quantum"), Is.True);
            Assert.That(game.TryTakeMilestone(out _), Is.False);
        }

        [Test]
        public void RecordedUnlockIsNotLostWhenThresholdIsRebalanced()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 1000 });
            var settings = new EconomyConfig { plasmaUnlock = 2000 };
            var restored = new GameEngine(game.Snapshot(), settings);
            Assert.That(restored.Unlocked("plasma"), Is.True);
            Assert.That(restored.TryTakeMilestone(out _), Is.False);
        }

        [Test]
        public void CatalogIsDeepCopiedIncludingGatesAndProduction()
        {
            var settings = new EconomyConfig();
            var game = new GameEngine(new GameState { coils = 1 }, settings);
            settings.generators[0].baseProduction = 999;
            settings.milestones[0].target = .1;
            settings.upgrades[0].name = "changed";
            game.Advance(1);
            Assert.That(game.Energy, Is.EqualTo(1));
            Assert.That(game.HasMilestone("ignition"), Is.False);
            Assert.That(game.Shop[0].Name, Is.EqualTo("POTENCIA"));
        }

        [Test]
        public void DuplicateIdsAndCyclicOrImpossibleUnlocksAreRejected()
        {
            var config = new EconomyConfig();
            config.generators[2].id = "coil";
            Assert.Throws<ArgumentException>(() => new GameEngine(settings: config));
            config = new EconomyConfig();
            config.milestones[2].goal = MilestoneGoal.GeneratorLevel;
            config.milestones[2].generatorId = "plasma";
            config.milestones[2].target = 1;
            Assert.Throws<ArgumentException>(() => new GameEngine(settings: config));
            config = new EconomyConfig();
            config.milestones[1].target = 101;
            Assert.Throws<ArgumentException>(() => new GameEngine(settings: config));
            config = new EconomyConfig();
            config.generators[2].productionGrowth = double.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => new GameEngine(settings: config));
        }

        [Test]
        public void RetiredGeneratorProgressIsPreservedWhenCatalogChanges()
        {
            var initial = new GameState { energy = 5000, lifetimeEnergy = 5000,
                generators = new[] { new GeneratorLevel { id = "retired_cell", level = 4 } } };
            var game = new GameEngine(initial);
            game.Buy("quantum");
            Assert.That(game.BaseProduction, Is.EqualTo(60)); // retired content does not produce.
            Assert.That(game.Snapshot().generators[0].id, Is.EqualTo("retired_cell"));
            Assert.That(game.Snapshot().generators[0].level, Is.EqualTo(4));
        }

        [Test]
        public void SavedCatalogCapacityIsCheckedBeforeGameplayMutatesState()
        {
            var entries = new GeneratorLevel[32];
            for (int i = 0; i < entries.Length; i++) entries[i] = new GeneratorLevel { id = "retired_" + i, level = 1 };
            Assert.Throws<ArgumentException>(() => new GameEngine(new GameState { generators = entries }));
        }

        [Test]
        public void DuplicateOrInvalidSavedProgressIsRejected()
        {
            Assert.That(new GameState { completedMilestones = new[] { "plasma", "plasma" } }.IsValid(), Is.False);
            Assert.That(new GameState { generators = new[] { new GeneratorLevel { id = "quantum", level = -1 } } }.IsValid(), Is.False);
            Assert.That(new GameState { generators = new[] { new GeneratorLevel { id = "coil", level = 1 } } }.IsValid(), Is.False);
        }
    }
}
