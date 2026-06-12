using System.Collections.Generic;
using UnityEngine;

namespace GameIdle
{
    // Removes the background from GPT-generated character images by flood-filling
    // from the borders. Handles solid white, light gray, AND the fake "checkerboard"
    // transparency pattern some images bake in as real pixels — anything connected
    // to the edge that isn't part of the character gets cleared to transparent.
    public static class SpriteBackgroundRemover
    {
        // Cache processed textures so we only do the work once per source.
        private static readonly Dictionary<Texture2D, Texture2D> _cache = new();

        // Per-step color distance allowed when walking the background region.
        // Big enough to bridge white<->gray checkerboard squares, small enough to
        // stop at the sharp, saturated edge of the character.
        private const float StepTolerance = 0.32f;

        // Same as Process() but skips the cache so the full sheet texture is always
        // returned (sheets are sliced into many sprites so caching the whole is fine).
        public static Texture2D ProcessSheet(Texture2D src) => Process(src);

        public static Texture2D Process(Texture2D src)
        {
            if (src == null) return null;
            if (_cache.TryGetValue(src, out var cached) && cached != null) return cached;

            Color32[] px;
            try { px = src.GetPixels32(); }
            catch
            {
                // Texture not readable — return original unchanged.
                _cache[src] = src;
                return src;
            }

            int w = src.width, h = src.height;
            var visited = new bool[w * h];
            var stack = new Stack<int>();

            // Seed from every border pixel.
            for (int x = 0; x < w; x++)
            {
                PushSeed(stack, visited, x, 0, w);
                PushSeed(stack, visited, x, h - 1, w);
            }
            for (int y = 0; y < h; y++)
            {
                PushSeed(stack, visited, 0, y, w);
                PushSeed(stack, visited, w - 1, y, w);
            }

            while (stack.Count > 0)
            {
                int idx = stack.Pop();
                int x = idx % w;
                int y = idx / w;
                Color32 cur = px[idx];

                // Only spread through "background-like" pixels: bright + unsaturated,
                // OR already transparent.
                if (cur.a < 16 || IsBackgroundish(cur))
                {
                    px[idx].a = 0;

                    TryNeighbor(px, visited, stack, x - 1, y, w, h, cur);
                    TryNeighbor(px, visited, stack, x + 1, y, w, h, cur);
                    TryNeighbor(px, visited, stack, x, y - 1, w, h, cur);
                    TryNeighbor(px, visited, stack, x, y + 1, w, h, cur);
                }
            }

            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            dst.SetPixels32(px);
            dst.Apply();
            _cache[src] = dst;
            return dst;
        }

        private static void PushSeed(Stack<int> stack, bool[] visited, int x, int y, int w)
        {
            int idx = y * w + x;
            if (!visited[idx]) { visited[idx] = true; stack.Push(idx); }
        }

        private static void TryNeighbor(Color32[] px, bool[] visited, Stack<int> stack,
            int x, int y, int w, int h, Color32 from)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            int idx = y * w + x;
            if (visited[idx]) return;

            Color32 c = px[idx];
            // Bridge to neighbor only if it's already transparent, background-ish,
            // or close in color to where we came from (walks the checkerboard).
            if (c.a < 16 || IsBackgroundish(c) || ColorDist(c, from) <= StepTolerance)
            {
                visited[idx] = true;
                stack.Push(idx);
            }
        }

        private static bool IsBackgroundish(Color32 c)
        {
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float maxC = Mathf.Max(r, Mathf.Max(g, b));
            float minC = Mathf.Min(r, Mathf.Min(g, b));
            float sat = maxC > 0.001f ? (maxC - minC) / maxC : 0f;
            // Any neutral gray (light OR dark checkerboard squares) — low saturation enough.
            return sat < 0.12f;
        }

        private static float ColorDist(Color32 a, Color32 b)
        {
            float dr = (a.r - b.r) / 255f;
            float dg = (a.g - b.g) / 255f;
            float db = (a.b - b.b) / 255f;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
