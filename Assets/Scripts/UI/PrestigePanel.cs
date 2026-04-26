using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class PrestigePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Show()
        {
            gameObject.SetActive(true);

            bool canPrestige = GameManager.Instance.CanPrestige();
            int nextCount = GameManager.Instance.PrestigeCount + 1;
            double nextMultiplier = 1.0 + nextCount * 0.5;

            string info = canPrestige
                ? $"Total ganho: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}\n\nReiniciar vai zerar personagens mas o multiplicador permanece!"
                : $"Nao pode dar prestigio ainda.\n\nNecessario: $1B total.\nAtual: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}";

            if (infoText != null)
                infoText.text = info;

            if (bonusText != null)
                bonusText.text = $"Multiplicador apos prestigio: x{nextMultiplier:F1}";
            else if (infoText != null)
                infoText.text += $"\n\nMultiplicador: x{nextMultiplier:F1}";

            if (confirmButton != null)
                confirmButton.interactable = canPrestige;
        }

        private void OnConfirmClicked()
        {
            GameManager.Instance.Prestige();
            gameObject.SetActive(false);
        }
    }
}
