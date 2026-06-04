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

        // Built-in Unity UI sprites — always available at runtime
        private static Sprite circleSprite;
        private static Sprite roundedSprite;
        private static Sprite GetCircleSprite()
        {
            if (circleSprite == null) circleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            return circleSprite;
        }
        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite == null) roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            return roundedSprite;
        }

        private CharacterInstance character;
        private int characterIndex;

        // Navy theme palette
        private static readonly Color CardColor       = new(0.106f, 0.169f, 0.275f, 1f); // #1b2b46
        private static readonly Color CardColorReady  = new(0.118f, 0.196f, 0.302f, 1f); // slightly lighter when affordable
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
                    case "CostText": case "StarsText": case "UpgradeLabel":
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

            backgroundImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            upgradeButton   = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            upgradeButton.targetGraphic = backgroundImage;
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            AutoFindComponents();

            var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 100;
            le.flexibleHeight = 0;

            // Rounded card background
            backgroundImage.sprite = GetRoundedSprite();
            backgroundImage.type   = Image.Type.Sliced;
            backgroundImage.color  = CardColor;

            SetupAvatar();
            SetupAffordableIndicator();
            SetupTierDots();
            ApplyCardLayout();
            LoadPortrait();

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            wasAffordable = false;
            SetupCostProgressBar();
            Refresh();
        }

        private void SetupAvatar()
        {
            foreach (var n in new[] { "AvatarRing", "AvatarBg", "Icon" })
            {
                var ex = transform.Find(n);
                if (ex != null) { ex.SetParent(null); Destroy(ex.gameObject); }
            }

            // Gold ring (behind), child of card so the mask below doesn't clip it
            var ringGO = new GameObject("AvatarRing", typeof(RectTransform), typeof(Image));
            ringGO.transform.SetParent(transform, false);
            var ringRT = ringGO.GetComponent<RectTransform>();
            ringRT.anchorMin = ringRT.anchorMax = ringRT.pivot = new Vector2(0f, 0.5f);
            ringRT.anchoredPosition = new Vector2(7f, 0f);
            ringRT.sizeDelta = new Vector2(82f, 82f);
            var ringImg = ringGO.GetComponent<Image>();
            ringImg.sprite = GetCircleSprite();
            ringImg.color  = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.5f);
            ringImg.raycastTarget = false;

            // Circular avatar background that also masks the portrait
            var bgGO = new GameObject("AvatarBg", typeof(RectTransform), typeof(Image), typeof(Mask));
            bgGO.transform.SetParent(transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = bgRT.anchorMax = bgRT.pivot = new Vector2(0f, 0.5f);
            bgRT.anchoredPosition = new Vector2(10f, 0f);
            bgRT.sizeDelta = new Vector2(76f, 76f);
            avatarBg = bgGO.GetComponent<Image>();
            avatarBg.sprite = GetCircleSprite();
            avatarBg.type   = Image.Type.Simple;
            avatarBg.color  = BlendWithNavy(character.data.tintColor, 0.3f);
            avatarBg.raycastTarget = false;
            bgGO.GetComponent<Mask>().showMaskGraphic = true;

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(bgGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            iconImage = iconGO.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        private void SetupTierDots()
        {
            var ex = transform.Find("TierDots");
            if (ex != null) { ex.SetParent(null); Destroy(ex.gameObject); }

            tierCount = GetStarCount(character.data.baseCost);

            var rowGO = new GameObject("TierDots", typeof(RectTransform));
            rowGO.transform.SetParent(transform, false);
            var rowRT = rowGO.GetComponent<RectTransform>();
            // Positioned in ApplyCardLayout
            rowRT.anchorMin = new Vector2(0f, 0.40f);
            rowRT.anchorMax = new Vector2(1f, 0.60f);

            const float dot = 11f, gap = 4f;
            for (int i = 0; i < 5; i++)
            {
                var d = new GameObject($"Dot{i}", typeof(RectTransform), typeof(Image));
                d.transform.SetParent(rowGO.transform, false);
                var drt = d.GetComponent<RectTransform>();
                drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0f, 0.5f);
                drt.anchoredPosition = new Vector2(i * (dot + gap), 0f);
                drt.sizeDelta = new Vector2(dot, dot);
                var img = d.GetComponent<Image>();
                img.sprite = GetCircleSprite();
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
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(4f, 0f);
            affordableIndicator = go.GetComponent<Image>();
            affordableIndicator.color = new Color(GreenColor.r, GreenColor.g, GreenColor.b, 0f);
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
                iconImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                iconImage.color  = Color.white;
                // hide initial letter
                var init = transform.Find("AvatarBg/Icon/Initial");
                if (init != null) init.gameObject.SetActive(false);
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color  = new Color(0f, 0f, 0f, 0f);
                if (transform.Find("AvatarBg/Icon/Initial") == null)
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

        private static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        private void ApplyCardLayout()
        {
            const float avatarW  = 96f;  // 10px margin + 76px avatar
            const float rightW   = 110f;
            const float pad      = 8f;

            if (nameText != null)
            {
                SetAnchors(nameText.rectTransform,
                    new Vector2(0f, 0.58f), new Vector2(1f, 1f),
                    new Vector2(avatarW, 0f), new Vector2(-rightW, -4f));
                nameText.fontSize  = 16;
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
                productionText.fontSize  = 14;
                productionText.fontStyle = FontStyles.Bold;
                productionText.alignment = TextAlignmentOptions.MidlineLeft;
                productionText.color     = GreenColor;
                productionText.textWrappingMode = TextWrappingModes.NoWrap;
                productionText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // Level badge top-right
            if (levelText != null)
            {
                SetAnchors(levelText.rectTransform,
                    new Vector2(1f, 0.55f), new Vector2(1f, 1f),
                    new Vector2(-rightW + pad, 0f), new Vector2(-pad, -4f));
                levelText.fontSize  = 12;
                levelText.fontStyle = FontStyles.Bold;
                levelText.alignment = TextAlignmentOptions.BottomRight;
                levelText.color     = BlueAccent;
                levelText.textWrappingMode = TextWrappingModes.NoWrap;
                levelText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // Cost bottom-right — gold, large
            if (costText != null)
            {
                SetAnchors(costText.rectTransform,
                    new Vector2(1f, 0f), new Vector2(1f, 0.58f),
                    new Vector2(-rightW + pad, 4f), new Vector2(-pad, 0f));
                costText.fontSize  = 17;
                costText.fontStyle = FontStyles.Bold;
                costText.alignment = TextAlignmentOptions.MidlineRight;
                costText.color     = GoldColor;
                costText.textWrappingMode = TextWrappingModes.NoWrap;
                costText.overflowMode     = TextOverflowModes.Ellipsis;
            }
        }

        private void SetupCostProgressBar()
        {
            var barGO = new GameObject("CostBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, 3f);
            costProgressBar = barGO.GetComponent<Image>();
            costProgressBar.type       = Image.Type.Filled;
            costProgressBar.fillMethod = Image.FillMethod.Horizontal;
            costProgressBar.raycastTarget = false;
        }

        public void Refresh()
        {
            if (character == null) return;

            if (nameText != null)       { nameText.text       = character.data.characterName;                              nameText.ForceMeshUpdate(); }
            if (levelText != null)      { levelText.text      = $"Nv. {character.level}";                                 levelText.ForceMeshUpdate(); }
            if (costText != null)       { costText.text       = $"${NumberFormatter.Format(character.GetCurrentCost())}"; costText.ForceMeshUpdate(); }

            if (productionText != null)
            {
                productionText.text = character.data.type switch
                {
                    CharacterType.Multiplier => $"x{character.GetCurrentMultiplier():F2} total",
                    _                        => $"+{NumberFormatter.Format(character.GetCurrentProduction())}/s"
                };
                productionText.ForceMeshUpdate();
            }

            bool affordable = GameManager.Instance.Money >= character.GetCurrentCost();

            if (backgroundImage != null)
                backgroundImage.color = affordable ? CardColorReady : CardColor;

            if (affordableIndicator != null)
                affordableIndicator.color = affordable
                    ? new Color(GreenColor.r, GreenColor.g, GreenColor.b, 1f)
                    : new Color(GreenColor.r, GreenColor.g, GreenColor.b, 0f);

            if (costProgressBar != null)
            {
                float fill = Mathf.Clamp01((float)(GameManager.Instance.Money / character.GetCurrentCost()));
                costProgressBar.fillAmount = fill;
                costProgressBar.color = affordable
                    ? new Color(GreenColor.r, GreenColor.g, GreenColor.b, 0.85f)
                    : new Color(1f, 1f, 1f, 0.15f);
            }

            wasAffordable = affordable;
        }

        private void OnUpgradeClicked()
        {
            bool success = CharacterManager.Instance.TryUpgrade(characterIndex);
            if (success)
                StartCoroutine(UpgradeEffect());
            else
                UIManager.Instance.ShowToast("Dinheiro insuficiente!", new Color(1f, 0.3f, 0.3f));
        }

        private IEnumerator UpgradeEffect()
        {
            if (backgroundImage != null)
            {
                float t = 0f;
                while (t < 0.12f)
                {
                    t += Time.deltaTime;
                    backgroundImage.color = Color.Lerp(Color.white, CardColor, t / 0.12f);
                    yield return null;
                }
                backgroundImage.color = CardColor;
            }
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCoroutine());
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
