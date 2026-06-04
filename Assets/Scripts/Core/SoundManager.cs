using UnityEngine;

namespace GameIdle
{
    // Lightweight SFX player. All clips are synthesised at runtime so the
    // project needs no audio assets.
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        public static bool Muted { get; private set; }
        public static void ToggleMute() => Muted = !Muted;

        public static SoundManager Get()
        {
            if (Instance == null)
            {
                var go = new GameObject("SoundManager");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<SoundManager>();
            }
            return Instance;
        }

        private AudioSource src;
        private AudioClip clickClip, buyClip, prestigeClip, errorClip, coinClip;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (Object.FindAnyObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;

            clickClip    = MakeClick();
            coinClip     = MakeCoin();
            buyClip      = MakeBuy();
            prestigeClip = MakePrestige();
            errorClip    = MakeError();
        }

        private void PlayClip(AudioClip clip, float pitch = 1f, float vol = 1f)
        {
            if (Muted || clip == null || src == null) return;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
        }

        public void PlayClick()    => PlayClip(clickClip,    Random.Range(0.97f, 1.04f), 0.55f);
        public void PlayCoin()     => PlayClip(coinClip,     Random.Range(0.95f, 1.08f), 0.45f);
        public void PlayBuy()      => PlayClip(buyClip,      1f, 0.65f);
        public void PlayPrestige() => PlayClip(prestigeClip, 1f, 0.70f);
        public void PlayError()    => PlayClip(errorClip,    1f, 0.50f);

        // ── Synthesis helpers ────────────────────────────────────────────────

        private const int Rate = 44100;

        // Warm tone: sine + small amount of 2nd/3rd harmonic + soft attack + exponential decay.
        private static float WarmTone(float freq, float t, float attack)
        {
            float env  = (1f - Mathf.Exp(-t / Mathf.Max(attack, 0.001f))) // attack
                       * Mathf.Exp(-t * 6f);                               // decay
            float phase = 2f * Mathf.PI * freq * t;
            return (Mathf.Sin(phase)                     // fundamental
                  + 0.30f * Mathf.Sin(2f * phase)        // 2nd harmonic (warms it up)
                  + 0.10f * Mathf.Sin(3f * phase))       // 3rd harmonic (slight body)
                  * env;
        }

        // Short "tock" click — muffled low thud, no harsh overtones.
        private AudioClip MakeClick()
        {
            int n = Rate / 20; // 50 ms
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t   = (float)i / Rate;
                float env = Mathf.Exp(-t * 55f);
                // Two slightly-detuned tones for a natural thud
                data[i] = (Mathf.Sin(2f * Mathf.PI * 420f * t)
                          + 0.5f * Mathf.Sin(2f * Mathf.PI * 390f * t)) * env * 0.5f;
            }
            return Make("click", data);
        }

        // Rising sweep with warm harmonics — feels like a coin being collected.
        private AudioClip MakeCoin()
        {
            float dur = 0.18f;
            int n = (int)(dur * Rate);
            var data = new float[n];
            float phase1 = 0, phase2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t    = (float)i / n;
                float freq = Mathf.Lerp(600f, 1400f, Mathf.Pow(t, 0.5f));
                float env  = Mathf.Exp(-t * 4.5f) * (1f - Mathf.Pow(t, 2f));
                float dt   = 1f / Rate;
                phase1 += 2f * Mathf.PI * freq * dt;
                phase2 += 2f * Mathf.PI * (freq * 1.5f) * dt; // perfect fifth on top
                data[i] = (0.7f * Mathf.Sin(phase1) + 0.3f * Mathf.Sin(phase2)) * env * 0.45f;
            }
            return Make("coin", data);
        }

        // Friendly two-note "ding-dong" purchase confirmation.
        private AudioClip MakeBuy()
        {
            float[] freqs = { 660f, 990f };
            float noteDur = 0.10f;
            int perNote = (int)(noteDur * Rate);
            int n = perNote * freqs.Length;
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / Rate;
                    data[k * perNote + i] = WarmTone(freqs[k], t, 0.008f) * 0.55f;
                }
            }
            return Make("buy", data);
        }

        // Triumphant 4-note arpeggio (major chord) — rewarding prestige feeling.
        private AudioClip MakePrestige()
        {
            // C5-E5-G5-C6 major arpeggio
            float[] freqs    = { 523f, 659f, 784f, 1047f };
            float[] durs     = { 0.10f, 0.10f, 0.10f, 0.22f };
            float overlap    = 0.04f; // notes bleed slightly into each other for richness

            int totalSamples = 0;
            foreach (var d in durs) totalSamples += (int)((d + overlap) * Rate);
            var data = new float[totalSamples];

            int offset = 0;
            for (int k = 0; k < freqs.Length; k++)
            {
                int noteSamples = (int)((durs[k] + overlap) * Rate);
                for (int i = 0; i < noteSamples && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / Rate;
                    data[offset + i] += WarmTone(freqs[k], t, 0.010f) * 0.55f;
                }
                offset += (int)(durs[k] * Rate);
            }
            return Make("prestige", data);
        }

        // Low descending buzz — clearly "wrong" but not harsh.
        private AudioClip MakeError()
        {
            float dur = 0.22f;
            int n = (int)(dur * Rate);
            var data = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t    = (float)i / Rate;
                float freq = Mathf.Lerp(280f, 160f, t / dur); // descend
                float env  = Mathf.Exp(-t * 8f);
                phase += 2f * Mathf.PI * freq / Rate;
                // Slight square-wave character (buzzy) without being pure square (harsh)
                float s = Mathf.Sin(phase);
                data[i] = Mathf.Lerp(s, Mathf.Sign(s), 0.3f) * env * 0.40f;
            }
            return Make("error", data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
