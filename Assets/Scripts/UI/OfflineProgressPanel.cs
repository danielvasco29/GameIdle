using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameIdle
{
    public class OfflineProgressPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI earningsText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button watchAdButton;

        private double pendingEarnings;

        private void Awake()
        {
            collectButton.onClick.AddListener(OnCollect);
            watchAdButton.onClick.AddListener(OnWatchAd);
            gameObject.SetActive(false);
        }

        public void Show(double earnings, long seconds)
        {
            pendingEarnings = earnings;
            gameObject.SetActive(true);

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            timeText.text = time.TotalHours >= 1
                ? $"Você ficou fora por {(int)time.TotalHours}h {time.Minutes}m"
                : $"Você ficou fora por {(int)time.TotalMinutes}m {time.Seconds}s";

            earningsText.text = $"Ganhou offline: ${NumberFormatter.Format(earnings)}";
        }

        private void OnCollect()
        {
            GameManager.Instance.AddMoney(pendingEarnings);
            UIManager.Instance.ShowToast($"Coletado: ${NumberFormatter.Format(pendingEarnings)}");
            gameObject.SetActive(false);
        }

        private void OnWatchAd()
        {
            // First collect the base amount, then bonus via monetization
            GameManager.Instance.AddMoney(pendingEarnings);
            MonetizationManager.Instance.RewardDoubleOfflineEarnings(pendingEarnings);
            gameObject.SetActive(false);
        }
    }
}
