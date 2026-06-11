using System.Collections.Generic;

namespace GameIdle
{
    public static class AchievementManager
    {
        public class Achievement
        {
            public string id, name, description;
            public int gemReward;
        }

        public static readonly Achievement[] All =
        {
            new() { id = "first_hire",    name = "Primeiro Contratado",   description = "Contratou seu primeiro funcionário.",  gemReward = 5  },
            new() { id = "tap_500",       name = "Trabalhador Dedicado",  description = "Clicou TRABALHAR 500 vezes.",          gemReward = 10 },
            new() { id = "tap_5000",      name = "Viciado em Trabalho",   description = "Clicou TRABALHAR 5.000 vezes.",        gemReward = 25 },
            new() { id = "millionaire",   name = "Milionário",            description = "Acumulou $1.000.000.",                 gemReward = 10 },
            new() { id = "billionaire",   name = "Bilionário",            description = "Acumulou $1.000.000.000.",             gemReward = 20 },
            new() { id = "trillionaire",  name = "Trilionário",           description = "Acumulou $1.000.000.000.000.",         gemReward = 50 },
            new() { id = "prestige_1",    name = "Recomeço",              description = "Realizou seu primeiro prestígio.",     gemReward = 15 },
            new() { id = "prestige_5",    name = "Veterano",              description = "Realizou 5 prestígios.",               gemReward = 50 },
            new() { id = "prestige_10",   name = "Lenda",                 description = "Realizou 10 prestígios.",              gemReward = 100},
            new() { id = "hire_10",       name = "Equipe Formada",        description = "Contratou 10 funcionários no total.",  gemReward = 10 },
            new() { id = "hire_50",       name = "Empresa Crescendo",     description = "Contratou 50 funcionários no total.",  gemReward = 30 },
            new() { id = "turbo_10",      name = "Velocidade Máxima",     description = "Usou o Turbo 10 vezes.",               gemReward = 15 },
            new() { id = "kill_10",    name = "Exterminador",    description = "Derrotou 10 monstros.",      gemReward = 8  },
            new() { id = "kill_50",    name = "Caçador de Elite",description = "Derrotou 50 monstros.",      gemReward = 12 },
            new() { id = "kill_100",   name = "Caçador",         description = "Derrotou 100 monstros.",     gemReward = 20 },
            new() { id = "kill_1000",  name = "Limpador de Bug", description = "Derrotou 1000 monstros.",    gemReward = 50 },
            new() { id = "boss_1",     name = "Mata-Dragão",     description = "Derrotou o Deadline Dragon pela primeira vez.", gemReward = 15 },
            new() { id = "boss_10",    name = "Lenda do Combate",description = "Derrotou 10 bosses.",        gemReward = 40 },
            new() { id = "cycle_2",    name = "Sobrevivente",    description = "Completou o 1º ciclo e enfrentou novos inimigos.", gemReward = 20 },
            new() { id = "cycle_5",    name = "Veterano do Abismo", description = "Completou 5 ciclos de batalha.", gemReward = 50 },
            new() { id = "cycle_10",   name = "Senhor dos Ciclos",  description = "Completou 10 ciclos de batalha.", gemReward = 100},
            // Gems lifetime
            new() { id = "gems_50",    name = "Colecionador",       description = "Coletou 50 gemas no total.",      gemReward = 5  },
            new() { id = "gems_200",   name = "Investidor",         description = "Coletou 200 gemas no total.",     gemReward = 10 },
            new() { id = "gems_1000",  name = "Magnata das Gemas",  description = "Coletou 1.000 gemas no total.",   gemReward = 30 },
            // Daily missions streak
            new() { id = "missions_1", name = "Comprometido",       description = "Completou todas as missões diárias.", gemReward = 10 },
            new() { id = "missions_7", name = "Dedicado",           description = "Completou todas as missões 7 dias seguidos.", gemReward = 50 },
            // Combo achievements
            new() { id = "combo_5",    name = "Combo Feroz",        description = "Atingiu combo de 5 no combate.", gemReward = 8  },
            new() { id = "combo_10",   name = "COMBO MÁXIMO!",      description = "Atingiu combo máximo de 10!",    gemReward = 20 },
        };

