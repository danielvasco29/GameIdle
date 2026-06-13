using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FontStyles = TMPro.FontStyles;

namespace GameIdle
{
    public class CombatUpgradePanel : MonoBehaviour
    {
        // ── Palette (matches UIManager navy-glass theme) ──────────────────────
        private static readonly Color NavyPanel  = new(0.055f, 0.094f, 0.165f, 0.92f);
        private static readonly Color NavyCard   = new(0.106f, 0.169f, 0.275f, 1f);
        private static readonly Color GoldColor  = new(1f, 0.808f, 0.227f, 1f);
        private static readonly Color GreenBtn   = new(0.247f, 0.749f, 0.353f, 1f);
        private static readonly Color TextPrimary= new(0.933f, 0.953f, 0.980f, 1f);
        private static readonly Color TextSec    = new(0.624f, 0.698f, 0.788f, 1f);
        private static readonly Color GrayBtn    = new(0.25f,  0.28f,  0.38f,  1f);
        private static readonly Color RedAccent  = new(0.85f,  0.22f,  0.22f,  1f);

        private static Sprite Circle()  => UiSpriteFactory.Circle();
        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();

        // Icon paths inside Resources/Icons/
        private static readonly string[] IconPaths = {
            "Icons/icon_sword", "Icons/icon_shield", "Icons/icon_frost",
            "Icons/icon_potion", "Icons/icon_gem"
        };

        // ── Row data ──────────────────────────────────────────────────────────
        private struct UpgradeRowUI
        {
            public TextMeshProUGUI levelText;
            public TextMeshProUGUI costText;
            public Button          buyButton;
            public Image           buyBg;
        }

        private UpgradeRowUI _rowSword;
        private UpgradeRowUI _rowArmor;
        private UpgradeRowUI _rowFrost;
        private UpgradeRowUI _rowPotion;
        private UpgradeRowUI _rowCritico;

        // Potion button has extra state text
        private TextMeshProUGUI _potionStateText;

        private TMP_FontAsset _font;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _font = UiFont.Get();
            if (_font == null)
            {
                var any = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (any != null) _font = any.font;
            }

            BuildUI();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsUpdated += RefreshState;
            RefreshState();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStatsUpdated -= RefreshState;
        }

        // Refresh potion cooldown every frame while open
        private void Update()
        {
            RefreshPotionRow();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            RefreshState();
        }

        // ── UI Construction ───────────────────────────────────────────────────

