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
            new() { id = "kill_100",   name = "Caçador",         description = "Derrotou 100 monstros.",     gemReward = 20 },
            new() { id = "kill_1000",  name = "Limpador de Bug", description = "Derrotou 1000 monstros.",    gemReward = 50 },
            new() { id = "boss_1",     name = "Mata-Dragão",     description = "Derrotou o Deadline Dragon pela primeira vez.", gemReward = 15 },
            new() { id = "boss_10",    name = "Lenda do Combate",description = "Derrotou 10 bosses.",        gemReward = 40 },
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
            if (gm.LifetimeKillCount     >= 100)  TryUnlock("kill_100");
            if (gm.LifetimeKillCount     >= 1000) TryUnlock("kill_1000");
            if (gm.LifetimeBossKillCount >= 1)    TryUnlock("boss_1");
            if (gm.LifetimeBossKillCount >= 10)   TryUnlock("boss_10");
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
