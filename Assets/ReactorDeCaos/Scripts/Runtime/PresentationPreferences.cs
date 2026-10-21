using System;
using UnityEngine;

namespace ReactorDeCaos
{
    [Serializable]
    public sealed class PresentationPreferences
    {
        public const string StorageKey = "reactor.presentation.v1";
        public int version = 2;
        public bool sound = true; // Existing preference now controls effects only.
        public bool music = true;
        public float effectsVolume = .75f;
        public float musicVolume = .35f;
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
                if (settings.version < 1 || settings.version > 2) return new PresentationPreferences();
                // Missing version-1 fields retain constructor defaults. Respect the old mute.
                if (!string.IsNullOrEmpty(json) && (!json.Contains("\"music\"") || settings.version == 1))
                    settings.music = settings.sound;
                settings.version = 2;
                settings.Normalize();
            }
            catch (ArgumentException) { return new PresentationPreferences(); }
            return settings;
        }

        private void Normalize()
        {
            effectsVolume = float.IsNaN(effectsVolume) || float.IsInfinity(effectsVolume) ? .75f : Mathf.Clamp01(effectsVolume);
            musicVolume = float.IsNaN(musicVolume) || float.IsInfinity(musicVolume) ? .35f : Mathf.Clamp01(musicVolume);
        }

        public void PreviewChanges() { Normalize(); Changed?.Invoke(); }

        public void Save()
        {
            Normalize();
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
        public void SaveAndNotify() { Save(); Changed?.Invoke(); }
    }
}
