using UnityEngine;

namespace GameIdle
{
    // Generates simple UI sprites at runtime so we never depend on Unity's
    // built-in editor resources (Resources.GetBuiltinResource fails on some
    // Unity 6 setups, which broke rendering AND blocked raycasts).
    public static class UiSpriteFactory
    {
        private static Sprite circle;
        private static Sprite glow;
        private static Sprite box;
        private static Sprite roundedBox;
        private static Sprite star;
        private static Sprite vGradient;
        private static Sprite hGradient;
        private static Sprite gear;
        private static Sprite check;
        private static Sprite bolt;
        private static Sprite vignette;
        private static Material whiteDiscardMat;

        // Vertical white gradient: opaque white at the TOP fading to transparent
        // at the bottom. Tint + flip it to make highlights (top) or shadows
        // (bottom) for a sense of depth.
        public static Sprite VerticalGradient()
        {
            if (vGradient != null) return vGradient;
            const int h = 64;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[4 * h];
            for (int y = 0; y < h; y++)
            {
                float a = Mathf.Pow(y / (float)(h - 1), 1.4f); // bright at top
                var c = new Color32(255, 255, 255, (byte)(a * 255));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            vGradient = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            vGradient.name = "GenVGradient";
            return vGradient;
        }

        // Gradiente horizontal: transparente na ESQUERDA -> branco opaco na DIREITA.
        // Tingir para fazer um fade suave do cenario para o navy nas laterais.
        public static Sprite HorizontalGradient()
        {
            if (hGradient != null) return hGradient;
            const int w = 64;
            var tex = new Texture2D(w, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * 4];
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Pow(x / (float)(w - 1), 1.4f); // opaco à direita
                var c = new Color32(255, 255, 255, (byte)(a * 255));
                for (int y = 0; y < 4; y++) px[y * w + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            hGradient = Sprite.Create(tex, new Rect(0, 0, w, 4), new Vector2(0.5f, 0.5f), 100f);
            hGradient.name = "GenHGradient";
            return hGradient;
        }

        // Engrenagem (cog) desenhada em runtime — usada como icone de configuracoes.
        public static Sprite Gear()
        {
            if (gear != null) return gear;
            const int N = 64;
            float c = (N - 1) / 2f;
            const int teeth = 8;
            const float hole = 9f, baseR = 19f, toothR = 28f;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float tw = Mathf.Cos(ang * teeth);          // -1..1 (cria os dentes)
                float t  = Mathf.SmoothStep(0f, 1f, (tw + 0.25f) / 0.5f);
                float outer = Mathf.Lerp(baseR, toothR, t);
                float a = 0f;
                if (r <= outer && r >= hole)
                    a = Mathf.Clamp01(Mathf.Min(outer - r, r - hole)); // AA ~1px nas bordas
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            gear = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            gear.name = "GenGear";
            return gear;
        }

        // Polilinha grossa desenhada em runtime (base para check, raio, etc.).
        private static Sprite Polyline(Vector2[] pts, float thickness, ref Sprite cache, string name)
        {
            if (cache != null) return cache;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            float half = thickness * 0.5f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = float.MaxValue;
                for (int i = 0; i < pts.Length - 1; i++)
                    d = Mathf.Min(d, DistToSegment(p, pts[i], pts[i + 1]));
                float a = Mathf.Clamp01(half - d + 0.5f); // AA ~1px
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            cache = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            cache.name = name;
            return cache;
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude);
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + ab * t);
        }

        // Check (✓) — icone de missoes (coords y de baixo p/ cima).
        public static Sprite Check() => Polyline(
            new[] { new Vector2(14f, 34f), new Vector2(27f, 20f), new Vector2(51f, 48f) },
            8f, ref check, "GenCheck");

        // Raio (lightning) — icone de bonus.
        public static Sprite Bolt() => Polyline(
            new[] { new Vector2(40f, 58f), new Vector2(22f, 31f), new Vector2(34f, 31f), new Vector2(20f, 6f) },
            7f, ref bolt, "GenBolt");

        // Vinheta radial: centro transparente -> bordas escuras (profundidade).
        public static Sprite Vignette()
        {
            if (vignette != null) return vignette;
            const int N = 128;
            float c = (N - 1) / 2f, maxR = c;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / maxR; // 0 centro .. ~1 borda
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.45f)) * 0.6f;
                px[y * N + x] = new Color32(2, 4, 10, (byte)(a * 255)); // quase preto levemente navy
            }
            tex.SetPixels32(px);
            tex.Apply();
            vignette = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            vignette.name = "GenVignette";
            return vignette;
        }

        // Material that discards near-white/background pixels from GPT-generated sprites.
        public static Material WhiteDiscardMaterial()
        {
            if (whiteDiscardMat != null) return whiteDiscardMat;
            var shader = Resources.Load<Shader>("Shaders/UIWhiteDiscard");
            if (shader == null)
            {
                Debug.LogWarning("[UiSpriteFactory] UIWhiteDiscard shader not found — falling back to default UI shader");
                shader = Shader.Find("UI/Default");
            }
            whiteDiscardMat = new Material(shader) { name = "WhiteDiscard" };
            return whiteDiscardMat;
        }

        // Filled 5-pointed star (anti-aliased via 2x2 supersampling).
        public static Sprite Star()
        {
            if (star != null) return star;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var c = new Vector2(s / 2f, s / 2f);
            float outer = s * 0.47f, inner = outer * 0.42f;
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.Deg2Rad * (-90f + i * 36f);
                float r = (i % 2 == 0) ? outer : inner;
                pts[i] = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int hits = 0;
                    for (int sx = 0; sx < 2; sx++)
                        for (int sy = 0; sy < 2; sy++)
                            if (PointInPoly(new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f), pts))
                                hits++;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(hits / 4f * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            star = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            star.name = "GenStar";
            return star;
        }

        private static bool PointInPoly(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        // 9-sliced rounded rectangle. Corners stay crisp at any size when the
        // Image uses Image.Type.Sliced.
        public static Sprite RoundedBox()
        {
            if (roundedBox != null) return roundedBox;
            const int s = 48;
            const float radius = 14f;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // distance into the nearest corner's rounding region
                    float dx = Mathf.Min(x + 0.5f, s - 0.5f - x);
                    float dy = Mathf.Min(y + 0.5f, s - 0.5f - y);
                    float a = 1f;
                    if (dx < radius && dy < radius)
                    {
                        float dist = Vector2.Distance(new Vector2(dx, dy), new Vector2(radius, radius));
                        a = Mathf.Clamp01(radius - dist + 0.5f);
                    }
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            roundedBox = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            roundedBox.name = "GenRoundedBox";
            return roundedBox;
        }

        // Anti-aliased solid white circle.
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = s / 2f;
            var center = new Vector2(r - 0.5f, r - 0.5f);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(r - d); // ~1px soft edge
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            circle.name = "GenCircle";
            return circle;
        }

        // Soft radial halo: bright at the centre, smoothly fading to transparent
        // at the rim. Tint it and place it behind a solid Circle() face to make a
        // premium glowing button (instead of a flat tinted disc).
        public static Sprite Glow()
        {
            if (glow != null) return glow;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = s / 2f;
            var center = new Vector2(r - 0.5f, r - 0.5f);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / r; // 0..1
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // quadratic falloff → soft halo
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            glow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            glow.name = "GenGlow";
            return glow;
        }

        // Plain solid white square — used for cards/buttons/panels (sharp corners).
        public static Sprite Box()
        {
            if (box != null) return box;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            box = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            box.name = "GenBox";
            return box;
        }
    }
}
