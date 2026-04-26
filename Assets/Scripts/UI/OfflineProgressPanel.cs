using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class OfflineProgressPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI earningsText;
        [SerializeField] private Button collectButton;

        private double pendingEarnings;
        private PanelAnimator animator;

        private PanelAnimator Anim =>
            animator != null ? animator : (animator = gameObject.AddComponent<PanelAnimator>());

        private void Awake()
        {
            if (collectButton == null)
                collectButton = GetComponentInChildren<Button>(true);

            if (collectButton != null)
                collectButton.onClick.AddListener(OnCollect);

            if (collectButton != null && collectButton.GetComponent<AnimatedButton>() == null)
                collectButton.gameObject.AddComponent<AnimatedButton>();
        }

        public void Show(double earnings, long seconds)
        {
            pendingEarnings = earnings;

            TimeSpan time   = TimeSpan.FromSeconds(seconds);
            string timeStr  = time.TotalHours >= 1
                ? $"Fora por {(int)time.TotalHours}h {time.Minutes}m"
                : $"Fora por {(int)time.TotalMinutes}m {time.Seconds}s";

            if (earningsText != null)
                earningsText.text = timeStr + "\n$" + NumberFormatter.Format(earnings);

            Anim.Open();
        }

        private void OnCollect()
        {
            GameManager.Instance.AddMoney(pendingEarnings);
            UIManager.Instance.ShowToast($"Coletado: ${NumberFormatter.Format(pendingEarnings)}");
            Anim.Close();
        }
    }
}
