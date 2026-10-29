using System;
using System.IO;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using StationCodec = ReactorDeCaos.UnityJsonCodec;
#else
using StationCodec = ReactorDeCaos.Tests.NetJsonCodec;
#endif

namespace ReactorDeCaos.Tests
{
    public sealed class StationTests
    {
        private static GameEngine Game(int chapter = 4, double energy = 100000, int coils = 5)
        {
            var s = GameEngine.NewCampaignState(); s.energy = s.lifetimeEnergy = energy; s.coils = coils;
            s.story.completed = s.story.read = s.story.narrative.scenesSeen = chapter;
            s.story.narrative.introSeen = s.story.narrative.briefingSeen = true;
            if (chapter == 6) s.story.choice = 1;
            return new GameEngine(s);
        }
        private static void Greenhouse(GameEngine game)
        {
            Assert.That(game.TryRestoreRoom(0, _ => true), Is.True);
            Assert.That(game.TryFinishStationMemory(0, _ => true), Is.True);
        }
        private static void Capsule(GameEngine game)
        {
            Greenhouse(game);
            Assert.That(game.TryStartExpedition(_ => true), Is.True);
            game.Advance(StationCatalog.ExpeditionSeconds);
            Assert.That(game.TryClaimExpedition(_ => true), Is.True);
            Assert.That(game.TryFinishStationMemory(1, _ => true), Is.True);
        }
        [Test]
        public void RoomsRespectCampaignPricesSequenceAndMemoryConsequences()
        {
            var locked = Game(0); Assert.That(locked.CanRestoreRoom(0), Is.False);
            var game = Game(1); double balance = game.Energy, rate = game.BaseProduction;
            Assert.That(game.TryRestoreRoom(1, _ => true), Is.False);
            Assert.That(game.TryRestoreRoom(0, _ => false), Is.False);
            Assert.That(game.Energy, Is.EqualTo(balance));
            Assert.That(game.TryRestoreRoom(0, _ => true), Is.True);
            Assert.That(game.Energy, Is.EqualTo(balance - 150));
            Assert.That(game.BaseProduction, Is.EqualTo(rate * 1.05).Within(.00001));
            Assert.That(game.ProductionOf("coil"), Is.EqualTo(game.BaseProduction));
            Assert.That(game.TryRestoreRoom(0, _ => true), Is.False);
            Assert.That(game.CanStartExpedition, Is.False);
            Assert.That(game.TryFinishStationMemory(1, _ => true), Is.False);
            Assert.That(game.TryFinishStationMemory(0, _ => false), Is.False);
            Assert.That(game.TryFinishStationMemory(0, _ => true), Is.True);
            Assert.That(game.CanStartExpedition, Is.True);
            Assert.That(game.RoomRequirement(1), Does.Contain("La puerta roja"));
        }
        [Test]
        public void InsufficientEnergyCannotRepairOrGoNegative()
        {
            var game = Game(1, 149, 0);
            Assert.That(game.TryRestoreRoom(0, _ => true), Is.False);
            Assert.That(game.Energy, Is.EqualTo(149));
            game = Game(1, 150, 0);
            Assert.That(game.TryRestoreRoom(0, _ => true), Is.True);
            Assert.That(game.Energy, Is.Zero);
            Assert.That(game.Snapshot().IsValid(), Is.True);
        }
        [Test]
        public void ExpeditionUsesRealSecondsHasOneSlotAndCannotBeClaimedEarlyOrTwice()
        {
            var game = Game(); Greenhouse(game);
            Assert.That(game.TryStartExpedition(_ => false), Is.False);
            Assert.That(game.ExpeditionActive, Is.False);
            Assert.That(game.TryStartExpedition(_ => true), Is.True);
            double reward = game.ExpeditionReward;
            Assert.That(game.TryStartExpedition(_ => true), Is.False);
            Assert.That(game.TryClaimExpedition(_ => true), Is.False);
            for (int i = 0; i < 1000; i++) game.Advance(.1);
            Assert.That(game.ExpeditionRemaining, Is.EqualTo(80).Within(.00001));
            game.Advance(80.01);
            Assert.That(game.ExpeditionReady, Is.True);
            Assert.That(game.TryClaimExpedition(_ => false), Is.False);
            double balance = game.Energy;
            Assert.That(game.TryClaimExpedition(_ => true), Is.True);
            Assert.That(game.Energy, Is.EqualTo(balance + reward).Within(.00001));
            Assert.That(game.ExpeditionClaims, Is.EqualTo(1));
            Assert.That(game.TryClaimExpedition(_ => true), Is.False);
            Assert.That(game.PendingStationMemory, Is.EqualTo(1));
        }
        [Test]
        public void ExpeditionRewardIsQuotedAtLaunchAndReturnNeverExpires()
        {
            var game = Game(); Greenhouse(game); game.TryStartExpedition(_ => true);
            double quoted = game.ExpeditionReward; game.Buy("coil"); game.ActivateOverload();
            game.TryCheckpointStation(1000, _ => true);
            Assert.That(game.TryResumeStation(1000 + 86400 * 30, _ => true), Is.True);
            Assert.That(game.ExpeditionReady, Is.True);
            Assert.That(game.ExpeditionReward, Is.EqualTo(quoted));
            var loaded = new GameEngine(game.Snapshot());
            loaded.TryResumeStation(1000 + 86400 * 60, _ => true);
            Assert.That(loaded.ExpeditionReady, Is.True);
            Assert.That(loaded.ExpeditionReward, Is.EqualTo(quoted));
        }
        [Test]
        public void FullMaraArcHasAnEndingAndRepeatsDoNotReopenIt()
        {
            var game = Game(); Capsule(game);
            Assert.That(game.TryRestoreRoom(1, _ => true), Is.True);
            Assert.That(game.CanRestoreRoom(2), Is.False);
            Assert.That(game.TryFinishStationMemory(2, _ => true), Is.True);
            Assert.That(game.TryRestoreRoom(2, _ => true), Is.True);
            Assert.That(game.TryFinishStationMemory(3, _ => true), Is.True);
            Assert.That(game.StationMemories, Is.EqualTo(4));
            Assert.That(game.PendingStationMemory, Is.EqualTo(-1));
            Assert.That(game.StationProductionMultiplier, Is.EqualTo(1.15).Within(.00001));
            Assert.That(game.StationNextStep, Does.Contain("hogar"));
            game.TryStartExpedition(_ => true); game.Advance(180); game.TryClaimExpedition(_ => true);
            Assert.That(game.ExpeditionClaims, Is.EqualTo(2));
            Assert.That(game.PendingStationMemory, Is.EqualTo(-1));
            Assert.That(game.TryFinishStationMemory(3, _ => true), Is.False);
        }
        [Test]
        public void OfflineClaimsDoNotBypassTheMainCampaign()
        {
            var game = Game(1, 1000);
            game.TryCheckpointStation(1000, _ => true);
            game.TryResumeStation(29800, _ => true);
            Assert.That(game.TryClaimOffline(_ => true), Is.True);
            Assert.That(game.Lifetime, Is.GreaterThan(100000));
            Assert.That(game.StoryCompleted, Is.EqualTo(1));
            Assert.That(game.Unlocked("plasma"), Is.False);
            Assert.That(game.CanRebirth, Is.False);
        }
        [Test]
        public void NoGeneratorsMeansNoAbsenceIncomeButTheDroneCanStillReturn()
        {
            var game = Game(1, 1000, 0); Greenhouse(game);
            game.TryStartExpedition(_ => true);
            game.TryCheckpointStation(1000, _ => true);
            game.TryResumeStation(2000, _ => true);
            Assert.That(game.PendingOfflineEnergy, Is.Zero);
            Assert.That(game.ExpeditionReady, Is.True);
            Assert.That(game.TryClaimExpedition(_ => true), Is.True);
            Assert.That(game.ExpeditionClaims, Is.EqualTo(1));
        }
        [Test]
        public void FirstVisitAndOldCampaignSavesNeverInventRetroactiveIncome()
        {
            var game = Game(); Assert.That(game.Snapshot().station.lastUtcSeconds, Is.Zero);
            Assert.That(game.TryResumeStation(1700000000, _ => true), Is.True);
            Assert.That(game.PendingOfflineEnergy, Is.Zero);
            Assert.That(game.PendingOfflineSeconds, Is.Zero);
        }
        [TestCase(60, 60)]
        [TestCase(28800, 28800)]
        [TestCase(864000, 28800)]
        public void AbsenceUsesSavedBaseRateAndEightHourCap(long absent, long credited)
        {
            var game = Game(); double rate = game.BaseProduction, balance = game.Energy;
            for (int i = 0; i < 25; i++) game.Tap(); game.ActivateOverload();
            Assert.That(game.Production, Is.GreaterThan(rate));
            Assert.That(game.TryCheckpointStation(1000, _ => true), Is.True);
            balance = game.Energy;
            var resumed = new GameEngine(game.Snapshot());
            Assert.That(resumed.TryResumeStation(1000 + absent, _ => true), Is.True);
            Assert.That(resumed.PendingOfflineEnergy, Is.EqualTo(rate * credited));
            Assert.That(resumed.Energy, Is.EqualTo(balance), "Return stages a claim instead of silently spending it.");
            Assert.That(resumed.TryResumeStation(1000 + absent, _ => true), Is.True);
            Assert.That(resumed.PendingOfflineEnergy, Is.EqualTo(rate * credited), "Same interval cannot be settled twice.");
            Assert.That(resumed.TryClaimOffline(_ => false), Is.False);
            Assert.That(resumed.TryClaimOffline(_ => true), Is.True);
            Assert.That(resumed.Energy, Is.EqualTo(balance + rate * credited));
            Assert.That(resumed.TryClaimOffline(_ => true), Is.False);
        }
        [Test]
        public void BackwardClockAndFailedPauseCheckpointCannotDoubleCountActiveTime()
        {
            var game = Game(); double rate = game.BaseProduction;
            game.TryCheckpointStation(1000, _ => true); game.Advance(10);
            Assert.That(game.TryCheckpointStation(1010, _ => false), Is.False);
            Assert.That(game.TryResumeStation(1020, _ => true, 1010), Is.True);
            Assert.That(game.PendingOfflineEnergy, Is.EqualTo(rate * 10));
            game.TryResumeStation(1000, _ => true, 1030);
            Assert.That(game.Snapshot().station.lastUtcSeconds, Is.EqualTo(1030));
            game.TryResumeStation(1025, _ => true);
            Assert.That(game.PendingOfflineEnergy, Is.EqualTo(rate * 10));
            game.TryResumeStation(1035, _ => true);
            Assert.That(game.PendingOfflineEnergy, Is.EqualTo(rate * 15));
        }
        [Test]
        public void FailedArrivalCanBeRetriedWithoutLosingOrDuplicatingRewards()
        {
            var game = Game(); Greenhouse(game); game.TryStartExpedition(_ => true);
            game.TryCheckpointStation(1000, _ => true);
            Assert.That(game.TryResumeStation(2000, _ => false), Is.False);
            Assert.That(game.PendingOfflineEnergy, Is.Zero);
            Assert.That(game.ExpeditionRemaining, Is.EqualTo(180));
            Assert.That(game.TryResumeStation(2000, _ => true), Is.True);
            Assert.That(game.PendingOfflineEnergy, Is.EqualTo(game.BaseProduction * 1000));
            Assert.That(game.ExpeditionReady, Is.True);
        }
        [Test]
        public void UnclaimedProductionSurvivesLaterSessionsAndRebirth()
        {
            var game = Game(6, 1000000); Capsule(game);
            game.TryStartExpedition(_ => true); game.TryCheckpointStation(1000, _ => true);
            game.TryResumeStation(1060, _ => true);
            double pending = game.PendingOfflineEnergy; double remaining = game.ExpeditionRemaining;
            Assert.That(game.TryPrepareRebirth(Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.RestoredRooms, Is.EqualTo(1));
            Assert.That(next.StationMemories, Is.EqualTo(2));
            Assert.That(next.PendingOfflineEnergy, Is.EqualTo(pending));
            Assert.That(next.ExpeditionRemaining, Is.EqualTo(remaining));
            Assert.That(next.Snapshot().station.offlineRate, Is.EqualTo(next.BaseProduction));
            Assert.That(next.TryClaimOffline(_ => true), Is.True);
            Assert.That(next.Energy, Is.EqualTo(pending));
        }
        [Test]
        public void TransactionsDetachCandidatesRejectReentryAndKeepComboTimers()
        {
            var game = Game(); for (int i = 0; i < 25; i++) game.Tap(); game.ActivateOverload();
            double remaining = game.Overload.Remaining; int combo = game.Combo.Count;
            Assert.That(game.TryRestoreRoom(0, candidate => {
                Assert.That(game.TryRestoreRoom(0, _ => true), Is.False);
                Assert.That(game.TryClaimAchievement("first_spark", _ => true), Is.False);
                candidate.station.rooms = 3; candidate.energy = 0; return true;
            }), Is.True);
            Assert.That(game.RestoredRooms, Is.EqualTo(1));
            Assert.That(game.Energy, Is.GreaterThan(0));
            Assert.That(game.Overload.Remaining, Is.EqualTo(remaining));
            Assert.That(game.Combo.Count, Is.EqualTo(combo));
            var detached = game.Snapshot(); detached.station.rooms = 0;
            Assert.That(game.RestoredRooms, Is.EqualTo(1));
        }
        [Test]
        public void CappedBalancesRetainClaimsAndInvalidValuesAreRejected()
        {
            var game = Game(4, EconomyConfig.MaxEnergy);
            game.TryCheckpointStation(1000, _ => true); game.TryResumeStation(2000, _ => true);
            double pending = game.PendingOfflineEnergy;
            Assert.That(pending, Is.GreaterThan(0));
            Assert.That(game.TryClaimOffline(_ => true), Is.False);
            Assert.That(game.PendingOfflineEnergy, Is.EqualTo(pending));
            foreach (var bad in new[] {
                new StationProgress { rooms = 4 }, new StationProgress { rooms = 2 },
                new StationProgress { memories = 1 }, new StationProgress { expeditionActive = true },
                new StationProgress { offlineRate = double.NaN }, new StationProgress { pendingEnergy = double.PositiveInfinity },
                new StationProgress { expeditionRemaining = -1 }, new StationProgress { lastUtcSeconds = -1 },
                new StationProgress { expeditionClaims = 1 } }) Assert.That(bad.IsValid(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => game.TryResumeStation(-1, _ => true));
        }
        [Test]
        public void JsonSaveAndBackupRetainRoomsPendingReturnAndMaraMemory()
        {
            string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try
            {
                var store = new SaveStore(Path.Combine(folder, "station.json"), new StationCodec());
                var game = Game(); Capsule(game); game.TryRestoreRoom(1, _ => true);
                game.TryCheckpointStation(1000, store.Save); game.TryResumeStation(2000, store.Save);
                var loaded = new GameEngine(store.Load());
                Assert.That(loaded.RestoredRooms, Is.EqualTo(2));
                Assert.That(loaded.PendingStationMemory, Is.EqualTo(2));
                Assert.That(loaded.PendingOfflineEnergy, Is.EqualTo(game.PendingOfflineEnergy));
                Assert.That(loaded.TryClaimOffline(store.Save), Is.True);
                Assert.That(new GameEngine(store.Load()).PendingOfflineEnergy, Is.Zero);
                File.WriteAllText(Path.Combine(folder, "station.json"), "corrupt");
                loaded = new GameEngine(store.Load());
                Assert.That(store.RecoveredBackup, Is.True);
                Assert.That(loaded.PendingOfflineEnergy, Is.GreaterThan(0));
            }
            finally { Directory.Delete(folder, true); }
        }
    }
}
