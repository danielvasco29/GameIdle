using UnityEngine;

namespace GameIdle
{
    // Generates simple UI sprites at runtime so we never depend on Unity's
    // built-in editor resources (Resources.GetBuiltinResource fails on some
    // Unity 6 setups, which broke rendering AND blocked raycasts).
    public static class UiSpriteFactory
    {
        private static Sprite circle;
        private static Sprite box;
        private static Sprite roundedBox;
        private static Sprite star;

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
