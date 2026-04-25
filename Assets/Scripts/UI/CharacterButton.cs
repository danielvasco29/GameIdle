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

        public void Setup(CharacterInstance instance, int index)
        {
            character = instance;
            characterIndex = index;

            if (instance.data.icon != null)
                iconImage.sprite = instance.data.icon;

            backgroundImage.color = instance.data.tintColor;

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

            Refresh();
        }

        public void Refresh()
        {
            if (character == null) return;

            nameText.text = character.data.characterName;
            levelText.text = $"Nv. {character.level}";
            costText.text = $"${NumberFormatter.Format(character.GetCurrentCost())}";

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

            upgradeButton.interactable = GameManager.Instance.Money >= character.GetCurrentCost();
        }

        private void OnUpgradeClicked() => CharacterManager.Instance.TryUpgrade(characterIndex);
    }
}
