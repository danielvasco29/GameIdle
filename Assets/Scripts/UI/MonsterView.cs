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
            _floatPhase += Time.deltaTime * FloatSpeed;
            float y = Mathf.Sin(_floatPhase) * FloatAmp;
            _spriteImg.rectTransform.anchoredPosition = new Vector2(0f, 120f + y);
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            _rt.anchorMin = new Vector2(0f, 0.15f);
            _rt.anchorMax = new Vector2(1f, 1f);
            _rt.offsetMin = _rt.offsetMax = Vector2.zero;

            var font = TMP_Settings.defaultFontAsset;

            // Wave badge (top-right)
            _waveText = MakeLabel("WaveBadge", "ONDA 1 / 10", 13f,
                new Color(0.7f, 0.8f, 1f, 0.9f),
                new Vector2(0.55f, 0.85f), new Vector2(1f, 1f),
                new Vector2(0f, -4f), new Vector2(-10f, -4f), font);
            _waveText.alignment = TextAlignmentOptions.TopRight;

            // Monster name tag
            {
                var go = new GameObject("NameBg", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f, 0.72f); rt.anchorMax = new Vector2(0.9f, 0.88f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced;
                img.color = new Color(0.5f, 0.05f, 0.05f, 0.82f);
                img.raycastTarget = false;
            }
            _nameText = MakeLabel("MonsterName", "BUG GIGANTE", 18f, Color.white,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f),
                Vector2.zero, Vector2.zero, font);
            _nameText.fontStyle = FontStyles.Bold;

            // HP bar background
            {
                var go = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.08f, 0.60f); rt.anchorMax = new Vector2(0.92f, 0.71f);
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

            // Monster sprite image (centered, floats)
            {
                var go = new GameObject("MonsterSprite", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(200f, 200f);
                rt.anchoredPosition = new Vector2(0f, 120f);
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
                rt.sizeDelta = new Vector2(140f, 18f);
                rt.anchoredPosition = new Vector2(0f, 116f);
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

            // Load sprite — some monster PNGs have the background baked in as real
            // white/checkerboard pixels, so run the flood-fill remover (textures in
            // Resources/Monsters/ are imported readable by CharacterSpriteImporter).
            var srcTex = Resources.Load<Texture2D>(def.spritePath);
            if (srcTex != null && _spriteImg != null)
            {
                var tex = SpriteBackgroundRemover.Process(srcTex);
                _spriteImg.sprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
                _spriteImg.color = Color.white;
                // Boss sprite slightly larger
                _spriteImg.rectTransform.sizeDelta = isBoss ? new Vector2(240f, 240f) : new Vector2(200f, 200f);
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
            if (isBoss) StartCoroutine(BossEntrance());
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
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRed());
            SpawnDamageNumber(dmg);
            StartCoroutine(ShakeSprite());
        }

        public void PlayDeathEffect()
        {
            StartCoroutine(DeathAnim());
        }

        public void ShowBetweenWaves(double reward)
        {
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
