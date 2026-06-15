using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameIdle
{
    // Missões INFINITAS: 5 slots que, ao serem coletados, regeneram com meta e
    // recompensa maiores (sobem de nível). Assim sempre há algo para fazer.
    public static class DailyMissionSystem
    {
        public class Mission
        {
            public string id;
            public int    tier = 1;        // nível atual (cresce a cada coleta)
            public long   target;          // meta calculada para o tier atual
            public int    gemReward;       // recompensa calculada para o tier atual
            public string name;            // texto exibido

            public Func<int, long>  targetAt;   // meta em função do tier
            public Func<int, int>   rewardAt;   // recompensa em função do tier
            public Func<long, string> nameAt;   // nome em função da meta

            // Recalcula meta/recompensa/nome a partir do tier atual.
            public void Build()
            {
                target    = Math.Max(1, targetAt(tier));
                gemReward = Math.Max(1, rewardAt(tier));
                name      = nameAt(target);
            }
        }

        public static readonly Mission[] All =
        {
            new() { id = "taps",
                    targetAt = t => (long)Math.Round(50 * Math.Pow(1.5, t - 1)),
                    rewardAt = t => 5 + 3 * (t - 1),
                    nameAt   = v => $"Trabalhar {v} vezes" },

            new() { id = "hires",
                    targetAt = t => (long)Math.Round(5 * Math.Pow(1.4, t - 1)),
                    rewardAt = t => 8 + 4 * (t - 1),
                    nameAt   = v => $"Contratar {v} funcionários" },

            new() { id = "prestige",
                    targetAt = t => t,                       // 1, 2, 3, ...
                    rewardAt = t => 20 + 8 * (t - 1),
                    nameAt   = v => v > 1 ? $"Realizar {v} prestígios" : "Realizar 1 prestígio" },

            new() { id = "kills",
                    targetAt = t => (long)Math.Round(10 * Math.Pow(1.6, t - 1)),
                    rewardAt = t => 12 + 5 * (t - 1),
                    nameAt   = v => $"Derrotar {v} monstros" },

            new() { id = "earn",
                    targetAt = t => (long)Math.Round(1_000_000 * Math.Pow(2.5, t - 1)),
                    rewardAt = t => 7 + 4 * (t - 1),
                    nameAt   = v => $"Ganhar ${NumberFormatter.Format(v)}" },
        };

        private static readonly long[] _progress = new long[5];
        private static readonly bool[] _claimed  = new bool[5];
        private static string _lastDate = "";

        static DailyMissionSystem()
        {
            foreach (var m in All) m.Build();
        }

        public static string LastMissionDate => _lastDate;

        public static long GetProgress(int i) => _progress[i];
        public static bool IsClaimed(int i)   => _claimed[i];
        public static bool IsComplete(int i)  => _progress[i] >= All[i].target;

        // True se há alguma missão concluída ainda não coletada.
        public static bool HasClaimable()
        {
            for (int i = 0; i < All.Length; i++)
                if (IsComplete(i) && !IsClaimed(i)) return true;
            return false;
        }

        // Missões agora são infinitas (não resetam por dia). Mantido por
        // compatibilidade de save/registro de data.
        public static void CheckDailyReset()
        {
            _lastDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public static void RegisterTap()
        {
            if (_claimed[0]) return;
            _progress[0] = Math.Min(_progress[0] + 1, All[0].target);
        }

        public static void RegisterHire()
        {
            if (_claimed[1]) return;
            _progress[1] = Math.Min(_progress[1] + 1, All[1].target);
        }

        public static void RegisterPrestige()
        {
            if (_claimed[2]) return;
            _progress[2] = Math.Min(_progress[2] + 1, All[2].target);
        }

        public static void RegisterKill()
        {
            if (_claimed[3]) return;
            _progress[3] = Math.Min(_progress[3] + 1, All[3].target);
        }

        public static void RegisterEarn(double amount)
        {
            if (_claimed[4]) return;
            _progress[4] = (long)Math.Min(All[4].target, _progress[4] + amount);
        }

        public static bool TryClaim(int i)
        {
            if (_claimed[i] || !IsComplete(i)) return false;

            int reward = All[i].gemReward;
            GameManager.Instance.AddGems(reward);
            UIManager.Instance.ShowToast($"Missão concluída! +{reward} gemas", new Color(0.4f, 0.9f, 1f));

            // Avança o slot para o próximo nível: meta/recompensa maiores e
            // progresso zerado → sempre há uma nova missão disponível.
            Advance(i);

            SaveSystem.Save();
            return true;
        }

        // Sobe o nível do slot e regenera a missão.
        private static void Advance(int i)
        {
            All[i].tier++;
            All[i].Build();
            _progress[i] = 0;
            _claimed[i]  = false;
        }

        public static void Load(string lastDate, List<long> progress, List<bool> claimed, List<int> tiers)
        {
            _lastDate = lastDate ?? "";
            for (int i = 0; i < All.Length; i++)
            {
                All[i].tier = (tiers != null && i < tiers.Count && tiers[i] >= 1) ? tiers[i] : 1;
                All[i].Build();
                _progress[i] = (progress != null && i < progress.Count) ? progress[i] : 0;
                _claimed[i]  = (claimed  != null && i < claimed.Count)  ? claimed[i]  : false;
                // Garante que um slot carregado já coletado nao fique travado.
                if (_claimed[i]) Advance(i);
            }
            CheckDailyReset();
        }

        public static void ResetAll()
        {
            _lastDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            for (int i = 0; i < All.Length; i++)
            {
                All[i].tier = 1;
                All[i].Build();
                _progress[i] = 0;
                _claimed[i] = false;
            }
        }

        public static List<long> GetProgressSave() => new(_progress);
        public static List<bool> GetClaimedSave()  => new(_claimed);
        public static List<int>  GetTierSave()
        {
            var l = new List<int>(All.Length);
            foreach (var m in All) l.Add(m.tier);
            return l;
        }
    }
}
