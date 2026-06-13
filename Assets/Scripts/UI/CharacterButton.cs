using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class CharacterButton : MonoBehaviour
    {
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI productionText;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI gainText;
        private Button upgradeButton;
        private Image iconImage;
        private Image backgroundImage;
        private Image avatarBg;
        private Image affordableIndicator;
        private Image costProgressBar;
        private bool wasAffordable;
        private Coroutine pulseCoroutine;

        private readonly Image[] tierDots = new Image[5];
        private int tierCount;

        // Runtime-generated UI sprites (no dependency on Unity built-in resources)
        private static Sprite GetCircleSprite()  => UiSpriteFactory.Circle();
        private static Sprite GetRoundedSprite() => UiSpriteFactory.RoundedBox();

        private CharacterInstance character;
        private int characterIndex;

        // Navy theme palette — matches the "Bem-vindo de volta" panel (translucent glass)
        private static readonly Color CardColor       = new(0.055f, 0.094f, 0.165f, 0.85f); // glass navy
        private static readonly Color CardColorReady  = new(0.090f, 0.150f, 0.250f, 0.88f); // brighter when affordable
        private static readonly Color GoldColor       = new(1f, 0.808f, 0.227f, 1f);     // #ffce3a
        private static readonly Color GreenColor      = new(0.247f, 0.749f, 0.353f, 1f); // #3fbf5a
        private static readonly Color StarEmpty       = new(0.227f, 0.290f, 0.388f, 1f); // #3a4a63
        private static readonly Color TextPrimary     = new(0.933f, 0.953f, 0.980f, 1f); // #eef3fa
        private static readonly Color TextSecondary   = new(0.624f, 0.698f, 0.788f, 1f); // #9fb2c9
        private static readonly Color BlueAccent      = new(0.290f, 0.620f, 1f,    1f);  // #4a9eff

        private static TMP_FontAsset sharedFont;

        private static TMP_FontAsset ResolveFont()
        {
            if (sharedFont != null) return sharedFont;
            sharedFont = TMP_Settings.defaultFontAsset;
            if (sharedFont == null)
            {
                var rf = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (rf != null && rf.font != null) sharedFont = rf.font;
            }
            return sharedFont;
        }

        private TextMeshProUGUI CreateLabel(string goName)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            var f = ResolveFont();
            if (f != null) tmp.font = f;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void AutoFindComponents()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                switch (child.name)
                {
                    case "NameText": case "LevelText": case "ProductionText":
                    case "CostText": case "GainText": case "StarsText": case "UpgradeLabel":
                    case "TierDots":
                        child.SetParent(null);
                        Destroy(child.gameObject);
                        break;
                }
            }

            nameText       = CreateLabel("NameText");
            levelText      = CreateLabel("LevelText");
            productionText = CreateLabel("ProductionText");
            costText       = CreateLabel("CostText");
            gainText       = CreateLabel("GainText");

            backgroundImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            upgradeButton   = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            upgradeButton.targetGraphic = backgroundImage;
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            AutoFindComponents();

            tierCount = GetStarCount(character.data.baseCost);
            _rarity   = RarityColor(tierCount);

            var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 108;
            le.flexibleHeight = 0;

            // Rounded card background
            backgroundImage.sprite = GetRoundedSprite();
            backgroundImage.type   = Image.Type.Sliced;
            backgroundImage.color  = CardColor;

            SetupAvatar();
            SetupAffordableIndicator();
            SetupTierDots();
            ApplyCardLayout();
            SetupLevelBadge();
            LoadPortrait();

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

                var hold = gameObject.GetComponent<HoldButton>() ?? gameObject.AddComponent<HoldButton>();
                hold.Init(OnUpgradeClicked);
            }

            wasAffordable = false;
            SetupCostProgressBar();
            Refresh();
        }

        private void SetupAvatar()
        {
            foreach (var n in new[] { "AvatarRing", "AvatarBg", "Icon", "AvatarShadow" })
            {
                var ex = transform.Find(n);
                if (ex != null) { ex.SetParent(null); Destroy(ex.gameObject); }
            }

            // Sombra suave no chao, para "aterrar" o personagem solto na cena
            var shadowGO = new GameObject("AvatarShadow", typeof(RectTransform), typeof(Image));
            shadowGO.transform.SetParent(transform, false);
            var shRT = shadowGO.GetComponent<RectTransform>();
            shRT.anchorMin = shRT.anchorMax = new Vector2(0f, 0.5f);
            shRT.pivot = new Vector2(0.5f, 0.5f);
            shRT.anchoredPosition = new Vector2(52f, -34f);
            shRT.sizeDelta = new Vector2(58f, 14f);
            var shImg = shadowGO.GetComponent<Image>();
            shImg.sprite = GetCircleSprite();
            shImg.color  = new Color(0f, 0f, 0f, 0.22f);
            shImg.raycastTarget = false;

            // Personagem solto — sem circulo nem mascara, fundo transparente
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(52f, 2f);
            iconRT.sizeDelta = new Vector2(92f, 92f);
            iconImage = iconGO.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            avatarBg = null; // sem fundo circular; HiredEffect agora pisca o proprio sprite
        }

        private void SetupTierDots()
        {
            var ex = transform.Find("TierDots");
            if (ex != null) { ex.SetParent(null); Destroy(ex.gameObject); }

            tierCount = GetStarCount(character.data.baseCost);
            _rarity   = RarityColor(tierCount);

            var rowGO = new GameObject("TierDots", typeof(RectTransform));
            rowGO.transform.SetParent(transform, false);
            var rowRT = rowGO.GetComponent<RectTransform>();
            // Positioned in ApplyCardLayout
            rowRT.anchorMin = new Vector2(0f, 0.40f);
            rowRT.anchorMax = new Vector2(1f, 0.60f);

            const float dot = 15f, gap = 3f;
            for (int i = 0; i < 5; i++)
            {
                var d = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                d.transform.SetParent(rowGO.transform, false);
                var drt = d.GetComponent<RectTransform>();
                drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0f, 0.5f);
                drt.anchoredPosition = new Vector2(i * (dot + gap), 0f);
                drt.sizeDelta = new Vector2(dot, dot);
                var img = d.GetComponent<Image>();
                img.sprite = UiSpriteFactory.Star();
                img.color  = i < tierCount ? GoldColor : StarEmpty;
                img.raycastTarget = false;
                tierDots[i] = img;
            }
        }

        private void SetupAffordableIndicator()
        {
            var go = new GameObject("AffordableBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(6f, 0f);
            affordableIndicator = go.GetComponent<Image>();
            // Rarity-colored accent: always visible, brightens when affordable.
            affordableIndicator.color = new Color(_rarity.r, _rarity.g, _rarity.b, 0.55f);
            affordableIndicator.raycastTarget = false;
        }

        private void LoadPortrait()
        {
            // Try characterId first, then characterName (for "AI Engineer" with space)
            var tex = Resources.Load<Texture2D>($"Characters/Sprites/{character.data.characterId}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"Characters/Sprites/{character.data.characterName}");

            if (tex != null)
            {
                // Remove o fundo branco/xadrez para o personagem ficar solto na cena
                tex = SpriteBackgroundRemover.Process(tex);

                // Character sheets are a 12-column x 2-row grid (top: walk cycle,
                // bottom: work pose). Earlier code assumed square single-row frames
                // and ended up cropping several characters at once. Grab the single
                // top-left frame for a clean standing portrait.
                int fw = tex.width;
                int fh = tex.height;
                int x = 0, y = 0;
                if (tex.width >= tex.height * 3)        // wide multi-frame sheet (~4:1)
                {
                    fw = tex.width  / 12;               // 12 columns
                    fh = tex.height / 2;                // 2 rows
                    y  = tex.height - fh;               // top row (y origin = bottom)
                }
                else if (tex.width >= tex.height * 2)   // single-row strip fallback
                {
                    fw = tex.width / Mathf.Max(2, Mathf.RoundToInt((float)tex.width / tex.height));
                }
                iconImage.sprite = Sprite.Create(tex, new Rect(x, y, fw, fh), new Vector2(0.5f, 0.5f));
                iconImage.color  = Color.white;
                // hide initial letter
                var init = transform.Find("Icon/Initial");
                if (init != null) init.gameObject.SetActive(false);
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color  = new Color(0f, 0f, 0f, 0f);
                if (transform.Find("Icon/Initial") == null)
                {
                    var initialGO = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
                    initialGO.transform.SetParent(iconImage.transform, false);
                    var trt = initialGO.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                    trt.offsetMin = trt.offsetMax = Vector2.zero;
                    var ttmp = initialGO.GetComponent<TextMeshProUGUI>();
                    ttmp.text = character.data.characterName.Length > 0
                        ? character.data.characterName[0].ToString().ToUpper() : "?";
                    ttmp.fontSize  = 36;
                    ttmp.fontStyle = FontStyles.Bold;
                    ttmp.alignment = TextAlignmentOptions.Center;
                    ttmp.color     = Color.white;
                    ttmp.raycastTarget = false;
                    var fi = ResolveFont();
                    if (fi != null) ttmp.font = fi;
                }
            }
        }

        private static Color BlendWithNavy(Color c, float t) =>
            Color.Lerp(c, new Color(0.106f, 0.169f, 0.275f), t);

        private static int GetStarCount(double baseCost)
        {
            if (baseCost >= 100_000) return 5;
            if (baseCost >= 10_000)  return 4;
            if (baseCost >= 1_000)   return 3;
            if (baseCost >= 100)     return 2;
            return 1;
        }

        // Rarity tint by tier — common -> legendary. Drives the left accent
        // strip, the level badge and a faint card wash so higher-tier hires
        // read as more prestigious at a glance.
        private static Color RarityColor(int tier) => tier switch
        {
            5 => new Color(1f,    0.808f, 0.227f, 1f), // gold   — legendary
            4 => new Color(0.643f, 0.369f, 0.910f, 1f), // purple — epic
            3 => new Color(0.290f, 0.620f, 1f,    1f), // blue   — rare
            2 => new Color(0.247f, 0.749f, 0.353f, 1f), // green  — uncommon
            _ => new Color(0.435f, 0.518f, 0.624f, 1f), // slate  — common
        };

        private Color _rarity;

        private static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        private void ApplyCardLayout()
        {
            const float avatarW   = 104f;  // 10px margin + 84px avatar
            const float rightW    = 124f;
            const float rightInset = 18f; // clears the vertical scrollbar on the right

            if (nameText != null)
            {
                SetAnchors(nameText.rectTransform,
                    new Vector2(0f, 0.58f), new Vector2(1f, 1f),
                    new Vector2(avatarW, 0f), new Vector2(-rightW, -4f));
                nameText.fontSize  = 18;
                nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.BottomLeft;
                nameText.color     = TextPrimary;
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
                nameText.overflowMode    = TextOverflowModes.Ellipsis;
            }

            var dotsRow = transform.Find("TierDots")?.GetComponent<RectTransform>();
            if (dotsRow != null)
                SetAnchors(dotsRow,
                    new Vector2(0f, 0.40f), new Vector2(1f, 0.60f),
                    new Vector2(avatarW, 0f), new Vector2(-rightW, 0f));

            if (productionText != null)
            {
                SetAnchors(productionText.rectTransform,
                    new Vector2(0f, 0.05f), new Vector2(1f, 0.42f),
                    new Vector2(avatarW, 0f), new Vector2(-rightW, 0f));
                productionText.fontSize  = 15;
                productionText.fontStyle = FontStyles.Bold;
                productionText.alignment = TextAlignmentOptions.MidlineLeft;
                productionText.color     = GreenColor;
                productionText.textWrappingMode = TextWrappingModes.NoWrap;
                productionText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // Cost — gold, right column, just under the level badge
            if (costText != null)
            {
                SetAnchors(costText.rectTransform,
                    new Vector2(1f, 0.34f), new Vector2(1f, 0.66f),
                    new Vector2(-rightW, 0f), new Vector2(-rightInset, 0f));
                costText.fontSize  = 20;
                costText.fontStyle = FontStyles.Bold;
                costText.alignment = TextAlignmentOptions.MidlineRight;
                costText.color     = GoldColor;
                costText.textWrappingMode = TextWrappingModes.NoWrap;
                costText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // Income gain preview — green, below the cost ("+X/s ao comprar")
            if (gainText != null)
            {
                SetAnchors(gainText.rectTransform,
                    new Vector2(1f, 0.04f), new Vector2(1f, 0.34f),
                    new Vector2(-rightW, 2f), new Vector2(-rightInset, 0f));
                gainText.fontSize  = 13;
                gainText.fontStyle = FontStyles.Bold;
                gainText.alignment = TextAlignmentOptions.MidlineRight;
                gainText.color     = GreenColor;
                gainText.textWrappingMode = TextWrappingModes.NoWrap;
                gainText.overflowMode     = TextOverflowModes.Ellipsis;
            }
        }

        // Small rounded blue badge in the top-right corner showing the level.
        private void SetupLevelBadge()
        {
            const float rightInset = 18f;

            var ex = transform.Find("LevelBadge");
            if (ex != null) { ex.SetParent(null); Destroy(ex.gameObject); }

            var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-rightInset, -8f);
            brt.sizeDelta = new Vector2(74f, 24f);
            var bImg = badge.GetComponent<Image>();
            bImg.sprite = GetRoundedSprite();
            bImg.type   = Image.Type.Sliced;
            bImg.color  = new Color(_rarity.r, _rarity.g, _rarity.b, 0.22f);
            bImg.raycastTarget = false;

            if (levelText != null)
            {
                levelText.transform.SetParent(badge.transform, false);
                var lrt = levelText.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                levelText.fontSize  = 14;
                levelText.fontStyle = FontStyles.Bold;
                levelText.alignment = TextAlignmentOptions.Center;
                levelText.color     = Color.Lerp(_rarity, Color.white, 0.55f);
                levelText.raycastTarget = false;
                levelText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private const float rightInsetBar = 18f;

        private void SetupCostProgressBar()
        {
            // Trilho de fundo (escuro) — dá a sensação de barra "encaixada"
            var trackGO = new GameObject("CostTrack", typeof(RectTransform), typeof(Image));
            trackGO.transform.SetParent(transform, false);
            var trt = trackGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f);
            trt.offsetMin = new Vector2(10f, 4f); trt.offsetMax = new Vector2(-rightInsetBar, 8f);
            var trackImg = trackGO.GetComponent<Image>();
            trackImg.sprite = GetRoundedSprite(); trackImg.type = Image.Type.Sliced;
            trackImg.color = new Color(0f, 0f, 0f, 0.30f);
            trackImg.raycastTarget = false;

            var barGO = new GameObject("CostBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(trackGO.transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            costProgressBar = barGO.GetComponent<Image>();
            costProgressBar.sprite     = GetRoundedSprite();
            costProgressBar.type       = Image.Type.Filled;
            costProgressBar.fillMethod = Image.FillMethod.Horizontal;
            costProgressBar.raycastTarget = false;
        }

        public void Refresh()
        {
            if (character == null) return;

            bool maxedOut  = character.IsAtSafeCap();
            int buyCount   = CharacterManager.Instance.GetPurchaseCount(characterIndex);
            double buyCost = CharacterManager.Instance.GetPurchaseCost(characterIndex);

            if (nameText != null)       { nameText.text       = character.data.characterName; nameText.ForceMeshUpdate(); }
            if (levelText != null)      { levelText.text      = $"Nv. {character.level}";      levelText.ForceMeshUpdate(); }
            if (costText != null)
            {
                costText.text = maxedOut
                    ? "MÁX"
                    : buyCount > 1
                        ? $"${NumberFormatter.Format(buyCost)} <size=70%><color=#9fb2c9>x{buyCount}</color></size>"
                        : $"${NumberFormatter.Format(buyCost)}";
                costText.ForceMeshUpdate();
            }

            if (gainText != null)
            {
                double gain = maxedOut ? 0 : CharacterManager.Instance.GetIncomeGain(characterIndex);
                gainText.text = gain > 0 ? $"+{NumberFormatter.Format(gain)}/s" : "";
                gainText.ForceMeshUpdate();
            }

            if (productionText != null)
            {
                productionText.text = character.data.type switch
                {
                    CharacterType.Multiplier => $"x{character.GetCurrentMultiplier():F2} total",
                    _                        => $"+{NumberFormatter.Format(character.GetCurrentProduction())}/s"
                };
                productionText.ForceMeshUpdate();
            }

            bool affordable = !maxedOut && buyCount >= 1 && GameManager.Instance.Money >= buyCost;

            if (backgroundImage != null)
                backgroundImage.color = affordable ? CardColorReady : CardColor;

            if (affordableIndicator != null)
                affordableIndicator.color = affordable
                    ? new Color(_rarity.r, _rarity.g, _rarity.b, 1f)    // full rarity glow when affordable
                    : new Color(_rarity.r, _rarity.g, _rarity.b, 0.50f); // dim rarity tint otherwise

            if (costProgressBar != null)
            {
                float fill = Mathf.Clamp01((float)(GameManager.Instance.Money / buyCost));
                costProgressBar.fillAmount = fill;
                costProgressBar.color = affordable
                    ? new Color(0.35f, 0.75f, 1f, 1f)
                    : new Color(GreenColor.r, GreenColor.g, GreenColor.b, 0.55f);
            }

            // Pulse the card when it first becomes affordable
            if (affordable && !wasAffordable)
            {
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(BecameAffordablePulse());
            }

            wasAffordable = affordable;
        }

        private void OnUpgradeClicked()
        {
            int prevLevel = character.level;
            bool success = CharacterManager.Instance.TryUpgrade(characterIndex);
            if (success)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayBuy();
                GameManager.Instance.IncrementHireCount();
                StartCoroutine(UpgradeEffect());
                if (prevLevel == 0)
                    StartCoroutine(HiredEffect());
            }
            else
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayError();
                UIManager.Instance.ShowToast("Dinheiro insuficiente!", new Color(1f, 0.3f, 0.3f));
            }
        }

        private IEnumerator HiredEffect()
        {
            // Celebração grande no centro do escritório (área não clipada).
            if (UIManager.Instance != null)
                UIManager.Instance.ShowHiredCelebration(character.data.characterName);

            // Flash dourado pulsante no proprio sprite para destacar a contratação
            var flashImg = avatarBg != null ? avatarBg : iconImage;
            if (flashImg != null)
            {
                Color orig = flashImg.color;
                Color gold = new(1f, 0.808f, 0.227f, 1f);
                for (int i = 0; i < 2; i++)
                {
                    float t = 0f;
                    while (t < 0.12f) { t += Time.deltaTime; flashImg.color = Color.Lerp(orig, gold, t / 0.12f); yield return null; }
                    t = 0f;
                    while (t < 0.12f) { t += Time.deltaTime; flashImg.color = Color.Lerp(gold, orig, t / 0.12f); yield return null; }
                }
                flashImg.color = orig;
            }
        }

        private IEnumerator UpgradeEffect()
        {
            if (backgroundImage != null)
            {
                float t = 0f;
                while (t < 0.12f)
                {
                    t += Time.deltaTime;
                    backgroundImage.color = Color.Lerp(new Color(0.55f, 0.80f, 1f, 1f), CardColorReady, t / 0.12f);
                    yield return null;
                }
                backgroundImage.color = CardColor;
            }
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private IEnumerator BecameAffordablePulse()
        {
            // Single gentle flash + small scale nudge
            var flash = new Color(0.40f, 0.78f, 1f, 1f);
            float e = 0f;
            while (e < 0.20f) { e += Time.deltaTime; if (backgroundImage) backgroundImage.color = Color.Lerp(CardColorReady, flash, e / 0.20f); yield return null; }
            e = 0f;
            while (e < 0.30f) { e += Time.deltaTime; if (backgroundImage) backgroundImage.color = Color.Lerp(flash, CardColorReady, e / 0.30f); yield return null; }
            if (backgroundImage) backgroundImage.color = CardColorReady;
            pulseCoroutine = null;
        }

        private IEnumerator PulseCoroutine()
        {
            const float half = 0.15f;
            transform.localScale = Vector3.one;
            float e = 0f;
            while (e < half) { e += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, e / half); yield return null; }
            e = 0f;
            while (e < half) { e += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, e / half); yield return null; }
            transform.localScale = Vector3.one;
            pulseCoroutine = null;
        }
    }
}
