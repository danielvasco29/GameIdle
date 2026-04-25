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
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            bool canPrestige = GameManager.Instance.CanPrestige();
            int nextPrestigeCount = GameManager.Instance.PrestigeCount + 1;
            double nextMultiplier = 1.0 + nextPrestigeCount * 0.5;

            infoText.text = canPrestige
                ? $"Total ganho: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}\n\nReiniciar o jogo vai zerar seus personagens mas o multiplicador permanece!"
                : $"Você ainda não pode fazer prestígio.\n\nNecessário: $1B total ganho.\nAtual: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}";

            bonusText.text = $"Multiplicador após prestígio: ×{nextMultiplier:F1}";
            confirmButton.interactable = canPrestige;
        }

        private void OnConfirmClicked()
        {
            GameManager.Instance.Prestige();
            gameObject.SetActive(false);
        }
    }
}
