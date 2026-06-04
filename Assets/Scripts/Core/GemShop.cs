using System;
using System.Collections.Generic;

namespace GameIdle
{
    // Permanent upgrades bought with gems. Unlike money/characters, these
    // persist through prestige — that's the point of a premium currency.
    public static class GemShop
    {
        public class Upgrade
        {
            public string id;
            public string name;
            public string description;
            public int    baseCost;
            public double  costGrowth;   // cost = baseCost * growth^level (rounded up)
            public double  bonusPerLevel;
            public int    maxLevel;
            public string  effectSuffix; // for UI, e.g. "+15% produção"
        }

        // Catalog — index order is stable and used as the save key.
        public static readonly Upgrade[] Upgrades =
        {
            new() { id = "prod",     name = "Produção Turbo",       description = "Aumenta toda a produção por segundo.",
                    baseCost = 3, costGrowth = 1.6, bonusPerLevel = 0.15, maxLevel = 50, effectSuffix = "produção" },
            new() { id = "tap",      name = "Clique Pesado",        description = "Aumenta o ganho de cada clique TRABALHAR.",
                    baseCost = 2, costGrowth = 1.5, bonusPerLevel = 0.30, maxLevel = 50, effectSuffix = "por clique" },
            new() { id = "prestige", name = "Mestre do Prestígio",  description = "Aumenta o multiplicador ganho a cada prestígio.",
                    baseCost = 5, costGrowth = 1.8, bonusPerLevel = 0.10, maxLevel = 30, effectSuffix = "mult. prestígio" },
            new() { id = "start",    name = "Capital Inicial",      description = "Começa com mais dinheiro após cada prestígio.",
                    baseCost = 4, costGrowth = 1.7, bonusPerLevel = 1.0,  maxLevel = 15, effectSuffix = "capital" },
        };

        private static readonly int[] levels = new int[4];

        public static int GetLevel(int i) => (i >= 0 && i < levels.Length) ? levels[i] : 0;

        public static int GetCost(int i)
        {
            var u = Upgrades[i];
            return (int)Math.Ceil(u.baseCost * Math.Pow(u.costGrowth, levels[i]));
        }

        public static bool IsMaxed(int i) => levels[i] >= Upgrades[i].maxLevel;

        public static bool CanBuy(int i) =>
            !IsMaxed(i) && GameManager.Instance != null && GameManager.Instance.Gems >= GetCost(i);

        public static bool Buy(int i)
        {
            if (!CanBuy(i)) return false;
            int cost = GetCost(i);
            if (!GameManager.Instance.SpendGems(cost)) return false;
            levels[i]++;
            GameManager.Instance.RecalculateStats();
            SaveSystem.Save();
            return true;
        }

        // ── Bonus accessors (used by GameManager) ────────────────────────────
        public static double GetProductionMult()  => 1.0 + levels[0] * Upgrades[0].bonusPerLevel;
        public static double GetTapMult()          => 1.0 + levels[1] * Upgrades[1].bonusPerLevel;
        public static double GetPrestigeBonus()    => 1.0 + levels[2] * Upgrades[2].bonusPerLevel;
        public static double GetStartMoney()       => 10.0 * Math.Pow(10.0, levels[3] * Upgrades[3].bonusPerLevel);

        // Human-readable current total effect, for the shop UI.
        public static string GetEffectText(int i)
        {
            int lvl = levels[i];
            return i switch
            {
                0 => $"+{lvl * Upgrades[0].bonusPerLevel * 100:0}% produção",
                1 => $"+{lvl * Upgrades[1].bonusPerLevel * 100:0}% por clique",
                2 => $"+{lvl * Upgrades[2].bonusPerLevel * 100:0}% mult. prestígio",
                3 => $"Início: ${NumberFormatter.Format(GetStartMoney())}",
                _ => ""
            };
        }

        // ── Persistence ──────────────────────────────────────────────────────
        public static void LoadLevels(List<int> saved)
        {
            for (int i = 0; i < levels.Length; i++)
                levels[i] = (saved != null && i < saved.Count) ? saved[i] : 0;
        }

        public static List<int> GetLevels()
        {
            var list = new List<int>(levels.Length);
            for (int i = 0; i < levels.Length; i++) list.Add(levels[i]);
            return list;
        }
    }
}
