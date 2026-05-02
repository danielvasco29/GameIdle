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
        public long LastLoginTimestamp { get; private set; }

        [SerializeField] private double prestigeRequirement = 1_000_000_000.0;

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
            Money += amount;
            TotalEarned += amount;
            OnMoneyChanged?.Invoke();
        }

        public bool SpendMoney(double amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            OnMoneyChanged?.Invoke();
            return true;
        }

        public void RecalculateStats()
        {
            double baseProduction = CharacterManager.Instance.GetTotalMPS();
            double multiplier = CharacterManager.Instance.GetTotalMultiplier();

            foreach (var effect in activeEffects)
            {
                if (effect.type == EffectType.MultiplierModifier || effect.type == EffectType.ProductionModifier)
                    multiplier *= 1.0 + effect.value;
            }

            multiplier = Math.Max(0.01, multiplier);
            MoneyPerSecond = baseProduction * multiplier * PrestigeMultiplier;
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
        public double GetPrestigeRequirement() => prestigeRequirement;

        public bool CanPrestige() => TotalEarned >= prestigeRequirement;

        public double GetTapValue() => System.Math.Max(1.0, MoneyPerSecond * 0.5);
        public void Tap() => AddMoney(GetTapValue());

        public void Prestige()
        {
            if (!CanPrestige()) return;
            PrestigeCount++;
            PrestigeMultiplier = 1.0 + PrestigeCount * 0.5;
            Money = 10;
            TotalEarned = 0;
            activeEffects.Clear();
            CharacterManager.Instance.ResetAll();
            RecalculateStats();
            RankingPanel.AddRecord(PrestigeCount);
            UIManager.Instance.RefreshAll();
            UIManager.Instance.ShowToast($"Prestígio #{PrestigeCount}! Multiplicador: x{PrestigeMultiplier:F1}",
                new UnityEngine.Color(1f, 0.84f, 0f));
            SaveSystem.Save();
        }

        public void ApplySaveData(SaveData data)
        {
            Money = data.money;
            TotalEarned = data.totalEarned;
            PrestigeCount = data.prestigeCount;
            PrestigeMultiplier = data.prestigeMultiplier > 0 ? data.prestigeMultiplier : 1.0;
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
                lastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                characters = CharacterManager.Instance.GetSaveData()
            };
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveSystem.Save(); }
        private void OnApplicationQuit() => SaveSystem.Save();
    }
}
