using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Procedural animated effects layered over the static battle arena art:
    //  - pulsing glow at the arena center (runic circle "breathing")
    //  - warm flicker tint simulating torch light
    //  - soft fog puffs drifting across the lower half
    //  - occasional spark flashes
    // All sprites are generated at runtime; nothing extra to import.
    public class ArenaFxLayer : MonoBehaviour
    {
        private Image _centerGlow;
        private Image _flickerTint;
        private readonly List<RectTransform> _fogPuffs = new();
        private readonly List<float> _fogSpeeds = new();
        private float _sparkTimer;
        private Image _sparkFlash;

        private float _pulsePhase;
        private float _flickerSeed;

        // Boss arena uses red/magenta tones; normal arena cyan/purple.
        private Color _glowColor   = new(0.55f, 0.75f, 1f, 0.16f);
        private Color _fogColor    = new(0.60f, 0.45f, 0.90f, 0.10f);
        private Color _flickColor  = new(0.85f, 0.40f, 1f, 0.05f);

        public void SetBossMode(bool boss)
        {
            // The boss background has no painted torch sconces to sit over.
            foreach (var t in _torchImgs)
                if (t != null) t.gameObject.SetActive(!boss);
            foreach (var g in _torchGlows)
                if (g != null) g.gameObject.SetActive(!boss);

            if (boss)
            {
                _glowColor  = new Color(1f, 0.35f, 0.30f, 0.14f);
                _fogColor   = new Color(0.85f, 0.30f, 0.50f, 0.10f);
                _flickColor = new Color(1f, 0.30f, 0.25f, 0.06f);
            }
            else
            {
                _glowColor  = new Color(0.55f, 0.75f, 1f, 0.16f);
                _fogColor   = new Color(0.60f, 0.45f, 0.90f, 0.10f);
                _flickColor = new Color(0.85f, 0.40f, 1f, 0.05f);
            }
        }

        // Atlas-based extras (only when Resources/FX/arena_atlas.png exists)
        private Image _runicOverlay;
        private readonly List<Image> _torchImgs = new();
        private readonly List<Image> _torchGlows = new();
        private readonly List<float> _torchAnimTimers = new();
        private readonly List<float> _torchAnimSpeeds = new();
        private readonly List<float> _torchGlowPhases = new();
        private Sprite[] _torchFrames;
        private int _torchFrame;

        private void Awake()
        {
            BuildFx();
            BuildAtlasFx();
            _flickerSeed = Random.value * 100f;
        }

        // Real art overlays sliced from the atlas: animated torch flames at the
        // arena edges, runic circle detail pulsing over the center.
        private void BuildAtlasFx()
        {
            if (!ArenaAtlas.Available) return;

            // Runic circle from the atlas, laid on the (empty) arena floor —
            // squashed vertically for perspective, pulsing in Update().
            var runic = ArenaAtlas.RunicCircle();
            if (runic != null)
            {
                var go = new GameObject("RunicOverlay", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.315f, 0.10f);
                rt.anchorMax = new Vector2(0.685f, 0.36f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                _runicOverlay = go.GetComponent<Image>();
                _runicOverlay.sprite = runic;
                _runicOverlay.preserveAspect = false;
                _runicOverlay.color = new Color(1f, 1f, 1f, 0.0f);
                _runicOverlay.raycastTarget = false;
                // render under the procedural glow
                go.transform.SetSiblingIndex(0);
            }

            // Swap procedural fog circles for the real mist art
            var fogSprites = ArenaAtlas.FogPuffs();
            if (fogSprites != null && fogSprites[0] != null)
            {
                for (int i = 0; i < _fogPuffs.Count; i++)
                {
                    var img = _fogPuffs[i].GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = fogSprites[i % fogSprites.Length];
                        img.preserveAspect = true;
                        var c = _fogColor; c.a = 0.35f; // art has its own softness
                        img.color = c;
                    }
                }
            }

            // Full animated torches (flame + sconce + bracket) from the atlas.
            // The battle_bg has no torches of its own, so these are placed on
            // clear stone-wall spots; the bg is stretched full-screen, so
            // bg-normalized coords ARE screen anchors. Box aspect matches the
            // 64x114 frame so the torch isn't distorted.
            _torchFrames = ArenaAtlas.TorchFrames();
            if (_torchFrames != null && _torchFrames[0] != null)
            {
                // (xMin, yMin, xMax, yMax), bottom-left origin
                Vector4[] spots =
                {
                    new(0.176f, 0.501f, 0.238f, 0.699f), // left circuit pillar
                    new(0.409f, 0.461f, 0.471f, 0.659f), // center wall, left of screen
                    new(0.529f, 0.461f, 0.591f, 0.659f), // center wall, right of screen
                    new(0.848f, 0.501f, 0.910f, 0.699f), // right pillar between arches
                };
                Sprite soft = SoftCircle();
                foreach (var s in spots)
                {
                    var go = new GameObject("TorchFlame", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(s.x, s.y);
                    rt.anchorMax = new Vector2(s.z, s.w);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    var img = go.GetComponent<Image>();
                    img.sprite = _torchFrames[0];
                    img.preserveAspect = false;
                    img.raycastTarget = false;
                    _torchImgs.Add(img);

                    // Glow aura around each torch — pulsing independently
                    var glowGO = new GameObject("TorchGlow", typeof(RectTransform), typeof(Image));
                    glowGO.transform.SetParent(transform, false);
                    var glowRT = glowGO.GetComponent<RectTransform>();
                    float glowSize = (s.z - s.x) * 1.8f;
                    glowRT.anchorMin = new Vector2(s.x + (s.z - s.x) * 0.5f, s.y + (s.w - s.y) * 0.5f);
                    glowRT.anchorMax = glowRT.anchorMin;
                    glowRT.pivot = new Vector2(0.5f, 0.5f);
                    glowRT.sizeDelta = new Vector2(glowSize, glowSize * 1.4f);
                    var glowImg = glowGO.GetComponent<Image>();
                    glowImg.sprite = soft;
                    glowImg.color = new Color(0.95f, 0.7f, 0.4f, 0.12f); // warm torch glow
                    glowImg.raycastTarget = false;
                    glowGO.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
                    _torchGlows.Add(glowImg);

                    // Per-torch animation: variable speed ±15% from base 10fps
                    _torchAnimTimers.Add(0f);
                    float baseInterval = 0.1f;
                    float speedVar = Random.Range(0.85f, 1.15f);
                    _torchAnimSpeeds.Add(baseInterval * speedVar);
                    _torchGlowPhases.Add(Random.Range(0f, Mathf.PI * 2f));
                }
            }
        }

        private void BuildFx()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Sprite soft = SoftCircle();

            // Center glow — sits roughly over the runic circle / arena center
            {
                var go = new GameObject("CenterGlow", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var grt = go.GetComponent<RectTransform>();
                grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.32f);
                grt.pivot = new Vector2(0.5f, 0.5f);
                grt.sizeDelta = new Vector2(620f, 280f);
                _centerGlow = go.GetComponent<Image>();
                _centerGlow.sprite = soft;
                _centerGlow.color = _glowColor;
                _centerGlow.raycastTarget = false;
            }

            // Full-screen flicker tint (torch light)
            {
                var go = new GameObject("FlickerTint", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var frt = go.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = frt.offsetMax = Vector2.zero;
                _flickerTint = go.GetComponent<Image>();
                _flickerTint.color = _flickColor;
                _flickerTint.raycastTarget = false;
            }

            // Fog puffs along the bottom
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject($"Fog{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var frt = go.GetComponent<RectTransform>();
                frt.anchorMin = frt.anchorMax = new Vector2(0f, 0f);
                frt.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(220f, 420f);
                frt.sizeDelta = new Vector2(size, size * 0.45f);
                frt.anchoredPosition = new Vector2(Random.Range(0f, 1920f), Random.Range(40f, 200f));
                var img = go.GetComponent<Image>();
                img.sprite = soft;
                img.color = _fogColor;
                img.raycastTarget = false;
                _fogPuffs.Add(frt);
                _fogSpeeds.Add(Random.Range(12f, 35f) * (Random.value < 0.5f ? 1f : -1f));
            }

            // Spark flash (hidden most of the time)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var srt = go.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(60f, 60f);
                _sparkFlash = go.GetComponent<Image>();
                _sparkFlash.sprite = soft;
                _sparkFlash.color = new Color(1f, 1f, 1f, 0f);
                _sparkFlash.raycastTarget = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Torch glow pulsing — torches are static (no animation), only glows breathe
            for (int i = 0; i < _torchGlows.Count; i++)
            {
                if (_torchGlows[i] == null) continue;
                _torchGlowPhases[i] += dt * 1.8f;
                float glowPulse = (Mathf.Sin(_torchGlowPhases[i]) + 1f) * 0.5f;
                float glowAlpha = Mathf.Lerp(0.06f, 0.18f, glowPulse);
                var glowColor = _torchGlows[i].color;
                glowColor.a = glowAlpha;
                _torchGlows[i].color = glowColor;
            }

            // Runic overlay pulse — synced with the center glow breathing
            if (_runicOverlay != null)
            {
                float p = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
                _runicOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.05f, 0.35f, p));
            }

            // Runic circle breathing — slow sinusoidal scale + alpha pulse
            _pulsePhase += dt * 1.4f;
            if (_centerGlow != null)
            {
                float p = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;   // 0..1
                float s = Mathf.Lerp(0.92f, 1.10f, p);
                _centerGlow.rectTransform.localScale = new Vector3(s, s, 1f);
                var c = _glowColor;
                c.a = Mathf.Lerp(_glowColor.a * 0.55f, _glowColor.a * 1.25f, p);
                _centerGlow.color = c;
            }

            // Torch flicker — layered Perlin noise, fast + slow
            if (_flickerTint != null)
            {
                float n = Mathf.PerlinNoise(_flickerSeed, Time.time * 7f) * 0.6f
                        + Mathf.PerlinNoise(_flickerSeed + 7f, Time.time * 1.7f) * 0.4f;
                var c = _flickColor;
                c.a = _flickColor.a * Mathf.Lerp(0.4f, 1.6f, n);
                _flickerTint.color = c;
            }

            // Fog drift — wraps around the screen edges
            float screenW = ((RectTransform)transform).rect.width;
            if (screenW < 100f) screenW = 1920f;
            for (int i = 0; i < _fogPuffs.Count; i++)
            {
                var frt = _fogPuffs[i];
                if (frt == null) continue;
                var pos = frt.anchoredPosition;
                pos.x += _fogSpeeds[i] * dt;
                float half = frt.sizeDelta.x * 0.5f;
                if (pos.x > screenW + half) pos.x = -half;
                else if (pos.x < -half)     pos.x = screenW + half;
                frt.anchoredPosition = pos;
            }

            // Occasional sparks from broken screens — random pos, quick fade
            _sparkTimer -= dt;
            if (_sparkTimer <= 0f)
            {
                _sparkTimer = Random.Range(2.5f, 7f);
                if (_sparkFlash != null)
                {
                    _sparkFlash.rectTransform.anchorMin = _sparkFlash.rectTransform.anchorMax =
                        new Vector2(Random.Range(0.06f, 0.94f), Random.Range(0.08f, 0.30f));
                    _sparkFlash.color = new Color(0.9f, 0.95f, 1f, 0.85f);
                }
            }
            if (_sparkFlash != null && _sparkFlash.color.a > 0f)
            {
                var c = _sparkFlash.color;
                c.a = Mathf.Max(0f, c.a - dt * 4f);
                _sparkFlash.color = c;
            }
        }

        // Soft radial-falloff circle sprite (white center fading to transparent).
        private static Sprite _softCircle;
        private static Sprite SoftCircle()
        {
            if (_softCircle != null) return _softCircle;
            int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - r + 0.5f) / r, dy = (y - r + 0.5f) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // soft falloff
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _softCircle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return _softCircle;
        }
    }
}
