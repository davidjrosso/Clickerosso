using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReactorDeCaos
{
    public sealed class ReactorFeedback : MonoBehaviour
    {
        private sealed class Particle
        {
            public RectTransform rect;
            public Graphic graphic;
            public Vector2 origin, velocity;
            public float age, duration, size;
            public Color color;
            public bool wave;
        }

        private ReactorHud hud;
        private Particle[] numbers, waves, sparks;
        private int numberIndex, waveIndex, sparkIndex;
        private float pulse;
        private AudioSource audioSource;
        private AudioClip tapSound, purchaseSound, overloadSound;
        private float lastSound;

        public void Initialize(ReactorHud view)
        {
            hud = view;
            numbers = new Particle[12]; waves = new Particle[6]; sparks = new Particle[32];
            for (int i = 0; i < numbers.Length; i++)
            {
                var label = hud.Label("Gain" + i, hud.EffectsRoot, "", 48, ReactorHud.Cyan, .5f, .65f, .5f, .65f);
                label.rectTransform.sizeDelta = new Vector2(400, 90);
                numbers[i] = Make(label, 1.0f, 1, false);
            }
            for (int i = 0; i < waves.Length; i++)
            {
                RingGraphic ring = ReactorHud.Ring("Wave" + i, hud.EffectsRoot, ReactorHud.Cyan);
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(.5f, .65f);
                ring.thickness = 4;
                waves[i] = Make(ring, .55f, 200, true);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                RectTransform rect = ReactorHud.Rect("Spark" + i, hud.EffectsRoot, .5f, .65f, .5f, .65f);
                var graphic = rect.gameObject.AddComponent<Image>();
                graphic.raycastTarget = false;
                rect.sizeDelta = new Vector2(7, 7);
                sparks[i] = Make(graphic, .45f, 1, false);
            }
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            tapSound = Resources.Load<AudioClip>("Audio/Tap");
            purchaseSound = Resources.Load<AudioClip>("Audio/Purchase");
            overloadSound = Resources.Load<AudioClip>("Audio/Overload");
        }

        private static Particle Make(Graphic graphic, float duration, float size, bool wave)
        {
            var particle = new Particle { graphic = graphic, rect = graphic.rectTransform,
                duration = duration, size = size, wave = wave, age = duration };
            graphic.gameObject.SetActive(false);
            return particle;
        }

        private void Emit(Particle particle, Vector2 velocity, Color color)
        {
            particle.age = 0;
            particle.origin = Vector2.zero;
            particle.velocity = velocity;
            particle.color = color;
            particle.graphic.color = color;
            particle.rect.anchoredPosition = Vector2.zero;
            particle.rect.localScale = Vector3.one;
            if (particle.wave) particle.rect.sizeDelta = Vector2.one * particle.size;
            particle.graphic.gameObject.SetActive(true);
        }

        public void Tap(double amount, bool overloaded)
        {
            Color color = overloaded ? ReactorHud.Warm : ReactorHud.Cyan;
            Particle number = numbers[numberIndex];
            numberIndex = (numberIndex + 1) % numbers.Length;
            ((TextMeshProUGUI)number.graphic).text = "+" + NumberFormat.Stat(amount);
            Emit(number, new Vector2(Random.Range(-30f, 30f), 145), color);
            number.origin = new Vector2(Random.Range(-75f, 75f), 20);
            Emit(waves[waveIndex], Vector2.zero, color);
            waveIndex = (waveIndex + 1) % waves.Length;
            for (int i = 0; i < (overloaded ? 8 : 4); i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                Emit(sparks[sparkIndex], direction * Random.Range(150, overloaded ? 440 : 330), color);
                sparkIndex = (sparkIndex + 1) % sparks.Length;
            }
            pulse = overloaded ? 1.5f : 1;
            hud.CoreVisual.localScale = Vector3.one * (1 + pulse * .075f);
            // Rate-limit sound on very fast multitouch input.
            if (Time.unscaledTime - lastSound > .06f)
            {
                audioSource.pitch = overloaded ? 1.2f : 1;
                Play(tapSound, overloaded ? .22f : .16f);
                lastSound = Time.unscaledTime;
            }
        }

        public void Purchase() { audioSource.pitch = 1; Play(purchaseSound, .3f); }

        public void Celebrate()
        {
            pulse = 1.8f;
            Emit(waves[waveIndex], Vector2.zero, ReactorHud.Warm);
            waveIndex = (waveIndex + 1) % waves.Length;
            audioSource.pitch = 1;
            hud.CoreVisual.localScale = Vector3.one * (1 + pulse * .075f);
            hud.Orbit.color = ReactorHud.Warm;
            Play(overloadSound, .3f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null) audioSource.PlayOneShot(clip, volume);
        }

        public void Animate(int stage, bool overloaded)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            pulse = Mathf.MoveTowards(pulse, 0, dt * 4);
            hud.CoreVisual.localScale = Vector3.one * (1 + pulse * .075f);
            hud.CoreVisual.localEulerAngles += new Vector3(0, 0, dt * (overloaded ? 35 : 6 + stage * 5));
            hud.Orbit.color = overloaded || stage >= 3 ? ReactorHud.Warm
                : stage == 0 ? ReactorHud.Cyan : ReactorHud.Violet;
            for (int i = 0; i < hud.EvolutionRings.Length; i++)
            {
                RingGraphic ring = hud.EvolutionRings[i];
                bool visible = stage > i;
                if (ring.gameObject.activeSelf != visible) ring.gameObject.SetActive(visible);
                if (!visible) continue;
                ring.color = overloaded ? ReactorHud.Warm : i < 2 ? ReactorHud.Violet : ReactorHud.Cyan;
                ring.rectTransform.localEulerAngles += new Vector3(0, 0,
                    dt * (i % 2 == 0 ? -1 : 1) * (overloaded ? 35 + 10 * i : 8 + 4 * i));
            }
            Tick(numbers, dt); Tick(waves, dt); Tick(sparks, dt);
        }

        private static void Tick(Particle[] pool, float dt)
        {
            foreach (Particle particle in pool)
            {
                if (particle.age >= particle.duration) continue;
                particle.age += dt;
                float t = Mathf.Clamp01(particle.age / particle.duration);
                if (t >= 1) { particle.graphic.gameObject.SetActive(false); continue; }
                particle.rect.anchoredPosition = particle.origin + particle.velocity * particle.age;
                Color color = particle.color;
                color.a = (1 - t) * particle.color.a;
                particle.graphic.color = color;
                if (particle.wave) particle.rect.sizeDelta = Vector2.one * particle.size * (1 + t * 1.6f);
            }
        }
    }
}
