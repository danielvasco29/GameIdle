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

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTexts)
            {
                switch (t.gameObject.name)
                {
                    case "NameText":      if (nameText == null)       nameText = t; break;
                    case "LevelText":     if (levelText == null)      levelText = t; break;
                    case "ProductionText":if (productionText == null) productionText = t; break;
                    case "CostText":      if (costText == null)       costText = t; break;
                }
            }

            if (upgradeButton == null)
                upgradeButton = GetComponent<Button>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            var allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img.gameObject.name == "Icon" && iconImage == null)
                    iconImage = img;
            }
        }

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            if (backgroundImage != null)
                backgroundImage.color = instance.data.tintColor;

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

            if (upgradeButton != null)
                upgradeButton.interactable = GameManager.Instance.Money >= character.GetCurrentCost();
        }

        private void OnUpgradeClicked() => CharacterManager.Instance.TryUpgrade(characterIndex);
    }
}
