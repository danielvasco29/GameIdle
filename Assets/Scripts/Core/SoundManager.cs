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

            // Make sure something can actually hear the audio. Unity only plays
            // 2D sounds if there is an active AudioListener in the scene.
            if (Object.FindAnyObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();

            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;

            clickClip    = Blip(0.06f, 520f, 0.35f, 0.7f);
            coinClip     = Sweep(0.10f, 700f, 1300f, 0.30f);
            buyClip      = Arp(new[] { 660f, 880f }, 0.07f, 0.30f);
            prestigeClip = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.09f, 0.32f);
            errorClip    = Blip(0.16f, 150f, 0.40f, 0.0f);
        }

        private void PlayClip(AudioClip clip, float pitch = 1f)
        {
            if (Muted || clip == null || src == null) return;
            src.pitch = pitch;
            src.PlayOneShot(clip);
        }

        public void PlayClick()    => PlayClip(clickClip, Random.Range(0.96f, 1.06f));
        public void PlayCoin()     => PlayClip(coinClip,  Random.Range(0.95f, 1.08f));
        public void PlayBuy()      => PlayClip(buyClip);
        public void PlayPrestige() => PlayClip(prestigeClip);
        public void PlayError()    => PlayClip(errorClip);

        // ── Synthesis helpers ────────────────────────────────────────────────

        // Single tone with exponential decay. `square` blends toward a square wave.
        private AudioClip Blip(float dur, float freq, float volume, float square)
        {
            int rate = 44100;
            int n = Mathf.Max(1, (int)(dur * rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 18f);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float sq = Mathf.Sign(sine);
                data[i] = Mathf.Lerp(sine, sq, square) * env * volume;
            }
            var clip = AudioClip.Create("blip", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Frequency sweep (used for coins).
        private AudioClip Sweep(float dur, float fStart, float fEnd, float volume)
        {
            int rate = 44100;
            int n = Mathf.Max(1, (int)(dur * rate));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float freq = Mathf.Lerp(fStart, fEnd, t);
                phase += 2f * Mathf.PI * freq / rate;
                float env = Mathf.Exp(-t * 4f) * (1f - t);
                data[i] = Mathf.Sin(phase) * env * volume;
            }
            var clip = AudioClip.Create("sweep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Quick ascending notes (used for purchases / prestige).
        private AudioClip Arp(float[] freqs, float noteDur, float volume)
        {
            int rate = 44100;
            int perNote = Mathf.Max(1, (int)(noteDur * rate));
            int n = perNote * freqs.Length;
            var data = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / rate;
                    float env = Mathf.Exp(-t * 12f);
                    data[k * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * env * volume;
                }
            }
            var clip = AudioClip.Create("arp", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
