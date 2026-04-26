using UnityEngine;

namespace GameIdle
{
    public static class CharacterIconFactory
    {
        private const int Size = 128;

        public static Sprite CreateIcon(Color baseColor)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color[Size * Size];

            float cx = Size / 2f;
            float cy = Size / 2f;
            float outerR = Size / 2f - 1f;
            float innerR = outerR * 0.65f;

            Color light = Color.Lerp(baseColor, Color.white, 0.45f);
            Color dark  = Color.Lerp(baseColor, Color.black, 0.35f);
            Color ring  = Color.Lerp(baseColor, Color.white, 0.15f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerR)
                    {
                        pixels[y * Size + x] = Color.clear;
                    }
                    else if (dist > outerR - 4f)
                    {
                        pixels[y * Size + x] = ring;
                    }
                    else
                    {
                        float t = dist / outerR;
                        Color radial = Color.Lerp(light, dark, t);

                        // top-left highlight
                        float angleFactor = Mathf.Clamp01((-dx - dy) / outerR * 0.5f + 0.5f);
                        Color final = Color.Lerp(radial, Color.white, angleFactor * 0.25f);

                        // inner bright circle
                        if (dist < innerR)
                        {
                            float innerT = dist / innerR;
                            final = Color.Lerp(Color.Lerp(light, Color.white, 0.3f), final, innerT);
                        }

                        final.a = 1f;
                        pixels[y * Size + x] = final;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