        private static readonly HashSet<string> _unlocked = new();
        private static int _turboUseCount;

        // True quando há conquista(s) desbloqueada(s) ainda não vistas no painel.
        public static bool HasUnseen { get; private set; }
        public static void MarkSeen() => HasUnseen = false;

        public static bool IsUnlocked(string id) => _unlocked.Contains(id);

        public static bool TryUnlock(string id)
        {
            if (_unlocked.Contains(id)) return false;
            _unlocked.Add(id);
            HasUnseen = true;
            var a = System.Array.Find(All, x => x.id == id);
            if (a == null) return true;
            if (GameManager.Instance != null) GameManager.Instance.AddGems(a.gemReward);
            if (UIManager.Instance   != null) UIManager.Instance.ShowAchievementToast(a.name, a.description, a.gemReward);
            SaveSystem.Save();
            return true;
        }

        public static void RegisterTurboUse()
        {
            _turboUseCount++;
            if (_turboUseCount >= 10) TryUnlock("turbo_10");
        }

        public static void CheckAll()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            if (gm.LifetimeTapCount  >= 500)  TryUnlock("tap_500");
            if (gm.LifetimeTapCount  >= 5000) TryUnlock("tap_5000");
            if (gm.LifetimeHireCount >= 1)    TryUnlock("first_hire");
            if (gm.LifetimeHireCount >= 10)   TryUnlock("hire_10");
            if (gm.LifetimeHireCount >= 50)   TryUnlock("hire_50");
            if (gm.TotalEarned       >= 1e6)  TryUnlock("millionaire");
            if (gm.TotalEarned       >= 1e9)  TryUnlock("billionaire");
            if (gm.TotalEarned       >= 1e12) TryUnlock("trillionaire");
            if (gm.LifetimePrestigeCount >= 1)  TryUnlock("prestige_1");
            if (gm.LifetimePrestigeCount >= 5)  TryUnlock("prestige_5");
            if (gm.LifetimePrestigeCount >= 10) TryUnlock("prestige_10");
            if (gm.LifetimeKillCount     >= 10)   TryUnlock("kill_10");
            if (gm.LifetimeKillCount     >= 50)   TryUnlock("kill_50");
            if (gm.LifetimeKillCount     >= 100)  TryUnlock("kill_100");
            if (gm.LifetimeKillCount     >= 1000) TryUnlock("kill_1000");
            if (gm.LifetimeBossKillCount >= 1)    TryUnlock("boss_1");
            if (gm.LifetimeBossKillCount >= 10)   TryUnlock("boss_10");

            if (CombatManager.Instance != null)
            {
                int cycle = CombatManager.Instance.Cycle;
                if (cycle >= 2)  TryUnlock("cycle_2");
                if (cycle >= 5)  TryUnlock("cycle_5");
                if (cycle >= 10) TryUnlock("cycle_10");
            }

            // Gems lifetime
            if (gm.LifetimeGemsEarned >= 50)   TryUnlock("gems_50");
            if (gm.LifetimeGemsEarned >= 200)  TryUnlock("gems_200");
            if (gm.LifetimeGemsEarned >= 1000) TryUnlock("gems_1000");

            // Mission streak
            if (gm.MissionStreakDays >= 1) TryUnlock("missions_1");
            if (gm.MissionStreakDays >= 7) TryUnlock("missions_7");
        }

        public static void CheckCombo(int combo)
        {
            if (combo >= 5)  TryUnlock("combo_5");
            if (combo >= 10) TryUnlock("combo_10");
        }

        public static void Load(List<string> saved)
        {
            _unlocked.Clear();
            if (saved != null)
                foreach (var s in saved) _unlocked.Add(s);
        }

        public static List<string> GetSaved() => new(_unlocked);
    }
}
