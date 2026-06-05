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

            if (elapsed < 10) return;   // ignore trivial gaps

            double mps = GameManager.Instance.MoneyPerSecond;
            if (mps <= 0) return;

            double earned = elapsed * mps * GemShop.GetOfflineMult();
            UIManager.Instance.ShowOfflineProgress(earned, elapsed);
        }
    }
}
