using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Spawned inside Panel_Main by UIManager.
    // Displays the current wave monster, HP bar, and handles visual feedback.
    public class MonsterView : MonoBehaviour
    {
        private static readonly Color RedHp    = new(0.85f, 0.15f, 0.15f, 1f);
        private static readonly Color OrangeHp = new(1f,    0.55f, 0.10f, 1f);
        private static readonly Color GreenHp  = new(0.25f, 0.75f, 0.35f, 1f);
        private static readonly Color GoldColor = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color NavyBg   = new(0.055f, 0.094f, 0.165f, 0.88f);

        private RectTransform _rt;
        private Image         _spriteImg;
        private Image         _hpFill;
        private TextMeshProUGUI _hpText;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _waveText;
        private GameObject    _deathOverlay;

        // Float animation
        private float _floatPhase;
        private const float FloatAmp   = 8f;
        private const float FloatSpeed = 1.8f;

        // Sprite sheet idle animation
        private Sprite[] _idleFrames;
        private float    _animTimer;
        private int      _animFrame;
        private float    _animFps = 10f; // frames per second (per-monster)

        // Damage flash
        private Coroutine _flashCo;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            BuildUI();
        }

        private void Update()
        {
            if (_spriteImg == null) return;

            // Idle animation — simple frame swap
            if (_idleFrames != null && _idleFrames.Length > 1)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= 1f / _animFps)
                {
                    _animTimer = 0f;
                    _animFrame = (_animFrame + 1) % _idleFrames.Length;
                    if (_idleFrames[_animFrame] != null)
                        _spriteImg.sprite = _idleFrames[_animFrame];
                }
            }

            _floatPhase += Time.deltaTime * FloatSpeed;
            float y = Mathf.Sin(_floatPhase) * FloatAmp;
            _spriteImg.rectTransform.anchoredPosition = new Vector2(0f, 10f + y);
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            // RectTransform layout (anchors/size) is owned by whoever spawns this
            // view (BattlePanel) — don't override it here.
            var font = TMP_Settings.defaultFontAsset;

            // Wave badge (top-right)
            _waveText = MakeLabel("WaveBadge", "ONDA 1 / 10", 13f,
                new Color(0.7f, 0.8f, 1f, 0.9f),
                new Vector2(0.55f, 0.85f), new Vector2(1f, 1f),
                new Vector2(0f, -4f), new Vector2(-10f, -4f), font);
            _waveText.alignment = TextAlignmentOptions.TopRight;
            // BattlePanel shows its own wave label at the top of the arena;
            // keep this one hidden to avoid duplication.
            _waveText.gameObject.SetActive(false);

            // Monster name tag — anchored to the TOP of the container
            {
                var go = new GameObject("NameBg", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.86f); rt.anchorMax = new Vector2(0.95f, 0.98f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced;
                img.color = new Color(0.5f, 0.05f, 0.05f, 0.82f);
                img.raycastTarget = false;
            }
            _nameText = MakeLabel("MonsterName", "BUG GIGANTE", 18f, Color.white,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f),
                Vector2.zero, Vector2.zero, font);
            _nameText.fontStyle = FontStyles.Bold;

            // HP bar background — just below the name tag
            {
                var go = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.05f, 0.75f); rt.anchorMax = new Vector2(0.95f, 0.85f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced;
                img.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
                img.raycastTarget = false;

                // HP fill
                var fillGO = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(go.transform, false);
                var frt = fillGO.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1f, 1f);
                frt.offsetMin = new Vector2(3f, 3f); frt.offsetMax = new Vector2(-3f, -3f);
                _hpFill = fillGO.GetComponent<Image>();
                _hpFill.sprite = UiSpriteFactory.RoundedBox(); _hpFill.type = Image.Type.Sliced;
                _hpFill.color = RedHp; _hpFill.raycastTarget = false;

                // HP text overlay
                _hpText = MakeLabel("HpText", "1000 / 1000", 11f, Color.white,
                    Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f), font);
                _hpText.transform.SetParent(go.transform, false);
                _hpText.alignment = TextAlignmentOptions.Center;
                _hpText.fontStyle = FontStyles.Bold;
            }

            // Monster sprite image — wide enough for horizontal creatures like the worm.
            // Anchored to the bottom-center of the container, below the HP/name block.
            {
                var go = new GameObject("MonsterSprite", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(640f, 400f);
                rt.anchoredPosition = new Vector2(0f, 10f);
                _spriteImg = go.GetComponent<Image>();
                _spriteImg.preserveAspect = true;
                _spriteImg.raycastTarget = true;

                // Tap the monster to attack
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = _spriteImg;
                btn.onClick.AddListener(() => CombatManager.Instance?.PlayerAttack());
            }

            // Shadow under sprite
            {
                var go = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(280f, 28f);
                rt.anchoredPosition = new Vector2(0f, 30f);
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.Circle();
                img.color = new Color(0f, 0f, 0f, 0.3f);
                img.raycastTarget = false;
                go.transform.SetAsFirstSibling();
            }

            // Death overlay (hidden)
            _deathOverlay = new GameObject("DeathOverlay", typeof(RectTransform), typeof(Image));
            _deathOverlay.transform.SetParent(transform, false);
            var drt = _deathOverlay.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;
            _deathOverlay.GetComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0f);
            _deathOverlay.GetComponent<Image>().raycastTarget = false;
            _deathOverlay.SetActive(false);
        }

        // ── Public ────────────────────────────────────────────────────────────

        public void SetMonster(CombatManager.MonsterDef def, int wave)
        {
            bool isBoss = def.type == CombatManager.MonsterType.Boss;

            if (_nameText != null)
            {
                _nameText.text = def.displayName;
                // Boss gets gold name
                _nameText.color = isBoss ? GoldColor : Color.white;
                // Boss name tag background goes dark-gold
                var nameBg = transform.Find("NameBg")?.GetComponent<Image>();
                if (nameBg != null)
                    nameBg.color = isBoss
                        ? new Color(0.35f, 0.18f, 0.02f, 0.88f)
                        : new Color(0.5f, 0.05f, 0.05f, 0.82f);
            }
            UpdateWave(wave);

            // Load sprite or animated sprite sheet.
            // Sheet naming convention: path ending in "_sheet" → slice idle row (8 frames).
            // All monster textures are imported readable by CharacterSpriteImporter.
            _idleFrames = null;
            _animFrame  = 0;
            _animTimer  = 0f;
            var srcTex = Resources.Load<Texture2D>(def.spritePath);
            if (srcTex != null && _spriteImg != null)
            {
                bool isSheet = def.spritePath.EndsWith("_sheet");
                bool isWorm  = def.spritePath.Contains("worm");
                if (isSheet)
                {
                    // Sheets are 4 stacked rows of DIFFERENT animations (movement /
                    // attack / hurt / death) with baked-in text labels. We only use
                    // the TOP movement row so the monster never jumps between poses.
                    var tex = SpriteBackgroundRemover.ProcessSheet(srcTex);
                    if (isWorm)
                    {
                        // The worm's MOVEMENT row is one continuous body (can't be
                        // framed), but its ATTACK row holds 3 complete poses —
                        // Antecipação / Estocada / Recuperação. We animate those as
                        // a slow writhe so the worm stays whole AND moves.
                        _idleFrames = SliceAttackRow(tex);
                        _animFps = 4.5f;
                    }
                    else
                    {
                        // Golem/ghost pack 8 discrete poses; tight-crop each.
                        _idleFrames = SliceMovementRow(tex, 8);
                        _animFps = 10f;
                    }
                    _spriteImg.sprite = _idleFrames[0];
                    _spriteImg.preserveAspect = true; // keep correct proportions
                }
                else
                {
                    var tex = SpriteBackgroundRemover.Process(srcTex);
                    _spriteImg.sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                        100f, 0, SpriteMeshType.FullRect);
                    _spriteImg.preserveAspect = true;
                }
                _spriteImg.color = Color.white;
                // Worm is a wide creature → give it a wide slot; others stay tall.
                _spriteImg.rectTransform.sizeDelta =
                    isBoss ? new Vector2(680f, 460f)
                    : isWorm ? new Vector2(702f, 288f)
                    : new Vector2(440f, 440f);
            }

            // Reset death overlay
            if (_deathOverlay != null) _deathOverlay.SetActive(false);
            if (_spriteImg != null)
            {
                _spriteImg.color = Color.white;
                _spriteImg.transform.localScale = Vector3.one;
            }

            _floatPhase = 0f;

            // Boss entrance flash
            if (isBoss && isActiveAndEnabled) StartCoroutine(BossEntrance());
        }

        private IEnumerator BossEntrance()
        {
            // Screen flash + scale punch
            if (_spriteImg != null)
            {
                _spriteImg.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                _spriteImg.color = GoldColor;
            }
            yield return new WaitForSeconds(0.12f);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = t / 0.3f;
                if (_spriteImg != null)
                {
                    float s = Mathf.Lerp(1.4f, 1f, p);
                    _spriteImg.transform.localScale = new Vector3(s, s, 1f);
                    _spriteImg.color = Color.Lerp(GoldColor, Color.white, p);
                }
                yield return null;
            }
            if (_spriteImg != null)
            {
                _spriteImg.transform.localScale = Vector3.one;
                _spriteImg.color = Color.white;
            }

            // "BOSS!" warning text
            var go = new GameObject("BossWarn", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 70f);
            rt.anchoredPosition = new Vector2(0f, 220f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "!! BOSS !!";
            tmp.fontSize = 36f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = GoldColor; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
            StartCoroutine(FloatUpAndFade(go, tmp, 1.8f));
        }

        public void UpdateHp(double current, double max)
        {
            float ratio = max > 0 ? (float)(current / max) : 0f;
            if (_hpFill != null)
            {
                var frt = _hpFill.rectTransform;
                frt.anchorMax = new Vector2(ratio, 1f);
                _hpFill.color = ratio > 0.5f ? GreenHp : (ratio > 0.25f ? OrangeHp : RedHp);
            }
            if (_hpText != null)
                _hpText.text = $"{NumberFormatter.Format(current)} / {NumberFormatter.Format(max)}";
        }

        public void UpdateWave(int wave)
        {
            if (_waveText == null) return;
            bool isBoss = wave == 10;
            _waveText.text  = isBoss ? "!! ONDA 10 - BOSS !!" : $"ONDA {wave} / 10";
            _waveText.color = isBoss ? GoldColor : new Color(0.7f, 0.8f, 1f, 0.9f);
        }

        public void PlayHitEffect(double dmg)
        {
            if (!isActiveAndEnabled) return; // panel closed — skip visuals
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRed());
            SpawnDamageNumber(dmg);
            StartCoroutine(ShakeSprite());
        }

        public void PlayDeathEffect()
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(DeathAnim());
        }

        public void ShowBetweenWaves(double reward)
        {
            if (!isActiveAndEnabled) return;
            SpawnRewardFloat(reward);
        }

        // ── Animations ────────────────────────────────────────────────────────

        private IEnumerator FlashRed()
        {
            if (_spriteImg == null) yield break;
            _spriteImg.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return new WaitForSeconds(0.12f);
            _spriteImg.color = Color.white;
        }

        private IEnumerator ShakeSprite()
        {
            if (_spriteImg == null) yield break;
            var origPos = _spriteImg.rectTransform.anchoredPosition;
            for (int i = 0; i < 4; i++)
            {
                float ox = (i % 2 == 0 ? 1 : -1) * 6f;
                _spriteImg.rectTransform.anchoredPosition = origPos + new Vector2(ox, 0);
                yield return new WaitForSeconds(0.04f);
            }
            _spriteImg.rectTransform.anchoredPosition = origPos;
        }

        private IEnumerator DeathAnim()
        {
            if (_spriteImg == null) yield break;
            float t = 0f;
            Vector3 startScale = Vector3.one;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float p = t / 0.4f;
                _spriteImg.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                _spriteImg.color = new Color(1f, 0.3f, 0.3f, 1f - p);
                yield return null;
            }
            _spriteImg.color = new Color(1f, 1f, 1f, 0f);
        }

        private void SpawnDamageNumber(double dmg)
        {
            var go = new GameObject("DmgNum", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(180f, 50f);
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-60f, 60f),
                UnityEngine.Random.Range(80f, 160f));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = $"-{NumberFormatter.Format(dmg)}";
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = GoldColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
            StartCoroutine(FloatUpAndFade(go, tmp));
        }

        private void SpawnRewardFloat(double reward)
        {
            var go = new GameObject("RewardFloat", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 60f);
            rt.anchoredPosition = new Vector2(0f, 160f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = $"+${NumberFormatter.Format(reward)}";
            tmp.fontSize = 30f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.25f, 0.9f, 0.35f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
            StartCoroutine(FloatUpAndFade(go, tmp, 1.4f));
        }

        private IEnumerator FloatUpAndFade(GameObject go, TextMeshProUGUI tmp, float duration = 0.75f)
        {
            var rt = go.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                rt.anchoredPosition = startPos + new Vector2(0f, 60f * p);
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f - p);
                yield return null;
            }
            Destroy(go);
        }

        // ── Sheet slicing ─────────────────────────────────────────────────────

        // Builds a worm animation from the ATTACK row, which holds discrete
        // complete poses (Antecipação / Estocada / Recuperação) separated by wide
        // gaps. Each pose is framed in a common-sized window centered on its body
        // so the worm animates in place without jittering in scale. Falls back to a
        // single static worm if the poses can't be detected.
        private static Sprite[] SliceAttackRow(Texture2D tex)
        {
            int w = tex.width;
            int rowH = tex.height / 4;
            // Attack row is the 2nd from the visual top → Unity y in [2*rowH, 3*rowH].
            int rowBase = 2 * rowH;
            int bandY0 = rowBase + Mathf.RoundToInt(rowH * 0.03f); // skip bottom labels
            int bandY1 = rowBase + Mathf.RoundToInt(rowH * 0.88f); // skip top label/numbers
            int bandH  = bandY1 - bandY0;

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch { return new[] { CropMovementRowFull(tex) }; }

            // Per-column opacity within the band.
            var colMass = new int[w];
            for (int x = 0; x < w; x++)
            {
                int c = 0;
                for (int y = bandY0; y < bandY1; y++)
                    if (px[y * w + x].a > 40) c++;
                colMass[x] = c;
            }

            // Find content runs separated by empty gaps (the 3 poses).
            int threshold = Mathf.Max(2, bandH / 10);
            var runs = new System.Collections.Generic.List<Vector2Int>();
            int start = -1;
            for (int x = 0; x < w; x++)
            {
                bool filled = colMass[x] > threshold;
                if (filled && start < 0) start = x;
                else if (!filled && start >= 0) { runs.Add(new Vector2Int(start, x)); start = -1; }
            }
            if (start >= 0) runs.Add(new Vector2Int(start, w));
            runs.RemoveAll(r => (r.y - r.x) < w / 18); // drop label specks

            if (runs.Count < 2) return new[] { CropMovementRowFull(tex) };

            // Compute each pose's bbox by scanning the whole region between the
            // gap-midpoints around its run (not just the thresholded columns) so
            // faint tail/leg tips are included — while the empty gaps guarantee no
            // neighbouring worm can leak in.
            var boxes = new RectInt[runs.Count];
            int maxBoxW = 0, maxBoxH = 0;
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
                maxBoxW = Mathf.Max(maxBoxW, boxes[i].width);
                maxBoxH = Mathf.Max(maxBoxH, boxes[i].height);
            }

            // Uniform transparent canvas sized to the largest pose (+ small margin).
            int canvasW = maxBoxW + 16;
            int canvasH = maxBoxH + 16;
            var frames = new Sprite[runs.Count];
            for (int i = 0; i < runs.Count; i++)
            {
                var canvas = new Color32[canvasW * canvasH]; // alpha 0 by default
                var box = boxes[i];
                int offX = (canvasW - box.width) / 2;
                int offY = (canvasH - box.height) / 2;
                for (int y = 0; y < box.height; y++)
                for (int x = 0; x < box.width; x++)
                {
                    var c = px[(box.y + y) * w + (box.x + x)];
                    if (c.a <= 40) continue; // skip background; only blit the worm
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

        // Crops the entire monster in the top movement row into a single sprite,
        // tight to its visible content within a label-free vertical band. Used for
        // the worm, whose movement row is one long continuous body (no per-frame
        // poses) — slicing it into cells would just show disconnected pieces.
        private static Sprite CropMovementRowFull(Texture2D tex)
        {
            int w = tex.width;
            int rowH = tex.height / 4;
            int rowY0 = tex.height - rowH;
            int bandY0 = rowY0 + Mathf.RoundToInt(rowH * 0.10f);
            int bandY1 = rowY0 + Mathf.RoundToInt(rowH * 0.92f);

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch
            {
                return Sprite.Create(tex, new Rect(0, rowY0, w, rowH),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }

            int minX = w, maxX = 0, minY = bandY1, maxY = bandY0;
            for (int y = bandY0; y < bandY1; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > 40)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }

            Rect r = (maxX <= minX || maxY <= minY)
                ? new Rect(0, bandY0, w, bandY1 - bandY0)
                : new Rect(minX, minY, maxX - minX, maxY - minY);
            return Sprite.Create(tex, r, new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
        }

        // Slices the top "movement" row of a monster sheet into `frameCount` even
        // cells, then tight-crops each cell to its visible content so the monster
        // fills the display slot (the raw cells have lots of empty headroom plus
        // baked-in label text along the row edges). The vertical crop window skips
        // the labels printed at the very top/bottom of the band.
        private static Sprite[] SliceMovementRow(Texture2D tex, int frameCount)
        {
            int w = tex.width;
            int rowH = tex.height / 4;
            int rowY0 = tex.height - rowH;            // bottom of the top row (Unity coords)
            int cw = w / frameCount;

            // Vertical band that holds the monster art, excluding label text rows.
            int bandY0 = rowY0 + Mathf.RoundToInt(rowH * 0.12f);
            int bandY1 = rowY0 + Mathf.RoundToInt(rowH * 0.88f);

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch
            {
                // Not readable — fall back to even cells, no cropping.
                var fb = new Sprite[frameCount];
                for (int i = 0; i < frameCount; i++)
                    fb[i] = Sprite.Create(tex, new Rect(i * cw, rowY0, cw, rowH),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return fb;
            }

            var frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int cellX0 = i * cw;
                int cellX1 = cellX0 + cw;

                // Find the content bounding box inside this cell (within the band).
                int minX = cellX1, maxX = cellX0, minY = bandY1, maxY = bandY0;
                for (int y = bandY0; y < bandY1; y++)
                for (int x = cellX0; x < cellX1; x++)
                    if (px[y * w + x].a > 40)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }

                Rect r;
                if (maxX <= minX || maxY <= minY)
                {
                    // Empty cell — use the whole cell so the animation never blanks.
                    r = new Rect(cellX0, bandY0, cw, bandY1 - bandY0);
                }
                else
                {
                    // Pad the bbox a little and keep frames a consistent square-ish
                    // size so the monster doesn't jitter between frames.
                    int pad = Mathf.RoundToInt(cw * 0.06f);
                    int fx = Mathf.Max(cellX0, minX - pad);
                    int fy = Mathf.Max(bandY0, minY - pad);
                    int fw = Mathf.Min(cellX1 - fx, (maxX - minX) + pad * 2);
                    int fh = Mathf.Min(bandY1 - fy, (maxY - minY) + pad * 2);
                    r = new Rect(fx, fy, fw, fh);
                }
                frames[i] = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
            }
            return frames;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private TextMeshProUGUI MakeLabel(string name, string text, float size, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }
    }
}
