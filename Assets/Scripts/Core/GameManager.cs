using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;

namespace GameIdle
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public double Money { get; private set; } = 10.0;
        public double MoneyPerSecond { get; private set; }
        public double TotalEarned { get; private set; }
        public double PrestigeMultiplier { get; private set; } = 1.0;
        public int PrestigeCount { get; private set; }
        public int Gems { get; private set; }
        public long LastLoginTimestamp { get; private set; }

        // Prestige requirement scales each reset: base * growth^prestigeCount
        [SerializeField] private double prestigeBaseRequirement = 1_000_000_000.0;
        [SerializeField] private double prestigeRequirementGrowth = 5.0;

        private readonly List<EventEffect> activeEffects = new();

        private float saveTimer;
        private const float SaveInterval = 30f;

        public event Action OnMoneyChanged;
        public event Action OnStatsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CharacterManager.Instance.Initialize();
            SaveSystem.Load();
            RecalculateStats();
            OfflineProgress.Calculate();
            GameEventSystem.Instance.StartEventCycle();
        }

        private void Update()
        {
            TickEffects(Time.deltaTime);
            if (MoneyPerSecond > 0)
                AddMoney(MoneyPerSecond * Time.deltaTime);

            saveTimer += Time.deltaTime;
            if (saveTimer >= SaveInterval)
            {
                saveTimer = 0f;
                SaveSystem.Save();
            }
        }

        public void AddMoney(double amount)
        {
            if (!double.IsFinite(amount)) return;
            Money += amount;
            TotalEarned += amount;
            if (!double.IsFinite(Money)) Money = double.MaxValue;
            if (!double.IsFinite(TotalEarned)) TotalEarned = double.MaxValue;
            OnMoneyChanged?.Invoke();
        }

        public bool SpendMoney(double amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            OnMoneyChanged?.Invoke();
            return true;
        }

        // Pure calculation of money/second for the current character state.
        // Used both to refresh stats and to preview a purchase's income gain.
        public double ComputeMoneyPerSecond()
        {
            double baseProduction = CharacterManager.Instance.GetTotalMPS();
            double multiplier = CharacterManager.Instance.GetTotalMultiplier();

            foreach (var effect in activeEffects)
            {
                if (effect.type == EffectType.MultiplierModifier || effect.type == EffectType.ProductionModifier)
                    multiplier *= 1.0 + effect.value;
            }

            multiplier = Math.Max(0.01, multiplier);
            double mps = baseProduction * multiplier
                         * PrestigeMultiplier * GemShop.GetPrestigeBonus()
                         * GemShop.GetProductionMult();
            return double.IsFinite(mps) ? mps : double.MaxValue;
        }

        public void RecalculateStats()
        {
            MoneyPerSecond = ComputeMoneyPerSecond();
            OnStatsUpdated?.Invoke();
        }

        public void ApplyEffect(EventEffect effect)
        {
            activeEffects.RemoveAll(e => e.eventId == effect.eventId);
            activeEffects.Add(effect);
            RecalculateStats();
        }

        private void TickEffects(float deltaTime)
        {
            bool anyExpired = false;
            foreach (var effect in activeEffects)
            {
                effect.Tick(deltaTime);
                if (effect.IsExpired) anyExpired = true;
            }
            if (anyExpired)
            {
                activeEffects.RemoveAll(e => e.IsExpired);
                RecalculateStats();
            }
        }

        public IReadOnlyList<EventEffect> GetActiveEffects() => activeEffects;

        // Scalable: each prestige raises the bar by `growth`x
        public double GetPrestigeRequirement() =>
            prestigeBaseRequirement * Math.Pow(prestigeRequirementGrowth, PrestigeCount);

        public bool CanPrestige() => TotalEarned >= GetPrestigeRequirement();

        // Gems awarded on prestige, scaling with how much was earned this run.
        public int GetPrestigeGemReward()
        {
            if (TotalEarned < GetPrestigeRequirement()) return 0;
            return Math.Max(1, (int)Math.Floor(5.0 * Math.Sqrt(TotalEarned / prestigeBaseRequirement)));
        }

        public bool SpendGems(int amount)
        {
            if (amount <= 0 || Gems < amount) return false;
            Gems -= amount;
            OnStatsUpdated?.Invoke();
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            OnStatsUpdated?.Invoke();
        }

        // ── Tap Boost ────────────────────────────────────────────────────────
        private float _tapBoostEnd      = -1f;
        private float _tapBoostCoolEnd  = -1f;
        private const float BoostDuration  = 30f;
        private const float BoostCooldown  = 120f;
        private const double BoostMult     = 5.0;

        public bool  TapBoostActive             => Time.time < _tapBoostEnd;
        public float TapBoostRemaining          => Mathf.Max(0f, _tapBoostEnd - Time.time);
        public float TapBoostCooldownRemaining  => Mathf.Max(0f, _tapBoostCoolEnd - Time.time);
        public bool  TapBoostOnCooldown         => !TapBoostActive && Time.time < _tapBoostCoolEnd;

        public void ActivateTapBoost()
        {
            if (TapBoostActive || TapBoostOnCooldown) return;
            _tapBoostEnd     = Time.time + BoostDuration;
            _tapBoostCoolEnd = Time.time + BoostDuration + BoostCooldown;
            OnStatsUpdated?.Invoke();
        }

        public double GetTapValue()
        {
            double base_ = System.Math.Max(1.0, MoneyPerSecond * 0.5) * GemShop.GetTapMult();
            return TapBoostActive ? base_ * BoostMult : base_;
        }

        public void Tap() => AddMoney(GetTapValue());

        public void Prestige()
        {
            if (!CanPrestige()) return;
            int gemsGained = GetPrestigeGemReward(); // compute before count/state reset
            Gems += gemsGained;
            PrestigeCount++;
            PrestigeMultiplier = 1.0 + PrestigeCount * 0.5;
            Money = GemShop.GetStartMoney();
            TotalEarned = 0;
            activeEffects.Clear();
            CharacterManager.Instance.ResetAll();
            RecalculateStats();
            RankingPanel.AddRecord(PrestigeCount);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayPrestige();
            UIManager.Instance.RefreshAll();
            UIManager.Instance.ShowToast(
                $"Prestígio #{PrestigeCount}! x{PrestigeMultiplier:F1} • +{gemsGained} gemas",
                new UnityEngine.Color(1f, 0.84f, 0f));
            SaveSystem.Save();
        }

        // Wipes all progress and starts a brand-new save.
        public void HardReset()
        {
            SaveSystem.Delete();
            Money = 10.0;
            TotalEarned = 0;
            MoneyPerSecond = 0;
            PrestigeCount = 0;
            PrestigeMultiplier = 1.0;
            Gems = 0;
            GemShop.LoadLevels(null);
            activeEffects.Clear();
            CharacterManager.Instance.ResetAll();
            RecalculateStats();
            UIManager.Instance.RefreshAll();
            UIManager.Instance.ShowToast("Novo jogo iniciado!", new UnityEngine.Color(0.4f, 0.9f, 1f));
            SaveSystem.Save();
        }

        public void ApplySaveData(SaveData data)
        {
            Money = data.money;
            TotalEarned = data.totalEarned;
            PrestigeCount = data.prestigeCount;
            PrestigeMultiplier = data.prestigeMultiplier > 0 ? data.prestigeMultiplier : 1.0;
            Gems = data.gems;
            GemShop.LoadLevels(data.gemUpgrades);
            LastLoginTimestamp = data.lastLoginTimestamp;
        }

        public SaveData GetSaveData()
        {
            return new SaveData
            {
                money = Money,
                totalEarned = TotalEarned,
                moneyPerSecond = MoneyPerSecond,
                prestigeCount = PrestigeCount,
                prestigeMultiplier = PrestigeMultiplier,
                gems = Gems,
                gemUpgrades = GemShop.GetLevels(),
                lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                characters = CharacterManager.Instance.GetSaveData()
            };
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
        private void OnApplicationQuit() => SaveSystem.Save();
    }
}
