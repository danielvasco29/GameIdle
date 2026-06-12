using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    /// <summary>
    /// Coloca mesas, cadeiras, monitores e props animados sobre o chão do escritório.
    /// As mesas são estáticas + trabalhadores genéricos sentados animados por cima.
    /// </summary>
    public class OfficePropsLayer : MonoBehaviour
    {
        private RectTransform _panel;

        // ── Posições das mesas ────────────────────────────────────────────────
        // 3 Kit1 (Executivo, esquerda) + 3 Kit2 (Desenvolvedor, direita)
        private static readonly Vector2[] DeskPositions =
        {
            new(-390f,  170f),
            new(-390f,   10f),
            new(-390f, -150f),

            new( 230f,  170f),
            new( 230f,   10f),
            new( 230f, -150f),
        };

        // 0 = Kit1 Executivo, 1 = Kit2 Desenvolvedor
        private static readonly int[] DeskKit = { 0, 0, 0, 1, 1, 1 };

        // ── Desk kit sheet layout (desk_kit.png) ─────────────────────────────
        // A imagem tem:
        //   Linha título  : ~8% da altura (ignoramos)
        //   Previews       : ~32% da altura (Kit1 esquerda | Kit2 direita)
        //   4 strips de anim: ~60% restantes, cada strip = 8 frames em 1 linha
        //     strip 0: cadeiras Kit1
        //     strip 1: cadeiras Kit2
        //     strip 2: monitores Kit1
        //     strip 3: monitores Kit2
        private const float TitleFrac   = 0.08f;
        private const float PreviewFrac = 0.32f;
        private const int   AnimStrips  = 4;
        private const int   AnimCols    = 8;

        // ── Props do ambiente ─────────────────────────────────────────────────
        // (path, pos, size, rows, cols)
        private static readonly (string path, Vector2 pos, Vector2 size, int rows, int cols)[] AmbientProps =
        {
            ("Props/water_cooler",   new Vector2(-310f,  230f), new Vector2( 90f, 140f), 2, 6),
            ("Props/coffee_machine", new Vector2( 370f,  200f), new Vector2(110f, 110f), 2, 4),
            ("Props/printer",        new Vector2( 430f, -130f), new Vector2(110f, 110f), 2, 5),
            ("Props/neon_rocket",    new Vector2(  55f,  330f), new Vector2(130f,  85f), 1, 4),
        };

        public void Init(RectTransform panel)
        {
            _panel = panel;
            SpawnDesks();
            SpawnAmbientProps();
        }

        // ── Mesas ─────────────────────────────────────────────────────────────

        private void SpawnDesks()
        {
            var tex = Resources.Load<Texture2D>("Props/desk_kit");
            if (tex == null)
            {
                Debug.LogWarning("[OfficePropsLayer] Props/desk_kit.png not found");
                return;
            }

            float totalH    = tex.height;
            float previewY0 = totalH * (1f - TitleFrac - PreviewFrac); // y bottom of preview band
            float previewH  = totalH * PreviewFrac;
            float animBandH = totalH * (1f - TitleFrac - PreviewFrac); // bottom band
            float stripH    = animBandH / AnimStrips;
            float frameW    = (float)tex.width / AnimCols;
            float halfW     = tex.width * 0.5f;

            for (int i = 0; i < DeskPositions.Length; i++)
            {
                int kit = DeskKit[i];

                // ── Base da mesa (preview) ──────────────────────────────────
                float baseX = kit == 0 ? 0f : halfW;
                var baseRect = new Rect(baseX, previewY0, halfW, previewH);
                var baseSprite = Sprite.Create(tex, baseRect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);

                var deskGO = new GameObject($"Desk_{i}", typeof(RectTransform), typeof(Image));
                deskGO.transform.SetParent(_panel, false);
                var deskRt = deskGO.GetComponent<RectTransform>();
                deskRt.anchorMin = deskRt.anchorMax = new Vector2(0.5f, 0.5f);
                deskRt.pivot = new Vector2(0.5f, 0f);
                deskRt.sizeDelta = new Vector2(210f, 120f);
                deskRt.anchoredPosition = DeskPositions[i];
                var deskImg = deskGO.GetComponent<Image>();
                deskImg.sprite = baseSprite;
                deskImg.preserveAspect = true;
                deskImg.raycastTarget = false;
                deskImg.color = new Color(1f, 1f, 1f, 0f); // oculto até ter sprite limpo de mesa

                // ── Animação do monitor (strips 2 e 3) ─────────────────────
                int monStripIdx = kit == 0 ? 2 : 3;
                float monStripY = monStripIdx * stripH; // y from bottom of anim band = 0
                var monFrames = SliceStrip(tex, frameW, stripH, monStripY, AnimCols);
                if (monFrames != null)
                    PlaceAnimLayer(deskGO.transform, "Monitor", monFrames,
                        new Vector2(10f, 85f), new Vector2(75f, 45f), fps: 6f);

                // ── Trabalhador sentado genérico ────────────────────────────
                SpawnSeatedWorker(deskGO.transform, i);
            }
        }

        private static Sprite[] SliceStrip(Texture2D tex, float frameW, float stripH, float stripYFromBottom, int cols)
        {
            var frames = new Sprite[cols];
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rect(c * frameW, stripYFromBottom, frameW, stripH);
                if (rect.xMax > tex.width || rect.yMax > tex.height) return null;
                frames[c] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            return frames;
        }

        private void SpawnSeatedWorker(Transform deskParent, int deskIdx)
        {
            string path = (deskIdx % 2 == 0) ? "Characters/Sprites/seated_a"
                                              : "Characters/Sprites/seated_b";
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return;

            tex = SpriteBackgroundRemover.Process(tex);

            // 12 frames: 2 linhas × 6 colunas
            const int cols = 6, rows = 2;
            int frameW = tex.width  / cols;
            int frameH = tex.height / rows;
            var frames = new Sprite[rows * cols];
            for (int r = 0; r < rows; r++)
            {
                int y = tex.height - (r + 1) * frameH;
                for (int c = 0; c < cols; c++)
                    frames[r * cols + c] = Sprite.Create(tex,
                        new Rect(c * frameW, y, frameW, frameH),
                        new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            }

            PlaceAnimLayer(deskParent, "SeatedWorker", frames,
                new Vector2(0f, 5f), new Vector2(105f, 125f), fps: 3f);
        }

        // ── Props do ambiente ─────────────────────────────────────────────────

        private void SpawnAmbientProps()
        {
            foreach (var (path, pos, size, rows, cols) in AmbientProps)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex == null) { Debug.LogWarning($"[OfficePropsLayer] {path} not found"); continue; }

                tex = SpriteBackgroundRemover.Process(tex);

                int fw = tex.width  / cols;
                int fh = tex.height / rows;
                var frames = new Sprite[rows * cols];
                for (int r = 0; r < rows; r++)
                {
                    int y = tex.height - (r + 1) * fh;
                    for (int c = 0; c < cols; c++)
                        frames[r * cols + c] = Sprite.Create(tex,
                            new Rect(c * fw, y, fw, fh),
                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }

                PlaceAnimLayer(_panel, path, frames, pos, size, fps: 5f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PlaceAnimLayer(Transform parent, string name, Sprite[] frames,
            Vector2 pos, Vector2 size, float fps)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = frames[0];
            img.preserveAspect = true;
            img.raycastTarget = false;

            if (frames.Length > 1)
                StartCoroutine(AnimLoop(img, frames, fps));
        }

        private static IEnumerator AnimLoop(Image img, Sprite[] frames, float fps)
        {
            float interval = 1f / fps;
            int i = 0;
            while (true)
            {
                yield return new WaitForSeconds(interval + Random.Range(-0.05f, 0.05f));
                if (img == null) yield break;
                i = (i + 1) % frames.Length;
                img.sprite = frames[i];
            }
        }
    }
}
