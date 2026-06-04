using System;

namespace GameIdle
{
    [Serializable]
    public class CharacterInstance
    {
        public CharacterData data;
        public int level;
        public bool isUnlocked;

        private const double CostGrowth = 1.15;

        // Cost formula: baseCost * 1.15^level
        public double GetCurrentCost() =>
            data.baseCost * Math.Pow(CostGrowth, level);

        // Total cost to buy `count` consecutive levels from the current level.
        // Geometric series: first * (r^count - 1) / (r - 1).
        public double GetCostForLevels(int count)
        {
            if (count <= 0) return 0;
            double first = GetCurrentCost();
            return first * (Math.Pow(CostGrowth, count) - 1) / (CostGrowth - 1);
        }

        // Largest number of levels purchasable with `money`.
        public int GetMaxAffordable(double money)
        {
            double first = GetCurrentCost();
            if (money < first) return 0;
            // money >= first * (r^n - 1)/(r-1)  =>  r^n <= 1 + money*(r-1)/first
            double rhs = 1.0 + money * (CostGrowth - 1) / first;
            int n = (int)Math.Floor(Math.Log(rhs) / Math.Log(CostGrowth));
            return Math.Max(0, n);
        }

        // Multiplier characters scale as multiplier^level, which overflows a
        // double once the level gets large. Cap their level so the resulting
        // multiplier stays finite and sane (no more "x∞" from a single Máx buy).
        private const double MultiplierCeiling = 1e30;

        public int SafeMaxLevel()
        {
            if (data.type != CharacterType.Multiplier || data.multiplier <= 1.0)
                return int.MaxValue;
            return (int)Math.Floor(Math.Log(MultiplierCeiling) / Math.Log(data.multiplier));
        }

        public bool IsAtSafeCap() => level >= SafeMaxLevel();

        // Clamp a requested purchase so it never pushes a multiplier past the cap.
        public int ClampPurchaseCount(int count)
        {
            if (count <= 0) return 0;
            int cap = SafeMaxLevel();
            if (cap == int.MaxValue) return count;     // producers: no cap needed
            return Math.Max(0, Math.Min(count, cap - level));
        }

        // Production formula: baseProduction * level
        public double GetCurrentProduction() =>
            data.type == CharacterType.Multiplier ? 0.0 : data.baseProduction * level;

        // Multiplier formula: multiplier^level
        public double GetCurrentMultiplier() =>
            data.type == CharacterType.Multiplier ? Math.Pow(data.multiplier, level) : 1.0;

        public bool CanAfford(double money) => money >= GetCurrentCost();
    }
}
