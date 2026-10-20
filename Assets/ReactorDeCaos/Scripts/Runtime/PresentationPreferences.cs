using System;
using UnityEngine;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class PresentationPreferences
    {
        public const string StorageKey = "reactor.presentation.v1";
        public int version = 1;
        public bool sound = true;
        public bool reducedFlashes;
        public bool reducedMotion;
        public bool limitedEffects;
        public bool vibration; // Explicit opt-in; never vibrate for every tap.
        public event Action Changed;

        public static PresentationPreferences Load()
        {
            var settings = new PresentationPreferences();
            try
            {
                string json = PlayerPrefs.GetString(StorageKey, "");
                if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, settings);
                if (settings.version != 1) return new PresentationPreferences();
            }
            catch (ArgumentException) { return new PresentationPreferences(); }
            return settings;
        }

        public void SaveAndNotify()
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
