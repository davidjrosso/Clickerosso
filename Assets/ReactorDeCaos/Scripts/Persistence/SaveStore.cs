using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ReactorDeCaos
{
    public interface IJsonCodec
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string json);
    }

    [Serializable]
    public sealed class SaveEnvelope
    {
        public int version = 1;
        public string payload;
        public string checksum;
    }

    public sealed class SaveStore
    {
        private readonly string path;
        private readonly IJsonCodec codec;
        public string LastIssue { get; private set; }
        public bool RecoveredBackup { get; private set; }
        public bool LoadedExisting { get; private set; }
        private const int MaxSaveBytes = 65536;

        public SaveStore(string path, IJsonCodec codec)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        public GameState Load()
        {
            LastIssue = null;
            RecoveredBackup = false;
            LoadedExisting = false;
            GameState state = TryRead(path);
            if (state != null) { LoadedExisting = true; return state; }
            state = TryRead(path + ".bak");
            if (state != null) { RecoveredBackup = true; LoadedExisting = true; return state; }
            return new GameState();
        }

        public bool Save(GameState state)
        {
            LastIssue = null;
            try
            {
                if (state == null || !state.IsValid()) throw new InvalidDataException("Invalid game state.");
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string payload = codec.Serialize(state);
                string text = codec.Serialize(new SaveEnvelope { payload = payload, checksum = Hash(payload) });
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    // Preserve a known good backup when the primary file was corrupt.
                    bool validPrimary = TryRead(path) != null;
                    File.Replace(path + ".tmp", path, validPrimary ? path + ".bak" : null);
                }
                else File.Move(path + ".tmp", path);
                LastIssue = null;
                return true;
            }
            catch (Exception error) when (Recoverable(error))
            {
                LastIssue = error.GetType().Name;
                return false;
            }
        }

        private GameState TryRead(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                if (new FileInfo(file).Length > MaxSaveBytes) throw new InvalidDataException("Save too large.");
                SaveEnvelope envelope = codec.Deserialize<SaveEnvelope>(File.ReadAllText(file));
                if (envelope == null || envelope.version != 1 || string.IsNullOrEmpty(envelope.payload)
                    || envelope.checksum != Hash(envelope.payload))
                    throw new InvalidDataException("Invalid save checksum or schema.");
                GameState state = codec.Deserialize<GameState>(envelope.payload);
                if (state == null || !state.IsValid()) throw new InvalidDataException("Invalid save values.");
                return state;
            }
            catch (Exception error) when (Recoverable(error))
            {
                LastIssue = error.GetType().Name;
                return null;
            }
        }

        private static bool Recoverable(Exception error)
        {
            return error is IOException || error is InvalidDataException || error is UnauthorizedAccessException
                || error is ArgumentException || error is FormatException
                || error is System.Runtime.Serialization.SerializationException;
        }

        private static string Hash(string text)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }
}
