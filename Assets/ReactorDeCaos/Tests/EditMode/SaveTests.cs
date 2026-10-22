using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
#if !UNITY_5_3_OR_NEWER
using System.Text.Json;
#endif

namespace ReactorDeCaos.Tests
{
    public sealed class SaveTests
    {
        private string directory;
        private string path;
        private SaveStore store;
        private IJsonCodec codec;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "reactor-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "save.json");
#if UNITY_5_3_OR_NEWER
            codec = new UnityJsonCodec();
#else
            codec = new NetJsonCodec();
#endif
            store = new SaveStore(path, codec);
        }

        [TearDown]
        public void TearDown() { Directory.Delete(directory, true); }

        [Test]
        public void MissingSaveStartsFresh()
        {
            Assert.That(store.Load().energy, Is.Zero);
            Assert.That(store.LastIssue, Is.Null);
            Assert.That(store.LoadedExisting, Is.False);
        }

        [Test]
        public void RoundTripPreservesAllProgressFields()
        {
            var state = new GameState { energy = 123.5, lifetimeEnergy = 1500, coils = 4,
                plasma = 1, tapLevel = 2, comboLevel = 3, overloadLevel = 4 };
            Assert.That(store.Save(state), Is.True);
            GameState loaded = store.Load();
            Assert.That(codec.Serialize(loaded), Is.EqualTo(codec.Serialize(state)));
            Assert.That(store.LoadedExisting, Is.True);
        }

        [Test]
        public void StoryAndEpilogueRoundTripThroughTheRealSaveEnvelope()
        {
            var game = new GameEngine(new GameState { energy = 1000000, lifetimeEnergy = 1000000 });
            for (int i = 0; i < StoryCatalog.Count; i++) Assert.That(game.TryReadStory(i, store.Save), Is.True);
            Assert.That(game.TryChooseStory(2, store.Save), Is.True);
            Assert.That(RebirthTransaction.TryCommit(game, store, Specialization.Automation, game.RebirthReward, out var next), Is.True);
            Assert.That(next.TryReadEpilogue(store.Save), Is.True);
            var loaded = new GameEngine(store.Load());
            Assert.That(loaded.StoryRead, Is.EqualTo(6));
            Assert.That(loaded.StoryChoice, Is.EqualTo(2));
            Assert.That(loaded.StoryEpilogueRead, Is.True);
            Assert.That(loaded.Unlocked("auxiliary"), Is.True);
            Assert.That(codec.Serialize(loaded.Snapshot()), Is.EqualTo(codec.Serialize(next.Snapshot())));
        }

        [Test]
        public void CorruptPrimaryRecoversPreviousGoodSave()
        {
            store.Save(new GameState { energy = 10, lifetimeEnergy = 10 });
            store.Save(new GameState { energy = 20, lifetimeEnergy = 20 });
            File.WriteAllText(path, "{broken");
            Assert.That(store.Load().energy, Is.EqualTo(10));
            Assert.That(store.RecoveredBackup, Is.True);
            Assert.That(store.LastIssue, Is.Not.Null);
            Assert.That(store.Save(new GameState { energy = 30, lifetimeEnergy = 30 }), Is.True);
            File.WriteAllText(path, "{broken again");
            Assert.That(store.Load().energy, Is.EqualTo(10));
        }

        [Test]
        public void CorruptFilesFailSafely()
        {
            File.WriteAllText(path, "garbage");
            File.WriteAllText(path + ".bak", "also garbage");
            Assert.That(store.Load().energy, Is.Zero);
            Assert.That(store.LastIssue, Is.Not.Null);
        }

        [Test]
        public void ChecksumDetectsAlteredPayload()
        {
            store.Save(new GameState { energy = 10, lifetimeEnergy = 10 });
            SaveEnvelope envelope = codec.Deserialize<SaveEnvelope>(File.ReadAllText(path));
            envelope.payload = codec.Serialize(new GameState { energy = 999, lifetimeEnergy = 999 });
            File.WriteAllText(path, codec.Serialize(envelope));
            Assert.That(store.LoadedExisting, Is.False);
            Assert.That(store.Load().energy, Is.Zero);
            Assert.That(store.LastIssue, Is.Not.Null);
        }

