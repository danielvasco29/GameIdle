using System;
using UnityEngine;

namespace GameIdle
{
    public static class OfflineProgress
    {
        private const long MaxOfflineSeconds = 8 * 3600; // cap at 8h

        public static void Calculate()
        {
            long lastTimestamp = GameManager.Instance.LastLoginTimestamp;
            if (lastTimestamp == 0) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = Math.Min(now - lastTimestamp, MaxOfflineSeconds);

            if (elapsed < 5) return;   // ignore trivial gaps

            double mps = GameManager.Instance.MoneyPerSecond;
            if (mps <= 0) return;

            double earned = elapsed * mps * GemShop.GetOfflineMult();

            // Offline combat rewards.
            // NAO usa CombatManager.Instance: este calculo roda no Start do GameManager,
            // antes do Start do CombatManager, quando CurrentDef ainda e null.
            if (CharacterManager.Instance != null)
            {
                int workers = 0;
                foreach (var c in CharacterManager.Instance.GetAllCharacters())
                    if (c.level > 0) workers++;

                if (workers > 0)
                {
                    const double baseKillInterval = 30.0;
                    const int maxOfflineKills = 500;

                    int armorLevel = CombatManager.ArmorLevel;
                    double killInterval = baseKillInterval * Math.Pow(0.9, armorLevel);
                    long kills = Math.Min((long)(elapsed / killInterval), maxOfflineKills);

                    if (kills > 0)
                    {
                        int cycle = Math.Max(1, GameManager.Instance.SavedCombatCycle);
                        int wave  = Math.Max(1, GameManager.Instance.SavedCombatWave);
                        double baseReward = CombatManager.GetBaseRewardFor(cycle, wave);
                        double combatEarned = kills * baseReward * (1 + (cycle - 1) * 1.2) * GemShop.GetOfflineMult();
                        earned += combatEarned;
                    }
                }
            }

            UIManager.Instance.ShowOfflineProgress(earned, elapsed);
        }
    }
}
