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

            if (upgradeButton != null)
                upgradeButton.interactable = GameManager.Instance.Money >= character.GetCurrentCost();
        }

        private void OnUpgradeClicked() => CharacterManager.Instance.TryUpgrade(characterIndex);
    }
}
