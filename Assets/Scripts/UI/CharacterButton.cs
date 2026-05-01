using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class CharacterButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI productionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;

        private CharacterInstance character;
        private int characterIndex;

        private Image costProgressBar;
        private Image glowOverlay;
        private Coroutine glowCoroutine;
        private bool wasAffordable;

        private static TMP_FontAsset sharedFont;

        private TextMeshProUGUI GetOrAddTMP(string childName)
        {
            var go = transform.Find(childName);
            if (go == null) return null;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                if (sharedFont == null)
                {
                    var ref2 = Object.FindAnyObjectByType<TextMeshProUGUI>();
                    if (ref2 != null && ref2.font != null) sharedFont = ref2.font;
                }
                if (sharedFont != null) tmp.font = sharedFont;
            }
            return tmp;
        }

        private void AutoFindComponents()
        {
            nameText       = nameText       ?? GetOrAddTMP("NameText");
            levelText      = levelText      ?? GetOrAddTMP("LevelText");
            productionText = productionText ?? GetOrAddTMP("ProductionText");
            costText       = costText       ?? GetOrAddTMP("CostText");

            // Background no root do card (se Image faltar, adiciona)
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                    backgroundImage = gameObject.AddComponent<Image>();
            }

            // Botão de upgrade no root do card (se Button faltar, adiciona)
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

            // GridLayoutGroup controla o tamanho — LayoutElement serve de fallback
            var le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.minHeight       = 155;
            le.preferredHeight = 155;
            le.flexibleHeight  = 0;

            // Cria Icon antes de ApplyCardLayout para que ele configure o RT
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
                // Tenta carregar sprite de Resources/Characters/Sprites/{characterId}
                Sprite fallbackSprite = null;
                var tex = Resources.Load<Texture2D>($"Characters/Sprites/{instance.data.characterId}");
                if (tex != null)
                    fallbackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                if (fallbackSprite != null)
                {
                    iconImage.sprite = fallbackSprite;
                    iconImage.color  = Color.white;
                }
                else
                {
                    // Placeholder: círculo escuro com inicial do nome
                    iconImage.sprite = null;
                    iconImage.color  = new Color(0f, 0f, 0f, 0.35f);
                    if (transform.Find("Icon/Initial") == null)
                    {
                        var initialGO = new GameObject("Initial", typeof(RectTransform), typeof(TextMeshProUGUI));
                        initialGO.transform.SetParent(iconImage.transform, false);
                        var trt = initialGO.GetComponent<RectTransform>();
                        trt.anchorMin = Vector2.zero;
                        trt.anchorMax = Vector2.one;
                        trt.offsetMin = Vector2.zero;
                        trt.offsetMax = Vector2.zero;
                        var ttmp = initialGO.GetComponent<TextMeshProUGUI>();
                        ttmp.text          = instance.data.characterName.Length > 0
                                              ? instance.data.characterName[0].ToString().ToUpper()
                                              : "?";
                        ttmp.fontSize      = 42;
                        ttmp.fontStyle     = FontStyles.Bold;
                        ttmp.alignment     = TextAlignmentOptions.Center;
                        ttmp.color         = Color.white;
                        ttmp.raycastTarget = false;
                        if (sharedFont == null) { var rf = Object.FindAnyObjectByType<TextMeshProUGUI>(); if (rf?.font != null) sharedFont = rf.font; }
                        if (sharedFont != null) ttmp.font = sharedFont;
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

            if (nameText != null)       nameText.text       = character.data.characterName;
            if (levelText != null)      levelText.text      = $"Nv. {character.level}";
            if (costText != null)       costText.text       = $"${NumberFormatter.Format(character.GetCurrentCost())}";

            if (productionText != null)
            {
                switch (character.data.type)
                {
                    case CharacterType.Production:
                    case CharacterType.Automation:
                        productionText.text = $"+{NumberFormatter.Format(character.GetCurrentProduction())}/s";
                        break;
                    case CharacterType.Multiplier:
                        productionText.text = $"x{character.GetCurrentMultiplier():F2} total";
                        break;
                }
            }

            bool affordable = GameManager.Instance.Money >= character.GetCurrentCost();
            // Button always stays interactable — OnUpgradeClicked handles the not-enough-money case

            // Item 5: cost progress bar
            if (costProgressBar != null)
            {
                float fill = Mathf.Clamp01((float)(GameManager.Instance.Money / character.GetCurrentCost()));
                costProgressBar.fillAmount = fill;
                costProgressBar.color = affordable
                    ? new Color(1f, 0.85f, 0f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.25f);
            }

            // Glow dourado quando pode comprar
            if (glowOverlay != null)
            {
                if (affordable && !wasAffordable)
                {
                    if (glowCoroutine != null) StopCoroutine(glowCoroutine);
                    glowCoroutine = StartCoroutine(GlowPulseCoroutine());
                }
                else if (!affordable && wasAffordable)
                {
                    if (glowCoroutine != null) { StopCoroutine(glowCoroutine); glowCoroutine = null; }
                    glowOverlay.color = new Color(1f, 0.85f, 0f, 0f);
                }
            }

            // Pulse de escala na primeira vez acessível (nível 0)
            if (affordable && !wasAffordable && character.level == 0)
                StartCoroutine(PulseCoroutine());

            wasAffordable = affordable;
        }

        private static void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
        }

        private void ApplyCardLayout()
        {
            // Card: 228x160 (GridLayoutGroup 2 colunas)
            const float iconSize     = 68f;
            const float textLeftPad  = iconSize + 10f;
            const float textRightPad = 8f;

            // Icon: à esquerda, vertical-center
            var iconRT = transform.Find("Icon")?.GetComponent<RectTransform>();
            if (iconRT != null)
            {
                iconRT.anchorMin        = new Vector2(0f, 0.5f);
                iconRT.anchorMax        = new Vector2(0f, 0.5f);
                iconRT.pivot            = new Vector2(0f, 0.5f);
                iconRT.anchoredPosition = new Vector2(8f, 0f);
                iconRT.sizeDelta        = new Vector2(iconSize, iconSize);
            }

            // NameText: topo (após ícone)
            if (nameText != null)
            {
                SetAnchors(nameText.rectTransform,
                    new Vector2(0f, 0.62f), new Vector2(0.78f, 1f),
                    new Vector2(textLeftPad, 0f), new Vector2(0f, -6f));
                nameText.fontSize  = 17;
                nameText.fontStyle = FontStyles.Bold;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.color     = Color.white;
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // LevelText: topo direito
            if (levelText != null)
            {
                SetAnchors(levelText.rectTransform,
                    new Vector2(0.78f, 0.62f), new Vector2(1f, 1f),
                    new Vector2(0f, 0f), new Vector2(-textRightPad, -6f));
                levelText.fontSize  = 13;
                levelText.fontStyle = FontStyles.Bold;
                levelText.alignment = TextAlignmentOptions.MidlineRight;
                levelText.color     = new Color(1f, 1f, 1f, 0.8f);
            }

            // ProductionText: meio
            if (productionText != null)
            {
                SetAnchors(productionText.rectTransform,
                    new Vector2(0f, 0.35f), new Vector2(1f, 0.62f),
                    new Vector2(textLeftPad, 0f), new Vector2(-textRightPad, 0f));
                productionText.fontSize  = 14;
                productionText.fontStyle = FontStyles.Bold;
                productionText.alignment = TextAlignmentOptions.MidlineLeft;
                productionText.color     = new Color(0.55f, 1f, 0.7f);
                productionText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // CostText: baixo direito, destaque em dourado
            if (costText != null)
            {
                SetAnchors(costText.rectTransform,
                    new Vector2(0f, 0.05f), new Vector2(1f, 0.38f),
                    new Vector2(textLeftPad, 0f), new Vector2(-textRightPad, 0f));
                costText.fontSize  = 19;
                costText.fontStyle = FontStyles.Bold;
                costText.alignment = TextAlignmentOptions.MidlineRight;
                costText.color     = new Color(1f, 0.92f, 0.35f);
                costText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private void SetupGlowOverlay()
        {
            var go = new GameObject("GlowOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            glowOverlay = go.GetComponent<Image>();
            glowOverlay.color = new Color(1f, 0.85f, 0f, 0f);
            glowOverlay.raycastTarget = false;
        }

        private IEnumerator GlowPulseCoroutine()
        {
            while (true)
            {
                float e = 0f;
                while (e < 0.6f)
                {
                    e += Time.deltaTime;
                    if (glowOverlay) glowOverlay.color = new Color(1f, 0.85f, 0f, Mathf.Lerp(0f, 0.28f, e / 0.6f));
                    yield return null;
                }
                e = 0f;
                while (e < 0.6f)
                {
                    e += Time.deltaTime;
                    if (glowOverlay) glowOverlay.color = new Color(1f, 0.85f, 0f, Mathf.Lerp(0.28f, 0f, e / 0.6f));
                    yield return null;
                }
            }
        }

        private void SetupCostProgressBar()
        {
            var barGO = new GameObject("CostBar", typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(transform, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, 4f);
            costProgressBar              = barGO.GetComponent<Image>();
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
            // Flash branco no background
            if (backgroundImage != null)
            {
                Color baseColor = character.data.tintColor;
                backgroundImage.color = Color.white;
                yield return new WaitForSeconds(0.08f);
                backgroundImage.color = baseColor;
            }

            // "NÍVEL X!" flutuando no centro do card
            SpawnBurstText($"NÍVEL {character.level}!", new Color(1f, 0.92f, 0.35f), 26f, Vector2.zero);

            // Burst de estrelas douradas em posições aleatórias
            for (int i = 0; i < 5; i++)
            {
                var offset = new Vector2(Random.Range(-75f, 75f), Random.Range(-25f, 25f));
                SpawnBurstText("✦", new Color(1f, 0.82f, 0.1f), 18f, offset);
                yield return null;
            }

            // Pulse de escala no card
            StartCoroutine(PulseCoroutine());
        }

        private void SpawnBurstText(string text, Color color, float fontSize, Vector2 offset)
        {
            var go = new GameObject("Burst",
                typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingText));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 55f);
            rt.anchoredPosition = offset;
            go.GetComponent<FloatingText>().Init(text, color, fontSize);
        }

        private IEnumerator PulseCoroutine()
        {
            const float duration = 0.3f;
            const float half     = duration * 0.5f;
            Vector3 baseScale    = transform.localScale;
            Vector3 bigScale     = baseScale * 1.15f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(baseScale, bigScale, elapsed / half);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(bigScale, baseScale, elapsed / half);
                yield return null;
            }
            transform.localScale = baseScale;
        }
    }
}
