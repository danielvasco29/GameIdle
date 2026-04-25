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
            if (collectButton != null)
                collectButton.onClick.AddListener(OnCollect);
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(OnWatchAd);
        }

        public void Show(double earnings, long seconds)
        {
            pendingEarnings = earnings;
            gameObject.SetActive(true);

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            string timeStr = time.TotalHours >= 1
                ? $"Fora por {(int)time.TotalHours}h {time.Minutes}m"
                : $"Fora por {(int)time.TotalMinutes}m {time.Seconds}s";

            string earningsStr = $"Ganhou: ${NumberFormatter.Format(earnings)}";

            if (timeText != null)
                timeText.text = timeStr + "\n" + earningsStr;
            if (earningsText != null && earningsText != timeText)
                earningsText.text = earningsStr;
        }

        private void OnCollect()
        {
            GameManager.Instance.AddMoney(pendingEarnings);
            UIManager.Instance.ShowToast($"Coletado: ${NumberFormatter.Format(pendingEarnings)}");
            gameObject.SetActive(false);
        }

        private void OnWatchAd()
        {
            GameManager.Instance.AddMoney(pendingEarnings);
            if (MonetizationManager.Instance != null)
                MonetizationManager.Instance.RewardDoubleOfflineEarnings(pendingEarnings);
            gameObject.SetActive(false);
        }
    }
}
