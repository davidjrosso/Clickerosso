using System;
using System.IO;
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
