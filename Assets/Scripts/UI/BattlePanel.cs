using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Full-screen battle arena panel — appears when the player taps BATALHAR.
    // Hides the office, shows a dungeon background + monster + combat UI.
    // Dismissed by tapping VOLTAR, returning to the office.
    public class BattlePanel : MonoBehaviour
    {
        private static readonly Color NavyDark  = new(0.055f, 0.094f, 0.165f, 0.92f);
        private static readonly Color RedMain   = new(0.80f,  0.12f,  0.12f,  1f);
        private static readonly Color GoldColor = new(1f,     0.808f, 0.227f, 1f);
        private static readonly Color GreenBtn  = new(0.247f, 0.749f, 0.353f, 1f);

        // Child components
        private MonsterView    _monsterView;
        private Image          _atkBtnFace;
        private RectTransform  _atkBtnRT;
        private TextMeshProUGUI _waveLabel;
        private Image          _bgImage;

        // Battle music source (separate from office music)
        private AudioSource _battleMusic;
        private bool        _musicStarted;

        // Arena backgrounds — normal and boss variants
        private Sprite _arenaSprite;
        private Sprite _arenaBossSprite;

        private static Sprite LoadBgSprite(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // Swap arena art when entering/leaving boss wave
        private void RefreshArena(bool isBoss)
        {
            if (_bgImage == null) return;
            _bgImage.sprite = (isBoss && _arenaBossSprite != null) ? _arenaBossSprite : _arenaSprite;
        }

        public static BattlePanel Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            BuildUI();
            gameObject.SetActive(false);
        }

        // ── Open / Close ──────────────────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            // Pause office ambient / music
            SoundManager.Get(); // ensure exists
            StartBattleMusic();

            // Sync current monster state
            var cm = CombatManager.Instance;
            if (cm != null && _monsterView != null)
            {
                _monsterView.SetMonster(cm.CurrentDef, cm.Wave);
                _monsterView.UpdateHp(cm.CurrentHp, cm.MaxHp);
                RefreshArena(cm.CurrentDef.type == CombatManager.MonsterType.Boss);
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
            StopBattleMusic();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // ── Dungeon background ─────────────────────────────────────────
            _bgImage = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            _bgImage.raycastTarget = true; // block clicks to office behind
            _bgImage.type = Image.Type.Simple;
            _bgImage.preserveAspect = false;
            _bgImage.color = Color.white;

            // Arena art from Resources (battle_bg / battle_bg_boss); procedural fallback
            _arenaSprite     = LoadBgSprite("Backgrounds/battle_bg");
            _arenaBossSprite = LoadBgSprite("Backgrounds/battle_bg_boss");
            if (_arenaSprite == null)
            {
                var bgTex = MakeDungeonBg();
                _arenaSprite = Sprite.Create(bgTex,
                    new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            }
            _bgImage.sprite = _arenaSprite;

            // Vignette overlay (dark edges)
            {
                var go = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var vrt = go.GetComponent<RectTransform>();
                vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
                vrt.offsetMin = vrt.offsetMax = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = UiSpriteFactory.Circle();
                // Dark radial-ish vignette using a circle with inverted alpha
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = false;
            }

            // ── Wave label top ────────────────────────────────────────────
            {
                var go = new GameObject("WaveLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.25f, 0.88f); lrt.anchorMax = new Vector2(0.75f, 1f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                _waveLabel = go.GetComponent<TextMeshProUGUI>();
                _waveLabel.text = "ONDA 1 / 10";
                _waveLabel.fontSize = 22f; _waveLabel.fontStyle = FontStyles.Bold;
                _waveLabel.color = GoldColor;
                _waveLabel.alignment = TextAlignmentOptions.Center;
                _waveLabel.raycastTarget = false;
                var f = TMP_Settings.defaultFontAsset; if (f != null) _waveLabel.font = f;
            }

            // ── Monster view — fixed-width column centered on screen ─────
            // Keeps name tag / HP bar / monster proportional on wide screens.
            var mvGO = new GameObject("MonsterView", typeof(RectTransform));
            mvGO.transform.SetParent(transform, false);
            var mvRT = mvGO.GetComponent<RectTransform>();
            mvRT.anchorMin = new Vector2(0.5f, 0.5f);
            mvRT.anchorMax = new Vector2(0.5f, 0.5f);
            mvRT.pivot     = new Vector2(0.5f, 0.5f);
            mvRT.sizeDelta = new Vector2(520f, 560f);
            mvRT.anchoredPosition = new Vector2(0f, 30f);
            _monsterView = mvGO.AddComponent<MonsterView>();

            // ── Bottom button row — fixed sizes, centered as a group ─────
            // VOLTAR (left of attack)
            MakeBottomBtn("VOLTAR", new Vector2(-310f, 56f), new Vector2(170f, 56f),
                NavyDark, 15f, Close);

            // ATACAR (center, big)
            var atkGO = new GameObject("AtkBtn", typeof(RectTransform), typeof(Button));
            atkGO.transform.SetParent(transform, false);
            _atkBtnRT = atkGO.GetComponent<RectTransform>();
            _atkBtnRT.anchorMin = _atkBtnRT.anchorMax = new Vector2(0.5f, 0f);
            _atkBtnRT.pivot     = new Vector2(0.5f, 0f);
            _atkBtnRT.anchoredPosition = new Vector2(0f, 24f);
            _atkBtnRT.sizeDelta = new Vector2(240f, 76f);
            _atkBtnFace = atkGO.AddComponent<Image>();
            _atkBtnFace.sprite = UiSpriteFactory.RoundedBox();
            _atkBtnFace.type = Image.Type.Sliced;
            _atkBtnFace.color = RedMain;
            var atkBtn = atkGO.GetComponent<Button>();
            atkBtn.targetGraphic = _atkBtnFace;
            atkBtn.onClick.AddListener(() => CombatManager.Instance?.PlayerAttack());
            var atkHold = atkGO.AddComponent<HoldButton>();
            atkHold.Init(() => CombatManager.Instance?.PlayerAttack());
            MakeLabel(atkGO, "ATACAR!", 22f, Color.white);

            // UPGRADES (right of attack)
            MakeBottomBtn("UPGRADES", new Vector2(140f, 56f), new Vector2(170f, 56f),
                new Color(0.35f, 0.18f, 0.02f, 0.90f), 15f, () => {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.CloseAllModals();
                        var cup = FindFirstObjectByType<CombatUpgradePanel>();
                        cup?.Open();
                    }
                });
        }

        // pos.x = offset from screen center; size = fixed button size
        private Button MakeBottomBtn(string label, Vector2 pos, Vector2 size,
            Color color, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var brt = go.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0f, 0f);
            brt.anchoredPosition = new Vector2(pos.x, 34f);
            brt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = UiSpriteFactory.RoundedBox(); img.type = Image.Type.Sliced; img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            MakeLabel(go, label, fontSize, Color.white);
            return btn;
        }

        private void MakeLabel(GameObject parent, string text, float size, Color color)
        {
            var go = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = FontStyles.Bold;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var f = TMP_Settings.defaultFontAsset; if (f != null) tmp.font = f;
        }

        // ── Subscribe to CombatManager events ─────────────────────────────────

        public void SubscribeToCombat(CombatManager cm)
        {
            cm.OnWaveStarted  += def =>
            {
                _monsterView?.SetMonster(def, cm.Wave);
                bool boss = def.type == CombatManager.MonsterType.Boss;
                RefreshArena(boss);
                if (_waveLabel != null)
                {
                    _waveLabel.text  = boss ? "!! ONDA 10 - BOSS !!" : $"ONDA {cm.Wave} / 10";
                    _waveLabel.color = boss ? GoldColor : new Color(0.7f, 0.85f, 1f, 1f);
                }
            };
            cm.OnHpChanged    += (hp, max) => _monsterView?.UpdateHp(hp, max);
            cm.OnMonsterDied  += reward    =>
            {
                _monsterView?.PlayDeathEffect();
                _monsterView?.ShowBetweenWaves(reward);
            };
            cm.OnPlayerDamage += dmg => _monsterView?.PlayHitEffect(dmg);
        }

        // ── Dungeon background (procedural) ───────────────────────────────────

        private static Texture2D MakeDungeonBg()
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Dark purple-navy gradient, darker at bottom
                float t = (float)y / h;
                // Vary slightly with x for a subtle cave texture
                float noise = (Mathf.Sin(x * 0.8f + y * 0.5f) * 0.5f + 0.5f) * 0.06f;
                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(8,  22, t) + noise * 255);
                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(5,  14, t) + noise * 255);
                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(18, 42, t) + noise * 255);
                px[y * w + x] = new Color32(r, g, b, 255);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Battle music ──────────────────────────────────────────────────────

        private void StartBattleMusic()
        {
            if (_battleMusic == null)
            {
                var go = new GameObject("BattleMusic");
                go.transform.SetParent(transform, false);
                _battleMusic = go.AddComponent<AudioSource>();
                _battleMusic.loop = true;
                _battleMusic.spatialBlend = 0f;
                _battleMusic.volume = 0.35f;
                _battleMusic.clip = MakeBattleLoop();
            }
            // Fade out office music
            if (SoundManager.Instance != null && !SoundManager.MusicMuted)
                StartCoroutine(FadeMusicSource(SoundManager.GetMusicSource(), 0.32f, 0f, 0.4f));

            if (!SoundManager.MusicMuted)
            {
                _battleMusic.Play();
                _musicStarted = true;
            }
        }

        private void StopBattleMusic()
        {
            if (_battleMusic != null) _battleMusic.Stop();
            _musicStarted = false;
            // Restore office music
            if (SoundManager.Instance != null && !SoundManager.MusicMuted)
                StartCoroutine(FadeMusicSource(SoundManager.GetMusicSource(), 0f, 0.32f, 0.4f));
        }

        private IEnumerator FadeMusicSource(AudioSource src, float from, float to, float dur)
        {
            if (src == null) yield break;
            if (to > 0f) src.Play();
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (src != null) src.volume = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            if (src != null)
            {
                src.volume = to;
                if (to <= 0f) src.Pause();
            }
        }

        // Battle loop: faster, more intense — minor key, driving bass + percussion
        private static AudioClip MakeBattleLoop()
        {
            const int Rate = 44100;
            float bpm = 128f;
            float beat = 60f / bpm;
            float loopLen = beat * 8; // 8-beat loop
            int n = Mathf.CeilToInt(loopLen * Rate);
            var data = new float[n];

            // Driving bass line — ominous minor riff (Am: A1 C2 E2 G2)
            float[] bassNotes = { 55f, 65.4f, 82.4f, 77.8f, 55f, 65.4f, 73.4f, 55f };
            for (int b = 0; b < 8; b++)
            {
                int start = Mathf.RoundToInt(b * beat * Rate);
                float freq = bassNotes[b];
                for (int i = start; i < Mathf.Min(start + Mathf.RoundToInt(beat * 0.7f * Rate), n); i++)
                {
                    float t = (float)(i - start) / Rate;
                    float env = Mathf.Exp(-t * 3.5f);
                    float v = (Mathf.Sin(2f * Mathf.PI * freq * t)
                             + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t)
                             + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t)) * env * 0.28f;
                    if (i < n) data[i] += v;
                }
            }

            // Percussion: kick on 1 & 5, snare on 3 & 7
            for (int b = 0; b < 8; b++)
            {
                int start = Mathf.RoundToInt(b * beat * Rate);
                bool isKick  = b == 0 || b == 4;
                bool isSnare = b == 2 || b == 6;
                if (!isKick && !isSnare) continue;

                int len = Mathf.RoundToInt(0.18f * Rate);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Exp(-t * (isKick ? 18f : 30f));
                    float v = isKick
                        ? Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, t * 12f) * t) * env * 0.55f
                        : (UnityEngine.Random.value * 2f - 1f) * env * 0.35f;
                    data[start + i] += v;
                }
            }

            // Hi-hat on every beat (eighth notes)
            for (int b = 0; b < 16; b++)
            {
                int start = Mathf.RoundToInt(b * beat * 0.5f * Rate);
                int len = Mathf.RoundToInt(0.04f * Rate);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Exp(-t * 80f);
                    data[start + i] += (UnityEngine.Random.value * 2f - 1f) * env * 0.12f;
                }
            }

            // Normalize
            float peak = 0f;
            foreach (var s in data) { float a = Mathf.Abs(s); if (a > peak) peak = a; }
            if (peak > 0.001f)
                for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.85f;

            var clip = AudioClip.Create("battle_music", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
