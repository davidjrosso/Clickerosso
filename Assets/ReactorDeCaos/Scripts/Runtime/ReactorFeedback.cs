using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    // Presentation observes engine state; it never advances time or grants income.
    public sealed class ReactorFeedback : MonoBehaviour
    {
        private sealed class Particle
        {
            public RectTransform rect;
            public Graphic graphic;
            public Vector2 origin, velocity;
            public float age, duration, size, endSize;
            public Color color;
            public bool wave;
        }

        private ReactorHud hud;
        private PresentationPreferences preferences;
        private Particle[] numbers, waves, sparks;
        private int numberIndex, waveIndex, sparkIndex, voiceIndex;
        private float pulse, flash, comboMix, overloadMix;
        public bool DialogueActive { get; set; }
        public bool CinematicSilence { get; set; }
        private float recoilAge = 5;
        private readonly AudioSource[] tapVoices = new AudioSource[2];
        private AudioSource eventVoice, humVoice, comboVoice, unlockVoice, musicVoice;
        private readonly System.Collections.Generic.Dictionary<AudioSource, float> effectLevels = new System.Collections.Generic.Dictionary<AudioSource, float>();
        private AudioClip comboSound, unlockSound, ascensionSound;
        private float lastComboSound = -10;
        private bool musicPaused;
        public AudioSource MusicVoice => musicVoice;
        public int ComboRiseCount { get; private set; }
        public int GeneratorUnlockCount { get; private set; }
        private AudioClip tapSound, purchaseSound, overloadSound, endSound, rewardSound;
        private float lastSound = -10, lastBurst = -10, lastHaptic = -10;
        private bool wasOverloaded, suspended;
        public int OverloadEndCount { get; private set; }

        public void Initialize(ReactorHud view)
        {
            hud = view;
            preferences = view.Preferences;
            numbers = new Particle[12]; waves = new Particle[6]; sparks = new Particle[32];
            for (int i = 0; i < numbers.Length; i++)
            {
                var label = hud.Label("Gain" + i, hud.EffectsRoot, "", 42, ReactorHud.Cyan, .5f, .57f, .5f, .57f);
                label.rectTransform.sizeDelta = new Vector2(400, 80);
                numbers[i] = Make(label, 1, 1, false);
            }
            for (int i = 0; i < waves.Length; i++)
            {
                RingGraphic ring = ReactorHud.Ring("Wave" + i, hud.EffectsRoot, ReactorHud.Cyan);
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(.5f, .57f);
                ring.thickness = 3;
                waves[i] = Make(ring, .55f, 190, true);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                RectTransform rect = ReactorHud.Rect("Spark" + i, hud.EffectsRoot, .5f, .57f, .5f, .57f);
                var graphic = rect.gameObject.AddComponent<Image>();
                graphic.raycastTarget = false;
                rect.sizeDelta = new Vector2(5, 10);
                sparks[i] = Make(graphic, .45f, 1, false);
            }
            for (int i = 0; i < tapVoices.Length; i++) tapVoices[i] = Voice();
            eventVoice = Voice(); humVoice = Voice(); humVoice.loop = true;
            comboVoice = Voice(); unlockVoice = Voice(); musicVoice = Voice();
            musicVoice.loop = true; musicVoice.priority = 200;
            musicVoice.clip = Resources.Load<AudioClip>("Audio/AmbientMusic");
            comboSound = Resources.Load<AudioClip>("Audio/ComboRise");
            unlockSound = Resources.Load<AudioClip>("Audio/GeneratorUnlock");
            ascensionSound = Resources.Load<AudioClip>("Audio/Ascension");
            humVoice.clip = Resources.Load<AudioClip>("Audio/OverloadHum");
            tapSound = Resources.Load<AudioClip>("Audio/Tap");
            purchaseSound = Resources.Load<AudioClip>("Audio/Purchase");
            overloadSound = Resources.Load<AudioClip>("Audio/Overload");
            endSound = Resources.Load<AudioClip>("Audio/OverloadEnd");
            rewardSound = Resources.Load<AudioClip>("Audio/Milestone");
            preferences.Changed += ApplyPreferences;
            ApplyPreferences();
        }

        private AudioSource Voice()
        {
            AudioSource voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            voice.volume = 0;
            return voice;
        }

        private static Particle Make(Graphic graphic, float duration, float size, bool wave)
        {
            var particle = new Particle { graphic = graphic, rect = graphic.rectTransform,
                duration = duration, size = size, endSize = size * 2.5f, wave = wave, age = duration };
            graphic.gameObject.SetActive(false);
            return particle;
        }

        private void Emit(Particle particle, Vector2 velocity, Color color)
        {
            particle.age = 0;
            particle.origin = Vector2.zero;
            particle.velocity = preferences.reducedMotion ? Vector2.zero : velocity;
            particle.color = color;
            particle.graphic.color = color;
            particle.rect.anchoredPosition = Vector2.zero;
            particle.rect.localScale = Vector3.one;
            if (particle.wave) particle.rect.sizeDelta = Vector2.one * particle.size;
            particle.graphic.gameObject.SetActive(true);
        }

        private void Wave(Color color, bool inward = false, bool special = false)
        {
            Particle wave = waves[waveIndex];
            waveIndex = (waveIndex + 1) % waves.Length;
            wave.duration = special ? .8f : .55f;
            wave.size = inward ? 430 : 180;
            wave.endSize = inward ? 190 : special ? 530 : 440;
            color.a = preferences.reducedFlashes ? .25f : special ? .7f : .45f;
            Emit(wave, Vector2.zero, color);
        }

        public void Tap(double amount, bool overloaded, float comboStrength = 0)
        {
            Color color = overloaded ? ReactorHud.Warm : Color.Lerp(ReactorHud.Cyan, ReactorHud.Violet, comboStrength * .6f);
            Particle number = numbers[numberIndex];
            numberIndex = (numberIndex + 1) % numbers.Length;
            ((TextMeshProUGUI)number.graphic).text = "+" + NumberFormat.Stat(amount);
            Emit(number, new Vector2(Random.Range(-25f, 25f), 115), color);
            number.origin = new Vector2(Random.Range(-65f, 65f), 25);
            number.rect.anchoredPosition = number.origin;
            // Every tap reacts immediately; bursts are rate-limited and always reuse the same pools.
            if (Time.unscaledTime - lastBurst >= (preferences.limitedEffects ? .2f : .04f))
            {
                Wave(color);
                int count = preferences.limitedEffects || preferences.reducedMotion ? 0 : overloaded ? 8 : 2 + Mathf.FloorToInt(comboStrength * 3);
                for (int i = 0; i < count; i++)
                {
                    Vector2 direction = Random.insideUnitCircle.normalized;
                    Color sparkColor = color; sparkColor.a = preferences.reducedFlashes ? .3f : .8f;
                    Emit(sparks[sparkIndex], direction * Random.Range(150, overloaded ? 400 : 280), sparkColor);
                    sparks[sparkIndex].rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
                    sparkIndex = (sparkIndex + 1) % sparks.Length;
                }
                lastBurst = Time.unscaledTime;
            }
            pulse = overloaded ? 1.25f : 1;
            recoilAge = 0;
            // Slow local highlight, never a full-screen strobe; repeated taps sustain it.
            flash = preferences.reducedFlashes ? 0 : .12f;
            ApplyCoreMotion();
            if (Time.unscaledTime - lastSound >= .06f)
            {
                AudioSource voice = tapVoices[voiceIndex]; voiceIndex = (voiceIndex + 1) % tapVoices.Length;
                voice.pitch = 1 + comboStrength * .15f + (overloaded ? .1f : 0);
                Play(voice, tapSound, overloaded ? .18f : .13f);
                lastSound = Time.unscaledTime;
            }
        }

        public void ComboRise(double multiplier)
        {
            if (suspended || Time.unscaledTime - lastComboSound < .22f) return;
            lastComboSound = Time.unscaledTime; ComboRiseCount++;
            comboVoice.pitch = Mathf.Clamp(1 + (float)System.Math.Log(multiplier, 2) * .12f, 1, 1.5f);
            Play(comboVoice, comboSound, .16f);
        }

        public void GeneratorUnlocked()
        {
            GeneratorUnlockCount++;
            Play(unlockVoice, unlockSound, .23f);
        }

        public void Purchase() { Play(eventVoice, purchaseSound, .25f); }

        public void Celebrate()
        {
            pulse = 1.3f;
            Wave(ReactorHud.Warm, false, true);
            Play(eventVoice, rewardSound, .25f);
            Haptic();
        }

        public void StartOverload()
        {
            wasOverloaded = true;
            pulse = 1.4f;
            flash = preferences.reducedFlashes ? 0 : .18f;
            Wave(ReactorHud.Magenta, false, true);
            Play(eventVoice, overloadSound, .28f);
            Haptic();
        }

        private void Haptic()
        {
            if (!suspended && preferences.vibration && Application.isMobilePlatform && Time.unscaledTime - lastHaptic >= 2)
            {
                Handheld.Vibrate();
                lastHaptic = Time.unscaledTime;
            }
        }

        private void Play(AudioSource voice, AudioClip clip, float volume)
        {
            if (clip == null || !preferences.sound || preferences.effectsVolume <= 0 || suspended) return;
            // Seven fixed sources; no PlayOneShot accumulation on fast input.
            effectLevels[voice] = volume;
            voice.clip = clip; voice.volume = volume * preferences.effectsVolume; voice.Play();
        }

        public void Reborn()
        {
            Clear(numbers); Clear(waves); Clear(sparks);
            foreach (var voice in tapVoices) voice.Stop();
            eventVoice.Stop(); humVoice.Stop(); comboVoice.Stop(); unlockVoice.Stop();
            wasOverloaded = false; overloadMix = comboMix = flash = 0;
            pulse = 1.3f;
            Wave(ReactorHud.Warm, true, true);
            Play(eventVoice, ascensionSound, .25f);
            Haptic();
        }

        public void SetSuspended(bool value)
        {
            suspended = value;
            ApplyPreferences();
        }

        private void ApplyPreferences()
        {
            if (!preferences.sound || suspended)
            {
                foreach (var voice in tapVoices) voice.Stop();
                eventVoice.Stop(); humVoice.Stop(); comboVoice.Stop(); unlockVoice.Stop();
            }
            foreach (var pair in effectLevels) pair.Key.volume = pair.Value * preferences.effectsVolume;
            humVoice.volume = overloadMix * .09f * preferences.effectsVolume;
            if (suspended)
            {
                if (musicVoice.isPlaying) { musicVoice.Pause(); musicPaused = true; }
            }
            else if (!preferences.music || preferences.musicVolume <= 0)
            {
                musicVoice.Stop(); musicPaused = false; musicVoice.volume = 0;
            }
            else if (musicVoice.clip != null)
            {
                if (musicPaused) { musicVoice.UnPause(); musicPaused = false; }
                else if (!musicVoice.isPlaying) { musicVoice.volume = 0; musicVoice.Play(); }
            }
            if (preferences.reducedFlashes) { flash = 0; hud.CoreFlash.color = Color.clear; }
            if (preferences.limitedEffects || preferences.reducedMotion || preferences.reducedFlashes)
            {
                Clear(sparks); Clear(waves);
            }
            if (preferences.reducedMotion)
            {
                hud.CoreVisual.localEulerAngles = Vector3.zero;
                hud.CoreVisual.anchoredPosition = Vector2.zero;
                foreach (var particle in numbers) particle.velocity = Vector2.zero;
            }
        }

        private static void Clear(Particle[] pool)
        {
            foreach (var particle in pool)
            {
                particle.age = particle.duration;
                particle.graphic.gameObject.SetActive(false);
            }
        }

        public void Animate(int stage, bool overloaded, float comboStrength = 0)
        {
            if (suspended) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            float musicTarget = preferences.music && !CinematicSilence ? .14f * preferences.musicVolume * (DialogueActive ? .28f : 1) : 0;
            recoilAge += dt;
            musicVoice.volume = Mathf.MoveTowards(musicVoice.volume, musicTarget, dt * .08f);
            if (wasOverloaded && !overloaded)
            {
                OverloadEndCount++;
                Wave(ReactorHud.Cyan, true, true);
                Play(eventVoice, endSound, .22f);
                hud.ShowNotice("Sobrecarga finalizada. Vuelve a cargar.");
            }
            wasOverloaded = overloaded;
            pulse = Mathf.MoveTowards(pulse, 0, dt * 4);
            flash = Mathf.MoveTowards(flash, 0, dt * .35f);
            comboMix = Mathf.MoveTowards(comboMix, Mathf.Clamp01(comboStrength), dt * 1.5f);
            overloadMix = Mathf.MoveTowards(overloadMix, overloaded ? 1 : 0, dt * 1.4f);
            ApplyCoreMotion();
            Color baseColor = Color.Lerp(ReactorAssembly.SkinColor(hud.Skin), ReactorHud.Violet, Mathf.Clamp01(stage * .15f + comboMix * .4f));
            Color stateColor = Color.Lerp(baseColor, ReactorHud.Warm, overloadMix);
            hud.CoreImage.color = Color.Lerp(Color.Lerp(Color.white, ReactorAssembly.SkinColor(hud.Skin), hud.Skin == 0 ? 0 : .35f), new Color(1, .65f, .89f), overloadMix * .65f);
            Color glowColor = Color.Lerp(baseColor, ReactorHud.Magenta, overloadMix);
            glowColor.a = (preferences.limitedEffects ? .18f : .24f) + comboMix * .08f + overloadMix * .15f;
            hud.CoreGlow.color = glowColor;
            hud.CoreFlash.color = new Color(.7f, 1, 1, preferences.reducedFlashes ? 0 : flash);
            hud.Orbit.color = stateColor;
            if (!preferences.reducedMotion)
                hud.Orbit.rectTransform.localEulerAngles += new Vector3(0, 0, dt * (8 + stage * 3 + comboMix * 10 + overloadMix * 20));
            for (int i = 0; i < hud.EvolutionRings.Length; i++)
            {
                RingGraphic ring = hud.EvolutionRings[i];
                bool visible = stage > i;
                if (ring.gameObject.activeSelf != visible) ring.gameObject.SetActive(visible);
                if (!visible) continue;
                ring.color = Color.Lerp(i < 2 ? ReactorHud.Violet : ReactorHud.Cyan, ReactorHud.Warm, overloadMix);
                if (!preferences.reducedMotion)
                    ring.rectTransform.localEulerAngles += new Vector3(0, 0,
                        dt * (i % 2 == 0 ? -1 : 1) * (8 + 4 * i + overloadMix * 25));
            }
            if (preferences.sound && preferences.effectsVolume > 0 && overloaded && humVoice.clip != null)
            {
                humVoice.volume = overloadMix * .09f * preferences.effectsVolume;
                if (!humVoice.isPlaying) humVoice.Play();
            }
            else
            {
                humVoice.volume = Mathf.MoveTowards(humVoice.volume, 0, dt * .15f);
                if (humVoice.volume <= 0) humVoice.Stop();
            }
            Tick(numbers, dt); Tick(waves, dt); Tick(sparks, dt);
        }

        private void ApplyCoreMotion()
        {
            if (preferences.reducedMotion)
            {
                hud.CoreVisual.localScale = Vector3.one * (1 + pulse * .015f);
                hud.CoreVisual.anchoredPosition = Vector2.zero;
                return;
            }
            float spring = Mathf.Exp(-recoilAge * 9) * Mathf.Sin(recoilAge * 22);
            float breath = preferences.limitedEffects ? 0 : Mathf.Sin(Time.unscaledTime * 1.7f) * .004f;
            hud.CoreVisual.localScale = new Vector3(1 + pulse * .055f + spring * .018f + breath,
                1 - pulse * .025f - spring * .012f + breath, 1);
            // Only the core moves. Prices, buttons and camera never shake.
            hud.CoreVisual.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 32) * pulse * 2, 0);
        }

        private void Tick(Particle[] pool, float dt)
        {
            foreach (Particle particle in pool)
            {
                if (particle.age >= particle.duration) continue;
                particle.age += dt;
                float t = Mathf.Clamp01(particle.age / particle.duration);
                if (t >= 1) { particle.graphic.gameObject.SetActive(false); continue; }
                particle.rect.anchoredPosition = particle.origin + particle.velocity * particle.age;
                Color color = particle.color;
                color.a *= 1 - t;
                particle.graphic.color = color;
                if (particle.wave)
                    particle.rect.sizeDelta = Vector2.one * (preferences.reducedMotion ? 260 : Mathf.Lerp(particle.size, particle.endSize, t));
            }
        }

        private void OnDestroy()
        {
            if (preferences != null) preferences.Changed -= ApplyPreferences;
        }
    }
}
