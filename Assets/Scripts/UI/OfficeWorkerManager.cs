using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class OfficeWorkerManager : MonoBehaviour
    {
        private RectTransform _panel;
        private readonly List<WorkerAvatar> _workers = new();

        // Posições atualmente reservadas por algum worker (evita dois no mesmo spot).
        internal static readonly HashSet<int> ClaimedTargets = new();

        // ── Coordenadas NORMALIZADAS do escritório ────────────────────────────
        // Os workers vivem no Panel_Main, mas a arte do escritório é renderizada
        // no Panel_BG em tela cheia com preserveAspect — ou seja, em outro
        // referencial que encolhe/centraliza conforme o aspecto da tela. Por isso
        // posições absolutas nunca batiam com o chão visível. Agora cada spot é
        // dado em [0..1] sobre a ARTE VISÍVEL (x: 0=esquerda,1=direita; y:
        // 0=base,1=topo) e convertido para o espaço do Panel_Main em runtime,
        // funcionando em qualquer aspecto.
        // Spots no chão aberto, evitando as mesas das paredes, o sofá (canto
        // sup. direito) e a barra de prestígio (base).
        public static readonly Vector2[] SpreadPositions =
        {
            new(0.34f, 0.60f), // fundo esquerda-centro
            new(0.42f, 0.66f), // fundo centro (à frente da porta/bebedouro)
            new(0.55f, 0.62f), // fundo direita-centro (antes do quadro)
            new(0.30f, 0.48f), // meio esquerda
            new(0.42f, 0.46f), // centro
            new(0.55f, 0.45f), // centro-direita
            new(0.36f, 0.37f), // frente esquerda-centro
            new(0.48f, 0.30f), // frente centro
            new(0.60f, 0.40f), // direita (entre as mesas da direita e o sofá)
            new(0.45f, 0.18f), // bem na frente (vão entre as duas ilhas de mesas)
        };

        // Nenhum spot é cadeira — os workers só roam pelo chão aberto.
        public static readonly bool[] IsSeat =
        {
            false, false, false, false, false,
            false, false, false, false, false,
        };

        // ── Mapeamento normalizado → local do Panel_Main ──────────────────────
        private static RectTransform _sPanel;   // espaço onde os workers vivem
        private static RectTransform _sFloorRt; // arte do escritório (tela cheia, preserveAspect)
        private static Camera _sCam;            // câmera do canvas (null se Overlay)
        private static float _sAspect = 16f / 9f;
        private static readonly Vector3[] _sCorners = new Vector3[4];

        // Converte um ponto [0..1] sobre a arte visível do escritório para a
        // anchoredPosition correspondente no espaço do Panel_Main.
        public static Vector2 OfficeToLocal(Vector2 n)
        {
            if (_sFloorRt == null || _sPanel == null)
                return new Vector2((n.x - 0.5f) * 600f, (n.y - 0.5f) * 340f); // fallback grosseiro

            _sFloorRt.GetWorldCorners(_sCorners); // 0=BL 1=TL 2=TR 3=BR
            Vector3 bl = _sCorners[0], tr = _sCorners[2];
            float rtW = tr.x - bl.x, rtH = tr.y - bl.y;
            if (rtW <= 0f || rtH <= 0f) return Vector2.zero;

            // Retângulo de conteúdo dentro do RT após preserveAspect (letterbox).
            float rtAspect = rtW / rtH;
            float cw, ch;
            if (rtAspect > _sAspect) { ch = rtH; cw = rtH * _sAspect; }
            else                     { cw = rtW; ch = rtW / _sAspect; }

            float cx = (bl.x + tr.x) * 0.5f, cy = (bl.y + tr.y) * 0.5f;
            var world = new Vector3(cx + (n.x - 0.5f) * cw, cy + (n.y - 0.5f) * ch, 0f);
            var screen = RectTransformUtility.WorldToScreenPoint(_sCam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_sPanel, screen, _sCam, out var local);
            return local;
        }

        // Retângulo do chão andável (em coords locais do Panel_Main), usado para
        // limitar o roam. Margens em normalizado evitam paredes/mesas/barra.
        public static Rect OfficeFloorBoundsLocal()
        {
            // y inferior 0.10 -> 0.22: mantém os workers acima da barra de
            // prestígio (que fica rente ao fundo), pra ninguém pisar nela.
            Vector2 lo = OfficeToLocal(new Vector2(0.15f, 0.22f));
            Vector2 hi = OfficeToLocal(new Vector2(0.90f, 0.80f));
            return Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.y, hi.y),
                                   Mathf.Max(lo.x, hi.x), Mathf.Max(lo.y, hi.y));
        }

        public void Init(RectTransform panelMain)
        {
            _panel = panelMain;
            _sPanel = panelMain;

            // Localiza a arte do escritório (movida para o Panel_BG em tela cheia,
            // agora aninhada dentro de PlayArea) para mapear coordenadas normalizadas.
            // Busca recursiva: o Find direto nao acha objetos aninhados.
            var bg = GameObject.Find("Panel_BG");
            RectTransform floor = null;
            if (bg != null)
            {
                foreach (var rtChild in bg.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rtChild.name == "BgFloor") { floor = rtChild; break; }
                }
            }
            if (floor != null)
            {
                _sFloorRt = floor;
                var img = floor.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    _sAspect = img.sprite.rect.width / img.sprite.rect.height;
            }
            var canvas = panelMain.GetComponentInParent<Canvas>();
            _sCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera : null;

            ClaimedTargets.Clear(); // limpa reservas de uma sessão anterior (editor replay)
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated += SyncWorkers;
            SyncWorkers();
        }

        private void OnDestroy()
        {
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharactersUpdated -= SyncWorkers;
        }

        public void SyncWorkers()
        {
            if (_panel == null) return;

            var chars = CharacterManager.Instance.GetAllCharacters();
            var active = new List<CharacterInstance>();
            foreach (var c in chars)
                if (c.level > 0) active.Add(c);
            if (active.Count > 10) active = active.GetRange(0, 10);

            while (_workers.Count > active.Count)
            {
                var w = _workers[_workers.Count - 1];
                _workers.RemoveAt(_workers.Count - 1);
                if (w != null) Destroy(w.gameObject);
            }

            for (int i = _workers.Count; i < active.Count; i++)
                _workers.Add(SpawnWorker(active[i], i));
        }

        // Loaded sheet: walk frames (always) + optional idle/working frames.
        public struct WorkerFrames
        {
            public Sprite[] walk;
            public Sprite[] idle;     // null when the sheet only has one row
            public bool     fullBody; // true = render the full-body sprite (vs. circle portrait)
        }

        // Wide character sheets (~4:1) are 12-col x 2-row walk/work grids, but
        // their column counts/poses vary, so we take only the clean top-left
        // frame as a static full-body sprite. A near-square image is treated as
        // a single portrait.
        private static WorkerFrames? LoadFrames(CharacterInstance ci)
        {
            var tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterId}");
            if (tex == null) tex = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterName}");
            if (tex == null) return null;

            // Strip baked-in white/gray/checkerboard backgrounds via flood-fill.
            tex = SpriteBackgroundRemover.Process(tex);

            // Portrait (not a sheet)
            if (tex.width < tex.height * 2)
                return new WorkerFrames
                {
                    walk = new[] { Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                                 new Vector2(0.5f, 0.5f)) },
                    idle = null,
                    fullBody = false
                };

            // Wide (~4:1) walk/work sheets. Column counts and pose layouts vary
            // per sheet, so a fixed grid sliced 1.5 characters per cell. Instead
            // detect each character by the transparent gaps between them and blit
            // it onto its own uniform canvas (same technique as the monster
            // sheets) — this auto-handles any frame count with no doubles.
            // O AI engineer e a CEO têm poses (gráfico / aglomerado de figuras
            // coladas) que o flood-fill não limpa direito. Para eles usamos o
            // fatiamento direto da textura CRUA (descarte de branco pixel a
            // pixel), que separa os quadros corretamente e descarta os blobs
            // largos demais. Os demais personagens continuam no flood-fill.
            string cid = (ci.data.characterId ?? "").Trim().ToLower();
            string cnm = (ci.data.characterName ?? "").Trim().ToLower();
            bool useRaw = cid.Contains("ai_engineer") || cnm.Contains("ai engineer")
                || cnm.Contains("ai_engineer") || cid == "ceo" || cnm == "ceo";

            Sprite[] walk, idle;
            if (useRaw)
            {
                var raw = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterId}");
                if (raw == null) raw = Resources.Load<Texture2D>($"Characters/Sprites/{ci.data.characterName}");
                if (raw == null) raw = tex;
                walk = SliceRowByContentRaw(raw, rowFromTop: 0, rows: 2);
                idle = SliceRowByContentRaw(raw, rowFromTop: 1, rows: 2);
            }
            else
            {
                walk = SliceRowByContent(tex, rowFromTop: 0, rows: 2);
                idle = SliceRowByContent(tex, rowFromTop: 1, rows: 2);
            }

            if (walk == null || walk.Length == 0)
            {
                // Fallback: static top-left frame so the worker is never invisible.
                int fw = tex.width / 12, fh = tex.height / 2;
                walk = new[] { Sprite.Create(tex, new Rect(0, tex.height - fh, fw, fh),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect) };
                idle = null;
            }

            return new WorkerFrames { walk = walk, idle = idle, fullBody = true };
        }

        // Slices one row of a sheet into per-character frames by detecting the
        // transparent gaps between bodies (the sheet must already have its
        // background removed). Each character is blitted onto its own uniform
        // canvas, bottom-aligned so feet stay on a constant baseline while the
        // walk pose changes. Robust to sheets with differing frame counts.
        private static Sprite[] SliceRowByContent(Texture2D tex, int rowFromTop, int rows)
        {
            int w = tex.width;
            int rowH = tex.height / rows;
            int bandY0 = tex.height - (rowFromTop + 1) * rowH; // texture y=0 is bottom
            int bandY1 = bandY0 + rowH;

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch { return null; }

            // Per-column opaque mass within the row band.
            var colMass = new int[w];
            for (int x = 0; x < w; x++)
            {
                int c = 0;
                for (int y = bandY0; y < bandY1; y++)
                    if (px[y * w + x].a > 40) c++;
                colMass[x] = c;
            }

            // Content runs separated by empty gaps = individual characters.
            int threshold = Mathf.Max(2, rowH / 14);
            var runs = new List<Vector2Int>();
            int start = -1;
            for (int x = 0; x < w; x++)
            {
                bool filled = colMass[x] > threshold;
                if (filled && start < 0) start = x;
                else if (!filled && start >= 0) { runs.Add(new Vector2Int(start, x)); start = -1; }
            }
            if (start >= 0) runs.Add(new Vector2Int(start, w));
            runs.RemoveAll(r => (r.y - r.x) < w / 40); // drop specks

            if (runs.Count == 0) return null;

            // Some sheets have two poses touching with no gap between them (e.g.
            // the investor's front-facing frames), which merge into one wide blob
            // and render as a doubled character. Split any run much wider than the
            // median into equal sub-frames.
            var ws = new List<int>();
            foreach (var r in runs) ws.Add(r.y - r.x);
            ws.Sort();
            int median = ws[ws.Count / 2];
            if (median > 0)
            {
                var split = new List<Vector2Int>();
                foreach (var r in runs)
                {
                    int wdt = r.y - r.x;
                    int parts = Mathf.Max(1, Mathf.RoundToInt((float)wdt / median));
                    if (parts <= 1) { split.Add(r); continue; }
                    for (int k = 0; k < parts; k++)
                        split.Add(new Vector2Int(r.x + wdt * k / parts, r.x + wdt * (k + 1) / parts));
                }
                runs = split;
            }

            // Bbox per character, scanned between the gap-midpoints so limbs are
            // fully captured while a neighbour can never leak in.
            var boxes = new RectInt[runs.Count];
            int maxW = 0, maxH = 0;
            for (int i = 0; i < runs.Count; i++)
            {
                int leftLimit  = (i == 0) ? 0 : (runs[i - 1].y + runs[i].x) / 2;
                int rightLimit = (i == runs.Count - 1) ? w : (runs[i].y + runs[i + 1].x) / 2;

                int minX = rightLimit, maxX = leftLimit, minY = bandY1, maxY = bandY0;
                for (int y = bandY0; y < bandY1; y++)
                for (int x = leftLimit; x < rightLimit; x++)
                    if (px[y * w + x].a > 40)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
                boxes[i] = new RectInt(minX, minY, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY));
                maxW = Mathf.Max(maxW, boxes[i].width);
                maxH = Mathf.Max(maxH, boxes[i].height);
            }

            // Drop "object-only" frames: in some sheets the flood-fill ate a
            // light body in a "presenting" pose, leaving just the small held
            // prop (the investor's floating flyer). A real full-body walk frame
            // is tall; a leftover prop is short — so discard any frame far
            // shorter than the tallest one. Isolated, height-only: never touches
            // sheets whose frames are all full-body.
            var keptBoxes = new List<RectInt>();
            int minKeepH = Mathf.RoundToInt(maxH * 0.6f);
            foreach (var b in boxes)
                if (b.height >= minKeepH) keptBoxes.Add(b);
            if (keptBoxes.Count == 0) keptBoxes.AddRange(boxes); // safety
            boxes = keptBoxes.ToArray();

            int canvasW = maxW + 12, canvasH = maxH + 12;
            const int footMargin = 6;
            var frames = new Sprite[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                var canvas = new Color32[canvasW * canvasH]; // alpha 0 by default
                var box = boxes[i];
                int offX = (canvasW - box.width) / 2;   // centre horizontally
                int offY = footMargin;                  // bottom-align feet
                for (int y = 0; y < box.height; y++)
                for (int x = 0; x < box.width; x++)
                {
                    var c = px[(box.y + y) * w + (box.x + x)];
                    if (c.a <= 40) continue;
                    canvas[(offY + y) * canvasW + (offX + x)] = c;
                }
                var ftex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                ftex.SetPixels32(canvas);
                ftex.Apply();
                frames[i] = Sprite.Create(ftex, new Rect(0, 0, canvasW, canvasH),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            return frames;
        }

        // Verdadeiro para pixels de fundo branco/cinza-claro/xadrez (claros E
        // sem saturação). Pixels de corpo (coloridos ou cinzas escuros) passam e
        // são mantidos.
        private static bool IsBg(Color32 c)
        {
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float mx = Mathf.Max(r, Mathf.Max(g, b));
            float mn = Mathf.Min(r, Mathf.Min(g, b));
            float sat = mx > 0.001f ? (mx - mn) / mx : 0f;
            return mx > 0.62f && sat < 0.20f;
        }

        // Fatiamento direto da textura CRUA com descarte de branco pixel a pixel
        // (sem flood-fill). Usado só no AI engineer: o flood-fill "comia" o corpo
        // claro e deixava o gráfico flutuando; o descarte por pixel só apaga o
        // branco de verdade, então os quadros saem inteiros. Blobs bem mais
        // largos que a mediana (aglomerados de poses frontais coladas no fim da
        // linha) são descartados, pois não há como separá-los de forma confiável.
        private static Sprite[] SliceRowByContentRaw(Texture2D tex, int rowFromTop, int rows)
        {
            int w = tex.width;
            int rowH = tex.height / rows;
            int bandY0 = tex.height - (rowFromTop + 1) * rowH; // y=0 é a base
            int bandY1 = bandY0 + rowH;

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch { return null; }

            // Contagem por coluna de pixels que NÃO são fundo (corpo).
            var colMass = new int[w];
            for (int x = 0; x < w; x++)
            {
                int c = 0;
                for (int y = bandY0; y < bandY1; y++)
                    if (!IsBg(px[y * w + x])) c++;
                colMass[x] = c;
            }

            int threshold = Mathf.Max(2, rowH / 50);
            var runs = new List<Vector2Int>();
            int start = -1;
            for (int x = 0; x < w; x++)
            {
                bool filled = colMass[x] > threshold;
                if (filled && start < 0) start = x;
                else if (!filled && start >= 0) { runs.Add(new Vector2Int(start, x)); start = -1; }
            }
            if (start >= 0) runs.Add(new Vector2Int(start, w));
            runs.RemoveAll(r => (r.y - r.x) < w / 40);
            if (runs.Count == 0) return null;

            // Descarta aglomerados (largura >> mediana de um personagem só).
            var ws = new List<int>();
            foreach (var r in runs) ws.Add(r.y - r.x);
            ws.Sort();
            int median = ws[ws.Count / 2];
            var single = new List<Vector2Int>();
            foreach (var r in runs)
                if ((r.y - r.x) <= median * 3 / 2) single.Add(r);
            if (single.Count == 0) single.AddRange(runs);
            runs = single;

            var boxes = new RectInt[runs.Count];
            int maxW = 0, maxH = 0;
            const int scanMargin = 6; // captura membros estendidos, sem alcançar vizinhos
            for (int i = 0; i < runs.Count; i++)
            {
                // IMPORTANTE: a varredura fica restrita à extensão do próprio run
                // + uma margem pequena. Usar o "meio do vão" do vizinho fazia o
                // bbox atravessar a região de um blob descartado (o aglomerado da
                // CEO) e capturar seus pixels, inflando a largura e encolhendo o
                // boneco. A margem é clampada para nunca invadir o run vizinho.
                int leftLimit  = Mathf.Max(0, runs[i].x - scanMargin);
                if (i > 0) leftLimit = Mathf.Max(leftLimit, runs[i - 1].y);
                int rightLimit = Mathf.Min(w, runs[i].y + scanMargin);
                if (i < runs.Count - 1) rightLimit = Mathf.Min(rightLimit, runs[i + 1].x);

                int minX = rightLimit, maxX = leftLimit, minY = bandY1, maxY = bandY0;
                for (int y = bandY0; y < bandY1; y++)
                for (int x = leftLimit; x < rightLimit; x++)
                    if (!IsBg(px[y * w + x]))
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
                boxes[i] = new RectInt(minX, minY, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY));
                maxW = Mathf.Max(maxW, boxes[i].width);
                maxH = Mathf.Max(maxH, boxes[i].height);
            }

            int canvasW = maxW + 12, canvasH = maxH + 12;
            const int footMargin = 6;
            var frames = new Sprite[runs.Count];
            for (int i = 0; i < runs.Count; i++)
            {
                var canvas = new Color32[canvasW * canvasH];
                var box = boxes[i];
                int offX = (canvasW - box.width) / 2;
                int offY = footMargin;
                for (int y = 0; y < box.height; y++)
                for (int x = 0; x < box.width; x++)
                {
                    var c = px[(box.y + y) * w + (box.x + x)];
                    if (IsBg(c)) continue; // descarte de branco pixel a pixel
                    canvas[(offY + y) * canvasW + (offX + x)] = c;
                }
                var ftex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                ftex.SetPixels32(canvas);
                ftex.Apply();
                frames[i] = Sprite.Create(ftex, new Rect(0, 0, canvasW, canvasH),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            return frames;
        }

        private WorkerAvatar SpawnWorker(CharacterInstance ci, int index)
        {
            var loaded = LoadFrames(ci);
            Sprite[] frames     = loaded?.walk;
            Sprite[] idleFrames = loaded?.idle;

            var go = new GameObject("Worker_" + ci.data.characterName,
                typeof(RectTransform), typeof(WorkerAvatar));
            go.transform.SetParent(_panel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150f, 172f);

            // Start at exact spread position — no jitter to prevent overlap
            rt.anchoredPosition = OfficeToLocal(SpreadPositions[index % SpreadPositions.Length]);

            bool hasSpriteSheet = loaded.HasValue && loaded.Value.fullBody;

            if (hasSpriteSheet)
            {
                // ── Sprite-sheet character ────────────────────────────────────
                // Sombra suave no chão
                var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowGO.transform.SetParent(go.transform, false);
                var shRt = shadowGO.GetComponent<RectTransform>();
                shRt.anchorMin = shRt.anchorMax = shRt.pivot = new Vector2(0.5f, 0.5f);
                shRt.anchoredPosition = new Vector2(0f, -67f);
                shRt.sizeDelta = new Vector2(80f, 18f);
                var shImg = shadowGO.GetComponent<Image>();
                shImg.sprite = UiSpriteFactory.Circle();
                shImg.color = new Color(0f, 0f, 0f, 0.20f);
                shImg.raycastTarget = false;

                // Corpo direto — o shader descarta o fundo branco, sem círculo nem máscara
                var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
                bodyGO.transform.SetParent(go.transform, false);
                var bodyRt2 = bodyGO.GetComponent<RectTransform>();
                bodyRt2.anchorMin = bodyRt2.anchorMax = bodyRt2.pivot = new Vector2(0.5f, 0.5f);
                bodyRt2.anchoredPosition = Vector2.zero;
                bodyRt2.sizeDelta = new Vector2(150f, 150f);

                var bodyImg = bodyGO.GetComponent<Image>();
                bodyImg.sprite = frames[0];
                bodyImg.raycastTarget = false;
                bodyImg.preserveAspect = true;

                var avatar = go.GetComponent<WorkerAvatar>();
                avatar.InitSheet(rt, bodyRt2, bodyImg, frames, idleFrames, null, OfficeFloorBoundsLocal());
            }
            else
            {
                // ── Procedural avatar: sprite flutuante com pernas, sem círculo ──
                var shadowGO = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowGO.transform.SetParent(go.transform, false);
                shadowGO.transform.SetAsFirstSibling();
                var srt = shadowGO.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(0f, -43f);
                srt.sizeDelta = new Vector2(42f, 10f);
                var sImg = shadowGO.GetComponent<Image>();
                sImg.sprite = UiSpriteFactory.Circle();
                sImg.color = new Color(0f, 0f, 0f, 0.22f);
                sImg.raycastTarget = false;

                Color legColor = new Color(
                    Mathf.Clamp01(ci.data.tintColor.r * 0.6f),
                    Mathf.Clamp01(ci.data.tintColor.g * 0.6f),
                    Mathf.Clamp01(ci.data.tintColor.b * 0.6f), 1f);
                var legL = MakeLeg(go.transform, "LegL", legColor, new Vector2(-10f, -14f));
                var legR = MakeLeg(go.transform, "LegR", legColor, new Vector2( 10f, -14f));

                Color footColor = new Color(0.18f, 0.12f, 0.08f, 1f);
                var footL = MakeFoot(go.transform, "FootL", footColor, new Vector2(-10f, -36f));
                var footR = MakeFoot(go.transform, "FootR", footColor, new Vector2( 10f, -36f));

                // Body: tinted circle only if no portrait available
                var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
                bodyGO.transform.SetParent(go.transform, false);
                var bodyRt = bodyGO.GetComponent<RectTransform>();
                bodyRt.anchorMin = bodyRt.anchorMax = bodyRt.pivot = new Vector2(0.5f, 0.5f);
                bodyRt.anchoredPosition = new Vector2(0f, 28f);
                bodyRt.sizeDelta = new Vector2(88f, 88f);

                bool hasPortrait = frames != null && frames.Length == 1;
                if (hasPortrait)
                {
                    // Show portrait clipped to circle — no white background ring
                    var bodyImg2 = bodyGO.GetComponent<Image>();
                    bodyImg2.color = new Color(0f, 0f, 0f, 0f); // transparent body

                    var maskGO2 = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
                    maskGO2.transform.SetParent(bodyGO.transform, false);
                    var mrt2 = maskGO2.GetComponent<RectTransform>();
                    mrt2.anchorMin = Vector2.zero; mrt2.anchorMax = Vector2.one;
                    mrt2.offsetMin = mrt2.offsetMax = Vector2.zero;
                    var mImg2 = maskGO2.GetComponent<Image>();
                    mImg2.sprite = UiSpriteFactory.Circle();
                    mImg2.raycastTarget = false;
                    maskGO2.GetComponent<Mask>().showMaskGraphic = false;

                    var portGO2 = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                    portGO2.transform.SetParent(maskGO2.transform, false);
                    var prt2 = portGO2.GetComponent<RectTransform>();
                    prt2.anchorMin = Vector2.zero; prt2.anchorMax = Vector2.one;
                    prt2.offsetMin = prt2.offsetMax = Vector2.zero;
                    var portImg2 = portGO2.GetComponent<Image>();
                    portImg2.sprite = frames[0];
                    portImg2.raycastTarget = false;
                    portImg2.preserveAspect = true;
                }
                else
                {
                    // No image — use tinted circle (small, subtle)
                    var bodyImg2 = bodyGO.GetComponent<Image>();
                    bodyImg2.sprite = UiSpriteFactory.Circle();
                    bodyImg2.color = new Color(
                        ci.data.tintColor.r, ci.data.tintColor.g, ci.data.tintColor.b, 0.80f);
                    bodyImg2.raycastTarget = false;
                }

                var avatar = go.GetComponent<WorkerAvatar>();
                avatar.InitProcedural(rt, bodyRt, legL, legR, footL, footR, srt, OfficeFloorBoundsLocal());
            }

            var wa = go.GetComponent<WorkerAvatar>();
            wa.ClaimSpawn(index % SpreadPositions.Length);
            wa.SetFacingDefault(FacesLeftByDefault(ci));
            return wa;
        }

        // Alguns sprites foram desenhados virados para a esquerda; para esses o
        // espelhamento precisa ser invertido (senão andam "de costas").
        private static bool FacesLeftByDefault(CharacterInstance ci)
        {
            string nm = (ci.data.characterName ?? "").Trim().ToUpper();
            string id = (ci.data.characterId ?? "").Trim().ToUpper();
            return nm == "CEO" || id == "CEO";
        }

        private static RectTransform MakeLeg(Transform parent, string name, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(9f, 22f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static RectTransform MakeFoot(Transform parent, string name, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(13f, 7f);
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox();
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }
    }

    public class WorkerAvatar : MonoBehaviour
    {
        // Shared
        private RectTransform _rt;
        private RectTransform _shadow;
        private Rect _bounds;
        private Vector2 _target;
        private float _walkSpeed;
        private bool _walking;
        private float _idleTimer;
        private float _idleDuration;

        // Sprite-sheet mode
        private bool _sheetMode;
        private Image _bodyImg;
        private Sprite[] _frames;      // walk cycle
        private Sprite[] _idleFrames;  // idle/working cycle (optional 2nd sheet row)
        private RectTransform _bodyRt;
        private float _frameTimer;
        private int _frameIndex;
        private const float WalkFps = 10f;
        private const float IdleFps = 5f;

        // Procedural mode
        private RectTransform _legL, _legR, _footL, _footR;
        private RectTransform _bodyProcRt;
        private float _stepPhase;

        // Sentar / trabalhar
        private bool _seatedTarget; // o destino atual é uma cadeira?
        private bool _seated;       // está sentado agora?
        private float _workPhase;

        // Orientação padrão do sprite (alguns foram desenhados virados p/ esquerda).
        private bool _faceLeftDefault;
        public void SetFacingDefault(bool faceLeft) => _faceLeftDefault = faceLeft;
        // Sinal do scale.x para o personagem encarar a direção desejada.
        private float FaceX(bool right) => (right ^ _faceLeftDefault) ? 1f : -1f;

        // ── Init ─────────────────────────────────────────────────────────────

        public void InitSheet(RectTransform rt, RectTransform bodyRt, Image bodyImg,
            Sprite[] frames, Sprite[] idleFrames, RectTransform shadow, Rect bounds)
        {
            _rt = rt; _bodyRt = bodyRt; _bodyImg = bodyImg;
            _frames = frames; _idleFrames = idleFrames;
            _shadow = shadow; _bounds = bounds;
            _sheetMode = true;
            StartCoroutine(Lifecycle());
        }

        public void InitProcedural(RectTransform rt, RectTransform bodyRt,
            RectTransform legL, RectTransform legR,
            RectTransform footL, RectTransform footR,
            RectTransform shadow, Rect bounds)
        {
            _rt = rt; _bodyProcRt = bodyRt;
            _legL = legL; _legR = legR;
            _footL = footL; _footR = footR;
            _shadow = shadow; _bounds = bounds;
            _sheetMode = false;
            StartCoroutine(Lifecycle());
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private IEnumerator Lifecycle()
        {
            yield return new WaitForSeconds(Random.Range(0f, 3f));
            PickNewTarget();

            while (true)
            {
                if (_walking)
                {
                    yield return StartCoroutine(WalkToTarget());
                    _walking = false;
                    _seated = _seatedTarget;
                    // Na mesa: fica mais tempo "trabalhando"; em pé livre: pausa curta.
                    _idleDuration = _seated ? Random.Range(7f, 14f) : Random.Range(2f, 6f);
                    _idleTimer = 0f;
                    SetIdlePose(); // sempre em pé (sprites só têm pose em pé)
                }
                else
                {
                    _idleTimer += Time.deltaTime;
                    if (_seated) TickWorking();
                    else if (_sheetMode) TickSheetIdle(Time.deltaTime);
                    else TickIdleBreath();
                    if (_idleTimer >= _idleDuration) PickNewTarget();
                }
                yield return null;
            }
        }

        private int _lastTargetIndex = -1;
        private int _claimedIndex = -1;

        // Reserva o spot inicial para que nenhum outro worker o tome.
        public void ClaimSpawn(int index)
        {
            _claimedIndex = index;
            _lastTargetIndex = index;
            OfficeWorkerManager.ClaimedTargets.Add(index);
        }

        private void OnDestroy()
        {
            if (_claimedIndex >= 0)
                OfficeWorkerManager.ClaimedTargets.Remove(_claimedIndex);
        }

        private void PickNewTarget()
        {
            var positions = OfficeWorkerManager.SpreadPositions;
            var claimed = OfficeWorkerManager.ClaimedTargets;

            // Libera o spot atual antes de procurar um novo livre.
            if (_claimedIndex >= 0) claimed.Remove(_claimedIndex);

            // Escolhe um spot que não esteja reservado por outro worker (e != atual).
            int idx = -1;
            int tries = 0;
            do
            {
                int cand = Random.Range(0, positions.Length);
                if (cand != _lastTargetIndex && !claimed.Contains(cand)) { idx = cand; break; }
            }
            while (++tries < 40);

            // Fallback: se tudo reservado, pega qualquer um diferente do atual.
            if (idx < 0)
            {
                do { idx = Random.Range(0, positions.Length); }
                while (idx == _lastTargetIndex && positions.Length > 1);
            }

            _claimedIndex = idx;
            _lastTargetIndex = idx;
            claimed.Add(idx);
            _seatedTarget = OfficeWorkerManager.IsSeat[idx];

            // Converte o spot normalizado para o espaço local e adiciona leve
            // variação (em px locais) para os personagens não ficarem rígidos.
            var jitter = _seatedTarget ? Vector2.zero
                                       : new Vector2(Random.Range(-10f, 10f), Random.Range(-8f, 8f));
            _target  = OfficeWorkerManager.OfficeToLocal(positions[idx]) + jitter;
            _bounds  = OfficeWorkerManager.OfficeFloorBoundsLocal(); // acompanha o aspecto atual
            _walkSpeed = Random.Range(55f, 85f);
            _walking = true;
        }

        // ── Walk ─────────────────────────────────────────────────────────────

        private IEnumerator WalkToTarget()
        {
            Vector2 start = _rt.anchoredPosition;
            float dist = Vector2.Distance(start, _target);
            if (dist < 4f) yield break;

            float duration = dist / _walkSpeed;
            float elapsed = 0f;
            bool facingRight = _target.x >= start.x;
            _rt.localScale = new Vector3(FaceX(facingRight), 1f, 1f);

            _stepPhase = 0f;
            float stepFreq = 2.5f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                _stepPhase += dt * stepFreq * Mathf.PI * 2f;

                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = t * t * (3f - 2f * t);
                _rt.anchoredPosition = Vector2.Lerp(start, _target, smooth);

                if (_sheetMode) TickSheetWalk(dt);
                else AnimateWalk(_stepPhase);

                yield return null;
            }

            _rt.anchoredPosition = _target;
            _rt.localScale = new Vector3(FaceX(facingRight), 1f, 1f);
        }

        // ── Sprite-sheet animation ────────────────────────────────────────────

        private void TickSheetWalk(float dt)
        {
            if (_frames == null || _frames.Length <= 1) return;
            _frameTimer += dt;
            if (_frameTimer >= 1f / WalkFps)
            {
                _frameTimer = 0f;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                _bodyImg.sprite = _frames[_frameIndex];
            }

            // Slight body bob based on frame
            float bob = Mathf.Sin(_frameIndex * Mathf.PI / 4f) * 2f;
            _bodyRt.anchoredPosition = new Vector2(0f, bob);
        }

        // Idle/working animation from the sheet's second row (slower than walk).
        private void TickSheetIdle(float dt)
        {
            if (_idleFrames == null || _idleFrames.Length <= 1) return;
            _frameTimer += dt;
            if (_frameTimer >= 1f / IdleFps)
            {
                _frameTimer = 0f;
                _frameIndex = (_frameIndex + 1) % _idleFrames.Length;
                _bodyImg.sprite = _idleFrames[_frameIndex];
            }
        }

        // ── Procedural animation ──────────────────────────────────────────────

        private void AnimateWalk(float phase)
        {
            float legSwingAmp = 12f;
            float legLiftAmp  = 7f;
            float bodyBobAmp  = 2.5f;
            float bodyTiltAmp = 4f;

            float sinL = Mathf.Sin(phase);
            float sinR = Mathf.Sin(phase + Mathf.PI);

            _legL.localRotation = Quaternion.Euler(0f, 0f, sinL * legSwingAmp);
            _legR.localRotation = Quaternion.Euler(0f, 0f, sinR * legSwingAmp);

            float liftL = Mathf.Max(0f, sinL) * legLiftAmp;
            float liftR = Mathf.Max(0f, sinR) * legLiftAmp;
            _footL.anchoredPosition = new Vector2(-10f, -36f + liftL);
            _footR.anchoredPosition = new Vector2( 10f, -36f + liftR);

            float scaleFL = 1f + Mathf.Max(0f, -sinL) * 0.25f;
            float scaleFR = 1f + Mathf.Max(0f, -sinR) * 0.25f;
            _footL.localScale = new Vector3(scaleFL, 1f / scaleFL, 1f);
            _footR.localScale = new Vector3(scaleFR, 1f / scaleFR, 1f);

            float bob = Mathf.Abs(Mathf.Sin(phase)) * bodyBobAmp;
            _bodyProcRt.anchoredPosition = new Vector2(0f, bob);
            _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * bodyTiltAmp);

            float shadowScale = 1f - bob * 0.03f;
            _shadow.localScale = new Vector3(shadowScale, 0.7f + bob * 0.02f, 1f);
        }

        private float _idleBreathPhase;

        private void TickIdleBreath()
        {
            _idleBreathPhase += Time.deltaTime * 1.2f;
            float breath = Mathf.Sin(_idleBreathPhase) * 1.2f;
            if (_bodyProcRt)
            {
                _bodyProcRt.anchoredPosition = new Vector2(0f, breath);
                _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_idleBreathPhase * 0.5f) * 0.8f);
            }
        }

        // Micro-movimento de "digitando" em pé na mesa (leve balanço do corpo).
        private void TickWorking()
        {
            _workPhase += Time.deltaTime * 3.2f;
            float b = Mathf.Sin(_workPhase) * 1.3f;
            if (_sheetMode)
            {
                TickSheetIdle(Time.deltaTime); // typing/working frames at the desk
                if (_bodyRt) _bodyRt.anchoredPosition = new Vector2(0f, b);
            }
            else if (_bodyProcRt)
            {
                _bodyProcRt.anchoredPosition = new Vector2(0f, Mathf.Abs(b) * 0.6f);
                _bodyProcRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_workPhase * 0.6f) * 1.4f);
            }
        }

        private void SetIdlePose()
        {
            if (_sheetMode)
            {
                // Prefer the idle row's first frame when available
                var poseFrames = _idleFrames ?? _frames;
                if (poseFrames != null && poseFrames.Length > 0)
                    _bodyImg.sprite = poseFrames[0];
                _frameIndex = 0;
                _frameTimer = 0f;
                if (_bodyRt) _bodyRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                if (_legL) _legL.localRotation = Quaternion.identity;
                if (_legR) _legR.localRotation = Quaternion.identity;
                if (_footL) { _footL.anchoredPosition = new Vector2(-10f, -36f); _footL.localScale = Vector3.one; }
                if (_footR) { _footR.anchoredPosition = new Vector2( 10f, -36f); _footR.localScale = Vector3.one; }
                if (_bodyProcRt) { _bodyProcRt.anchoredPosition = Vector2.zero; _bodyProcRt.localRotation = Quaternion.identity; }
                if (_shadow) _shadow.localScale = Vector3.one;
            }
        }
    }
}
