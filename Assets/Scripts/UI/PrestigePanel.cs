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

        private PanelAnimator animator;

        private PanelAnimator Anim =>
            animator != null ? animator : (animator = gameObject.AddComponent<PanelAnimator>());

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => Anim.Close());

            if (confirmButton != null && confirmButton.GetComponent<AnimatedButton>() == null)
                confirmButton.gameObject.AddComponent<AnimatedButton>();
            if (cancelButton != null && cancelButton.GetComponent<AnimatedButton>() == null)
                cancelButton.gameObject.AddComponent<AnimatedButton>();
        }

        public void Show()
        {
            bool canPrestige  = GameManager.Instance.CanPrestige();
            int nextCount     = GameManager.Instance.PrestigeCount + 1;
            double nextMult   = 1.0 + nextCount * 0.5;
            double requirement = GameManager.Instance.GetPrestigeRequirement();

            string info = canPrestige
                ? $"Total ganho: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}\n\nReiniciar vai zerar personagens mas o multiplicador permanece!"
                : $"Nao pode dar prestigio ainda.\n\nNecessario: ${NumberFormatter.Format(requirement)} total.\nAtual: ${NumberFormatter.Format(GameManager.Instance.TotalEarned)}";

            if (infoText != null)  infoText.text  = info;
            if (bonusText != null) bonusText.text = $"Multiplicador apos prestigio: x{nextMult:F1}";
            else if (infoText != null) infoText.text += $"\n\nMultiplicador: x{nextMult:F1}";

            if (confirmButton != null) confirmButton.interactable = canPrestige;

            Anim.Open();
        }

        private void OnConfirmClicked()
        {
            GameManager.Instance.Prestige();
            Anim.Close();
        }
    }
}
