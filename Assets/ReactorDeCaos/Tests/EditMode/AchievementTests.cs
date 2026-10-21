using System;
using NUnit.Framework;

namespace ReactorDeCaos.Tests
{
    public sealed class AchievementTests
    {
        [Test]
        public void OlderSavesCatchUpWithoutAutomaticallyGrantingRewards()
        {
            var game = new GameEngine(new GameState { energy = 5, lifetimeEnergy = 100, coils = 1,
                unlockedAchievements = null, claimedAchievements = null });
            Assert.That(game.AchievementUnlocked("first_spark"), Is.True);
            Assert.That(game.AchievementUnlocked("first_machine"), Is.True);
            Assert.That(game.Energy, Is.EqualTo(5));
            Assert.That(game.AvailableAchievements, Is.EqualTo(2));
        }
        [Test]
        public void LockedUnknownAndRepeatedClaimsNeverWriteOrGrantEnergy()
        {
            var game = new GameEngine();
            int writes = 0;
            Func<GameState, bool> save = x => { writes++; return true; };
            Assert.That(game.TryClaimAchievement("unknown", save), Is.False);
            Assert.That(game.TryClaimAchievement("first_spark", save), Is.False);
            Assert.That(writes, Is.Zero);
            for (int i = 0; i < 100; i++) game.Tap();
            double before = game.Energy;
            Assert.That(game.TryClaimAchievement("first_spark", save), Is.True);
            Assert.That(game.Energy, Is.EqualTo(before + 25));
            Assert.That(game.TryClaimAchievement("first_spark", save), Is.False);
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void CandidateIsDurableBeforeLiveRewardAndTimersStayIntact()
        {
            var game = new GameEngine();
            for (int i = 0; i < 100; i++) game.Tap();
            game.ActivateOverload(); game.Advance(.5);
            double before = game.Energy, remaining = game.Overload.Remaining;
            int combo = game.Combo.Count;
            Assert.That(game.TryClaimAchievement("first_spark", candidate =>
            {
                Assert.That(game.Energy, Is.EqualTo(before));
                Assert.That(game.AchievementClaimed("first_spark"), Is.False);
                Assert.That(candidate.energy, Is.EqualTo(before + 25));
                Assert.That(candidate.claimedAchievements, Does.Contain("first_spark"));
                Assert.That(candidate.IsValid(), Is.True);
                return true;
            }), Is.True);
            Assert.That(game.Overload.Remaining, Is.EqualTo(remaining));
            Assert.That(game.Combo.Count, Is.EqualTo(combo));
        }
        [Test]
        public void FailedSaveKeepsTheClaimAvailableAndBalanceUnchanged()
        {
            var game = new GameEngine(new GameState { energy = 100, lifetimeEnergy = 100 });
            Assert.That(game.TryClaimAchievement("first_spark", _ => false), Is.False);
            Assert.That(game.Energy, Is.EqualTo(100));
            Assert.That(game.Lifetime, Is.EqualTo(100));
            Assert.That(game.CanClaimAchievement("first_spark"), Is.True);
            Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.True);
        }
        [Test]
        public void RecursiveClaimCannotDuplicateAReward()
        {
            var game = new GameEngine(new GameState { energy = 100, lifetimeEnergy = 100 });
            Assert.That(game.TryClaimAchievement("first_spark", _ =>
            {
                Assert.That(game.TryClaimAchievement("first_spark", __ => true), Is.False);
                return true;
            }), Is.True);
            Assert.That(game.Energy, Is.EqualTo(125));
        }
        [Test]
        public void AscensionKeepsClaimsAndPendingGeneratorAchievements()
        {
            var game = new GameEngine(new GameState { energy = 1000000, lifetimeEnergy = 1000000, coils = 5 });
            game.TryClaimAchievement("first_spark", _ => true);
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.AchievementClaimed("first_spark"), Is.True);
            Assert.That(next.CanClaimAchievement("micro_network"), Is.True);
            Assert.That(next.CanClaimAchievement("first_ascent"), Is.True);
            Assert.That(next.Energy, Is.Zero);
            Assert.That(next.TryClaimAchievement("first_ascent", _ => true), Is.True);
            Assert.That(next.Energy, Is.EqualTo(100));
            Assert.That(next.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void ClaimAtTheEnergyLimitStaysPendingInsteadOfLosingTheReward()
        {
            var game = new GameEngine(new GameState { energy = EconomyConfig.MaxEnergy, lifetimeEnergy = EconomyConfig.MaxEnergy });
            Assert.That(game.AchievementUnlocked("first_spark"), Is.True);
            Assert.That(game.CanClaimAchievement("first_spark"), Is.False);
            Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.False);
            Assert.That(game.AchievementClaimed("first_spark"), Is.False);
        }
        [Test]
        public void InvalidLedgersAreRejectedAndSnapshotsDoNotAlias()
        {
            Assert.That(new GameState { claimedAchievements = new[] { "first_spark" } }.IsValid(), Is.False);
            Assert.That(new GameState { unlockedAchievements = new[] { "first_spark", "first_spark" } }.IsValid(), Is.False);
            var game = new GameEngine(new GameState { lifetimeEnergy = 100 });
            var snapshot = game.Snapshot(); snapshot.unlockedAchievements[0] = "changed";
            Assert.That(game.AchievementUnlocked("first_spark"), Is.True);
        }
        [Test]
        public void AchievementCatalogIsConfigurableAndCopied()
        {
            var config = new EconomyConfig();
            config.achievements[0].target = 10; config.achievements[0].energyReward = 7;
            var game = new GameEngine(new GameState { energy = 10, lifetimeEnergy = 10 }, config);
            config.achievements[0].energyReward = 999;
            game.AchievementAt(0).energyReward = 999;
            Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.True);
            Assert.That(game.Energy, Is.EqualTo(17));
            config.achievements[0].energyReward = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEngine(settings: config));
        }
        [Test]
        public void TenMinuteSessionWithClaimsPurchasesAndAscensionRemainsValid()
        {
            var game = new GameEngine();
            int ascensions = 0;
            for (int i = 0; i < 6000; i++)
            {
                game.Advance(.1); if (i % 5 == 0) game.Tap();
                if (game.Overload.Ready) game.ActivateOverload();
                if (i % 10 == 0)
                {
                    foreach (var entry in game.Shop) if (game.CanBuy(entry.Id)) game.Buy(entry.Id);
                    for (int a = 0; a < game.AchievementCount; a++) game.TryClaimAchievement(game.AchievementAt(a).id, x => x.IsValid());
                    if (game.CanRebirth && ascensions < 2)
                    {
                        Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
                        game = next; ascensions++;
                    }
                    Assert.That(game.Snapshot().IsValid(), Is.True);
                }
            }
            Assert.That(ascensions, Is.GreaterThanOrEqualTo(1));
            Assert.That(game.AchievementClaimed("first_spark"), Is.True);
            Assert.That(game.AchievementClaimed("first_ascent"), Is.True);
        }
    }
}
