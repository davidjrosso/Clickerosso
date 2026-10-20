using System;
using UnityEngine;

namespace ReactorDeCaos
{
    public sealed class UnityJsonCodec : IJsonCodec
    {
        public string Serialize<T>(T value) { return JsonUtility.ToJson(value); }
        public T Deserialize<T>(string json)
        {
            try { return JsonUtility.FromJson<T>(json); }
            catch (ArgumentException error) { throw new FormatException("Invalid save JSON.", error); }
        }
    }
}
