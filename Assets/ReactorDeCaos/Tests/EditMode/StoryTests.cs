using System;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class StoryTests
    {
        [Test]
        public void OldSavesCatchUpWithoutInventingActionsOrReplayingReadRecords()
        {
            var game = new GameEngine(new GameState { energy = 7, lifetimeEnergy = 125000, coils = 3 });
            Assert.That(game.StoryCompleted, Is.EqualTo(5));
            Assert.That(game.StoryRead, Is.Zero);
            Assert.That(game.StoryChoice, Is.Zero);
            Assert.That(game.Snapshot().story.overloads, Is.Zero);
            Assert.That(game.Snapshot().story.bestCombo, Is.EqualTo(1));
            Assert.That(game.Energy, Is.EqualTo(7));
            Assert.That(game.Unlocked("auxiliary"), Is.True);
        }
        [TestCase(99, 0)]
        [TestCase(100, 0)]
        [TestCase(200, 1)]
        [TestCase(1000, 1)]
        [TestCase(1500, 2)]
        [TestCase(5000, 2)]
        [TestCase(7500, 3)]
        [TestCase(25000, 3)]
        [TestCase(30000, 4)]
        [TestCase(100000, 4)]
        [TestCase(125000, 5)]
        [TestCase(1000000, 6)]
        public void PassiveAlternativesCompleteEveryChapterWithoutAComboOrOverload(double energy, int completed)
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = energy });
            Assert.That(game.StoryCompleted, Is.EqualTo(completed));
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void BuyingAndActivatingMechanicsCompleteTheirObjectivesImmediately()
        {
            var game = new GameEngine(new GameState { energy = 100, lifetimeEnergy = 100 });
            Assert.That(game.Buy("coil"), Is.True);
            Assert.That(game.StoryCompleted, Is.EqualTo(1));
            game = new GameEngine(new GameState { energy = 5000, lifetimeEnergy = 5000 });
            Assert.That(game.StoryCompleted, Is.EqualTo(2));
            for (int i = 0; i < 25; i++) game.Tap();
            Assert.That(game.ActivateOverload(), Is.True);
            Assert.That(game.StoryCompleted, Is.EqualTo(3));
            Assert.That(game.ActivateOverload(), Is.False);
            Assert.That(game.Snapshot().story.overloads, Is.EqualTo(1));
            game = new GameEngine(new GameState { energy = 25000, lifetimeEnergy = 25000 });
            Assert.That(game.Buy("quantum"), Is.True);
            Assert.That(game.StoryCompleted, Is.EqualTo(4));
            game = new GameEngine(new GameState { energy = 100000, lifetimeEnergy = 100000 });
            Assert.That(game.Buy("auxiliary"), Is.True);
            Assert.That(game.StoryCompleted, Is.EqualTo(5));
        }
        [Test]
        public void ComboObjectiveSurvivesDecayAndReload()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 1000 });
            for (int i = 0; i < 25; i++) game.Tap();
            Assert.That(game.StoryCompleted, Is.EqualTo(2));
            game.Advance(100);
            game = new GameEngine(game.Snapshot());
            Assert.That(game.ComboMultiplier, Is.EqualTo(1));
            Assert.That(game.StoryCompleted, Is.EqualTo(2));
            Assert.That(game.Snapshot().story.bestCombo, Is.GreaterThanOrEqualTo(1.5));
        }
        [Test]
        public void LockedOutOfOrderAndRepeatedReadActionsNeverWrite()
        {
            var game = new GameEngine(); int writes = 0;
            Func<GameState, bool> save = _ => { writes++; return true; };
            Assert.That(game.TryReadStory(0, save), Is.False);
            Assert.That(game.TryChooseStory(1, save), Is.False);
            Assert.That(game.TryReadEpilogue(save), Is.False);
            game = new GameEngine(new GameState { lifetimeEnergy = 125000 });
            Assert.That(game.TryReadStory(1, save), Is.False);
            Assert.That(game.TryReadStory(-1, save), Is.False);
            Assert.That(game.TryReadStory(100, save), Is.False);
            Assert.That(writes, Is.Zero);
            Assert.That(game.TryReadStory(0, save), Is.True);
            Assert.That(game.TryReadStory(0, save), Is.False);
            Assert.That(writes, Is.EqualTo(1));
        }
        [Test]
        public void FailedOrThrowingSavePreservesTheLiveStoryAndAllowsRetry()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 125000 });
            Assert.That(game.TryReadStory(0, _ => false), Is.False);
            Assert.Throws<InvalidOperationException>(() => game.TryReadStory(0, _ => throw new InvalidOperationException()));
            Assert.That(game.StoryRead, Is.Zero);
            Assert.That(game.TryChooseStory(2, _ => false), Is.False);
            Assert.That(game.StoryChoice, Is.Zero);
            Assert.That(game.TryReadStory(0, _ => true), Is.True);
        }
        [Test]
        public void StoryTransactionsAreDetachedReentrancySafeAndKeepGameplayTimers()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 125000 });
            for (int i = 0; i < 25; i++) game.Tap();
            game.ActivateOverload(); game.Advance(.25);
            var before = game.Snapshot(); double remaining = game.Overload.Remaining; int combo = game.Combo.Count;
            Assert.That(game.TryReadStory(0, candidate =>
            {
                Assert.That(candidate.story.read, Is.EqualTo(1));
                Assert.That(game.StoryRead, Is.Zero);
                Assert.That(game.TryReadStory(0, _ => true), Is.False);
                Assert.That(game.TryChooseStory(1, _ => true), Is.False);
                candidate.story.read = 5; // Persistence cannot alias the live candidate.
                return true;
            }), Is.True);
            Assert.That(game.StoryRead, Is.EqualTo(1));
            Assert.That(game.Energy, Is.EqualTo(before.energy));
            Assert.That(game.Lifetime, Is.EqualTo(before.lifetimeEnergy));
            Assert.That(game.Overload.Remaining, Is.EqualTo(remaining));
            Assert.That(game.Combo.Count, Is.EqualTo(combo));
            var snapshot = game.Snapshot(); snapshot.story.read = 0;
            Assert.That(game.StoryRead, Is.EqualTo(1));
        }
        [Test]
        public void BothTuningsHaveIdenticalEconomyAndMayBeChangedWithoutRewards()
        {
            var game = new GameEngine(new GameState { energy = 125000, lifetimeEnergy = 125000, coils = 5 });
            double tap = game.TapPower, production = game.BaseProduction, cost = game.Cost("plasma");
            foreach (int choice in new[] { 1, 2, 1 })
            {
                Assert.That(game.TryChooseStory(choice, _ => true), Is.True);
                Assert.That(game.StoryChoice, Is.EqualTo(choice));
                Assert.That(game.Energy, Is.EqualTo(125000));
                Assert.That(game.TapPower, Is.EqualTo(tap));
                Assert.That(game.BaseProduction, Is.EqualTo(production));
                Assert.That(game.Cost("plasma"), Is.EqualTo(cost));
            }
            Assert.That(game.TryChooseStory(1, _ => true), Is.False);
            Assert.That(game.TryChooseStory(0, _ => true), Is.False);
            Assert.That(game.TryChooseStory(3, _ => true), Is.False);
        }
        [Test]
        public void AscensionKeepsBlueprintsLogsAndChoiceAndUnlocksEpilogueOnlyOnce()
        {
            var game = new GameEngine(new GameState { energy = 1000000, lifetimeEnergy = 1000000 });
            for (int i = 0; i < StoryCatalog.Count; i++) Assert.That(game.TryReadStory(i, _ => true), Is.True);
            game.TryChooseStory(2, _ => true);
            Assert.That(game.StoryEpilogueAvailable, Is.False);
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.StoryCompleted, Is.EqualTo(6));
            Assert.That(next.StoryRead, Is.EqualTo(6));
            Assert.That(next.StoryChoice, Is.EqualTo(2));
            Assert.That(next.StoryEpilogueAvailable, Is.True);
            Assert.That(next.UnreadStory, Is.EqualTo(1));
            foreach (string id in new[] { "plasma", "quantum", "auxiliary" })
            {
                Assert.That(next.Unlocked(id), Is.True);
                Assert.That(next.Level(id), Is.Zero);
                Assert.That(next.CanBuy(id), Is.False, "Blueprints do not waive prices.");
            }
            Assert.That(next.TryReadEpilogue(_ => false), Is.False);
            Assert.That(next.StoryEpilogueRead, Is.False);
            Assert.That(next.TryReadEpilogue(_ => true), Is.True);
            Assert.That(next.TryReadEpilogue(_ => true), Is.False);
            Assert.That(new GameEngine(next.Snapshot()).UnreadStory, Is.Zero);
        }
        [Test]
        public void InvalidStoryStatesAreRejectedBeforeMigration()
        {
            Assert.That(new StoryProgress { completed = 6, epilogueRead = true }.IsValid(), Is.False);
            foreach (var invalid in new[] {
                new StoryProgress { completed = 7 }, new StoryProgress { completed = -1 },
                new StoryProgress { read = 1 }, new StoryProgress { choice = 1 },
                new StoryProgress { completed = 5, choice = 3 }, new StoryProgress { bestCombo = double.NaN },
                new StoryProgress { bestCombo = double.PositiveInfinity }, new StoryProgress { bestCoils = 101 },
                new StoryProgress { overloads = -1 }, new StoryProgress { completed = 6, epilogueRead = true } })
                Assert.Throws<ArgumentException>(() => new GameEngine(new GameState { story = invalid }));
        }
        [Test]
        public void AchievementEnergyCanCompleteStoryWithoutDuplicatingAReward()
        {
            var game = new GameEngine(new GameState { lifetimeEnergy = 190 });
            Assert.That(game.TryClaimAchievement("first_spark", _ => false), Is.False);
            Assert.That(game.StoryCompleted, Is.Zero);
            Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.True);
            Assert.That(game.StoryCompleted, Is.EqualTo(1));
            Assert.That(game.Lifetime, Is.EqualTo(215));
            Assert.That(new GameEngine(game.Snapshot()).StoryCompleted, Is.EqualTo(1));
        }
    }
}
