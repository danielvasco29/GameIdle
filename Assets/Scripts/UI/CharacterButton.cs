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

                var referenceFont = FindObjectOfType<TextMeshProUGUI>();
                if (referenceFont != null && referenceFont.font != null)
                    tmp.font = referenceFont.font;
                else if (TMPro.TMP_FontAsset.defaultFontAsset != null)
                    tmp.font = TMPro.TMP_FontAsset.defaultFontAsset;
            }
            return tmp;
        }

        private void AutoFindComponents()
        {
            nameText       = nameText       ?? GetOrAddTMP("NameText");
            levelText      = levelText      ?? GetOrAddTMP("LevelText");
            productionText = productionText ?? GetOrAddTMP("ProductionText");
            costText       = costText       ?? GetOrAddTMP("CostText");
            if (upgradeButton == null)  upgradeButton  = GetComponentInChildren<Button>(true);
            if (backgroundImage == null)
            {
                var bg = transform.Find("Background");
                if (bg != null) backgroundImage = bg.GetComponent<Image>();
            }
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            AutoFindComponents();

            if (iconImage != null && instance.data.icon != null)
                iconImage.sprite = instance.data.icon;

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
