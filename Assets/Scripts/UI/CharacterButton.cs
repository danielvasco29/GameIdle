using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class CharacterButton : MonoBehaviour
    {
        // These fields are populated at runtime — prefab TMP children are discarded
        // in AutoFindComponents() because their serialized state is often broken.
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI productionText;
        private TextMeshProUGUI costText;
        private Button upgradeButton;
        private Image iconImage;
        private Image backgroundImage;

        private CharacterInstance character;
        private int characterIndex;

        private Image costProgressBar;
        private Image glowOverlay;
        private bool wasAffordable;
        private Coroutine pulseCoroutine;

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
            // Discard ALL prefab text children — their serialized TMP state is often
            // broken (null font, wrong color). We always create fresh instances.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                switch (child.name)
                {
                    case "NameText": case "LevelText":
                    case "ProductionText": case "CostText":
                    case "UpgradeLabel":
                        child.SetParent(null); // remove from layout immediately
                        Destroy(child.gameObject);
                        break;
                }
            }

            nameText       = CreateLabel("NameText");
            levelText      = CreateLabel("LevelText");
            productionText = CreateLabel("ProductionText");
            costText       = CreateLabel("CostText");

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                    backgroundImage = gameObject.AddComponent<Image>();
            }

            if (upgradeButton == null)
            {
                upgradeButton = GetComponent<Button>();
                if (upgradeButton == null)
                    upgradeButton = gameObject.AddComponent<Button>();
                upgradeButton.targetGraphic = backgroundImage;
            }
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            AutoFindComponents();

            var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            le.minHeight       = 120;
            le.preferredHeight = 120;
            le.flexibleHeight  = 0;

            if (iconImage == null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(transform, false);
                iconImage = iconGO.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget  = false;
            }

            ApplyCardLayout();

            if (instance.data.icon != null)
            {
                iconImage.sprite = instance.data.icon;
                iconImage.color  = Color.white;
            }
            else
            {
                var tex = Resources.Load<Texture2D>($"Characters/Sprites/{instance.data.characterId}");
                if (tex != null)
                {
                    iconImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    iconImage.color  = Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color  = new Color(0f, 0f, 0f, 0.35f);
                    if (transform.Find("Icon/Initial") == null)
                    {
                        var initialGO = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
                        initialGO.transform.SetParent(iconImage.transform, false);
                        var trt = initialGO.GetComponent<RectTransform>();
                        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                        trt.offsetMin = trt.offsetMax = Vector2.zero;
                        var ttmp = initialGO.GetComponent<TextMeshProUGUI>();
                        ttmp.text      = instance.data.characterName.Length > 0
                                          ? instance.data.characterName[0].ToString().ToUpper() : "?";
                        ttmp.fontSize  = 42;
                        ttmp.fontStyle = FontStyles.Bold;
                        ttmp.alignment = TextAlignmentOptions.Center;
                        ttmp.color     = Color.white;
                        ttmp.raycastTarget = false;
                        var fi = ResolveFont();
                        if (fi != null) ttmp.font = fi;
                    }
                }
            }

            if (backgroundImage != null)
                backgroundImage.color = instance.data.tintColor;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            wasAffordable = false;
            SetupCostProgressBar();
            SetupGlowOverlay();
            Refresh();
        }

        public void Refresh()
        {
            if (character == null) return;

            if (nameText       != null) { nameText.text       = character.data.characterName;                          nameText.ForceMeshUpdate(); }
            if (levelText      != null) { levelText.text      = $"Nv. {character.level}";                             levelText.ForceMeshUpdate(); }
            if (costText       != null) { costText.text       = $"${NumberFormatter.Format(character.GetCurrentCost())}"; costText.ForceMeshUpdate(); }

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

            if (backgroundImage != null && affordable != wasAffordable)
            {
                var c = character.data.tintColor;
                backgroundImage.color = affordable ? Color.Lerp(c, Color.white, 0.08f) : c;
            }

            if (costProgressBar != null)
            {
                float fill = Mathf.Clamp01((float)(GameManager.Instance.Money / character.GetCurrentCost()));
                costProgressBar.fillAmount = fill;
                costProgressBar.color = affordable ? new Color(1f, 0.85f, 0f, 0.9f) : new Color(1f, 1f, 1f, 0.25f);
            }

            if (glowOverlay != null)
                glowOverlay.color = affordable ? new Color(1f, 0.85f, 0f, 0.12f) : new Color(0f, 0f, 0f, 0f);

            wasAffordable = affordable;
        }

        private static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        private void ApplyCardLayout()
        {
            const float iconSize    = 68f;
            const float leftPad     = iconSize + 10f;  // 78px
            const float rightPad    = 8f;

            var iconRT = transform.Find("Icon")?.GetComponent<RectTransform>();
            if (iconRT != null)
            {
                iconRT.anchorMin        = new Vector2(0f, 0.5f);
                iconRT.anchorMax        = new Vector2(0f, 0.5f);
                iconRT.pivot            = new Vector2(0f, 0.5f);
                iconRT.anchoredPosition = new Vector2(8f, 0f);
                iconRT.sizeDelta        = new Vector2(iconSize, iconSize);
            }

            if (nameText != null)
            {
                SetAnchors(nameText.rectTransform,
                    new Vector2(0f, 0.62f), new Vector2(0.78f, 1f),
                    new Vector2(leftPad, 0f), new Vector2(0f, -4f));
                nameText.fontSize  = 17; nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.color     = Color.white;
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
                nameText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (levelText != null)
            {
                SetAnchors(levelText.rectTransform,
                    new Vector2(0.78f, 0.62f), new Vector2(1f, 1f),
                    new Vector2(0f, 0f), new Vector2(-rightPad, -4f));
                levelText.fontSize  = 13; levelText.fontStyle = FontStyles.Bold;
                levelText.alignment = TextAlignmentOptions.MidlineRight;
                levelText.color     = new Color(1f, 1f, 1f, 0.85f);
                levelText.textWrappingMode = TextWrappingModes.NoWrap;
                levelText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (productionText != null)
            {
                SetAnchors(productionText.rectTransform,
                    new Vector2(0f, 0.35f), new Vector2(1f, 0.62f),
                    new Vector2(leftPad, 0f), new Vector2(-rightPad, 0f));
                productionText.fontSize  = 14; productionText.fontStyle = FontStyles.Bold;
                productionText.alignment = TextAlignmentOptions.MidlineLeft;
                productionText.color     = new Color(0.55f, 1f, 0.7f);
                productionText.textWrappingMode = TextWrappingModes.NoWrap;
                productionText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (costText != null)
            {
                SetAnchors(costText.rectTransform,
                    new Vector2(0f, 0.05f), new Vector2(1f, 0.38f),
                    new Vector2(leftPad, 0f), new Vector2(-rightPad, 0f));
                costText.fontSize  = 19; costText.fontStyle = FontStyles.Bold;
                costText.alignment = TextAlignmentOptions.MidlineRight;
                costText.color     = new Color(1f, 0.92f, 0.35f);
                costText.textWrappingMode = TextWrappingModes.NoWrap;
                costText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void SetupGlowOverlay()
        {
            var go = new GameObject("GlowOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling(); // renders behind all text
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            glowOverlay = go.GetComponent<Image>();
            glowOverlay.color = new Color(1f, 0.85f, 0f, 0f);
            glowOverlay.raycastTarget = false;
        }

        private void SetupCostProgressBar()
        {
            var barGO = new GameObject("CostBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, 4f);
            costProgressBar = barGO.GetComponent<Image>();
            costProgressBar.type         = Image.Type.Filled;
            costProgressBar.fillMethod   = Image.FillMethod.Horizontal;
            costProgressBar.raycastTarget = false;
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
                Color base0 = character.data.tintColor;
                var bright = Color.Lerp(base0, Color.white, 0.6f);
                float t = 0f;
                while (t < 0.12f) { t += Time.deltaTime; backgroundImage.color = Color.Lerp(bright, base0, t / 0.12f); yield return null; }
                backgroundImage.color = base0;
            }
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            const float duration = 0.3f;
            const float half     = duration * 0.5f;
            transform.localScale = Vector3.one; // always start from 1
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.15f, elapsed / half);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, elapsed / half);
                yield return null;
            }
            transform.localScale = Vector3.one;
            pulseCoroutine = null;
        }
    }
}