        [Test]
        public void UnknownSchemaDoesNotLoad()
        {
            store.Save(new GameState());
            SaveEnvelope envelope = codec.Deserialize<SaveEnvelope>(File.ReadAllText(path));
            envelope.version = 99;
            File.WriteAllText(path, codec.Serialize(envelope));
            store.Load();
            Assert.That(store.LoadedExisting, Is.False);
            Assert.That(store.LastIssue, Is.Not.Null);
        }

        [Test]
        public void InvalidStateCannotOverwriteGoodSave()
        {
            store.Save(new GameState { energy = 10, lifetimeEnergy = 10 });
            Assert.That(store.Save(new GameState { energy = -1 }), Is.False);
            Assert.That(store.Load().energy, Is.EqualTo(10));
        }

        [Test]
        public void InterruptedTemporaryWriteDoesNotReplaceCommittedSave()
        {
            store.Save(new GameState { energy = 42, lifetimeEnergy = 42 });
            File.WriteAllText(path + ".tmp", "{unfinished");
            Assert.That(store.Load().energy, Is.EqualTo(42));
        }

        [Test]
        public void OversizedSaveIsRejected()
        {
            File.WriteAllText(path, new string('x', 65537));
            Assert.That(store.Load().energy, Is.Zero);
            Assert.That(store.LastIssue, Is.Not.Null);
        }

        [Test]
        public void UnwritableLocationReportsFailure()
        {
            File.WriteAllText(Path.Combine(directory, "not-a-directory"), "file");
            var blocked = new SaveStore(Path.Combine(directory, "not-a-directory", "save.json"), codec);
            Assert.That(blocked.Save(new GameState()), Is.False);
            Assert.That(blocked.LastIssue, Is.Not.Null);
        }

        [Test]
        public void ExpandedGeneratorsAndClaimedMilestonesSurviveDiskRoundTrip()
        {
            var game = new GameEngine(new GameState { energy = 25000, lifetimeEnergy = 25000 });
            game.Buy("quantum"); game.Buy("auxiliary");
            Assert.That(store.Save(game.Snapshot()), Is.True);
            var restored = new GameEngine(store.Load());
            Assert.That(restored.Level("quantum"), Is.EqualTo(1));
            Assert.That(restored.Level("auxiliary"), Is.EqualTo(1));
            Assert.That(restored.HasMilestone("auxiliary"), Is.True);
            Assert.That(restored.BaseProduction, Is.EqualTo(410));
            Assert.That(restored.TryTakeMilestone(out _), Is.False);
        }

        [Test]
        public void LowOrderBalancesSurviveSaveAndContinueAccumulating()
        {
            var game = new GameEngine(new GameState { energy = 1e16, lifetimeEnergy = 1e16, coils = 1 });
            game.Advance(.25);
            Assert.That(store.Save(game.Snapshot()), Is.True);
            var restored = new GameEngine(store.Load());
            for (int i = 0; i < 7; i++) restored.Advance(.25);
            Assert.That(restored.Energy, Is.EqualTo(1e16 + 2));
            Assert.That(restored.Snapshot().IsValid(), Is.True);
        }

        [Test]
        public void LegacyVersionOneWithoutRemaindersStillLoads()
        {
            const string payload = "{\"version\":1,\"energy\":42,\"lifetimeEnergy\":100,\"tapLevel\":1,\"comboLevel\":0,\"overloadLevel\":0,\"coils\":2,\"plasma\":0}";
            string checksum;
            using (var sha = SHA256.Create())
                checksum = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
            File.WriteAllText(path, codec.Serialize(new SaveEnvelope { payload = payload, checksum = checksum }));
            GameState loaded = store.Load();
            Assert.That(store.LoadedExisting, Is.True);
            Assert.That(loaded.energy, Is.EqualTo(42));
            Assert.That(loaded.coils, Is.EqualTo(2));
            Assert.That(loaded.energyRemainder, Is.Zero);
            Assert.That(loaded.lifetimeRemainder, Is.Zero);
        }

