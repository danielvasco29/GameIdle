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

        // Música de fundo (lounge/jazz estilo Sims 1) — gerada em runtime.
        public static bool MusicMuted { get; private set; }
        public static void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            if (Instance != null && Instance.musicSrc != null)
            {
                if (MusicMuted) Instance.musicSrc.Pause();
                else            Instance.musicSrc.UnPause();
            }
        }

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
        private AudioSource musicSrc;
        private AudioSource ambientSrc;
        private AudioClip clickClip, buyClip, prestigeClip, errorClip, coinClip;
        private AudioClip turboClip, hireClip;
        private AudioClip typingClip;
        private AudioClip[] chatterClips;
        private AudioClip hitClip, deathClip, bossRoarClip;

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
            turboClip    = MakeTurbo();
            hireClip     = MakeHire();

            // Trilha de fundo em loop
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.playOnAwake = false;
            musicSrc.spatialBlend = 0f;
            musicSrc.loop = true;
            musicSrc.volume = 0.32f;
            musicSrc.clip = MakeMusicLoop();
            if (!MusicMuted) musicSrc.Play();

            // Ambiente do escritório: teclado + conversas (Simlish)
            ambientSrc = gameObject.AddComponent<AudioSource>();
            ambientSrc.playOnAwake = false;
            ambientSrc.spatialBlend = 0f;
            typingClip   = MakeTyping();
            chatterClips = new[] { MakeChatter(0), MakeChatter(1), MakeChatter(2) };
            hitClip      = MakeHit();
            deathClip    = MakeDeath();
            bossRoarClip = MakeBossRoar();
            StartCoroutine(AmbientLoop());
        }

        private System.Collections.IEnumerator AmbientLoop()
        {
            // Espera inicial para não disparar tudo junto com a música
            yield return new WaitForSeconds(Random.Range(3f, 7f));
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(5f, 11f));
                if (Muted || ambientSrc == null) continue;

                // Quase sempre um período de digitação...
                ambientSrc.pitch = Random.Range(0.96f, 1.06f);
                ambientSrc.PlayOneShot(typingClip, 0.22f);

                // ...e de vez em quando uma conversinha por cima
                if (Random.value < 0.45f && chatterClips != null)
                {
                    yield return new WaitForSeconds(Random.Range(0.4f, 1.6f));
                    if (Muted) continue;
                    var c = chatterClips[Random.Range(0, chatterClips.Length)];
                    ambientSrc.pitch = Random.Range(0.92f, 1.12f);
                    ambientSrc.PlayOneShot(c, 0.16f);
                }
            }
        }

        // Exposes the music source so BattlePanel can fade it in/out
        public static AudioSource GetMusicSource() => Instance?.musicSrc;

        private void PlayClip(AudioClip clip, float pitch = 1f, float vol = 1f)
        {
            if (Muted || clip == null || src == null) return;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
        }

        public void PlayClick()    => PlayClip(clickClip,    Random.Range(0.90f, 1.10f), 0.50f);
        public void PlayCoin()     => PlayClip(coinClip,     Random.Range(0.95f, 1.08f), 0.45f);
        public void PlayBuy()      => PlayClip(buyClip,      Random.Range(0.98f, 1.03f), 0.35f);
        public void PlayHire()     => PlayClip(hireClip,     Random.Range(0.98f, 1.04f), 0.50f);
        public void PlayTurbo()    => PlayClip(turboClip,    1f, 0.60f);
        public void PlayPrestige() => PlayClip(prestigeClip, 1f, 0.70f);
        public void PlayError()    => PlayClip(errorClip,    1f, 0.50f);
        public void PlayHit()      => PlayClip(hitClip,      Random.Range(0.88f, 1.12f), 0.55f);
        public void PlayDeath()    => PlayClip(deathClip,    Random.Range(0.92f, 1.05f), 0.65f);
        public void PlayBossRoar() => PlayClip(bossRoarClip, 1f, 0.75f);

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
        // Power-up ascendente para a ativação do Turbo (mais agudo/rápido que o prestígio).
        private AudioClip MakeTurbo()
        {
            float[] freqs = { 440f, 587f, 784f, 1047f, 1319f, 1568f };
            var data = new float[Rate * 45 / 100];
            const float step = 0.045f;
            for (int k = 0; k < freqs.Length; k++)
                RenderTock(data, (int)(k * step * Rate), freqs[k], 20f, 0.5f);
            return Make("turbo", data);
        }

        // "Ding" amigável de duas notas para contratação (distinto da compra na loja).
        private AudioClip MakeHire()
        {
            var data = new float[Rate * 24 / 100];
            RenderTock(data, 0, 784f, 28f, 0.5f);
            RenderTock(data, (int)(0.085f * Rate), 1175f, 24f, 0.5f);
            return Make("hire", data);
        }

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

        // ── Ambiente: teclado e conversas (Simlish) ─────────────────────────────

        // Período de digitação: vários toques de tecla curtos espalhados.
        private AudioClip MakeTyping()
        {
            int n = Rate * 18 / 10; // 1.8 s
            var data = new float[n];
            int clicks = Random.Range(13, 20);
            for (int k = 0; k < clicks; k++)
                RenderKey(data, Random.Range(0, n - Rate / 8), Random.Range(0.5f, 1f));
            return Make("typing", data);
        }

        private static void RenderKey(float[] data, int start, float vol)
        {
            int dur = Rate * 4 / 100; // 40 ms
            float hp = 0f, prev = 0f;
            for (int i = 0; i < dur && start + i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 130f);
                float raw = Random.value * 2f - 1f;
                hp = 0.85f * (hp + raw - prev); prev = raw;       // clicky passa-altas
                float body = Mathf.Sin(2f * Mathf.PI * 2100f * t) * Mathf.Exp(-t * 220f);
                data[start + i] += (hp * 0.5f + body * 0.30f) * env * vol * 0.5f;
            }
        }

        // Vogais (F1,F2) — pares de formantes que dão o caráter "a/e/i/o/u".
        private static readonly float[,] Vowels =
        {
            { 800f, 1150f }, // a
            { 500f, 1800f }, // é
            { 320f, 2300f }, // i
            { 500f,  900f }, // ó
            { 350f,  800f }, // u
        };

        // Conversa estilo Simlish: frase de sílabas-vogais com entonação. Usa
        // síntese de formantes (fonte glotal + 2 ressonadores) para soar "falado".
        // Escala alegre (pentatônica maior) para os saltos não soarem dissonantes.
        private static readonly int[] PentaSteps = { 0, 2, 4, 7, 9, 12 };

        private AudioClip MakeChatter(int variant)
        {
            int n = Rate * 16 / 10; // 1.6 s
            var data = new float[n];
            // Voz mais aguda e fofa (em vez do tom grave macabro)
            float basePitch = 240f + variant * 24f + Random.Range(-12f, 12f);
            int syl = Random.Range(4, 7);
            bool question = variant == 2;            // variação "pergunta" (sobe no fim)
            float pos = 0.06f;
            for (int s = 0; s < syl; s++)
            {
                int vi = Random.Range(0, 5);
                // contorno melódico em notas da pentatônica (sem dissonância)
                int note = PentaSteps[Random.Range(0, PentaSteps.Length)];
                if (s == syl - 1) note = question ? 12 : 0; // resolve grave / sobe (pergunta)
                float pitch = basePitch * Mathf.Pow(2f, note / 12f);
                // glissando cantadinho dentro da sílaba
                float glide = Mathf.Pow(2f, Random.Range(-2, 3) / 12f);
                float dur = Random.Range(0.09f, 0.16f); // mais curtinhas/bouncy
                if (pos + dur > n / (float)Rate - 0.05f) break;

                // consoante bem leve antes de ~metade das sílabas
                if (Random.value < 0.45f) RenderKey(data, (int)(pos * Rate) - Rate / 200, 0.06f);

                RenderSyllable(data, (int)(pos * Rate), pitch, glide, dur, Vowels[vi, 0], Vowels[vi, 1], 0.5f);
                pos += dur + Random.Range(0.03f, 0.09f);
            }

            // Normaliza o clip (os ressonadores têm ganho imprevisível)
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 0.0001f) { float g = 0.7f / peak; for (int i = 0; i < n; i++) data[i] *= g; }

            return Make("chatter" + variant, data);
        }

        // Síntese de formantes: um pulso glotal SUAVE (não impulso seco) por período
        // excita dois ressonadores nas frequências de formante. Saída passa por um
        // lowpass leve para tirar a aspereza e soar mais "fala" e menos zumbido.
        private static void RenderSyllable(float[] data, int start, float pitch, float glide,
            float dur, float f1, float f2, float vol)
        {
            int d = (int)(dur * Rate);
            // ressonadores mais amortecidos (menos drone fantasmagórico)
            float r1 = Mathf.Exp(-Mathf.PI * 130f / Rate);
            float r2 = Mathf.Exp(-Mathf.PI * 170f / Rate);
            float a1 = 2f * r1 * Mathf.Cos(2f * Mathf.PI * f1 / Rate), c1 = -r1 * r1;
            float a2 = 2f * r2 * Mathf.Cos(2f * Mathf.PI * f2 / Rate), c2 = -r2 * r2;
            float y1a = 0f, y1b = 0f, y2a = 0f, y2b = 0f;
            float phase = 0f, lp = 0f;
            const float duty = 0.42f; // largura do pulso glotal

            for (int i = 0; i < d && start + i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float prog = t / dur;
                float p = pitch * Mathf.Lerp(1f, glide, prog)        // glissando
                                * (1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 6.5f * t)); // vibrato
                phase += p / Rate;
                if (phase >= 1f) phase -= 1f;
                // pulso glotal suave (raised-cosine ao quadrado) — sem buzz metálico
                float x = phase < duty ? Mathf.Pow(Mathf.Sin(Mathf.PI * phase / duty), 2f) : 0f;

                float y1 = x + a1 * y1a + c1 * y1b; y1b = y1a; y1a = y1;
                float y2 = x + a2 * y2a + c2 * y2b; y2b = y2a; y2a = y2;
                float raw = 0.7f * y1 + 0.45f * y2;
                lp += 0.45f * (raw - lp);                            // lowpass suave

                float env = Mathf.Sin(Mathf.PI * prog);              // vogal entra/sai suave
                if (env < 0f) env = 0f;
                data[start + i] += lp * env * vol;
            }
        }

        // ── Música de fundo: lounge/jazz relaxado (pegada Sims 1) ───────────────
        // Tudo gerado por código: baixo caminhando, comping de vibrafone com
        // tremolo, melodia esparsa com swing e um chiado de vassoura bem leve.
        // Renderizado num buffer que dá loop perfeito (caudas dão wrap-around).

        private static float MidiToFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        // Soma um sample com wrap circular para o loop fechar sem clique.
        private static void Add(float[] buf, int i, float v)
        {
            int L = buf.Length;
            buf[((i % L) + L) % L] += v;
        }

        // Voz tonal quente (sine + harmônicos) com envelope de decaimento e tremolo
        // opcional (caráter de vibrafone).
        private static void RenderVoice(float[] buf, int start, float freq, float durSec,
            float decay, float vol, float h2, float h3, float tremHz)
        {
            int dur = (int)(durSec * Rate);
            for (int k = 0; k < dur; k++)
            {
                float t = (float)k / Rate;
                float env = Mathf.Exp(-t * decay);
                if (env < 0.0006f) break;
                // ataque suave (~6 ms) para não estalar
                float atk = t < 0.006f ? t / 0.006f : 1f;
                float w = 2f * Mathf.PI * freq * t;
                float s = Mathf.Sin(w) + h2 * Mathf.Sin(2f * w) + h3 * Mathf.Sin(3f * w);
                float trem = tremHz > 0f ? (0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * tremHz * t)) : 1f;
                Add(buf, start + k, s * env * atk * trem * vol);
            }
        }

        // Chiado curto de vassoura/hi-hat (ruído filtrado passa-altas leve).
        private static void RenderBrush(float[] buf, int start, float durSec, float decay, float vol)
        {
            int dur = (int)(durSec * Rate);
            float prev = 0f, hp = 0f;
            for (int k = 0; k < dur; k++)
            {
                float t = (float)k / Rate;
                float env = Mathf.Exp(-t * decay);
                if (env < 0.0006f) break;
                float raw = Random.value * 2f - 1f;
                hp = 0.9f * (hp + raw - prev); // passa-altas simples
                prev = raw;
                Add(buf, start + k, hp * env * vol);
            }
        }

        private AudioClip MakeMusicLoop()
        {
            const float bpm = 92f;
            float beat = 60f / bpm;                 // seg por tempo
            int beatN = (int)(beat * Rate);          // amostras por tempo
            const int bars = 4, bpb = 4;             // 4 compassos, 4/4
            int L = bars * bpb * beatN;
            var buf = new float[L];

            // Progressão I-VI-ii-V em Dó: Cmaj7, A7, Dm7, G7 (fecha o loop redondo)
            int[][] chords =
            {
                new[] { 60, 64, 67, 71 }, // Cmaj7  C E G B
                new[] { 57, 61, 64, 67 }, // A7     A C# E G
                new[] { 62, 65, 69, 72 }, // Dm7    D F A C
                new[] { 55, 59, 62, 67 }, // G7     G B D F
            };
            int[] roots = { 36, 33, 38, 43 };        // baixo grave (C2,A1,D2,G2)

            float swing = 0.62f;                      // colcheia "and" atrasada

            for (int bar = 0; bar < bars; bar++)
            {
                int barStart = bar * bpb * beatN;
                int[] ch = chords[bar];
                int root = roots[bar];
                int nextRoot = roots[(bar + 1) % bars];

                // Baixo caminhando: tônica, quinta, oitava, aproximação cromática
                int approach = nextRoot + (root <= nextRoot ? -1 : 1);
                int[] walk = { root, root + 7, root + 12, approach };
                for (int b = 0; b < bpb; b++)
                    RenderVoice(buf, barStart + b * beatN, MidiToFreq(walk[b]),
                        beat * 0.95f, 4.2f, 0.50f, 0.25f, 0.06f, 0f);

                // Comping de vibrafone: acorde no tempo 1 (cheio) e no "and" do 2 e do 4
                void Comp(float beatPos, float v)
                {
                    int s = barStart + (int)(beatPos * beatN);
                    foreach (int n in ch)
                        RenderVoice(buf, s, MidiToFreq(n + 12), 0.9f, 3.0f, v, 0.30f, 0.10f, 5.5f);
                }
                Comp(0f, 0.085f);
                Comp(1f + swing, 0.06f);
                Comp(3f + swing, 0.06f);

                // Melodia esparsa e relaxada (tons do acorde, oitava acima)
                void Mel(float beatPos, int note, float dur, float v)
                {
                    int s = barStart + (int)(beatPos * beatN);
                    RenderVoice(buf, s, MidiToFreq(note + 12), dur, 2.4f, v, 0.22f, 0.07f, 6f);
                }
                Mel(0.5f + swing, ch[2], beat * 1.1f, 0.16f);  // quinta
                Mel(2f,           ch[3], beat * 0.9f, 0.15f);  // sétima
                Mel(2.5f + swing, ch[1], beat * 1.2f, 0.16f);  // terça

                // Groove leve: hi-hat em cada tempo + vassoura nos tempos 2 e 4
                for (int b = 0; b < bpb; b++)
                {
                    RenderBrush(buf, barStart + b * beatN, 0.05f, 70f, 0.030f);
                    RenderBrush(buf, barStart + (int)((b + swing) * beatN), 0.04f, 90f, 0.020f);
                }
                RenderBrush(buf, barStart + 1 * beatN, 0.12f, 26f, 0.045f);
                RenderBrush(buf, barStart + 3 * beatN, 0.12f, 26f, 0.045f);
            }

            // Normaliza para evitar clipping
            float peak = 0f;
            for (int i = 0; i < L; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            if (peak > 0.0001f)
            {
                float g = 0.85f / peak;
                for (int i = 0; i < L; i++) buf[i] *= g;
            }

            return Make("music", buf);
        }

        // ── Combat sound effects ─────────────────────────────────────────────────

        // Sharp sword/punch impact: brief descending noise burst (swoosh attack)
        // layered with a low sine thud that decays fast. Total ~60 ms.
        private AudioClip MakeHit()
        {
            int n = Rate * 6 / 100; // 60 ms
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;

                // Descending noise swoosh: noise filtered to mid-freq, pitch-swept down
                float raw = Random.value * 2f - 1f;
                lp = Mathf.Lerp(lp, raw, 0.35f);                     // bandpass-ish low cutoff
                float swooshEnv = Mathf.Exp(-t * 55f);
                float swoosh = lp * swooshEnv;

                // Low thud body: sine at ~80 Hz with fast pitch droop (impact "weight")
                float droopFreq = 80f * Mathf.Exp(-t * 28f);         // freq glides down quickly
                float phase = 0f;
                // accumulate phase inline via a small look-around isn't practical;
                // approximate the swept sine with a fixed-freq body + modulated decay
                float thud = Mathf.Sin(2f * Mathf.PI * droopFreq * t) * Mathf.Exp(-t * 38f);

                data[i] = swoosh * 0.55f + thud * 0.65f;
            }

            // Re-integrate phase properly for the thud so the pitch sweep is smooth
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float droopFreq = 80f * Mathf.Exp(-t * 28f);
                ph += droopFreq / Rate;
                float thud = Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Exp(-t * 38f);
                // Blend into already-written data: replace the thud portion
                data[i] = (data[i] - Mathf.Sin(2f * Mathf.PI * droopFreq * t) * Mathf.Exp(-t * 38f) * 0.65f)
                          + thud * 0.65f;
            }

            return Make("hit", data);
        }

        // Monster death: cartoon "explosion dissolve" ~300 ms.
        // Initial noise burst, then three sine partials with fast descending pitch
        // glide that fade out — like a cartoon monster popping.
        private AudioClip MakeDeath()
        {
            int n = Rate * 30 / 100; // 300 ms
            var data = new float[n];

            // Opening noise burst (first ~25 ms): filtered mid-noise "splat"
            float lp = 0f;
            int burstLen = Rate * 25 / 1000;
            for (int i = 0; i < burstLen; i++)
            {
                float t = (float)i / Rate;
                float raw = Random.value * 2f - 1f;
                lp = Mathf.Lerp(lp, raw, 0.4f);
                data[i] += lp * Mathf.Exp(-t * 70f) * 0.75f;
            }

            // Three descending pitch-glide partials with staggered start/volume
            // partial 0: root ~320 Hz → drops fast, dominant body
            // partial 1: a fifth above ~480 Hz → slightly slower drop
            // partial 2: two octaves up ~640 Hz → fastest drop, adds "cartoony" top
            float[] startFreqs = { 320f, 480f, 640f };
            float[] glideRates  = { 18f,  14f,  22f  };
            float[] vols        = { 0.60f, 0.45f, 0.30f };
            int[]   offsets     = { 0, Rate * 2 / 1000, Rate * 5 / 1000 }; // slight stagger

            for (int p = 0; p < 3; p++)
            {
                float ph = 0f;
                int start = offsets[p];
                for (int i = start; i < n; i++)
                {
                    float t = (float)(i - start) / Rate;
                    float freq = startFreqs[p] * Mathf.Exp(-t * glideRates[p]);
                    ph += freq / Rate;
                    float env = Mathf.Exp(-t * 12f);
                    if (env < 0.0006f) break;
                    data[i] += Mathf.Sin(2f * Mathf.PI * ph) * env * vols[p];
                }
            }

            // Normalise
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 0.0001f) { float g = 0.9f / peak; for (int i = 0; i < n; i++) data[i] *= g; }

            return Make("death", data);
        }

        // Boss wave announcement: ~500 ms deep rumble with harmonics.
        // Starts at ~60 Hz, builds slightly then decays. Layered odd harmonics
        // (3rd, 5th) give it a powerful, ominous character.
        private AudioClip MakeBossRoar()
        {
            int n = Rate * 50 / 100; // 500 ms
            var data = new float[n];

            const float baseFreq = 62f;   // just below a low C — heavy and ominous

            // Main sub-bass tone with slow build then decay envelope
            float ph1 = 0f, ph3 = 0f, ph5 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float dur = (float)n / Rate;

                // Envelope: ramp up over first ~80 ms, hold briefly, then decay
                float attack = Mathf.Min(t / 0.08f, 1f);
                float release = Mathf.Exp(-Mathf.Max(t - 0.15f, 0f) * 7.5f);
                float env = attack * release;
                if (t > 0.1f && env < 0.0006f) break;

                // Slight pitch sag: starts a semitone high, settles to baseFreq
                float freq = baseFreq * Mathf.Lerp(1.059f, 1f, Mathf.Min(t / 0.12f, 1f));

                ph1 += freq / Rate;
                ph3 += (freq * 3f) / Rate;
                ph5 += (freq * 5f) / Rate;

                // Fundamental + 3rd harmonic + muted 5th — odd harmonics = hollow power
                float tone = Mathf.Sin(2f * Mathf.PI * ph1) * 0.70f
                           + Mathf.Sin(2f * Mathf.PI * ph3) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * ph5) * 0.15f;

                // Low-level noise modulation for "breath/growl" texture
                float growl = (Random.value * 2f - 1f) * 0.08f;

                data[i] = (tone + growl) * env;
            }

            // Normalise
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 0.0001f) { float g = 0.92f / peak; for (int i = 0; i < n; i++) data[i] *= g; }

            return Make("bossRoar", data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
