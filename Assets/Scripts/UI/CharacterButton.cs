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
        private bool wasAffordable;

        private TextMeshProUGUI GetOrAddTMP(string childName)
        {
            var go = transform.Find(childName);
            if (go == null) return null;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = go.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = "";

                var referenceFont = Object.FindFirstObjectByType<TextMeshProUGUI>();
                if (referenceFont != null && referenceFont.font != null)
                    tmp.font = referenceFont.font;
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

            // Garante altura fixa para o VerticalLayoutGroup
            var le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.minHeight       = 100;
            le.preferredHeight = 100;
            le.flexibleHeight  = 0;

            // Cria GO Icon dinamicamente se necessário
            if (iconImage == null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(transform, false);
                var irt = iconGO.GetComponent<RectTransform>();
                irt.anchorMin        = new Vector2(0f, 0.5f);
                irt.anchorMax        = new Vector2(0f, 0.5f);
                irt.pivot            = new Vector2(0f, 0.5f);
                irt.anchoredPosition = new Vector2(8f, 0f);
                irt.sizeDelta        = new Vector2(72f, 72f);
                iconImage = iconGO.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget  = false;
            }

            if (instance.data.icon != null)
            {
                iconImage.sprite = instance.data.icon;
                iconImage.color  = Color.white;
            }
            else
            {
                // Tenta carregar sprite de Resources/Characters/Sprites/{characterId}
                var fallbackSprite = Resources.Load<Sprite>($"Characters/Sprites/{instance.data.characterId}");
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
                        var refFont = Object.FindFirstObjectByType<TextMeshProUGUI>();
                        if (refFont != null && refFont.font != null) ttmp.font = refFont.font;
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
            if (upgradeButton != null) upgradeButton.interactable = affordable;

            // Item 5: cost progress bar
            if (costProgressBar != null)
            {
                float fill = Mathf.Clamp01((float)(GameManager.Instance.Money / character.GetCurrentCost()));
                costProgressBar.fillAmount = fill;
                costProgressBar.color = affordable
                    ? new Color(1f, 0.85f, 0f, 0.9f)
                    : new Color(1f, 1f, 1f, 0.25f);
            }

            // Item 11: first-affordable pulse (level 0 only)
            if (affordable && !wasAffordable && character.level == 0)
                StartCoroutine(PulseCoroutine());

            wasAffordable = affordable;
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
            if (success) StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            if (backgroundImage == null) yield break;
            Color baseColor = character.data.tintColor;
            backgroundImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            backgroundImage.color = baseColor;
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
