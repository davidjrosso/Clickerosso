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
