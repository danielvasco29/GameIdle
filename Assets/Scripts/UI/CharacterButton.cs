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
        private Outline affordableHighlight;

        private void Awake()
        {
            ResolveReferences();
            if (GetComponent<AnimatedButton>() == null)
                gameObject.AddComponent<AnimatedButton>();
        }

        private void ResolveReferences()
        {
            nameText = null; levelText = null; productionText = null; costText = null;

            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                switch (t.gameObject.name)
                {
                    case "NameText":       nameText = t;       break;
                    case "LevelText":      levelText = t;      break;
                    case "ProductionText": productionText = t; break;
                    case "CostText":       costText = t;       break;
                }
            }

            if (upgradeButton == null) upgradeButton = GetComponent<Button>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();

            foreach (var img in GetComponentsInChildren<Image>(true))
                if (img.gameObject.name == "Icon") { iconImage = img; break; }
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(instance.data.tintColor, Color.black, 0.3f);

            if (iconImage != null)
                iconImage.sprite = instance.data.icon != null
                    ? instance.data.icon
                    : CharacterIconFactory.CreateIcon(instance.data.tintColor, instance.data.characterId);

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (character == null) return;

            if (nameText != null) nameText.text = character.data.characterName;
            if (levelText != null) levelText.text = $"Nv. {character.level}";
            if (costText != null) costText.text = $"${NumberFormatter.Format(character.GetCurrentCost())}";

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

            bool canAfford = GameManager.Instance.Money >= character.GetCurrentCost();
            if (upgradeButton != null)
                upgradeButton.interactable = canAfford;

            if (affordableHighlight == null && backgroundImage != null)
            {
                affordableHighlight = backgroundImage.gameObject.GetComponent<Outline>()
                    ?? backgroundImage.gameObject.AddComponent<Outline>();
                affordableHighlight.effectDistance = new Vector2(2f, -2f);
            }
            if (affordableHighlight != null)
                affordableHighlight.effectColor = canAfford
                    ? new Color(1f, 0.85f, 0.2f, 0.95f)
                    : new Color(0f, 0f, 0f, 0f);
        }

        private void OnUpgradeClicked()
        {
            bool ok = CharacterManager.Instance.TryUpgrade(characterIndex);
            if (ok) SpawnUpgradeText();
        }

        private void SpawnUpgradeText()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                rt.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPos);

            FloatingText.Spawn(canvas.transform, localPos,
                $"↑ Nv.{character.level}", new Color(1f, 0.85f, 0.1f));
        }
    }
}