        private void BuildUI()
        {
            // Panel root — centered, fixed size
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(480f, 636f);

            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.sprite = Rounded();
            bg.type   = Image.Type.Sliced;
            bg.color  = NavyPanel;

            // Title
            var title = MakeText(transform, "Title", "UPGRADES DE COMBATE",
                20, GoldColor, FontStyles.Bold, TextAlignmentOptions.Center);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(48f, -54f); trt.offsetMax = new Vector2(-48f, -10f);

            // Close (X) button — top right
            var closeGO = new GameObject("CloseBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            closeGO.transform.SetParent(transform, false);
            var crt = closeGO.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-8f, -8f);
            crt.sizeDelta = new Vector2(36f, 36f);
            var cImg = closeGO.GetComponent<Image>();
            cImg.sprite = Circle(); cImg.type = Image.Type.Simple;
            cImg.color = new Color(0.70f, 0.12f, 0.12f, 1f);
            closeGO.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var cLabel = MakeText(closeGO.transform, "X", "X",
                18, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            var clrt = cLabel.rectTransform;
            clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
            clrt.offsetMin = clrt.offsetMax = Vector2.zero;
            cLabel.raycastTarget = false;

            // Upgrade rows  (top → bottom inside the panel)
            // We layout 5 rows starting 62px from top, each 96px tall, 10px gap
            float startY   = 64f;
            float rowH     = 96f;
            float rowGap   = 10f;
            float panelH   = 636f;

            _rowSword  = BuildRow("Espada Afiada",
                "+25% dano por tap",
                0, startY, rowH, panelH);

            _rowArmor  = BuildRow("Armadura",
                "Workers atacam mais rápido\n(-15% intervalo por nível)",
                1, startY + (rowH + rowGap), rowH, panelH);

            _rowFrost  = BuildRow("Feitiço de Gelo",
                "Boss recebe +20% de dano\npor nível",
                2, startY + 2 * (rowH + rowGap), rowH, panelH);

            _rowPotion = BuildRow("Poção de Poder",
                "2x dano por 30s\n(recarga: 5 min)",
                3, startY + 3 * (rowH + rowGap), rowH, panelH);

            _rowCritico = BuildRow("Golpe Crítico",
                "+10% chance de acerto crítico\n(2x dano) por nível",
                4, startY + 4 * (rowH + rowGap), rowH, panelH);

            // Icon colors for each upgrade slot (used when no texture found)
            Color[] iconColors = {
                new(0.9f, 0.85f, 0.2f, 1f),  // sword - gold
                new(0.4f, 0.7f, 1.0f, 1f),   // armor - blue
                new(0.4f, 0.9f, 1.0f, 1f),   // frost - cyan
                new(0.8f, 0.3f, 0.9f, 1f),   // potion - purple
                new(1.0f, 0.5f, 0.2f, 1f),   // critico - orange
            };
            string[] iconSymbols = { "⚔", "🛡", "❄", "⚗", "⚡" };

            // Attach icons to each row
            for (int i = 0; i < IconPaths.Length; i++)
            {
                var card = transform.GetChild(i + 2); // 0=bg (component on self), so children: 0=title, 1=close, then rows
                // Find the correct card by name
                Transform cardT = null;
                for (int ci = 0; ci < transform.childCount; ci++)
                {
                    if (transform.GetChild(ci).name == $"UpgradeRow_{i}") { cardT = transform.GetChild(ci); break; }
                }
                if (cardT == null) continue;

                // Colored circle background for icon
                var bgGO = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(cardT, false);
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(0f, 0.5f); bgRT.anchorMax = new Vector2(0f, 0.5f);
                bgRT.pivot = new Vector2(0f, 0.5f);
                bgRT.anchoredPosition = new Vector2(8f, 0f);
                bgRT.sizeDelta = new Vector2(56f, 56f);
                var bgImg = bgGO.GetComponent<Image>();
                bgImg.sprite = Circle(); bgImg.type = Image.Type.Simple;
                bgImg.color = new Color(iconColors[i].r * 0.25f, iconColors[i].g * 0.25f, iconColors[i].b * 0.25f, 0.8f);
                bgImg.raycastTarget = false;

                var iconTex = Resources.Load<Texture2D>(IconPaths[i]);
                if (iconTex != null)
                {
                    var processed = SpriteBackgroundRemover.Process(iconTex);
                    var sp = Sprite.Create(processed,
                        new Rect(0, 0, processed.width, processed.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGO.transform.SetParent(cardT, false);
                    var irt = iconGO.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
                    irt.pivot = new Vector2(0f, 0.5f);
                    irt.anchoredPosition = new Vector2(12f, 0f);
                    irt.sizeDelta = new Vector2(48f, 48f);
                    var iImg = iconGO.GetComponent<Image>();
                    iImg.sprite = sp; iImg.preserveAspect = true; iImg.raycastTarget = false;
                }
                else
                {
                    // Fallback: colored symbol text
                    var symGO = new GameObject("IconSym", typeof(RectTransform), typeof(TextMeshProUGUI));
                    symGO.transform.SetParent(cardT, false);
                    var srt = symGO.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(0f, 0.5f);
                    srt.pivot = new Vector2(0f, 0.5f);
                    srt.anchoredPosition = new Vector2(8f, 0f);
                    srt.sizeDelta = new Vector2(56f, 56f);
                    var sTMP = symGO.GetComponent<TextMeshProUGUI>();
                    sTMP.text = iconSymbols[i]; sTMP.fontSize = 28;
                    sTMP.color = iconColors[i]; sTMP.alignment = TextAlignmentOptions.Center;
                    sTMP.raycastTarget = false;
                    if (_font != null) sTMP.font = _font;
                }

                // Shift name/desc text right to make room for icon
                var nameRT = cardT.Find("Name")?.GetComponent<RectTransform>();
                var descRT = cardT.Find("Desc")?.GetComponent<RectTransform>();
                if (nameRT != null) nameRT.offsetMin = new Vector2(72f, 0f);
                if (descRT != null) descRT.offsetMin = new Vector2(72f, 0f);
            }

            // Wire buttons
            _rowSword.buyButton.onClick.AddListener(OnBuySword);
            _rowArmor.buyButton.onClick.AddListener(OnBuyArmor);
            _rowFrost.buyButton.onClick.AddListener(OnBuyFrost);
            _rowPotion.buyButton.onClick.AddListener(OnBuyPotion);
            _rowCritico.buyButton.onClick.AddListener(OnBuyCritico);

            // Extra potion state label (cooldown / active timer)
            _potionStateText = MakeText(transform, "PotionState", "",
                12, RedAccent, FontStyles.Bold, TextAlignmentOptions.Center);
            var psrt = _potionStateText.rectTransform;
            // Position it just below the 4th row
            float row4Bottom = startY + 3 * (rowH + rowGap) + rowH;
            psrt.anchorMin = new Vector2(0f, 1f); psrt.anchorMax = new Vector2(1f, 1f);
            psrt.offsetMin = new Vector2(12f, -(row4Bottom + 32f));
            psrt.offsetMax = new Vector2(-12f, -(row4Bottom + 4f));
        }

        /// <summary>Build one upgrade row card and return handles to dynamic elements.</summary>
        private UpgradeRowUI BuildRow(string upgradeName, string description,
                                      int rowIndex, float topOffset, float height, float panelH)
        {
            var card = new GameObject($"UpgradeRow_{rowIndex}",
                typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);

            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(12f, -(topOffset + height));
            rt.offsetMax = new Vector2(-12f, -topOffset);

            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = Rounded(); cardImg.type = Image.Type.Sliced;
            cardImg.color  = NavyCard;

            // ── Name ──────────────────────────────────────────────────────────
            var nameT = MakeText(card.transform, "Name", upgradeName,
                18, TextPrimary, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            var nrt = nameT.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(0.58f, 1f);
            nrt.offsetMin = new Vector2(14f, 0f); nrt.offsetMax = new Vector2(0f, -6f);

            // ── Description ───────────────────────────────────────────────────
            var descT = MakeText(card.transform, "Desc", description,
                13, TextSec, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            var drt = descT.rectTransform;
            drt.anchorMin = new Vector2(0f, 0.1f); drt.anchorMax = new Vector2(0.58f, 0.55f);
            drt.offsetMin = new Vector2(14f, 0f); drt.offsetMax = Vector2.zero;
            descT.textWrappingMode = TextWrappingModes.Normal;

            // ── Level badge (top-right of card) ───────────────────────────────
            var lvlT = MakeText(card.transform, "Level", "",
                15, GoldColor, FontStyles.Bold, TextAlignmentOptions.TopRight);
            var lrt = lvlT.rectTransform;
            lrt.anchorMin = new Vector2(0.58f, 0.55f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(-14f, -6f);

            // ── Buy button (right side, vertically centered) ──────────────────
            var buyGO = new GameObject("BuyBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            buyGO.transform.SetParent(card.transform, false);
            var brt = buyGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.60f, 0.12f); brt.anchorMax = new Vector2(1f, 0.50f);
            brt.offsetMin = new Vector2(6f, 0f); brt.offsetMax = new Vector2(-12f, 0f);
            var buyBg = buyGO.GetComponent<Image>();
            buyBg.sprite = Rounded(); buyBg.type = Image.Type.Sliced;
            buyBg.color  = GreenBtn;

            // Cost label inside button
            var costT = MakeText(buyGO.transform, "Cost", "",
                15, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            var costRT = costT.rectTransform;
            costRT.anchorMin = Vector2.zero; costRT.anchorMax = Vector2.one;
            costRT.offsetMin = costRT.offsetMax = Vector2.zero;
            costT.raycastTarget = false;

            return new UpgradeRowUI
            {
                levelText = lvlT,
                costText  = costT,
                buyButton = buyGO.GetComponent<Button>(),
                buyBg     = buyBg
            };
        }

        // ── Buy callbacks ─────────────────────────────────────────────────────

        private void OnBuySword()
        {
            if (CombatManager.SwordLevel >= CombatManager.SwordMax) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.Money < CombatManager.GetSwordCost()) { PlayError(); return; }
            CombatManager.UpgradeSword();
            PlayBuy();
            RefreshState();
            ShowToast("Espada Afiada melhorada!");
        }

        private void OnBuyArmor()
        {
            if (CombatManager.ArmorLevel >= CombatManager.ArmorMax) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.Money < CombatManager.GetArmorCost()) { PlayError(); return; }
            CombatManager.UpgradeArmor();
            PlayBuy();
            RefreshState();
            ShowToast("Armadura melhorada!");
        }

        private void OnBuyFrost()
        {
            if (CombatManager.FrostLevel >= CombatManager.FrostMax) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.Money < CombatManager.GetFrostCost()) { PlayError(); return; }
            CombatManager.UpgradeFrost();
            PlayBuy();
            RefreshState();
            ShowToast("Feitiço de Gelo melhorado!");
        }

        private void OnBuyCritico()
        {
            if (CombatManager.CriticoLevel >= CombatManager.CriticoMax) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.Money < CombatManager.GetCriticoCost()) { PlayError(); return; }
            CombatManager.UpgradeCritico();
            PlayBuy();
            RefreshState();
            ShowToast("Golpe Crítico melhorado!");
        }

        private void OnBuyPotion()
        {
            // If not yet unlocked → buy it
            if (!CombatManager.PotionUnlocked)
            {
                if (GameManager.Instance == null) return;
                if (GameManager.Instance.Money < CombatManager.GetPotionCost()) { PlayError(); return; }
                CombatManager.BuyPotion();
                PlayBuy();
                RefreshState();
                ShowToast("Poção de Poder desbloqueada!");
                return;
            }

            // Already unlocked → activate
            if (CombatManager.PotionActive || CombatManager.PotionCooldown > 0f)
            {
                PlayError();
                return;
            }

            CombatManager.ActivatePotion();
            PlayBuy();
            RefreshState();
            ShowToast("Poção de Poder ativada!");
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void RefreshState()
        {
            if (GameManager.Instance == null) return;
            double money = GameManager.Instance.Money;

            // Sword
            bool swordMaxed = CombatManager.SwordLevel >= CombatManager.SwordMax;
            _rowSword.levelText.text = swordMaxed
                ? $"MÁX ({CombatManager.SwordMax}/{CombatManager.SwordMax})"
                : $"{CombatManager.SwordLevel}/{CombatManager.SwordMax}";
            if (swordMaxed)
            {
                SetRowMaxed(ref _rowSword);
            }
            else
            {
                double sc = CombatManager.GetSwordCost();
                bool canS = money >= sc;
                _rowSword.costText.text = $"${NumberFormatter.Format(sc)}";
                _rowSword.buyButton.interactable = canS;
                _rowSword.buyBg.color = canS ? GreenBtn : GrayBtn;
            }

            // Armor
            bool armorMaxed = CombatManager.ArmorLevel >= CombatManager.ArmorMax;
            _rowArmor.levelText.text = armorMaxed
                ? $"MÁX ({CombatManager.ArmorMax}/{CombatManager.ArmorMax})"
                : $"{CombatManager.ArmorLevel}/{CombatManager.ArmorMax}";
            if (armorMaxed)
            {
                SetRowMaxed(ref _rowArmor);
            }
            else
            {
                double ac = CombatManager.GetArmorCost();
                bool canA = money >= ac;
                _rowArmor.costText.text = $"${NumberFormatter.Format(ac)}";
                _rowArmor.buyButton.interactable = canA;
                _rowArmor.buyBg.color = canA ? GreenBtn : GrayBtn;
            }

            // Frost
            bool frostMaxed = CombatManager.FrostLevel >= CombatManager.FrostMax;
            _rowFrost.levelText.text = frostMaxed
                ? $"MÁX ({CombatManager.FrostMax}/{CombatManager.FrostMax})"
                : $"{CombatManager.FrostLevel}/{CombatManager.FrostMax}";
            if (frostMaxed)
            {
                SetRowMaxed(ref _rowFrost);
            }
            else
            {
                double fc = CombatManager.GetFrostCost();
                bool canF = money >= fc;
                _rowFrost.costText.text = $"${NumberFormatter.Format(fc)}";
                _rowFrost.buyButton.interactable = canF;
                _rowFrost.buyBg.color = canF ? GreenBtn : GrayBtn;
            }

            // Potion
            RefreshPotionRow();

            // Crítico
            bool criticoMaxed = CombatManager.CriticoLevel >= CombatManager.CriticoMax;
            _rowCritico.levelText.text = criticoMaxed
                ? $"MÁX ({CombatManager.CriticoMax}/{CombatManager.CriticoMax})"
                : $"{CombatManager.CriticoLevel}/{CombatManager.CriticoMax} ({(int)(CombatManager.GetCriticoChance()*100)}% crit)";
            if (criticoMaxed)
            {
                SetRowMaxed(ref _rowCritico);
            }
            else
            {
                double cc = CombatManager.GetCriticoCost();
                bool canC = money >= cc;
                _rowCritico.costText.text = $"${NumberFormatter.Format(cc)}";
                _rowCritico.buyButton.interactable = canC;
                _rowCritico.buyBg.color = canC ? GreenBtn : GrayBtn;
            }
        }

        private void RefreshPotionRow()
        {
            if (_rowPotion.costText == null) return;

            if (!CombatManager.PotionUnlocked)
            {
                // Show purchase option
                _rowPotion.levelText.text = "0/1";
                double pc = CombatManager.GetPotionCost();
                bool canP = GameManager.Instance != null && GameManager.Instance.Money >= pc;
                _rowPotion.costText.text = $"${NumberFormatter.Format(pc)}";
                _rowPotion.buyButton.interactable = canP;
                _rowPotion.buyBg.color = canP ? GreenBtn : GrayBtn;
                if (_potionStateText != null) _potionStateText.text = "";
                return;
            }

            // Already purchased — show active/cooldown/ready state
            _rowPotion.levelText.text = "1/1";

            if (CombatManager.PotionActive)
            {
                int rem = Mathf.CeilToInt(CombatManager.PotionRemaining);
                _rowPotion.costText.text  = $"ATIVO {rem}s";
                _rowPotion.buyButton.interactable = false;
                _rowPotion.buyBg.color = new Color(1f, 0.75f, 0.08f, 1f); // gold while active
                if (_potionStateText != null) _potionStateText.text = "";
            }
            else if (CombatManager.PotionCooldown > 0f)
            {
                int cd  = Mathf.CeilToInt(CombatManager.PotionCooldown);
                int min = cd / 60;
                int sec = cd % 60;
                _rowPotion.costText.text  = $"Recarga {min}:{sec:D2}";
                _rowPotion.buyButton.interactable = false;
                _rowPotion.buyBg.color = GrayBtn;
                if (_potionStateText != null) _potionStateText.text = "";
            }
            else
            {
                _rowPotion.costText.text  = "USAR";
                _rowPotion.buyButton.interactable = true;
                _rowPotion.buyBg.color = RedAccent;
                if (_potionStateText != null) _potionStateText.text = "";
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetRowMaxed(ref UpgradeRowUI row)
        {
            row.costText.text = "MÁX";
            row.buyButton.interactable = false;
            row.buyBg.color = GrayBtn;
        }

        private static void PlayBuy()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayBuy();
        }

        private static void PlayError()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
        }

        private static void ShowToast(string msg)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast(msg, new Color(0.25f, 0.9f, 0.35f, 1f));
        }

        private TextMeshProUGUI MakeText(Transform parent, string goName, string text,
            int size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return tmp;
        }
    }
}
