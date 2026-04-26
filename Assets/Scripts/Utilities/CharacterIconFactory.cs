using UnityEngine;

namespace GameIdle
{
    public static class CharacterIconFactory
    {
        private const int Size = 128;

        public static Sprite CreateIcon(Color baseColor, string characterId = "")
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color[Size * Size];

            float cx = Size / 2f;
            float cy = Size / 2f;
            float outerR = Size / 2f - 1f;
            float innerR = outerR * 0.72f;

            Color light = Color.Lerp(baseColor, Color.white, 0.5f);
            Color dark  = Color.Lerp(baseColor, Color.black, 0.4f);
            Color rim   = Color.Lerp(baseColor, Color.white, 0.2f);

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
                    else if (dist > outerR - 5f)
                    {
                        pixels[y * Size + x] = new Color(rim.r, rim.g, rim.b, 1f);
                    }
                    else
                    {
                        float t = dist / outerR;
                        Color radial = Color.Lerp(light, dark, t * t);

                        float angleFactor = Mathf.Clamp01((-dx - dy) / outerR * 0.4f + 0.5f);
                        Color final = Color.Lerp(radial, Color.white, angleFactor * 0.2f);

                        if (dist < innerR * 0.4f)
                        {
                            float centerT = dist / (innerR * 0.4f);
                            final = Color.Lerp(Color.Lerp(Color.white, light, 0.4f), final, centerT);
                        }

                        final.a = 1f;
                        pixels[y * Size + x] = final;
                    }
                }
            }

            DrawSymbol(pixels, Size, cx, cy, outerR, characterId);

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static void DrawSymbol(Color[] pixels, int size, float cx, float cy, float outerR, string id)
        {
            Color symbolColor = new Color(1f, 1f, 1f, 0.9f);
            float r = outerR * 0.38f;

            switch (id)
            {
                case "dev":
                    // < > brackets
                    DrawLine(pixels, size, cx - r, cy, cx - r * 0.35f, cy + r * 0.6f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx - r, cy, cx - r * 0.35f, cy - r * 0.6f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx + r, cy, cx + r * 0.35f, cy + r * 0.6f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx + r, cy, cx + r * 0.35f, cy - r * 0.6f, symbolColor, 3.5f);
                    break;

                case "marketing":
                    // Megaphone / signal bars
                    DrawFilledRect(pixels, size, cx - r * 0.15f, cy - r * 0.2f, r * 0.3f, r * 0.4f, symbolColor);
                    DrawFilledRect(pixels, size, cx + r * 0.2f, cy - r * 0.5f, r * 0.3f, r * 0.7f, symbolColor);
                    DrawFilledRect(pixels, size, cx + r * 0.6f, cy - r * 0.8f, r * 0.3f, r * 1.0f, symbolColor);
                    DrawFilledRect(pixels, size, cx - r * 0.6f, cy - r * 0.4f, r * 0.35f, r * 0.6f, symbolColor);
                    break;

                case "designer":
                    // Diamond shape
                    DrawLine(pixels, size, cx, cy - r, cx + r * 0.7f, cy, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx + r * 0.7f, cy, cx, cy + r, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx, cy + r, cx - r * 0.7f, cy, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy, cx, cy - r, symbolColor, 3.5f);
                    break;

                case "ceo":
                    // Crown: three points
                    DrawLine(pixels, size, cx - r * 0.8f, cy + r * 0.4f, cx - r * 0.8f, cy - r * 0.1f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx - r * 0.8f, cy - r * 0.1f, cx, cy - r * 0.8f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx, cy - r * 0.8f, cx + r * 0.8f, cy - r * 0.1f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx + r * 0.8f, cy - r * 0.1f, cx + r * 0.8f, cy + r * 0.4f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx - r * 0.8f, cy + r * 0.4f, cx + r * 0.8f, cy + r * 0.4f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx, cy - r * 0.8f, cx, cy - r * 0.1f, symbolColor, 3.5f);
                    break;

                case "cto":
                    // Circuit / gear: circle + lines
                    DrawCircleOutline(pixels, size, cx, cy, r * 0.5f, symbolColor, 3f);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        float x0 = cx + Mathf.Cos(angle) * r * 0.5f;
                        float y0 = cy + Mathf.Sin(angle) * r * 0.5f;
                        float x1 = cx + Mathf.Cos(angle) * r * 0.95f;
                        float y1 = cy + Mathf.Sin(angle) * r * 0.95f;
                        DrawLine(pixels, size, x0, y0, x1, y1, symbolColor, 2.5f);
                    }
                    break;

                case "ai_engineer":
                    // Neural network: 3 nodes connected
                    DrawCircleOutline(pixels, size, cx - r * 0.7f, cy - r * 0.4f, r * 0.18f, symbolColor, 2.5f);
                    DrawCircleOutline(pixels, size, cx - r * 0.7f, cy + r * 0.4f, r * 0.18f, symbolColor, 2.5f);
                    DrawCircleOutline(pixels, size, cx, cy, r * 0.18f, symbolColor, 2.5f);
                    DrawCircleOutline(pixels, size, cx + r * 0.7f, cy - r * 0.4f, r * 0.18f, symbolColor, 2.5f);
                    DrawCircleOutline(pixels, size, cx + r * 0.7f, cy + r * 0.4f, r * 0.18f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy - r * 0.4f, cx, cy, symbolColor, 2f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy + r * 0.4f, cx, cy, symbolColor, 2f);
                    DrawLine(pixels, size, cx, cy, cx + r * 0.7f, cy - r * 0.4f, symbolColor, 2f);
                    DrawLine(pixels, size, cx, cy, cx + r * 0.7f, cy + r * 0.4f, symbolColor, 2f);
                    break;

                case "investor":
                    // Dollar sign: vertical line + S curve approximated
                    DrawLine(pixels, size, cx, cy - r * 0.9f, cx, cy + r * 0.9f, symbolColor, 3.5f);
                    DrawArc(pixels, size, cx, cy - r * 0.3f, r * 0.5f, Mathf.PI * 0.1f, Mathf.PI * 1.1f, symbolColor, 3f);
                    DrawArc(pixels, size, cx, cy + r * 0.3f, r * 0.5f, Mathf.PI * 1.1f, Mathf.PI * 2.1f, symbolColor, 3f);
                    break;

                case "manager":
                    // Phone receiver shape
                    DrawArc(pixels, size, cx - r * 0.1f, cy, r * 0.7f, Mathf.PI * 0.3f, Mathf.PI * 1.7f, symbolColor, 3.5f);
                    DrawFilledCircle(pixels, size, cx - r * 0.6f, cy - r * 0.4f, r * 0.18f, symbolColor);
                    DrawFilledCircle(pixels, size, cx + r * 0.4f, cy + r * 0.4f, r * 0.18f, symbolColor);
                    break;

                case "puxa_saco":
                    // Heart shape
                    DrawArc(pixels, size, cx - r * 0.3f, cy + r * 0.15f, r * 0.35f, Mathf.PI, Mathf.PI * 2f, symbolColor, 3.5f);
                    DrawArc(pixels, size, cx + r * 0.3f, cy + r * 0.15f, r * 0.35f, Mathf.PI, Mathf.PI * 2f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx - r * 0.65f, cy + r * 0.15f, cx, cy - r * 0.65f, symbolColor, 3.5f);
                    DrawLine(pixels, size, cx + r * 0.65f, cy + r * 0.15f, cx, cy - r * 0.65f, symbolColor, 3.5f);
                    break;

                case "analista_dados":
                    // Bar chart + trend line
                    DrawFilledRect(pixels, size, cx - r * 0.85f, cy + r * 0.2f, r * 0.35f, r * 0.5f, symbolColor);
                    DrawFilledRect(pixels, size, cx - r * 0.35f, cy - r * 0.1f, r * 0.35f, r * 0.8f, symbolColor);
                    DrawFilledRect(pixels, size, cx + r * 0.15f, cy - r * 0.4f, r * 0.35f, r * 1.1f, symbolColor);
                    DrawLine(pixels, size, cx - r * 0.9f, cy + r * 0.6f, cx + r * 0.6f, cy + r * 0.6f, symbolColor, 2f);
                    break;

                case "suporte_n1":
                    // Headset: arc + bar + mic
                    DrawArc(pixels, size, cx, cy - r * 0.1f, r * 0.65f, Mathf.PI * 0.1f, Mathf.PI * 0.9f, symbolColor, 3.5f);
                    DrawFilledRect(pixels, size, cx - r * 0.75f, cy - r * 0.1f, r * 0.2f, r * 0.45f, symbolColor);
                    DrawFilledRect(pixels, size, cx + r * 0.55f, cy - r * 0.1f, r * 0.2f, r * 0.45f, symbolColor);
                    DrawLine(pixels, size, cx - r * 0.05f, cy + r * 0.55f, cx - r * 0.05f, cy + r * 0.9f, symbolColor, 3f);
                    DrawFilledCircle(pixels, size, cx - r * 0.05f, cy + r * 0.9f, r * 0.15f, symbolColor);
                    break;

                case "suporte_n2":
                    // Headset with wifi signal
                    DrawArc(pixels, size, cx, cy - r * 0.2f, r * 0.55f, Mathf.PI * 0.1f, Mathf.PI * 0.9f, symbolColor, 3.5f);
                    DrawFilledRect(pixels, size, cx - r * 0.65f, cy - r * 0.2f, r * 0.2f, r * 0.4f, symbolColor);
                    DrawFilledRect(pixels, size, cx + r * 0.45f, cy - r * 0.2f, r * 0.2f, r * 0.4f, symbolColor);
                    DrawArc(pixels, size, cx, cy - r * 0.7f, r * 0.3f, Mathf.PI * 1.2f, Mathf.PI * 1.8f, symbolColor, 2.5f);
                    DrawArc(pixels, size, cx, cy - r * 0.7f, r * 0.55f, Mathf.PI * 1.25f, Mathf.PI * 1.75f, symbolColor, 2.5f);
                    break;

                case "analista_redes":
                    // Network: central node + 4 connections
                    DrawFilledCircle(pixels, size, cx, cy, r * 0.2f, symbolColor);
                    DrawFilledCircle(pixels, size, cx, cy - r * 0.8f, r * 0.15f, symbolColor);
                    DrawFilledCircle(pixels, size, cx, cy + r * 0.8f, r * 0.15f, symbolColor);
                    DrawFilledCircle(pixels, size, cx - r * 0.8f, cy, r * 0.15f, symbolColor);
                    DrawFilledCircle(pixels, size, cx + r * 0.8f, cy, r * 0.15f, symbolColor);
                    DrawLine(pixels, size, cx, cy, cx, cy - r * 0.65f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx, cy, cx, cy + r * 0.65f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx, cy, cx - r * 0.65f, cy, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx, cy, cx + r * 0.65f, cy, symbolColor, 2.5f);
                    break;

                case "analista_infra":
                    // Server stack: 3 rectangles
                    DrawFilledRect(pixels, size, cx - r * 0.75f, cy - r * 0.85f, r * 1.5f, r * 0.4f, symbolColor);
                    DrawFilledRect(pixels, size, cx - r * 0.75f, cy - r * 0.3f, r * 1.5f, r * 0.4f, symbolColor);
                    DrawFilledRect(pixels, size, cx - r * 0.75f, cy + r * 0.25f, r * 1.5f, r * 0.4f, symbolColor);
                    DrawFilledCircle(pixels, size, cx + r * 0.45f, cy - r * 0.65f, r * 0.09f,
                        new Color(0.1f, 0.1f, 0.1f, 0.8f));
                    DrawFilledCircle(pixels, size, cx + r * 0.45f, cy - r * 0.1f, r * 0.09f,
                        new Color(0.1f, 0.1f, 0.1f, 0.8f));
                    DrawFilledCircle(pixels, size, cx + r * 0.45f, cy + r * 0.45f, r * 0.09f,
                        new Color(0.1f, 0.1f, 0.1f, 0.8f));
                    break;

                case "escovador_bits":
                    // Bug: rounded body + antenna + legs
                    DrawFilledCircle(pixels, size, cx, cy, r * 0.4f, symbolColor);
                    DrawFilledCircle(pixels, size, cx, cy - r * 0.65f, r * 0.25f, symbolColor);
                    DrawLine(pixels, size, cx - r * 0.25f, cy - r * 0.55f, cx - r * 0.6f, cy - r * 0.9f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx + r * 0.25f, cy - r * 0.55f, cx + r * 0.6f, cy - r * 0.9f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx - r * 0.4f, cy - r * 0.1f, cx - r * 0.85f, cy - r * 0.3f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx + r * 0.4f, cy - r * 0.1f, cx + r * 0.85f, cy - r * 0.3f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx - r * 0.4f, cy + r * 0.2f, cx - r * 0.85f, cy + r * 0.4f, symbolColor, 2.5f);
                    DrawLine(pixels, size, cx + r * 0.4f, cy + r * 0.2f, cx + r * 0.85f, cy + r * 0.4f, symbolColor, 2.5f);
                    break;

                case "secretaria":
                    // Calendar: rectangle with grid
                    DrawFilledRect(pixels, size, cx - r * 0.7f, cy - r * 0.65f, r * 1.4f, r * 1.3f, new Color(symbolColor.r, symbolColor.g, symbolColor.b, 0.3f));
                    DrawLine(pixels, size, cx - r * 0.7f, cy - r * 0.65f, cx + r * 0.7f, cy - r * 0.65f, symbolColor, 3f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy - r * 0.65f, cx - r * 0.7f, cy + r * 0.65f, symbolColor, 3f);
                    DrawLine(pixels, size, cx + r * 0.7f, cy - r * 0.65f, cx + r * 0.7f, cy + r * 0.65f, symbolColor, 3f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy + r * 0.65f, cx + r * 0.7f, cy + r * 0.65f, symbolColor, 3f);
                    DrawLine(pixels, size, cx - r * 0.7f, cy - r * 0.2f, cx + r * 0.7f, cy - r * 0.2f, symbolColor, 2f);
                    DrawLine(pixels, size, cx, cy - r * 0.65f, cx, cy + r * 0.65f, symbolColor, 2f);
                    DrawFilledCircle(pixels, size, cx - r * 0.35f, cy + r * 0.15f, r * 0.12f, symbolColor);
                    DrawFilledCircle(pixels, size, cx + r * 0.35f, cy + r * 0.15f, r * 0.12f, symbolColor);
                    DrawFilledCircle(pixels, size, cx - r * 0.35f, cy + r * 0.5f, r * 0.12f, symbolColor);
                    DrawFilledCircle(pixels, size, cx + r * 0.35f, cy + r * 0.5f, r * 0.12f, symbolColor);
                    break;

                default:
                    DrawFilledCircle(pixels, size, cx, cy, r * 0.35f, symbolColor);
                    break;
            }
        }

        private static void DrawLine(Color[] pixels, int size, float x0, float y0, float x1, float y1, Color col, float thickness)
        {
            int steps = (int)(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2) + 1;
            float half = thickness / 2f;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float px = Mathf.Lerp(x0, x1, t);
                float py = Mathf.Lerp(y0, y1, t);
                PaintDisk(pixels, size, px, py, half, col);
            }
        }

        private static void DrawCircleOutline(Color[] pixels, int size, float cx, float cy, float radius, Color col, float thickness)
        {
            int steps = (int)(radius * Mathf.PI * 2 * 2);
            for (int i = 0; i < steps; i++)
            {
                float angle = (float)i / steps * Mathf.PI * 2f;
                float px = cx + Mathf.Cos(angle) * radius;
                float py = cy + Mathf.Sin(angle) * radius;
                PaintDisk(pixels, size, px, py, thickness / 2f, col);
            }
        }

        private static void DrawArc(Color[] pixels, int size, float cx, float cy, float radius, float startAngle, float endAngle, Color col, float thickness)
        {
            int steps = (int)(radius * Mathf.Abs(endAngle - startAngle) * 2) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                float px = cx + Mathf.Cos(angle) * radius;
                float py = cy + Mathf.Sin(angle) * radius;
                PaintDisk(pixels, size, px, py, thickness / 2f, col);
            }
        }

        private static void DrawFilledCircle(Color[] pixels, int size, float cx, float cy, float radius, Color col)
        {
            int x0 = Mathf.Max(0, (int)(cx - radius - 1));
            int x1 = Mathf.Min(size - 1, (int)(cx + radius + 1));
            int y0 = Mathf.Max(0, (int)(cy - radius - 1));
            int y1 = Mathf.Min(size - 1, (int)(cy + radius + 1));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                        BlendPixel(pixels, size, x, y, col);
                }
            }
        }

        private static void DrawFilledRect(Color[] pixels, int size, float left, float bottom, float w, float h, Color col)
        {
            int x0 = Mathf.Max(0, (int)left);
            int x1 = Mathf.Min(size - 1, (int)(left + w));
            int y0 = Mathf.Max(0, (int)bottom);
            int y1 = Mathf.Min(size - 1, (int)(bottom + h));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    BlendPixel(pixels, size, x, y, col);
        }

        private static void PaintDisk(Color[] pixels, int size, float cx, float cy, float radius, Color col)
        {
            int x0 = Mathf.Max(0, (int)(cx - radius));
            int x1 = Mathf.Min(size - 1, (int)(cx + radius + 1));
            int y0 = Mathf.Max(0, (int)(cy - radius));
            int y1 = Mathf.Min(size - 1, (int)(cy + radius + 1));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - d);
                        BlendPixel(pixels, size, x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                    }
                }
            }
        }

        private static void BlendPixel(Color[] pixels, int size, int x, int y, Color col)
        {
            int idx = y * size + x;
            Color bg = pixels[idx];
            float a = col.a;
            pixels[idx] = new Color(
                bg.r * (1 - a) + col.r * a,
                bg.g * (1 - a) + col.g * a,
                bg.b * (1 - a) + col.b * a,
                Mathf.Max(bg.a, a)
            );
        }
    }
}
