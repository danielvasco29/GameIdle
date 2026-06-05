using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    // Tycoon-style card: square rarity-framed portrait on the left, name on top,
    // and a capsule "buy bar" that fills emerald as you save up and turns gold
    // when you can afford the next upgrade. Theme: Esmeralda + Ouro.
    public class CharacterButton : MonoBehaviour
    {
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI productionText;
        private TextMeshProUGUI costText;
        private TextMeshProUGUI gainText;     // kept for API; hidden
        private Button upgradeButton;
        private Image backgroundImage;
        private Image iconImage;
        private Image avatarBg;               // inner portrait bg (flashed on hire)
        private Image capsuleFill;            // progress fill
        private RectTransform capsuleFillRT;
        private bool wasAffordable;
        private Coroutine pulseCoroutine;

        private readonly Image[] tierStars = new Image[5];
        private int tierCount;

        private static Sprite Circle()  => UiSpriteFactory.Circle();
        private static Sprite Rounded() => UiSpriteFactory.RoundedBox();

        private CharacterInstance character;
        private int characterIndex;

        // ── Grafite + Verde Neon palette ──────────────────────────────────────
        private static readonly Color CardColor      = new(0.125f, 0.145f, 0.157f, 1f);
        private static readonly Color CardColorReady = new(0.160f, 0.205f, 0.180f, 1f);
        private static readonly Color CapsuleEmpty   = new(0.071f, 0.086f, 0.094f, 1f);
        private static readonly Color FillEmerald    = new(0.157f, 0.588f, 0.392f, 1f);
        private static readonly Color FillReady      = new(0.314f, 0.961f, 0.588f, 1f); // verde neon
        private static readonly Color InnerBg        = new(0.086f, 0.106f, 0.118f, 1f);
        private static readonly Color TextPrimary    = new(0.922f, 0.961f, 0.941f, 1f);
        private static readonly Color TextSecondary  = new(0.667f, 0.784f, 0.725f, 1f);
        private static readonly Color GoldStar       = new(1f,     0.824f, 0.275f, 1f);
        private static readonly Color StarEmpty      = new(0.220f, 0.250f, 0.255f, 1f);

        // Rarity frame colors by tier (1..5)
        private static readonly Color[] RarityColors =
        {
            new(0.40f, 0.46f, 0.42f, 1f), // 1 comum    — slate green
            new(0.27f, 0.67f, 0.43f, 1f), // 2          — green
            new(0.27f, 0.59f, 1.00f, 1f), // 3 raro     — blue
            new(0.67f, 0.35f, 0.92f, 1f), // 4 epico    — purple
            new(1.00f, 0.80f, 0.31f, 1f), // 5 lendario — gold
        };

        // Card geometry (matches the 342px visible card width)
        private const float AvatarSize = 100f;
        private const float AvatarLeft = 9f;
        private const float RightInset = 8f;
        private float RightLeft => AvatarLeft + AvatarSize + 10f; // 119

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

        private TextMeshProUGUI CreateLabel(string goName, Transform parent = null)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent ?? transform, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            var f = ResolveFont();
            if (f != null) tmp.font = f;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            // Clear any previous children (rebuild safe)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                c.SetParent(null);
                Destroy(c.gameObject);
            }

            tierCount = GetStarCount(character.data.baseCost);

            backgroundImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backgroundImage.sprite = Rounded();
            backgroundImage.type   = Image.Type.Sliced;
            backgroundImage.color  = CardColor;

            upgradeButton = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            upgradeButton.targetGraphic = backgroundImage;
            upgradeButton.transition = Selectable.Transition.None;

            BuildAvatar();
            BuildTexts();
            BuildCapsule();

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            var hold = gameObject.GetComponent<HoldButton>() ?? gameObject.AddComponent<HoldButton>();
            hold.Init(OnUpgradeClicked);

            LoadPortrait();
            wasAffordable = false;
            Refresh();
        }

        // ── Square rarity-framed portrait ─────────────────────────────────────
        private void BuildAvatar()
        {
            Color rarity = RarityColors[Mathf.Clamp(tierCount - 1, 0, 4)];

            // Outer frame (rarity colored rounded square)
            var frameGO = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(transform, false);
            var frRT = frameGO.GetComponent<RectTransform>();
            frRT.anchorMin = frRT.anchorMax = frRT.pivot = new Vector2(0f, 0.5f);
            frRT.anchoredPosition = new Vector2(AvatarLeft, 0f);
            frRT.sizeDelta = new Vector2(AvatarSize, AvatarSize);
            var frImg = frameGO.GetComponent<Image>();
            frImg.sprite = Rounded(); frImg.type = Image.Type.Sliced;
            frImg.color = rarity;
            frImg.raycastTarget = false;

            // Inner dark box that also masks the portrait to a rounded square
            var innerGO = new GameObject("Inner", typeof(RectTransform), typeof(Image), typeof(Mask));
            innerGO.transform.SetParent(frameGO.transform, false);
            var inRT = innerGO.GetComponent<RectTransform>();
            inRT.anchorMin = Vector2.zero; inRT.anchorMax = Vector2.one;
            inRT.offsetMin = new Vector2(5f, 5f); inRT.offsetMax = new Vector2(-5f, -5f);
            avatarBg = innerGO.GetComponent<Image>();
            avatarBg.sprite = Rounded(); avatarBg.type = Image.Type.Sliced;
            avatarBg.color = BlendDark(character.data.tintColor);
            avatarBg.raycastTarget = false;
            innerGO.GetComponent<Mask>().showMaskGraphic = true;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(innerGO.transform, false);
            var icRT = iconGO.GetComponent<RectTransform>();
            icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
            icRT.offsetMin = icRT.offsetMax = Vector2.zero;
            iconImage = iconGO.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Level badge (top-left corner, on top of frame, not masked)
            var badgeGO = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(frameGO.transform, false);
            var bRT = badgeGO.GetComponent<RectTransform>();
            bRT.anchorMin = bRT.anchorMax = bRT.pivot = new Vector2(0f, 1f);
            bRT.anchoredPosition = new Vector2(4f, -4f);
            bRT.sizeDelta = new Vector2(46f, 22f);
            var bImg = badgeGO.GetComponent<Image>();
            bImg.sprite = Rounded(); bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.04f, 0.09f, 0.07f, 0.92f);
            bImg.raycastTarget = false;

            levelText = CreateLabel("LevelText", badgeGO.transform);
            var lRT = levelText.rectTransform;
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            levelText.fontSize = 13; levelText.fontStyle = FontStyles.Bold;
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.color = TextSecondary;

            // Stars row at the bottom of the portrait (on top, not masked)
            var starsGO = new GameObject("Stars", typeof(RectTransform));
            starsGO.transform.SetParent(frameGO.transform, false);
            var sRT = starsGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.5f, 0f); sRT.anchorMax = new Vector2(0.5f, 0f);
            sRT.pivot = new Vector2(0.5f, 0f);
            sRT.anchoredPosition = new Vector2(0f, 6f);
            const float ss = 14f, sgap = 2f;
            float totalW = 5 * ss + 4 * sgap;
            sRT.sizeDelta = new Vector2(totalW, ss);
            for (int i = 0; i < 5; i++)
            {
                var st = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                st.transform.SetParent(starsGO.transform, false);
                var stRT = st.GetComponent<RectTransform>();
                stRT.anchorMin = stRT.anchorMax = stRT.pivot = new Vector2(0f, 0.5f);
                stRT.anchoredPosition = new Vector2(i * (ss + sgap), 0f);
                stRT.sizeDelta = new Vector2(ss, ss);
                var stImg = st.GetComponent<Image>();
                stImg.sprite = UiSpriteFactory.Star();
                stImg.color = i < tierCount ? GoldStar : StarEmpty;
                stImg.raycastTarget = false;
                tierStars[i] = stImg;
            }
        }

        // ── Name (top of right column) ────────────────────────────────────────
        private void BuildTexts()
        {
            nameText = CreateLabel("NameText");
            SetAnchors(nameText.rectTransform,
                new Vector2(0f, 0.60f), new Vector2(1f, 1f),
                new Vector2(RightLeft, 0f), new Vector2(-RightInset, -6f));
            nameText.fontSize = 21;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.BottomLeft;
            nameText.color = TextPrimary;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        // ── Capsule buy bar (bottom of right column) ──────────────────────────
        private void BuildCapsule()
        {
            var capGO = new GameObject("Capsule", typeof(RectTransform), typeof(Image));
            capGO.transform.SetParent(transform, false);
            var capRT = capGO.GetComponent<RectTransform>();
            SetAnchors(capRT,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.54f),
                new Vector2(RightLeft, 0f), new Vector2(-RightInset, 0f));
            var capImg = capGO.GetComponent<Image>();
            capImg.sprite = Rounded(); capImg.type = Image.Type.Sliced;
            capImg.color = CapsuleEmpty;
            capImg.raycastTarget = false;

            // Fill (width driven by progress through anchorMax.x)
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(capGO.transform, false);
            capsuleFillRT = fillGO.GetComponent<RectTransform>();
            capsuleFillRT.anchorMin = new Vector2(0f, 0f);
            capsuleFillRT.anchorMax = new Vector2(0.5f, 1f);
            capsuleFillRT.offsetMin = new Vector2(3f, 3f);
            capsuleFillRT.offsetMax = new Vector2(-3f, -3f);
            capsuleFill = fillGO.GetComponent<Image>();
            capsuleFill.sprite = Rounded(); capsuleFill.type = Image.Type.Sliced;
            capsuleFill.color = FillEmerald;
            capsuleFill.raycastTarget = false;

            // Production hint (left, over fill)
            productionText = CreateLabel("ProductionText", capGO.transform);
            SetAnchors(productionText.rectTransform,
                new Vector2(0f, 0f), new Vector2(0.52f, 1f),
                new Vector2(14f, 0f), new Vector2(0f, 0f));
            productionText.fontSize = 14;
            productionText.fontStyle = FontStyles.Bold;
            productionText.alignment = TextAlignmentOptions.MidlineLeft;
            productionText.color = Color.white;
            productionText.textWrappingMode = TextWrappingModes.NoWrap;
            productionText.overflowMode = TextOverflowModes.Ellipsis;

            // Cost (right, big)
            costText = CreateLabel("CostText", capGO.transform);
            SetAnchors(costText.rectTransform,
                new Vector2(0.46f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(-14f, 0f));
            costText.fontSize = 19;
            costText.fontStyle = FontStyles.Bold;
            costText.alignment = TextAlignmentOptions.MidlineRight;
            costText.color = Color.white;
            costText.textWrappingMode = TextWrappingModes.NoWrap;
            costText.overflowMode = TextOverflowModes.Ellipsis;

            // Unused but kept so API/refresh stays simple
            gainText = CreateLabel("GainText", capGO.transform);
            gainText.gameObject.SetActive(false);
        }

        private void LoadPortrait()
        {
            var tex = Resources.Load<Texture2D>($"Characters/Sprites/{character.data.characterId}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"Characters/Sprites/{character.data.characterName}");

            if (tex != null)
            {
                tex = SpriteBackgroundRemover.Process(tex);
                int fw = tex.width, fh = tex.height;
                if (tex.width >= tex.height * 2)
                    fw = tex.width / Mathf.Max(2, Mathf.RoundToInt((float)tex.width / tex.height));
                iconImage.sprite = Sprite.Create(tex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0.5f));
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = new Color(0f, 0f, 0f, 0f);
                var initial = CreateLabel("Initial", iconImage.transform);
                var trt = initial.rectTransform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
                initial.text = character.data.characterName.Length > 0
                    ? character.data.characterName[0].ToString().ToUpper() : "?";
                initial.fontSize = 46; initial.fontStyle = FontStyles.Bold;
                initial.alignment = TextAlignmentOptions.Center;
                initial.color = Color.white;
            }
        }

        private static Color BlendDark(Color c) =>
            Color.Lerp(c, InnerBg, 0.55f);

        private static int GetStarCount(double baseCost)
        {
            if (baseCost >= 100_000) return 5;
            if (baseCost >= 10_000)  return 4;
            if (baseCost >= 1_000)   return 3;
            if (baseCost >= 100)     return 2;
            return 1;
        }

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
            bImg.color  = new Color(BlueAccent.r, BlueAccent.g, BlueAccent.b, 0.20f);
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
                levelText.color     = new Color(0.7f, 0.85f, 1f, 1f);
                levelText.raycastTarget = false;
                levelText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

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

        private const float rightInsetBar = 18f;

        public void Refresh()
        {
            if (character == null) return;

            bool maxedOut  = character.IsAtSafeCap();
            int buyCount   = CharacterManager.Instance.GetPurchaseCount(characterIndex);
            double buyCost = CharacterManager.Instance.GetPurchaseCost(characterIndex);

            if (nameText != null)  nameText.text  = character.data.characterName;
            if (levelText != null) levelText.text = character.level.ToString();

            if (costText != null)
            {
                costText.text = maxedOut
                    ? "MAX"
                    : buyCount > 1
                        ? $"${NumberFormatter.Format(buyCost)} <size=68%>x{buyCount}</size>"
                        : $"${NumberFormatter.Format(buyCost)}";
            }

            if (productionText != null)
            {
                productionText.text = character.data.type switch
                {
                    CharacterType.Multiplier => $"x{character.GetCurrentMultiplier():F2}",
                    _                        => $"+{NumberFormatter.Format(character.GetCurrentProduction())}/s"
                };
            }

            bool affordable = !maxedOut && buyCount >= 1 && GameManager.Instance.Money >= buyCost;

            // Capsule fill: progress toward affording the next purchase
            float fill = maxedOut ? 1f
                : (buyCost > 0 ? Mathf.Clamp01((float)(GameManager.Instance.Money / buyCost)) : 0f);
            if (capsuleFillRT != null)
                capsuleFillRT.anchorMax = new Vector2(Mathf.Max(0.0001f, fill), 1f);
            if (capsuleFill != null)
                capsuleFill.color = affordable || maxedOut ? FillReady : FillEmerald;

            // Keep text readable: dark over the full gold fill, white otherwise
            if (costText != null)
                costText.color = (affordable || maxedOut)
                    ? new Color(0.10f, 0.16f, 0.10f, 1f)
                    : Color.white;
            if (productionText != null)
                productionText.color = (affordable || maxedOut)
                    ? new Color(0.10f, 0.16f, 0.10f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.92f);

            if (backgroundImage != null)
                backgroundImage.color = affordable ? CardColorReady : CardColor;

            if (affordableIndicator != null)
                affordableIndicator.color = affordable
                    ? new Color(0.35f, 0.75f, 1f, 1f)   // cyan-blue when affordable
                    : new Color(0f, 0f, 0f, 0f);         // invisible otherwise

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
            if (UIManager.Instance != null)
                UIManager.Instance.ShowHiredCelebration(character.data.characterName);

            if (avatarBg != null)
            {
                Color orig = avatarBg.color;
                Color gold = FillReady;
                for (int i = 0; i < 2; i++)
                {
                    float t = 0f;
                    while (t < 0.12f) { t += Time.deltaTime; avatarBg.color = Color.Lerp(orig, gold, t / 0.12f); yield return null; }
                    t = 0f;
                    while (t < 0.12f) { t += Time.deltaTime; avatarBg.color = Color.Lerp(gold, orig, t / 0.12f); yield return null; }
                }
                avatarBg.color = orig;
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
                    backgroundImage.color = Color.Lerp(new Color(0.30f, 0.65f, 0.45f, 1f), CardColorReady, t / 0.12f);
                    yield return null;
                }
                backgroundImage.color = CardColor;
            }
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private IEnumerator BecameAffordablePulse()
        {
            var flash = new Color(0.30f, 0.70f, 0.48f, 1f);
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
            while (e < half) { e += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.06f, e / half); yield return null; }
            e = 0f;
            while (e < half) { e += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.one * 1.06f, Vector3.one, e / half); yield return null; }
            transform.localScale = Vector3.one;
            pulseCoroutine = null;
        }
    }
}
