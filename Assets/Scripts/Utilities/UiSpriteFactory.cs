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
