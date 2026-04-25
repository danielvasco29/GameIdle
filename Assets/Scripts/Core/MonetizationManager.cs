using UnityEngine;

namespace GameIdle
{
    /// <summary>
    /// Simulates ad rewards and IAP. In production, integrate Unity Ads / Unity IAP here.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        [SerializeField] private float adRewardDuration = 300f;  // 5 minutes

        public bool AdsRemoved { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Call this to reward the player with 2x income after watching an ad
        public void RewardDoubleIncome()
        {
            var effect = new EventEffect
            {
                eventId = "ad_double_income",
                type = EffectType.ProductionModifier,
                value = 1.0f,   // +100% = 2x
                duration = adRewardDuration,
                timeRemaining = adRewardDuration,
                isPermanent = false
            };
            GameManager.Instance.ApplyEffect(effect);
            UIManager.Instance.ShowToast($"Receita 2x por {adRewardDuration / 60:F0} minutos!");
        }

        // Resolve the active event by watching an ad (auto-picks the best choice)
        public void RewardResolveEvent(EventData eventData)
        {
            GameEventSystem.Instance.ResolveBestChoice(eventData);
        }

        // Reward 2x offline earnings after watching an ad
        public void RewardDoubleOfflineEarnings(double baseEarnings)
        {
            GameManager.Instance.AddMoney(baseEarnings); // adds the bonus (base already added)
            UIManager.Instance.ShowToast("Ganhos offline dobrados!");
        }

        // Simulates removing ads (in production, call after IAP validation)
        public void PurchaseRemoveAds()
        {
            AdsRemoved = true;
            UIManager.Instance.ShowToast("Anúncios removidos! Obrigado pelo suporte!");
        }

        // Simulates purchasing a permanent 50% multiplier
        public void PurchasePermanentMultiplier()
        {
            var effect = new EventEffect
            {
                eventId = "iap_permanent_multiplier",
                type = EffectType.MultiplierModifier,
                value = 0.5f,  // +50%
                duration = 0,
                isPermanent = true
            };
            GameManager.Instance.ApplyEffect(effect);
            UIManager.Instance.ShowToast("Multiplicador permanente x1.5 ativado!");
        }
    }
}
