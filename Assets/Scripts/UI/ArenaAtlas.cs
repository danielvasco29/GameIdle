using UnityEngine;

namespace GameIdle
{
    // Slices the arena FX atlas (Assets/Resources/FX/arena_atlas.png) into
    // individual sprites. Coordinates were measured on the 1376x768 source and
    // are scaled to the actual texture size, so a re-export at another
    // resolution with the same layout still works.
    //
    // The source PNG has an opaque gray checkerboard baked in instead of real
    // transparency, so the texture is preprocessed once: neutral-gray pixels
    // (the checker squares) become fully transparent.
    public static class ArenaAtlas
    {
        private const float SrcW = 1376f, SrcH = 768f;

        private static Texture2D _tex;
        private static bool _loaded;

        public static bool Available => Load() != null;

        private static Texture2D Load()
        {
            if (_loaded) return _tex;
            _loaded = true;
            var raw = Resources.Load<Texture2D>("FX/arena_atlas");
            if (raw == null) return null;
            _tex = raw.isReadable ? RemoveChecker(raw) : raw;
            return _tex;
        }

        // Checker squares are neutral gray while the art is mostly colorful
        // neon. Low-chroma gray pixels become transparent, and slightly
        // colored edge pixels fade in so the cutout stays soft.
        private static Texture2D RemoveChecker(Texture2D src)
        {
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                int mn = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                int mx = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                int chroma = mx - mn;
                if (mn >= 35 && mn <= 145 && chroma <= 60)
                {
                    int a = Mathf.Clamp((chroma - 18) * 255 / 42, 0, 255);
                    px[i] = new Color32(p.r, p.g, p.b, (byte)a);
                }
            }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        // Slice by pixel coords on the 1376x768 reference layout; y is from the
        // top of the image for readability.
        private static Sprite Slice(float x, float yTop, float w, float h)
        {
            var tex = Load();
            if (tex == null) return null;
            float sx = tex.width / SrcW, sy = tex.height / SrcH;
            var r = new Rect(
                Mathf.Round(x * sx),
                Mathf.Round((SrcH - yTop - h) * sy),
                Mathf.Round(w * sx),
                Mathf.Round(h * sy));
            return Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite SliceCleanDark(float x, float yTop, float w, float h)
        {
            var tex = Load();
            if (tex == null) return null;
            float sx = tex.width / SrcW, sy = tex.height / SrcH;
            int rx = Mathf.RoundToInt(x * sx);
            int ry = Mathf.RoundToInt((SrcH - yTop - h) * sy);
            int rw = Mathf.RoundToInt(w * sx);
            int rh = Mathf.RoundToInt(h * sy);

            try
            {
                var frame = new Texture2D(rw, rh, TextureFormat.RGBA32, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 4
                };
                var srcPx = tex.GetPixels32();
                var framePx = new Color32[rw * rh];
                for (int yy = 0; yy < rh; yy++)
                {
                    int srcRow = (ry + yy) * tex.width + rx;
                    int dstRow = yy * rw;
                    for (int xx = 0; xx < rw; xx++)
                        framePx[dstRow + xx] = srcPx[srcRow + xx];
                }
                frame.SetPixels32(framePx);
                frame.Apply(true);

                var clean = SpriteBackgroundRemover.ProcessDarkBg(frame);
                return Sprite.Create(clean, new Rect(0, 0, clean.width, clean.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch
            {
                return Slice(x, yTop, w, h);
            }
        }

        // ── Torch flames: 12 animation frames (2 rows x 6) ───────────────────
        // Flame cores measured at these column centers; each cell is 64 wide.
        // Row 1: y 86..200, row 2: y 200..316 (flame + sconce + wall bracket).
        public static Sprite[] TorchFrames()
        {
            if (!Available) return null;
            float[] cx = { 745f, 810f, 864f, 917f, 969f, 1021f };
            var frames = new Sprite[12];
            for (int i = 0; i < 6; i++)
                frames[i] = SliceCleanDark(cx[i] - 32f, 86f, 64f, 114f);
            for (int i = 0; i < 6; i++)
                frames[6 + i] = SliceCleanDark(cx[i] - 32f, 200f, 64f, 116f);
            return frames;
        }

        // Flame-only variant (no sconce/bracket): used to animate just the fire
        // on top of torches already painted into the battle background.
        public static Sprite[] FlameFrames()
        {
            if (!Available) return null;
            float[] cx = { 745f, 810f, 864f, 917f, 969f, 1021f };
            var frames = new Sprite[12];
            for (int i = 0; i < 6; i++)
                frames[i] = SliceCleanDark(cx[i] - 32f, 86f, 64f, 84f);
            for (int i = 0; i < 6; i++)
                frames[6 + i] = SliceCleanDark(cx[i] - 32f, 200f, 64f, 84f);
            return frames;
        }

        // No dedicated fog art in this atlas — callers fall back to the
        // procedural soft-circle fog.
        public static Sprite[] FogPuffs() => null;

        // ── Runic circle detail (large stone square, below the mushrooms) ────
        // The art is a square stone tile; an elliptical alpha mask is baked in
        // so only the circle shows when laid on the arena floor.
        private static Sprite _runic;
        public static Sprite RunicCircle()
        {
            if (_runic != null) return _runic;
            var tex = Load();
            if (tex == null || !tex.isReadable) return null;
            float sx = tex.width / SrcW, sy = tex.height / SrcH;
            int x0 = Mathf.RoundToInt(675f * sx), w = Mathf.RoundToInt(327f * sx);
            int y0 = Mathf.RoundToInt((SrcH - 465f - 295f) * sy), h = Mathf.RoundToInt(295f * sy);
            var px = tex.GetPixels32(0);
            var outPx = new Color32[w * h];
            float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = px[(y0 + y) * tex.width + (x0 + x)];
                float dx = (x - cx) / cx, dy = (y - cy) / cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                // solid inside, fades out between 88% and 100% of the radius
                float m = Mathf.Clamp01((1f - d) / 0.12f);
                p.a = (byte)(p.a * m);
                outPx[y * w + x] = p;
            }
            var rt = new Texture2D(w, h, TextureFormat.RGBA32, false);
            rt.filterMode = FilterMode.Bilinear;
            rt.SetPixels32(outPx);
            rt.Apply(false, false);
            _runic = Sprite.Create(rt, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                                   100f, 0, SpriteMeshType.FullRect);
            return _runic;
        }

        // ── Glowing mushroom clusters x3 ──────────────────────────────────────
        public static Sprite[] Mushrooms()
        {
            if (!Available) return null;
            return new[]
            {
                Slice(700f, 320f, 118f, 80f),
                Slice(823f, 328f, 105f, 72f),
                Slice(930f, 320f, 78f,  80f),
            };
        }
    }
}
