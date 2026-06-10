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

        // Checker squares are neutral gray (~64 and ~100) while the art is
        // colorful neon, so within the checker brightness range the pixel's
        // saturation works as its alpha: pure gray vanishes, glow halos over
        // the checker fade out smoothly, saturated art stays opaque. Pixels
        // darker or brighter than the checker range are solid art.
        private static Texture2D RemoveChecker(Texture2D src)
        {
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                int mn = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                int mx = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                if (mn >= 50 && mn <= 118)
                {
                    int a = Mathf.Min(255, (mx - mn) * 255 / 70);
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

        // ── Torch flames: 12 animation frames (2 rows x 6) ───────────────────
        // Flame cores measured at these column centers; each cell is 64 wide.
        // Row 1: y 86..200, row 2: y 200..316 (flame + sconce + wall bracket).
        public static Sprite[] TorchFrames()
        {
            if (!Available) return null;
            float[] cx = { 745f, 810f, 864f, 917f, 969f, 1021f };
            var frames = new Sprite[12];
            for (int i = 0; i < 6; i++)
                frames[i] = Slice(cx[i] - 32f, 86f, 64f, 114f);
            for (int i = 0; i < 6; i++)
                frames[6 + i] = Slice(cx[i] - 32f, 200f, 64f, 116f);
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
                frames[i] = Slice(cx[i] - 32f, 86f, 64f, 84f);
            for (int i = 0; i < 6; i++)
                frames[6 + i] = Slice(cx[i] - 32f, 200f, 64f, 84f);
            return frames;
        }

        // No dedicated fog art in this atlas — callers fall back to the
        // procedural soft-circle fog.
        public static Sprite[] FogPuffs() => null;

        // ── Runic circle detail (large stone square, below the mushrooms) ────
        public static Sprite RunicCircle() =>
            Available ? Slice(675f, 465f, 327f, 295f) : null;

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