        [Test]
        public void RebirthIsCommittedBeforeReplacingTheLiveCycleAndSurvivesReload()
        {
            var old = new GameEngine(new GameState { energy = 10, lifetimeEnergy = 1000000 });
            Assert.That(store.Save(old.Snapshot()), Is.True);
            Assert.That(RebirthTransaction.TryCommit(old, store, Specialization.Automation, 3, out var next), Is.True);
            Assert.That(old.Lifetime, Is.EqualTo(1000000));
            Assert.That(next.Lifetime, Is.Zero);
            Assert.That(next.BuyMastery(Specialization.Tap), Is.True);
            next.SelectSkin(0);
            Assert.That(store.Save(next.Snapshot()), Is.True);
            var loaded = new GameEngine(store.Load());
            Assert.That(loaded.RebirthCount, Is.EqualTo(1));
            Assert.That(loaded.Essence, Is.EqualTo(2));
            Assert.That(loaded.MasteryLevel(Specialization.Tap), Is.EqualTo(1));
            Assert.That(loaded.Specialization, Is.EqualTo(Specialization.Automation));
            Assert.That(loaded.Skin, Is.Zero);
            Assert.That(loaded.AllTimeEnergy, Is.EqualTo(1000000));
        }

        [Test]
        public void FailedRebirthSaveKeepsEnergyPurchasesAndOverloadAlive()
        {
            File.WriteAllText(Path.Combine(directory, "blocked"), "file");
            var blocked = new SaveStore(Path.Combine(directory, "blocked", "save.json"), codec);
            var old = new GameEngine(new GameState { energy = 10, lifetimeEnergy = 1000000, coils = 5 });
            for (int i = 0; i < 25; i++) old.Tap();
            old.ActivateOverload();
            var before = codec.Serialize(old.Snapshot());
            Assert.That(RebirthTransaction.TryCommit(old, blocked, Specialization.Tap, 3, out var current), Is.False);
            Assert.That(current, Is.SameAs(old));
            Assert.That(codec.Serialize(current.Snapshot()), Is.EqualTo(before));
            Assert.That(current.Overload.Active, Is.True);
            Assert.That(current.Essence, Is.Zero);
        }

        [Test]
        public void AchievementClaimSurvivesDiskReloadAndFailedWriteIsRetryable()
        {
            var game = new GameEngine(new GameState { energy = 100, lifetimeEnergy = 100 });
            File.WriteAllText(Path.Combine(directory, "blocked-claim"), "file");
            var blocked = new SaveStore(Path.Combine(directory, "blocked-claim", "save.json"), codec);
            Assert.That(game.TryClaimAchievement("first_spark", blocked.Save), Is.False);
            Assert.That(game.Energy, Is.EqualTo(100));
            Assert.That(game.TryClaimAchievement("first_spark", store.Save), Is.True);
            var loaded = new GameEngine(store.Load());
            Assert.That(loaded.Energy, Is.EqualTo(125));
            Assert.That(loaded.AchievementClaimed("first_spark"), Is.True);
            Assert.That(loaded.TryClaimAchievement("first_spark", store.Save), Is.False);
        }

        [Test]
        public void InvalidLowOrderValuesCannotBeSaved()
        {
            Assert.That(store.Save(new GameState { energy = 10, lifetimeEnergy = 10, energyRemainder = 10 }), Is.False);
            Assert.That(store.Save(new GameState { energy = 10, lifetimeEnergy = 10, energyRemainder = double.NaN }), Is.False);
        }
    }

#if !UNITY_5_3_OR_NEWER
    internal sealed class NetJsonCodec : IJsonCodec
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
        public string Serialize<T>(T value) { return JsonSerializer.Serialize(value, options); }
        public T Deserialize<T>(string json)
        {
            try { return JsonSerializer.Deserialize<T>(json, options); }
            catch (JsonException error) { throw new FormatException("Invalid JSON", error); }
        }
    }
#endif
}
