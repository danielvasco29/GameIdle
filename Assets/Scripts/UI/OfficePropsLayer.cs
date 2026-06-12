using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    /// <summary>
    /// Coloca estações de trabalho (kits completos) e props animados sobre o
    /// chão limpo do escritório.
    /// </summary>
    public class OfficePropsLayer : MonoBehaviour
    {
        private RectTransform _panel;

        // ── Posições das mesas ────────────────────────────────────────────────
        // 3 Kit1 (Executivo, esquerda) + 3 Kit2 (Desenvolvedor, direita)
        private static readonly Vector2[] DeskPositions =
        {
            new(-420f,  150f),
            new(-420f,  -40f),
            new(-420f, -230f),

            new( 300f,  150f),
            new( 300f,  -40f),
            new( 300f, -230f),
        };

        // 0 = Kit1 Executivo, 1 = Kit2 Desenvolvedor
        private static readonly int[] DeskKit = { 0, 0, 0, 1, 1, 1 };

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
            // Kits completos (mesa + monitores + cadeira) como sprites estáticos limpos.
            var kitSprites = new Sprite[2];
            for (int k = 0; k < 2; k++)
            {
                var tex = Resources.Load<Texture2D>($"Props/desk_kit{k + 1}");
                if (tex == null) { Debug.LogWarning($"[OfficePropsLayer] Props/desk_kit{k + 1}.png not found"); continue; }
                tex = SpriteBackgroundRemover.Process(tex);
                kitSprites[k] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            }

            for (int i = 0; i < DeskPositions.Length; i++)
            {
                var sprite = kitSprites[DeskKit[i]];
                if (sprite == null) continue;

                var deskGO = new GameObject($"Desk_{i}", typeof(RectTransform), typeof(Image));
                deskGO.transform.SetParent(_panel, false);
                var deskRt = deskGO.GetComponent<RectTransform>();
                deskRt.anchorMin = deskRt.anchorMax = new Vector2(0.5f, 0.5f);
                deskRt.pivot = new Vector2(0.5f, 0f);
                deskRt.sizeDelta = new Vector2(330f, 185f);
                deskRt.anchoredPosition = DeskPositions[i];
                var deskImg = deskGO.GetComponent<Image>();
                deskImg.sprite = sprite;
                deskImg.preserveAspect = true;
                deskImg.raycastTarget = false;
            }
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
