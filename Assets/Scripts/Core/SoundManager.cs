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

        public void PlayClick()    => PlayClip(clickClip,    Random.Range(0.90f, 1.10f), 0.50f);
        public void PlayCoin()     => PlayClip(coinClip,     Random.Range(0.95f, 1.08f), 0.45f);
        public void PlayBuy()      => PlayClip(buyClip,      Random.Range(0.98f, 1.03f), 0.35f);
        public void PlayPrestige() => PlayClip(prestigeClip, 1f, 0.70f);
        public void PlayError()    => PlayClip(errorClip,    1f, 0.50f);

        // ── Synthesis helpers ────────────────────────────────────────────────

        private const int Rate = 44100;

        // Woody percussive "tock" (wood-block / marimba style): a pitched body
        // with fast exponential decay plus a tiny noise transient for the attack.
        // Sums into `data` starting at sample `start`, so several can be layered.
        private static void RenderTock(float[] data, int start, float freq, float decay, float vol)
        {
            for (int i = start; i < data.Length; i++)
            {
                float t   = (float)(i - start) / Rate;
                float env = Mathf.Exp(-t * decay);
                if (env < 0.0008f) break;
                // Fundamental + a slightly-flat lower partial → hollow "wood" body
                float body  = Mathf.Sin(2f * Mathf.PI * freq * t)
                            + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 0.92f * t);
                // Brief noise burst for the "t" attack of the tock
                float trans = t < 0.0025f
                            ? (1f - t / 0.0025f) * (Random.value * 2f - 1f) * 0.45f
                            : 0f;
                data[i] += (body * 0.5f + trans) * env * vol;
            }
        }

        // Tap (TRABALHAR): a soft mechanical key press ("pock") — warm and rounded
        // rather than a sharp typewriter strike, so rapid presses sound pleasant
        // instead of like machine-gun typing. A gently low-passed noise transient
        // plus a warm low "thock" body; the harsh metallic tick was removed.
        private AudioClip MakeClick()
        {
            int n = Rate * 8 / 100; // 80 ms
            var data = new float[n];
            float lp = 0f; // one-pole low-pass state to tame the noise hiss
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;

                // Very soft noise transient — only a hint of attack, heavily damped
                float raw = Random.value * 2f - 1f;
                lp = Mathf.Lerp(lp, raw, 0.18f);           // very low cutoff = muffled
                float clack = lp * Mathf.Exp(-t * 90f);

                // Deep warm thock body (key bottoming out) — the dominant element
                float body = Mathf.Sin(2f * Mathf.PI * 130f * t) * Mathf.Exp(-t * 42f);

                data[i] = (clack * 0.18f + body * 0.62f) * 0.72f;
            }
            return Make("click", data);
        }

        // Coin pickup: bright, short tock.
        private AudioClip MakeCoin()
        {
            var data = new float[Rate * 9 / 100];
            RenderTock(data, 0, 820f, 52f, 0.5f);
            return Make("coin", data);
        }

        // Employee upgrade: a crisp tock a bit brighter than the tap, so the two
        // are distinguishable. Fires often (x10/Max) → kept short and tight.
        private AudioClip MakeBuy()
        {
            var data = new float[Rate * 9 / 100];
            RenderTock(data, 0, 640f, 56f, 0.55f);
            return Make("buy", data);
        }

        // Prestige: an ascending wooden run (xylophone-like) — still the same tock
        // timbre, but a rising figure that reads as a reward.
        private AudioClip MakePrestige()
        {
            float[] freqs = { 523f, 659f, 784f, 988f, 1175f }; // rising pentatonic-ish run
            var data = new float[Rate * 60 / 100]; // 600 ms
            const float step = 0.072f;
            for (int k = 0; k < freqs.Length; k++)
                RenderTock(data, (int)(k * step * Rate), freqs[k], 17f, 0.55f);
            return Make("prestige", data);
        }

        // Not enough money: two low descending tocks — a wooden "uh-oh".
        private AudioClip MakeError()
        {
            var data = new float[Rate * 30 / 100]; // 300 ms
            RenderTock(data, 0,                    200f, 26f, 0.55f);
            RenderTock(data, (int)(0.11f * Rate),  150f, 24f, 0.55f);
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
