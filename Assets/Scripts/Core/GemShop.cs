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
        // Gem economy reference (prestigeRequirementGrowth = 5):
        //   P#1=5  P#2=11  P#3=25  P#4=56  P#5=125  P#6=280  P#7=625  P#10=12639 cumulative
        // Targets: lvl 1-5 accessible in 1-3 prestiges; max requires many prestiges.
        public static readonly Upgrade[] Upgrades =
        {
            // prod: lvl5≈34 gems, lvl10≈250, max25≈30k
            new() { id = "prod",       name = "Producao Turbo",       description = "Aumenta toda a producao por segundo.",
                    baseCost = 2, costGrowth = 1.45, bonusPerLevel = 0.15, maxLevel = 25, effectSuffix = "producao" },
            // tap: lvl5≈28 gems, lvl10≈180, max20≈4k
            new() { id = "tap",        name = "Clique Pesado",        description = "Aumenta o ganho de cada clique TRABALHAR.",
                    baseCost = 2, costGrowth = 1.40, bonusPerLevel = 0.25, maxLevel = 20, effectSuffix = "por clique" },
            // prestige: lvl5≈119 gems, lvl10≈1k, max20≈25k — intentionally expensive
            new() { id = "prestige",   name = "Mestre do Prestigio",  description = "Aumenta o multiplicador ganho a cada prestigio.",
                    baseCost = 5, costGrowth = 1.55, bonusPerLevel = 0.12, maxLevel = 20, effectSuffix = "mult. prestigio" },
            // start: lvl1=4, max12≈3.4k
            new() { id = "start",      name = "Capital Inicial",      description = "Comeca com mais dinheiro apos cada prestigio.",
                    baseCost = 4, costGrowth = 1.70, bonusPerLevel = 1.0,  maxLevel = 12, effectSuffix = "capital" },
            // turbo: total≈96 gems to max5 — fully unlockable around P#4-5
            new() { id = "turbo_cd",   name = "Turbo Acelerado",      description = "Reduz o cooldown do Turbo em 20s por nivel.",
                    baseCost = 5, costGrowth = 1.70, bonusPerLevel = 20.0, maxLevel = 5,  effectSuffix = "s cooldown" },
            // offline: total≈236 gems to max8
            new() { id = "offline",    name = "Rendimento Offline",   description = "Aumenta os ganhos enquanto offline.",
                    baseCost = 4, costGrowth = 1.55, bonusPerLevel = 0.40, maxLevel = 8,  effectSuffix = "ganho offline" },
            // gems: total≈2.7k to max10 — invest early for compounding benefit
            new() { id = "gem_bonus",  name = "Cofre de Gemas",       description = "Ganha mais gemas a cada prestigio.",
                    baseCost = 6, costGrowth = 1.80, bonusPerLevel = 0.15, maxLevel = 10, effectSuffix = "gemas prestigio" },
            // combat_shield: reduces damage taken by workers in combat (future use) / makes auto-attacks deal +15% per level
            new() { id = "combat_shield", name = "Escudo de Combate", description = "Aumenta o dano dos workers em combate.",
                    baseCost = 8, costGrowth = 1.60, bonusPerLevel = 0.15, maxLevel = 10, effectSuffix = "dano worker" },
            // kill_bonus: kills reward extra money
            new() { id = "kill_bonus", name = "Caçador Recompensado", description = "Kills de monstros rendem mais dinheiro.",
                    baseCost = 6, costGrowth = 1.55, bonusPerLevel = 0.20, maxLevel = 15, effectSuffix = "recompensa kill" },
        };

        private static readonly int[] levels = new int[9];

        public static int GetLevel(int i) => (i >= 0 && i < levels.Length) ? levels[i] : 0;

        public static int GetCost(int i)
        {
            var u = Upgrades[i];
            return (int)Math.Ceiling(u.baseCost * Math.Pow(u.costGrowth, levels[i]));
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
        public static double GetProductionMult()        => 1.0 + levels[0] * Upgrades[0].bonusPerLevel;
        public static double GetTapMult()               => 1.0 + levels[1] * Upgrades[1].bonusPerLevel;
        public static double GetPrestigeBonus()         => 1.0 + levels[2] * Upgrades[2].bonusPerLevel;
        public static double GetStartMoney()            => 10.0 * Math.Pow(10.0, levels[3] * Upgrades[3].bonusPerLevel);
        public static float  GetTurboCooldownReduction()=> (float)(levels[4] * Upgrades[4].bonusPerLevel);
        public static double GetOfflineMult()           => 1.0 + levels[5] * Upgrades[5].bonusPerLevel;
        public static double GetGemBonus()              => 1.0 + levels[6] * Upgrades[6].bonusPerLevel;
        public static double GetCombatShieldMult()      => 1.0 + levels[7] * Upgrades[7].bonusPerLevel;
        public static double GetKillBonusMult()         => 1.0 + levels[8] * Upgrades[8].bonusPerLevel;

        // Human-readable current total effect, for the shop UI.
        public static string GetEffectText(int i)
        {
            int lvl = levels[i];
            return i switch
            {
                0 => lvl == 0 ? "+0% producao" : $"+{lvl * Upgrades[0].bonusPerLevel * 100:0}% producao",
                1 => lvl == 0 ? "+0% por clique" : $"+{lvl * Upgrades[1].bonusPerLevel * 100:0}% por clique",
                2 => lvl == 0 ? "+0% mult. prestigio" : $"+{lvl * Upgrades[2].bonusPerLevel * 100:0}% mult. prestigio",
                3 => $"Inicio: ${NumberFormatter.Format(GetStartMoney())}",
                4 => $"-{lvl * (int)Upgrades[4].bonusPerLevel}s cooldown",
                5 => lvl == 0 ? "+0% offline" : $"+{lvl * Upgrades[5].bonusPerLevel * 100:0}% offline",
                6 => lvl == 0 ? "+0% gemas" : $"+{lvl * Upgrades[6].bonusPerLevel * 100:0}% gemas prestigio",
                7 => lvl == 0 ? "+0% dano worker" : $"+{lvl * Upgrades[7].bonusPerLevel * 100:0}% dano worker",
                8 => lvl == 0 ? "+0% recompensa" : $"+{lvl * Upgrades[8].bonusPerLevel * 100:0}% recompensa kill",
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
