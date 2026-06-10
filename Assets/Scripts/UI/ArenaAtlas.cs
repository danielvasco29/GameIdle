using UnityEngine;

namespace GameIdle
{
    // Slices the arena FX atlas (Assets/Resources/FX/arena_atlas.png) into
    // individual sprites using normalized rects, so the art works at any
    // export resolution with the same layout.
    //
    // Atlas layout (left-to-right, top-to-bottom):
    //   - server racks / cables / floor panels (left half)
    //   - torch flame frames: 2 rows x 6 columns (top-right) — animation frames
    //   - glowing mushroom clusters x3
    //   - runic circle detail (large square)
    //   - fog/mist puffs x6 (bottom-center-left)
    //   - CRT screens / broken keyboards (right)
    public static class ArenaAtlas
    {
        private static Texture2D _tex;
        private static bool _loaded;

        public static bool Available => Load() != null;

        private static Texture2D Load()
        {
            if (_loaded) return _tex;
            _loaded = true;
            _tex = Resources.Load<Texture2D>("FX/arena_atlas");
            return _tex;
        }

        // Creates a sprite from a normalized rect (0-1 in atlas space, origin bottom-left).
        private static Sprite Slice(float x, float y, float w, float h)
        {
            var tex = Load();
            if (tex == null) return null;
            var r = new Rect(
                Mathf.Round(x * tex.width),
                Mathf.Round((1f - y - h) * tex.height), // y given from top for readability
                Mathf.Round(w * tex.width),
                Mathf.Round(h * tex.height));
            return Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // ── Torch flames: 12 animation frames (2 rows x 6) ───────────────────
        // Top-right block, each torch ~0.045 wide, rows at y 0.11 / 0.26.
        public static Sprite[] TorchFrames()
        {
            if (!Available) return null;
            var frames = new Sprite[12];
            float[] colX = { 0.523f, 0.566f, 0.605f, 0.643f, 0.682f, 0.722f };
            for (int i = 0; i < 6; i++)
                frames[i] = Slice(colX[i], 0.110f, 0.042f, 0.180f);
            for (int i = 0; i < 6; i++)
                frames[6 + i] = Slice(colX[i], 0.265f, 0.042f, 0.180f);
            return frames;
        }

        // ── Fog puffs x6 (two rows of three, bottom-center-left) ─────────────
        public static Sprite[] FogPuffs()
        {
            if (!Available) return null;
            return new[]
            {
                Slice(0.318f, 0.715f, 0.055f, 0.090f),
                Slice(0.378f, 0.715f, 0.055f, 0.090f),
                Slice(0.432f, 0.715f, 0.055f, 0.090f),
                Slice(0.318f, 0.835f, 0.055f, 0.090f),
                Slice(0.378f, 0.835f, 0.055f, 0.090f),
                Slice(0.432f, 0.835f, 0.055f, 0.090f),
            };
        }

        // ── Runic circle detail (large square, center-right) ─────────────────
        public static Sprite RunicCircle() =>
            Available ? Slice(0.517f, 0.568f, 0.208f, 0.360f) : null;

        // ── Glowing mushroom clusters x3 ──────────────────────────────────────
        public static Sprite[] Mushrooms()
        {
            if (!Available) return null;
            return new[]
            {
                Slice(0.515f, 0.420f, 0.085f, 0.090f),
                Slice(0.605f, 0.420f, 0.075f, 0.090f),
                Slice(0.678f, 0.420f, 0.060f, 0.090f),
            };
        }
    }
}
